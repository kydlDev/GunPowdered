using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Dynamite.Domain;
using GunPowdered.Features.Skilling.Domain;
using GunPowdered.Features.Skilling.Infrastructure;
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
        internal const string LitKey = "gp_dyn_lit", LighterKey = "gp_dyn_lighter", FuseKey = "gp_dyn_fuse";
        const string LightRpc = "GP_DynamiteLight";
        const float SparkEvery = 0.35f;
        static readonly string[] SparkEffects = { "vfx_torch_hit", DynamiteSetup.QuietFuseSound };

        /// <summary>The charge prefab this one was made from (DynamiteSetup.ChargeKind): the stick or the Deep North Dynamite Pack. Serialized, so clones keep it.</summary>
        public string m_kindId = DynamiteSetup.StickId;

        ZNetView _nview;
        float _nextSpark, _nextTimer;

        // D-158: the light red sparkle of a placed, unlit stick (DynamiteSetup.AddSparkle) - every client hides it once the fuse burns
        Transform _sparkle;
        bool _sparkleChecked;

        void UpdateSparkle()
        {
            if (!_sparkleChecked) { _sparkle = transform.Find(DynamiteSetup.SparkleName); _sparkleChecked = true; }
            if (_sparkle == null) { if (RuleLog.Due("DynamiteCharge.UpdateSparkle:none", 2d)) ModLog.Every("DynamiteCharge.UpdateSparkle:none", 2d, "DynamiteCharge.UpdateSparkle: no sparkle - returns"); return; }
            bool show = !IsLit;
            if (_sparkle.gameObject.activeSelf == show) { if (RuleLog.Due("DynamiteCharge.UpdateSparkle:same", 2d)) ModLog.Every("DynamiteCharge.UpdateSparkle:same", 2d, "DynamiteCharge.UpdateSparkle: unchanged - returns"); return; }
            _sparkle.gameObject.SetActive(show);
            ModLog.Verbose("DynamiteCharge: sparkle " + (show ? "shown" : "hidden (fuse lit)") + " at " + transform.position.ToString("0.0"));
        }

        /// <summary>D-158 test hooks: whether the light red sparkle shows now, and its colour.</summary>
        internal string SparkleReport()
        {
            Transform s = transform.Find(DynamiteSetup.SparkleName);
            ParticleSystem ps = s != null ? s.GetComponent<ParticleSystem>() : null;
            string r = ps == null ? "sparkle none" : "sparkle " + (s.gameObject.activeInHierarchy ? "shown" : "hidden") + ", playing " + ps.isPlaying + ", colour " + ps.main.startColor.color.ToString("0.00");
            ModLog.Verbose("DynamiteCharge.SparkleReport: returns " + r);
            return r;
        }
        bool _blown;

        DynamiteSetup.ChargeKind Kind => DynamiteSetup.KindOfCharge(m_kindId);

        /// <summary>How many blasts this client set off, and where the last one was (test hooks).</summary>
        internal static int Blasts { get; private set; }
        internal static Vector3 LastBlast { get; private set; }

        void Awake()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteCharge.Awake()");
            _nview = GetComponent<ZNetView>();
            if (_nview != null && _nview.GetZDO() != null) _nview.Register<long, float>(LightRpc, RPC_Light);
        }

        /// <summary>When the stick was lit (game seconds), or -1 when it is not burning.</summary>
        internal double LitAt => _nview != null && _nview.IsValid() ? _nview.GetZDO().GetLong(LitKey, -1L) / 1000d : -1d;

        internal bool IsLit => LitAt >= 0d;

        /// <summary>D-157: this stick's fuse (s), set by the lighter's Explosives skill; the old 4 s for a stick lit before the skill existed.</summary>
        internal float Fuse => _nview != null && _nview.IsValid() ? _nview.GetZDO().GetFloat(FuseKey, DynamiteRules.FuseSeconds) : DynamiteRules.FuseSeconds;

        public string GetHoverName() => Kind.Name;

        public float GetHoverOffset() => 0.2f;

        public string GetHoverText()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteCharge.GetHoverText()");
            if (IsLit) return Kind.Name + "\n" + DynamiteRules.Burning + " (" + DynamiteRules.FuseLeft(LitAt, Now(), Fuse).ToString("0.0") + " s)";
            { var returnedAt82 = Localization.instance.Localize(Kind.Name + "\n[<color=yellow><b>$KEY_Use</b></color>] Light the fuse (hold a Torch)"); if (RuleLog.Due("DynamiteCharge.GetHoverText:82", 2d)) ModLog.Every("DynamiteCharge.GetHoverText:82", 2d, $"DynamiteCharge.GetHoverText: returns {returnedAt82} (line 82)"); return returnedAt82; }
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (RuleLog.Due("DynamiteCharge.Interact", 2d)) ModLog.Every("DynamiteCharge.Interact", 2d, $"DynamiteCharge.Interact(user={RuleLog.Shown(user)}, hold={RuleLog.Shown(hold)}, alt={RuleLog.Shown(alt)})");
            return !hold && Light(user) == null;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        /// <summary>Lights the fuse for this user. Expected outcome: null when it now burns, otherwise why not (also told on screen).</summary>
        internal string Light(Humanoid user)
        {
            if (_nview == null || !_nview.IsValid() || user == null) { if (RuleLog.Due("DynamiteCharge.Light:67", 2d)) ModLog.Every("DynamiteCharge.Light:96", 2d, "DynamiteCharge.Light: returns \"no stick\" (line 96)"); return "no stick"; }
            string why = DynamiteRules.WhyNotLight(IsLit, TorchInHand(user));
            if (why != null)
            {
                user.Message(MessageHud.MessageType.Center, why);
                { var returnedAt101 = why; if (RuleLog.Due("DynamiteCharge.Light:101", 2d)) ModLog.Every("DynamiteCharge.Light:101", 2d, $"DynamiteCharge.Light: returns {returnedAt101} (line 101)"); return returnedAt101; }
            }
            Player p = user as Player;
            // D-157: the lighter's Explosives skill sets the fuse (5 s at level 0 .. 3 s at level 100); skills live on the lighter's client, so it is sent along
            float fuse = SkillRules.DynamiteFuseSeconds(GunSkills.Level(user, GunSkills.Explosives));
            _nview.InvokeRPC(LightRpc, p != null ? p.GetPlayerID() : 0L, fuse);
            GunSkills.Raise(user, GunSkills.Explosives, SkillRules.GainLight);
            user.Message(MessageHud.MessageType.Center, DynamiteRules.Burning);
            ModLog.Info("DynamiteCharge: " + Kind.Name + " lit at " + transform.position.ToString("0.0") + ", fuse " + fuse.ToString("0.00") + " s (Explosives skill)");
            return null;
        }

        void RPC_Light(long sender, long playerId, float fuse)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteCharge.RPC_Light(sender={RuleLog.Shown(sender)}, playerId={RuleLog.Shown(playerId)}, fuse={fuse})");
            if (!_nview.IsOwner() || IsLit) return;
            ZDO zdo = _nview.GetZDO();
            zdo.Set(LitKey, (long)(Now() * 1000d));
            zdo.Set(LighterKey, playerId);
            zdo.Set(FuseKey, fuse > 0.5f ? fuse : DynamiteRules.FuseSeconds);
            GunPowdered.Integrations.Valheim.GunNoise.Alert(transform.position, NoiseRules.FuseRadius, Player.GetPlayer(playerId), "dynamite fuse lit");      // D-149: a little, like a pickaxe hit
        }

        void Update()
        {
            if (_blown || _nview == null || !_nview.IsValid()) { if (RuleLog.Due("DynamiteCharge.Update:126", 2d)) ModLog.Every("DynamiteCharge.Update:126", 2d, $"DynamiteCharge.Update: returns because _blown || _nview == null || !_nview.IsValid() (line 126)"); return; }
            Countdown();
            UpdateSparkle();
            if (!_nview.IsOwner()) { if (RuleLog.Due("DynamiteCharge.Update:129", 2d)) ModLog.Every("DynamiteCharge.Update:129", 2d, $"DynamiteCharge.Update: returns because !_nview.IsOwner() (line 129)"); return; }
            double lit = LitAt;
            if (lit < 0d) { if (RuleLog.Due("DynamiteCharge.Update:131", 2d)) ModLog.Every("DynamiteCharge.Update:131", 2d, $"DynamiteCharge.Update: returns because lit < 0d (line 131)"); return; }
            double now = Now();
            ModLog.Every("dynamite-fuse", 1f, "DynamiteCharge: fuse at " + transform.position.ToString("0.0") + ", " + DynamiteRules.FuseLeft(lit, now, Fuse).ToString("0.0") + " s left");
            if (Time.time >= _nextSpark)
            {
                _nextSpark = Time.time + SparkEvery;
                Sparks();
            }
            if (DynamiteRules.Blows(lit, now, Fuse)) Blow();
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
            if (!kind.ShowsTimer || Time.time < _nextTimer) { if (RuleLog.Due("DynamiteCharge.Countdown:152", 2d)) ModLog.Every("DynamiteCharge.Countdown:152", 2d, $"DynamiteCharge.Countdown: returns because !kind.ShowsTimer || Time.time < _nextTimer (line 152)"); return; }
            double lit = LitAt;
            Player me = Player.m_localPlayer;
            if (lit < 0d || me == null || MessageHud.instance == null) { if (RuleLog.Due("DynamiteCharge.Countdown:155", 2d)) ModLog.Every("DynamiteCharge.Countdown:155", 2d, $"DynamiteCharge.Countdown: returns because lit < 0d || me == null || MessageHud.instance == null (line 155)"); return; }
            _nextTimer = Time.time + 0.1f;
            float d = Vector3.Distance(me.transform.position, transform.position);
            if (!DynamiteRules.ShowsTimer(d, kind.Radius())) { if (RuleLog.Due("DynamiteCharge.Countdown:158", 2d)) ModLog.Every("DynamiteCharge.Countdown:158", 2d, $"DynamiteCharge.Countdown: returns because !DynamiteRules.ShowsTimer(d, kind.Radius()) (line 158)"); return; }
            LastTimerText = DynamiteRules.TimerText(kind.Name, DynamiteRules.FuseLeft(lit, Now(), Fuse));
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
            if (ZNetScene.instance == null) return;
            Vector3 top = transform.position + Vector3.up * 0.35f;
            _sparks++;
            foreach (string n in SparkEffects)
            {
                GameObject fx = ZNetScene.instance.GetPrefab(n);
                if (fx == null) { if (RuleLog.Due("DynamiteCharge.Sparks:187", 2d)) ModLog.Every("DynamiteCharge.Sparks:187", 2d, $"DynamiteCharge.Sparks: skips to the next item (continue) because fx == null (line 187)"); continue; }
                bool sound = fx.GetComponentInChildren<ZSFX>(true) != null;
                if (sound && _sparks % FuseSoundEvery != 0) { if (RuleLog.Due("DynamiteCharge.Sparks:189", 2d)) ModLog.Every("DynamiteCharge.Sparks:189", 2d, $"DynamiteCharge.Sparks: skips to the next item (continue) because sound && _sparks % FuseSoundEvery != 0 (line 189)"); continue; }
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
            if (inv == null) { if (RuleLog.Due("DynamiteCharge.TorchInHand:203", 2d)) ModLog.Every("DynamiteCharge.TorchInHand:203", 2d, $"DynamiteCharge.TorchInHand: returns false because inv == null (line 203)"); return false; }
            foreach (ItemDrop.ItemData i in inv.GetAllItems())
                if (i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch) { if (RuleLog.Due("DynamiteCharge.TorchInHand:205", 2d)) ModLog.Every("DynamiteCharge.TorchInHand:205", 2d, $"DynamiteCharge.TorchInHand: returns true because i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch (line 205)"); return true; }
            { if (RuleLog.Due("DynamiteCharge.TorchInHand:206", 2d)) ModLog.Every("DynamiteCharge.TorchInHand:206", 2d, $"DynamiteCharge.TorchInHand: returns false (line 206)"); return false; }
        }

        static double Now() => ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : Time.time;
    }
}
