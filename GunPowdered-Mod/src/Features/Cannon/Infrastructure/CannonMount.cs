// kydl:read-only-observer - Humanoid / Player here is only the player who uses the stock; no creature is moved, commanded or spawned
using System;
using System.Collections;
using System.Globalization;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Features.Cannon.Infrastructure
{
    /// <summary>
    /// The Cannon Stock piece with the Hand Cannon on it (D-052 addendum, D-053). Interacting mounts the cannon from the inventory, then loads it at
    /// the muzzle (gunpowder + a Cannon Ball, 4 s, the ball in the player's hand), then starts aiming from the stock (CannonAim). Shift + interact
    /// takes the cannon (and its load) back. Breaking or dismantling the stock drops the cannon (and its load) next to the wood. The state lives in
    /// the stock's ZDO: mounted, loaded, the fuse's world time (ms) and the aim (yaw / pitch); the client that acts claims the stock first.
    /// Every client shows the barrel, its aim and the fuse / muzzle effects from that state.
    /// </summary>
    internal sealed partial class CannonMount : MonoBehaviour, Hoverable, Interactable
    {
        internal const string KeyMounted = "gp_cannon_mounted", KeyLoaded = "gp_cannon_loaded", KeyFuse = "gp_cannon_fuse",
            KeyYaw = "gp_cannon_yaw", KeyPitch = "gp_cannon_pitch", KeyBall = "gp_cannon_ball";   // D-145: which ball is loaded
        internal const string PivotName = "gp_pivot", BarrelName = "gp_barrel";
        const float UseRange = 3.5f, AnimEvery = 1.2f, AnimTail = 0.9f, FuseSparkEvery = 0.12f, SyncEvery = 0.1f;
        static readonly System.Reflection.MethodInfo UseHandVisual = AccessTools.Method(typeof(Humanoid), "SetUseHandVisual");

        ZNetView _nview;
        Transform _pivot, _barrel, _muzzle, _touchHole;
        float _lastSync, _localYaw, _localPitch;
        bool _loading, _fireQueued;

        internal bool Loading => _loading;
        internal Transform Muzzle => _muzzle;
        internal Transform TouchHole => _touchHole;
        internal Transform Barrel => _barrel;
        internal bool IsValid => _nview != null && _nview.IsValid();

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _pivot = transform.Find(PivotName);
            _barrel = _pivot != null ? _pivot.Find(BarrelName) : null;
            _muzzle = Deep(_barrel, "Muzzle");
            _touchHole = Deep(_barrel, "TouchHole");
            WearNTear wnt = GetComponent<WearNTear>();
            if (wnt != null) wnt.m_onDestroyed = (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(OnDestroyed));
            if (_barrel == null || _muzzle == null) ModLog.Warn("CannonMount: stock without barrel parts (barrel " + (_barrel != null) + ", muzzle " + (_muzzle != null) + ")");
        }

        void OnDisable()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"CannonMount.OnDisable()");
            if (CannonAim.Current == this) CannonAim.End("the stock is gone");
        }

        internal CannonStatus Status()
        {
            if (RuleLog.Due("CannonMount.Status", 2d)) ModLog.Every("CannonMount.Status", 2d, $"CannonMount.Status()");
            if (!IsValid) return new CannonStatus(false, false, false);
            ZDO z = _nview.GetZDO();
            bool loaded = z.GetBool(KeyLoaded, false);
            { var returnedAt65 = new CannonStatus(z.GetBool(KeyMounted, false), loaded, loaded && FuseBurning(z.GetLong(KeyFuse, 0L))); if (RuleLog.Due("CannonMount.Status:65", 2d)) ModLog.Every("CannonMount.Status:65", 2d, $"CannonMount.Status: returns {returnedAt65} (line 65)"); return returnedAt65; }
        }

        void Update()
        {
            try
            {
                if (!IsValid) { if (RuleLog.Due("CannonMount.Update:72", 2d)) ModLog.Every("CannonMount.Update:72", 2d, $"CannonMount.Update: returns because !IsValid (line 72)"); return; }
                ZDO z = _nview.GetZDO();
                bool mounted = z.GetBool(KeyMounted, false);
                if (_barrel != null && _barrel.gameObject.activeSelf != mounted) _barrel.gameObject.SetActive(mounted);
                if (CannonAim.Current == this) CannonAim.Tick(this);
                else ShowAim(z.GetFloat(KeyYaw, 0f), z.GetFloat(KeyPitch, ModSettings.CannonPitch.Value));
                ModLog.Every("cannon-mount-" + GetInstanceID(), 30d, "CannonMount: mounted " + mounted + ", loaded " + z.GetBool(KeyLoaded, false)
                    + ", aiming " + (CannonAim.Current == this) + ", yaw " + Fmt(z.GetFloat(KeyYaw, 0f)) + ", pitch " + Fmt(z.GetFloat(KeyPitch, 0f)));
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("CannonMount.Update: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("CannonMount.Update: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("CannonMount.Update: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
        }

        static bool FuseBurning(long fuse)
        {
            if (RuleLog.Due("CannonMount.FuseBurning", 2d)) ModLog.Every("CannonMount.FuseBurning", 2d, $"CannonMount.FuseBurning(fuse={RuleLog.Shown(fuse)})");
            if (fuse == 0L || ZNet.instance == null) return false;
            double since = ZNet.instance.GetTimeSeconds() - (fuse / 1000d);
            { var returnedAt100 = since >= 0d && since < ModSettings.CannonFuseSeconds.Value; if (RuleLog.Due("CannonMount.FuseBurning:100", 2d)) ModLog.Every("CannonMount.FuseBurning:100", 2d, $"CannonMount.FuseBurning: returns {returnedAt100} (line 100)"); return returnedAt100; }
        }

        /// <summary>Turns the barrel to the aim: yaw degrees about the stock's up axis, pitch degrees above the horizon. Local only.</summary>
        internal void ShowAim(float yaw, float pitch)
        {
            if (RuleLog.Due("CannonMount.ShowAim", 2d)) ModLog.Every("CannonMount.ShowAim", 2d, $"CannonMount.ShowAim(yaw={RuleLog.Shown(yaw)}, pitch={RuleLog.Shown(pitch)})");
            if (_pivot == null || _barrel == null) return;
            _pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            _barrel.localRotation = Quaternion.Euler(-pitch, 0f, 0f);
        }

        /// <summary>
        /// Aims the barrel at a spot (the aiming player): sets yaw and pitch for the low arc at the cannon's speed and stores them for the others (at
        /// most every 0.1 s). Expected outcome: the launch velocity from the muzzle; reachable false when the spot is out of reach.
        /// </summary>
        internal Vector3 AimAt(Vector3 spot, out bool reachable)
        {
            if (RuleLog.Due("CannonMount.AimAt", 2d)) ModLog.Every("CannonMount.AimAt", 2d, $"CannonMount.AimAt(spot={RuleLog.Shown(spot)})");
            reachable = false;
            if (_pivot == null || _muzzle == null) { var returnedAt120 = Vector3.zero; if (RuleLog.Due("CannonMount.AimAt:120", 2d)) ModLog.Every("CannonMount.AimAt:120", 2d, $"CannonMount.AimAt: returns {returnedAt120} because _pivot == null || _muzzle == null (line 120)"); return returnedAt120; }
            Vector3 flat = spot - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f) _localYaw = Mathf.DeltaAngle(transform.eulerAngles.y, Quaternion.LookRotation(flat).eulerAngles.y);
            float speed = Speed();
            LaunchSolution s = default(LaunchSolution);
            for (int i = 0; i < 2; i++)                // the muzzle moves with the pitch: solve, turn, solve again from where it is now
            {
                ShowAim(_localYaw, _localPitch);
                s = LaunchSolver.Solve(ToArc(_muzzle.position), ToArc(spot), speed, ModSettings.CannonGravity.Value, Time.fixedDeltaTime);
                _localPitch = s.ElevationDegrees;
            }
            ShowAim(_localYaw, _localPitch);
            reachable = s.Reachable;
            if (Time.time - _lastSync >= SyncEvery && _nview.IsOwner())
            {
                _lastSync = Time.time;
                _nview.GetZDO().Set(KeyYaw, _localYaw);
                _nview.GetZDO().Set(KeyPitch, _localPitch);
            }
            { var returnedAt140 = new Vector3(s.Direction.X, s.Direction.Y, s.Direction.Z) * speed; if (RuleLog.Due("CannonMount.AimAt:140", 2d)) ModLog.Every("CannonMount.AimAt:140", 2d, $"CannonMount.AimAt: returns {returnedAt140} (line 140)"); return returnedAt140; }
        }

        /// <summary>The ball's launch speed: the one that carries it the range at the stock's tilt (D-052).</summary>
        internal static float Speed() => CannonRules.SpeedForRange(ModSettings.CannonRange.Value, ModSettings.CannonGravity.Value, ModSettings.CannonPitch.Value);

        public string GetHoverName() => ItemCatalog.CannonStock.Name;

        public float GetHoverOffset() => 0f;

        public string GetHoverText()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"CannonMount.GetHoverText()");
            if (!IsValid) return "";
            CannonStatus s = Status();
            string use = "[<color=yellow><b>$KEY_Use</b></color>] ", alt = "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] ";
            string text;
            if (!s.Mounted) text = ItemCatalog.CannonStock.Name + "\n" + use + "Mount " + ItemCatalog.Cannon.Name;
            else if (_loading) text = ItemCatalog.Cannon.Name + " (loading ...)";
            else if (s.Lit) text = ItemCatalog.Cannon.Name + " (the fuse burns!)";
            else if (!s.Loaded) text = ItemCatalog.Cannon.Name + " (empty)\n" + use + "Load (" + PowderPerShot() + " Gunpowder + 1 " + ItemCatalog.CannonBall.Name + " or better)\n" + alt + "Take the cannon off";
            else text = ItemCatalog.Cannon.Name + " (loaded)\n" + use + (CannonAim.Current == this ? "Stop aiming" : "Aim") + "\n" + alt + "Take the cannon off";
            { var returnedAt162 = Localization.instance != null ? Localization.instance.Localize(text) : text; if (RuleLog.Due("CannonMount.GetHoverText:162", 2d)) ModLog.Every("CannonMount.GetHoverText:162", 2d, $"CannonMount.GetHoverText: returns {returnedAt162} (line 162)"); return returnedAt162; }
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (RuleLog.Due("CannonMount.Interact", 2d)) ModLog.Every("CannonMount.Interact", 2d, $"CannonMount.Interact(user={RuleLog.Shown(user)}, hold={RuleLog.Shown(hold)}, alt={RuleLog.Shown(alt)})");
            Player p = user as Player;
            if (hold || p == null || p != Player.m_localPlayer || !IsValid) { if (RuleLog.Due("CannonMount.Interact:169", 2d)) ModLog.Every("CannonMount.Interact:169", 2d, $"CannonMount.Interact: returns false because hold || p == null || p != Player.m_localPlayer || !IsValid (line 169)"); return false; }
            CannonStatus s = Status();
            if (alt) { var returnedAt171 = Unmount(p, s); if (RuleLog.Due("CannonMount.Interact:171", 2d)) ModLog.Every("CannonMount.Interact:171", 2d, $"CannonMount.Interact: returns {returnedAt171} because alt (line 171)"); return returnedAt171; }
            if (!s.Mounted) { var returnedAt172 = Mount(p, s); if (RuleLog.Due("CannonMount.Interact:172", 2d)) ModLog.Every("CannonMount.Interact:172", 2d, $"CannonMount.Interact: returns {returnedAt172} because !s.Mounted (line 172)"); return returnedAt172; }
            if (_loading || CannonAim.Current == this) { if (RuleLog.Due("CannonMount.Interact:173", 2d)) ModLog.Every("CannonMount.Interact:173", 2d, $"CannonMount.Interact: returns false because _loading || CannonAim.Current == this (line 173)"); return false; }     // the aim itself ends on the next Use press
            if (!s.Loaded) { var returnedAt174 = Load(p, s); if (RuleLog.Due("CannonMount.Interact:174", 2d)) ModLog.Every("CannonMount.Interact:174", 2d, $"CannonMount.Interact: returns {returnedAt174} because !s.Loaded (line 174)"); return returnedAt174; }
            string why = CannonStateRules.WhyNotAim(s);
            if (why != null) { var returnedAt176 = Tell(p, why); if (RuleLog.Due("CannonMount.Interact:176", 2d)) ModLog.Every("CannonMount.Interact:176", 2d, $"CannonMount.Interact: returns {returnedAt176} because why != null (line 176)"); return returnedAt176; }
            { var returnedAt177 = CannonAim.Begin(this, p); if (RuleLog.Due("CannonMount.Interact:177", 2d)) ModLog.Every("CannonMount.Interact:177", 2d, $"CannonMount.Interact: returns {returnedAt177} (line 177)"); return returnedAt177; }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"CannonMount.UseItem(user={RuleLog.Shown(user)}, item={RuleLog.Shown(item)})");
            Player p = user as Player;
            if (p == null || item == null || item.m_dropPrefab == null || item.m_dropPrefab.name != GunPowderedPrefabs.Cannon || !IsValid) { if (RuleLog.IsOn()) ModLog.Verbose($"CannonMount.UseItem: returns false because p == null || item == null || item.m_dropPrefab == null || item.m_dropPrefab.name != GunPowderedPrefabs.Cannon || !IsValid (line 184)"); return false; }
            { var returnedAt185 = Mount(p, Status()); if (RuleLog.IsOn()) ModLog.Verbose($"CannonMount.UseItem: returns {returnedAt185} (line 185)"); return returnedAt185; }
        }

        bool Mount(Player p, CannonStatus s)
        {
            string why = CannonStateRules.WhyNotMount(s, CannonInventory.Count(p.GetInventory(), GunPowderedPrefabs.Cannon) > 0);
            if (why != null) { var returnedAt191 = Tell(p, why); if (RuleLog.Due("CannonMount.Mount:191", 2d)) ModLog.Every("CannonMount.Mount:191", 2d, $"CannonMount.Mount: returns {returnedAt191} because why != null (line 191)"); return returnedAt191; }
            if (!CannonInventory.Take(p.GetInventory(), GunPowderedPrefabs.Cannon, 1)) { var returnedAt192 = Tell(p, CannonStateRules.NeedCannon); if (RuleLog.Due("CannonMount.Mount:192", 2d)) ModLog.Every("CannonMount.Mount:192", 2d, $"CannonMount.Mount: returns {returnedAt192} because !CannonInventory.Take(p.GetInventory(), GunPowderedPrefabs.Cannon, 1) (line 192)"); return returnedAt192; }
            Claim();
            _nview.GetZDO().Set(KeyMounted, true);
            _nview.GetZDO().Set(KeyLoaded, false);
            ModLog.Verbose("CannonMount: cannon mounted at " + transform.position.ToString("0.0"));
            p.Message(MessageHud.MessageType.TopLeft, ItemCatalog.Cannon.Name + " mounted");
            { if (RuleLog.Due("CannonMount.Mount:198", 2d)) ModLog.Every("CannonMount.Mount:198", 2d, $"CannonMount.Mount: returns true (line 198)"); return true; }
        }

        bool Unmount(Player p, CannonStatus s)
        {
            string why = CannonStateRules.WhyNotUnmount(s);
            if (why != null || _loading) { var returnedAt204 = Tell(p, why ?? "Wait for the loading to finish"); if (RuleLog.Due("CannonMount.Unmount:204", 2d)) ModLog.Every("CannonMount.Unmount:204", 2d, $"CannonMount.Unmount: returns {returnedAt204} because why != null || _loading (line 204)"); return returnedAt204; }
            if (CannonAim.Current == this) CannonAim.End("the cannon was taken off");
            Claim();
            _nview.GetZDO().Set(KeyMounted, false);
            _nview.GetZDO().Set(KeyLoaded, false);
            CannonInventory.Give(p, GunPowderedPrefabs.Cannon, 1);
            if (s.Loaded) GiveLoad(p);
            ModLog.Verbose("CannonMount: cannon taken off" + (s.Loaded ? " with its load" : ""));
            p.Message(MessageHud.MessageType.TopLeft, ItemCatalog.Cannon.Name + " taken off");
            { if (RuleLog.Due("CannonMount.Unmount:213", 2d)) ModLog.Every("CannonMount.Unmount:213", 2d, $"CannonMount.Unmount: returns true (line 213)"); return true; }
        }

        /// <summary>The ball in the barrel (D-145; a cannon loaded before D-145 holds an Iron Cannon Ball).</summary>
        internal string LoadedBall()
        {
            if (RuleLog.Due("CannonMount.LoadedBall", 2d)) ModLog.Every("CannonMount.LoadedBall", 2d, $"CannonMount.LoadedBall()");
            return CannonAmmoRules.LoadedOrOld(IsValid ? _nview.GetZDO().GetString(KeyBall, "") : "");
        }

    }
}
