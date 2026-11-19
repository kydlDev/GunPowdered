using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Dynamite.Domain;

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
        static readonly string[] SparkEffects = { "vfx_torch_hit", "sfx_FireAddFuel" };

        ZNetView _nview;
        float _nextSpark;
        bool _blown;

        /// <summary>How many blasts this client set off, and where the last one was (test hooks).</summary>
        internal static int Blasts { get; private set; }
        internal static Vector3 LastBlast { get; private set; }

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
            if (_nview != null && _nview.GetZDO() != null) _nview.Register<long>(LightRpc, RPC_Light);
        }

        /// <summary>When the stick was lit (game seconds), or -1 when it is not burning.</summary>
        internal double LitAt => _nview != null && _nview.IsValid() ? _nview.GetZDO().GetLong(LitKey, -1L) / 1000d : -1d;

        internal bool IsLit => LitAt >= 0d;

        public string GetHoverName() => "Dynamite";

        public float GetHoverOffset() => 0.2f;

        public string GetHoverText()
        {
            if (IsLit) return "Dynamite\n" + DynamiteRules.Burning + " (" + DynamiteRules.FuseLeft(LitAt, Now()).ToString("0.0") + " s)";
            return Localization.instance.Localize("Dynamite\n[<color=yellow><b>$KEY_Use</b></color>] Light the fuse (hold a Torch)");
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            return !hold && Light(user) == null;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        /// <summary>Lights the fuse for this user. Expected outcome: null when it now burns, otherwise why not (also told on screen).</summary>
        internal string Light(Humanoid user)
        {
            if (_nview == null || !_nview.IsValid() || user == null) return "no stick";
            string why = DynamiteRules.WhyNotLight(IsLit, TorchInHand(user));
            if (why != null)
            {
                user.Message(MessageHud.MessageType.Center, why);
                return why;
            }
            Player p = user as Player;
            _nview.InvokeRPC(LightRpc, p != null ? p.GetPlayerID() : 0L);
            user.Message(MessageHud.MessageType.Center, DynamiteRules.Burning);
            ModLog.Info("DynamiteCharge: lit at " + transform.position.ToString("0.0"));
            return null;
        }

        void RPC_Light(long sender, long playerId)
        {
            if (!_nview.IsOwner() || IsLit) return;
            ZDO zdo = _nview.GetZDO();
            zdo.Set(LitKey, (long)(Now() * 1000d));
            zdo.Set(LighterKey, playerId);
        }

        void Update()
        {
            if (_blown || _nview == null || !_nview.IsValid() || !_nview.IsOwner()) return;
            double lit = LitAt;
            if (lit < 0d) return;
            double now = Now();
            ModLog.Every("dynamite-fuse", 1f, "DynamiteCharge: fuse at " + transform.position.ToString("0.0") + ", " + DynamiteRules.FuseLeft(lit, now).ToString("0.0") + " s left");
            if (Time.time >= _nextSpark)
            {
                _nextSpark = Time.time + SparkEvery;
                Sparks();
            }
            if (DynamiteRules.Blows(lit, now)) Blow();
        }

        void Blow()
        {
            _blown = true;
            Vector3 at = transform.position + Vector3.up * 0.15f;
            Player lighter = Player.GetPlayer(_nview.GetZDO().GetLong(LighterKey, 0L));
            int made = DynamiteSetup.Blast(at, lighter);
            Blasts++;
            LastBlast = at;
            ModLog.Info("DynamiteCharge: blast at " + at.ToString("0.0") + ", " + made + " object(s) made, lit by " + (lighter != null ? lighter.GetPlayerName() : "nobody known"));
            ZNetScene.instance.Destroy(gameObject);
        }

        // sparks at the end of the fuse while it burns (vanilla effects with a network view: only the owner makes them, everyone sees them)
        void Sparks()
        {
            if (ZNetScene.instance == null) return;
            Vector3 top = transform.position + Vector3.up * 0.35f;
            foreach (string n in SparkEffects)
            {
                GameObject fx = ZNetScene.instance.GetPrefab(n);
                if (fx != null) Instantiate(fx, top, Quaternion.LookRotation(Vector3.up));
            }
        }

        // a Torch type item equipped in either hand (the hand cannon's rule, CannonInventory.HoldsTorch)
        internal static bool TorchInHand(Humanoid user)
        {
            Inventory inv = user != null ? user.GetInventory() : null;
            if (inv == null) return false;
            foreach (ItemDrop.ItemData i in inv.GetAllItems())
                if (i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch) return true;
            return false;
        }

        static double Now() => ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : Time.time;
    }
}
