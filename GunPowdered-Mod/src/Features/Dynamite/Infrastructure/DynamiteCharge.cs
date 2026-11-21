using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Dynamite.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Dynamite.Infrastructure
{
    /// <summary>
    /// A stick of dynamite set down in the world (D-100). Hover: "Dynamite - [E] Light the fuse (hold a Torch)". Use with a Torch in hand lights it (like the
    /// hand cannon's fuse); the time it was lit and who lit it go into the stick's ZDO, so every client shows the burning fuse and the owner of the stick sets
    /// off the blast after <see cref="DynamiteRules.FuseSeconds"/>. A stick that is not lit stays where it is (saved with the world).
    /// </summary>
    internal sealed class DynamiteCharge : MonoBehaviour, Hoverable, Interactable
    {
        internal const string LitKey = "gp_dyn_lit", LighterKey = "gp_dyn_lighter";
        const string LightRpc = "GP_DynamiteLight";
        const float SparkEvery = 0.35f;
        static readonly string[] SparkEffects = { "vfx_torch_hit", DynamiteSetup.QuietFuseSound };

        /// <summary>The charge prefab this one was made from (DynamiteSetup.ChargeKind): the stick or the Deep North Dynamite Pack. Serialized, so clones keep it.</summary>
        public string m_kindId = DynamiteSetup.StickId;

        ZNetView _nview;
        float _nextSpark, _nextTimer;
        bool _blown;

        DynamiteSetup.ChargeKind Kind => DynamiteSetup.KindOfCharge(m_kindId);

        /// <summary>How many blasts this client set off, and where the last one was (test hooks).</summary>
        internal static int Blasts { get; private set; }
        internal static Vector3 LastBlast { get; private set; }

        void Awake()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteCharge.Awake()");
            _nview = GetComponent<ZNetView>();
            if (_nview != null && _nview.GetZDO() != null) _nview.Register<long>(LightRpc, RPC_Light);
        }

        /// <summary>When the stick was lit (game seconds), or -1 when it is not burning.</summary>
        internal double LitAt => _nview != null && _nview.IsValid() ? _nview.GetZDO().GetLong(LitKey, -1L) / 1000d : -1d;

        internal bool IsLit => LitAt >= 0d;

        public string GetHoverName() => Kind.Name;

        public float GetHoverOffset() => 0.2f;

        public string GetHoverText()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteCharge.GetHoverText()");
            if (IsLit) { var returnedAt52 = Kind.Name + "\n" + DynamiteRules.Burning + " (" + DynamiteRules.FuseLeft(LitAt, Now()).ToString("0.0") + " s)"; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteCharge.GetHoverText: returns {RuleLog.Shown(returnedAt52)} (line 52)"); return returnedAt52; }
            { var returnedAt53 = Localization.instance.Localize(Kind.Name + "\n[<color=yellow><b>$KEY_Use</b></color>] Light the fuse (hold a Torch)"); if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteCharge.GetHoverText: returns {RuleLog.Shown(returnedAt53)} (line 53)"); return returnedAt53; }
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (RuleLog.Due("DynamiteCharge.Interact", 2d)) ModLog.Every("DynamiteCharge.Interact", 2d, $"DynamiteCharge.Interact(user={RuleLog.Shown(user)}, hold={RuleLog.Shown(hold)}, alt={RuleLog.Shown(alt)})");
            { var returnedAt59 = !hold && Light(user) == null; if (RuleLog.Due("DynamiteCharge.Interact:59", 2d)) ModLog.Every("DynamiteCharge.Interact:59", 2d, $"DynamiteCharge.Interact: returns {RuleLog.Shown(returnedAt59)} (line 59)"); return returnedAt59; }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        /// <summary>Lights the fuse for this user. Expected outcome: null when it now burns, otherwise why not (also told on screen).</summary>
        internal string Light(Humanoid user)
        {
            if (_nview == null || !_nview.IsValid() || user == null) { if (RuleLog.Due("DynamiteCharge.Light:67", 2d)) ModLog.Every("DynamiteCharge.Light:67", 2d, "DynamiteCharge.Light: returns \"no stick\" (line 67)"); return "no stick"; }
            string why = DynamiteRules.WhyNotLight(IsLit, TorchInHand(user));
            if (why != null)
            {
                user.Message(MessageHud.MessageType.Center, why);
                { var returnedAt72 = why; if (RuleLog.Due("DynamiteCharge.Light:72", 2d)) ModLog.Every("DynamiteCharge.Light:72", 2d, $"DynamiteCharge.Light: returns {RuleLog.Shown(returnedAt72)} (line 72)"); return returnedAt72; }
            }
            Player p = user as Player;
            _nview.InvokeRPC(LightRpc, p != null ? p.GetPlayerID() : 0L);
            user.Message(MessageHud.MessageType.Center, DynamiteRules.Burning);
            ModLog.Info("DynamiteCharge: " + Kind.Name + " lit at " + transform.position.ToString("0.0"));
            return null;
        }

        void RPC_Light(long sender, long playerId)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteCharge.RPC_Light(sender={RuleLog.Shown(sender)}, playerId={RuleLog.Shown(playerId)})");
            if (!_nview.IsOwner() || IsLit) { if (RuleLog.IsOn()) ModLog.Verbose("DynamiteCharge.RPC_Light: returns (line 84)"); return; }
            ZDO zdo = _nview.GetZDO();
            zdo.Set(LitKey, (long)(Now() * 1000d));
            zdo.Set(LighterKey, playerId);
            GunPowdered.Integrations.Valheim.GunNoise.Alert(transform.position, NoiseRules.FuseRadius, Player.GetPlayer(playerId), "dynamite fuse lit");      // D-149: a little, like a pickaxe hit
        }

        void Update()
        {
            if (_blown || _nview == null || !_nview.IsValid()) { if (RuleLog.Due("DynamiteCharge.Update:92", 2d)) ModLog.Every("DynamiteCharge.Update:92", 2d, "DynamiteCharge.Update: returns (line 92)"); return; }
            Countdown();
            if (!_nview.IsOwner()) { if (RuleLog.Due("DynamiteCharge.Update:94", 2d)) ModLog.Every("DynamiteCharge.Update:94", 2d, "DynamiteCharge.Update: returns (line 94)"); return; }
            double lit = LitAt;
            if (lit < 0d) { if (RuleLog.Due("DynamiteCharge.Update:96", 2d)) ModLog.Every("DynamiteCharge.Update:96", 2d, "DynamiteCharge.Update: returns (line 96)"); return; }
            double now = Now();
            ModLog.Every("dynamite-fuse", 1f, "DynamiteCharge: fuse at " + transform.position.ToString("0.0") + ", " + DynamiteRules.FuseLeft(lit, now).ToString("0.0") + " s left");
            if (Time.time >= _nextSpark)
            {
                _nextSpark = Time.time + SparkEvery;
                Sparks();
            }
            if (DynamiteRules.Blows(lit, now)) Blow();
        }

        /// <summary>The text the local player last saw on screen for a burning pack (test hooks), and how often it was shown.</summary>
        internal static string LastTimerText { get; private set; }
        internal static int TimerShows { get; private set; }

        // D-108 (user: "show timer on screen"): every client shows the countdown of a burning pack to its own player when they are within the blast or
        // a little beyond it (DynamiteRules.ShowsTimer), as the big centre message, ten times a second.
        void Countdown()
        {
            if (RuleLog.Due("DynamiteCharge.Countdown", 2d)) ModLog.Every("DynamiteCharge.Countdown", 2d, $"DynamiteCharge.Countdown()");
            DynamiteSetup.ChargeKind kind = Kind;
            if (!kind.ShowsTimer || Time.time < _nextTimer) { if (RuleLog.Due("DynamiteCharge.Countdown:117", 2d)) ModLog.Every("DynamiteCharge.Countdown:117", 2d, "DynamiteCharge.Countdown: returns (line 117)"); return; }
            double lit = LitAt;
            Player me = Player.m_localPlayer;
            if (lit < 0d || me == null || MessageHud.instance == null) { if (RuleLog.Due("DynamiteCharge.Countdown:120", 2d)) ModLog.Every("DynamiteCharge.Countdown:120", 2d, "DynamiteCharge.Countdown: returns (line 120)"); return; }
            _nextTimer = Time.time + 0.1f;
            float d = Vector3.Distance(me.transform.position, transform.position);
            if (!DynamiteRules.ShowsTimer(d, kind.Radius())) { if (RuleLog.Due("DynamiteCharge.Countdown:123", 2d)) ModLog.Every("DynamiteCharge.Countdown:123", 2d, "DynamiteCharge.Countdown: returns (line 123)"); return; }
            LastTimerText = DynamiteRules.TimerText(kind.Name, DynamiteRules.FuseLeft(lit, Now()));
            TimerShows++;
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, LastTimerText);
        }

        void Blow()
        {
            _blown = true;
            Vector3 at = transform.position + Vector3.up * 0.15f;
            Player lighter = Player.GetPlayer(_nview.GetZDO().GetLong(LighterKey, 0L));
            int made = DynamiteSetup.Blast(Kind, at, lighter);
            GunPowdered.Integrations.Valheim.GunNoise.Alert(at, Kind == DynamiteSetup.PackKind ? NoiseRules.PackBlastRadius : NoiseRules.StickBlastRadius, lighter, Kind.Name + " blast");      // D-149: many mobs come
            Blasts++;
            LastBlast = at;
            ModLog.Info("DynamiteCharge: " + Kind.Name + " blast at " + at.ToString("0.0") + " (radius " + Kind.Radius().ToString("0.#") + " m)" + ", " + made + " object(s) made, lit by " + (lighter != null ? lighter.GetPlayerName() : "nobody known"));
            ZNetScene.instance.Destroy(gameObject);
        }

        // sparks at the end of the fuse while it burns (vanilla effects with a network view: only the owner makes them, everyone sees them)
        void Sparks()
        {
            if (RuleLog.Due("DynamiteCharge.Sparks", 2d)) ModLog.Every("DynamiteCharge.Sparks", 2d, $"DynamiteCharge.Sparks()");
            if (ZNetScene.instance == null) { if (RuleLog.Due("DynamiteCharge.Sparks:145", 2d)) ModLog.Every("DynamiteCharge.Sparks:145", 2d, "DynamiteCharge.Sparks: returns (line 145)"); return; }
            Vector3 top = transform.position + Vector3.up * 0.35f;
            _sparks++;
            foreach (string n in SparkEffects)
            {
                GameObject fx = ZNetScene.instance.GetPrefab(n);
                if (fx == null) continue;
                bool sound = fx.GetComponentInChildren<ZSFX>(true) != null;
                if (sound && _sparks % FuseSoundEvery != 0) continue;
                Instantiate(fx, top, Quaternion.LookRotation(Vector3.up));
            }
        }

        // D-149 (user 2026-11-21: "dynamite burning sound is loud, too loud"): the crackle (DynamiteSetup.QuietFuseSound, quieter) on every third spark
        int _sparks;
        const int FuseSoundEvery = 3;

        // a Torch type item equipped in either hand (the hand cannon's rule, CannonInventory.HoldsTorch)
        internal static bool TorchInHand(Humanoid user)
        {
            if (RuleLog.Due("DynamiteCharge.TorchInHand", 2d)) ModLog.Every("DynamiteCharge.TorchInHand", 2d, $"DynamiteCharge.TorchInHand(user={RuleLog.Shown(user)})");
            Inventory inv = user != null ? user.GetInventory() : null;
            if (inv == null) { if (RuleLog.Due("DynamiteCharge.TorchInHand:159", 2d)) ModLog.Every("DynamiteCharge.TorchInHand:159", 2d, "DynamiteCharge.TorchInHand: returns false (line 159)"); return false; }
            foreach (ItemDrop.ItemData i in inv.GetAllItems())
                if (i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch) { if (RuleLog.Due("DynamiteCharge.TorchInHand:161", 2d)) ModLog.Every("DynamiteCharge.TorchInHand:161", 2d, "DynamiteCharge.TorchInHand: returns true (line 161)"); return true; }
            { if (RuleLog.Due("DynamiteCharge.TorchInHand:162", 2d)) ModLog.Every("DynamiteCharge.TorchInHand:162", 2d, "DynamiteCharge.TorchInHand: returns false (line 162)"); return false; }
        }

        static double Now() => ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : Time.time;
    }
}
