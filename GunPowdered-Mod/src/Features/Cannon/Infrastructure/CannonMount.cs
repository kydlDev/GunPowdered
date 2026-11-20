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
    internal sealed class CannonMount : MonoBehaviour, Hoverable, Interactable
    {
        internal const string KeyMounted = "gp_cannon_mounted", KeyLoaded = "gp_cannon_loaded", KeyFuse = "gp_cannon_fuse",
            KeyYaw = "gp_cannon_yaw", KeyPitch = "gp_cannon_pitch";
        internal const string PivotName = "gp_pivot", BarrelName = "gp_barrel";
        const float UseRange = 3.5f, AnimEvery = 1.2f, FuseSparkEvery = 0.12f, SyncEvery = 0.1f;
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
            if (RuleLog.On) ModLog.Verbose($"CannonMount.OnDisable()");
            if (CannonAim.Current == this) CannonAim.End("the stock is gone");
        }

        internal CannonStatus Status()
        {
            if (RuleLog.On) ModLog.Every("CannonMount.Status", 2d, $"CannonMount.Status()");
            if (!IsValid) { var returnedAt62 = new CannonStatus(false, false, false); if (RuleLog.On) ModLog.Every("CannonMount.Status:62", 2d, $"CannonMount.Status: returns {returnedAt62} (line 62)"); return returnedAt62; }
            ZDO z = _nview.GetZDO();
            bool loaded = z.GetBool(KeyLoaded, false);
            { var returnedAt65 = new CannonStatus(z.GetBool(KeyMounted, false), loaded, loaded && FuseBurning(z.GetLong(KeyFuse, 0L))); if (RuleLog.On) ModLog.Every("CannonMount.Status:65", 2d, $"CannonMount.Status: returns {returnedAt65} (line 65)"); return returnedAt65; }
        }

        void Update()
        {
            try
            {
                if (!IsValid) { if (RuleLog.On) ModLog.Every("CannonMount.Update:72", 2d, "CannonMount.Update: returns (line 72)"); return; }
                ZDO z = _nview.GetZDO();
                bool mounted = z.GetBool(KeyMounted, false);
                if (_barrel != null && _barrel.gameObject.activeSelf != mounted) _barrel.gameObject.SetActive(mounted);
                if (CannonAim.Current == this) CannonAim.Tick(this);
                else ShowAim(z.GetFloat(KeyYaw, 0f), z.GetFloat(KeyPitch, ModSettings.CannonPitch.Value));
                ModLog.Every("cannon-mount-" + GetInstanceID(), 30d, "CannonMount: mounted " + mounted + ", loaded " + z.GetBool(KeyLoaded, false)
                    + ", aiming " + (CannonAim.Current == this) + ", yaw " + Fmt(z.GetFloat(KeyYaw, 0f)) + ", pitch " + Fmt(z.GetFloat(KeyPitch, 0f)));
            }
            catch (Exception e)
            {
                ModLog.Every("cannon-mount-error", 10d, "CannonMount.Update: " + e.GetType().Name + ": " + e.Message);
            }
        }

        static bool FuseBurning(long fuse)
        {
            if (RuleLog.On) ModLog.Every("CannonMount.FuseBurning", 2d, $"CannonMount.FuseBurning(fuse={fuse})");
            if (fuse == 0L || ZNet.instance == null) { if (RuleLog.On) ModLog.Every("CannonMount.FuseBurning:90", 2d, "CannonMount.FuseBurning: returns false (line 90)"); return false; }
            double since = ZNet.instance.GetTimeSeconds() - (fuse / 1000d);
            { var returnedAt92 = since >= 0d && since < ModSettings.CannonFuseSeconds.Value; if (RuleLog.On) ModLog.Every("CannonMount.FuseBurning:92", 2d, $"CannonMount.FuseBurning: returns {returnedAt92} (line 92)"); return returnedAt92; }
        }

        /// <summary>Turns the barrel to the aim: yaw degrees about the stock's up axis, pitch degrees above the horizon. Local only.</summary>
        internal void ShowAim(float yaw, float pitch)
        {
            if (RuleLog.On) ModLog.Every("CannonMount.ShowAim", 2d, $"CannonMount.ShowAim(yaw={yaw}, pitch={pitch})");
            if (_pivot == null || _barrel == null) { if (RuleLog.On) ModLog.Every("CannonMount.ShowAim:99", 2d, "CannonMount.ShowAim: returns (line 99)"); return; }
            _pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            _barrel.localRotation = Quaternion.Euler(-pitch, 0f, 0f);
        }

        /// <summary>
        /// Aims the barrel at a spot (the aiming player): sets yaw and pitch for the low arc at the cannon's speed and stores them for the others (at
        /// most every 0.1 s). Expected outcome: the launch velocity from the muzzle; reachable false when the spot is out of reach.
        /// </summary>
        internal Vector3 AimAt(Vector3 spot, out bool reachable)
        {
            if (RuleLog.On) ModLog.Every("CannonMount.AimAt", 2d, $"CannonMount.AimAt(spot={spot})");
            reachable = false;
            if (_pivot == null || _muzzle == null) { var returnedAt112 = Vector3.zero; if (RuleLog.On) ModLog.Every("CannonMount.AimAt:112", 2d, $"CannonMount.AimAt: returns {returnedAt112} (line 112)"); return returnedAt112; }
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
            { var returnedAt132 = new Vector3(s.Direction.X, s.Direction.Y, s.Direction.Z) * speed; if (RuleLog.On) ModLog.Every("CannonMount.AimAt:132", 2d, $"CannonMount.AimAt: returns {returnedAt132} (line 132)"); return returnedAt132; }
        }

        /// <summary>The ball's launch speed: the one that carries it the range at the stock's tilt (D-052).</summary>
        internal static float Speed() => CannonRules.SpeedForRange(ModSettings.CannonRange.Value, ModSettings.CannonGravity.Value, ModSettings.CannonPitch.Value);

        public string GetHoverName() => ItemCatalog.CannonStock.Name;

        public float GetHoverOffset() => 0f;

        public string GetHoverText()
        {
            if (RuleLog.On) ModLog.Verbose($"CannonMount.GetHoverText()");
            if (!IsValid) { if (RuleLog.On) ModLog.Verbose("CannonMount.GetHoverText: returns \"\" (line 145)"); return ""; }
            CannonStatus s = Status();
            string use = "[<color=yellow><b>$KEY_Use</b></color>] ", alt = "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] ";
            string text;
            if (!s.Mounted) text = ItemCatalog.CannonStock.Name + "\n" + use + "Mount " + ItemCatalog.Cannon.Name;
            else if (_loading) text = ItemCatalog.Cannon.Name + " (loading ...)";
            else if (s.Lit) text = ItemCatalog.Cannon.Name + " (the fuse burns!)";
            else if (!s.Loaded) text = ItemCatalog.Cannon.Name + " (empty)\n" + use + "Load (" + PowderPerShot() + " Gunpowder + 1 " + ItemCatalog.CannonBall.Name + ")\n" + alt + "Take the cannon off";
            else text = ItemCatalog.Cannon.Name + " (loaded)\n" + use + (CannonAim.Current == this ? "Stop aiming" : "Aim") + "\n" + alt + "Take the cannon off";
            { var returnedAt154 = Localization.instance != null ? Localization.instance.Localize(text) : text; if (RuleLog.On) ModLog.Verbose($"CannonMount.GetHoverText: returns {returnedAt154} (line 154)"); return returnedAt154; }
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (RuleLog.On) ModLog.Every("CannonMount.Interact", 2d, $"CannonMount.Interact(user={user}, hold={hold}, alt={alt})");
            Player p = user as Player;
            if (hold || p == null || p != Player.m_localPlayer || !IsValid) { if (RuleLog.On) ModLog.Every("CannonMount.Interact:161", 2d, "CannonMount.Interact: returns false (line 161)"); return false; }
            CannonStatus s = Status();
            if (alt) { var returnedAt163 = Unmount(p, s); if (RuleLog.On) ModLog.Every("CannonMount.Interact:163", 2d, $"CannonMount.Interact: returns {returnedAt163} (line 163)"); return returnedAt163; }
            if (!s.Mounted) { var returnedAt164 = Mount(p, s); if (RuleLog.On) ModLog.Every("CannonMount.Interact:164", 2d, $"CannonMount.Interact: returns {returnedAt164} (line 164)"); return returnedAt164; }
            if (_loading || CannonAim.Current == this) { if (RuleLog.On) ModLog.Every("CannonMount.Interact:165", 2d, "CannonMount.Interact: returns false (line 165)"); return false; }     // the aim itself ends on the next Use press
            if (!s.Loaded) { var returnedAt166 = Load(p, s); if (RuleLog.On) ModLog.Every("CannonMount.Interact:166", 2d, $"CannonMount.Interact: returns {returnedAt166} (line 166)"); return returnedAt166; }
            string why = CannonStateRules.WhyNotAim(s);
            if (why != null) { var returnedAt168 = Tell(p, why); if (RuleLog.On) ModLog.Every("CannonMount.Interact:168", 2d, $"CannonMount.Interact: returns {returnedAt168} (line 168)"); return returnedAt168; }
            { var returnedAt169 = CannonAim.Begin(this, p); if (RuleLog.On) ModLog.Every("CannonMount.Interact:169", 2d, $"CannonMount.Interact: returns {returnedAt169} (line 169)"); return returnedAt169; }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (RuleLog.On) ModLog.Verbose($"CannonMount.UseItem(user={user}, item={item})");
            Player p = user as Player;
            if (p == null || item == null || item.m_dropPrefab == null || item.m_dropPrefab.name != GunPowderedPrefabs.Cannon || !IsValid) { if (RuleLog.On) ModLog.Verbose("CannonMount.UseItem: returns false (line 176)"); return false; }
            { var returnedAt177 = Mount(p, Status()); if (RuleLog.On) ModLog.Verbose($"CannonMount.UseItem: returns {returnedAt177} (line 177)"); return returnedAt177; }
        }

        bool Mount(Player p, CannonStatus s)
        {
            string why = CannonStateRules.WhyNotMount(s, CannonInventory.Count(p.GetInventory(), GunPowderedPrefabs.Cannon) > 0);
            if (why != null) { var returnedAt183 = Tell(p, why); if (RuleLog.On) ModLog.Every("CannonMount.Mount:183", 2d, $"CannonMount.Mount: returns {returnedAt183} (line 183)"); return returnedAt183; }
            if (!CannonInventory.Take(p.GetInventory(), GunPowderedPrefabs.Cannon, 1)) { var returnedAt184 = Tell(p, CannonStateRules.NeedCannon); if (RuleLog.On) ModLog.Every("CannonMount.Mount:184", 2d, $"CannonMount.Mount: returns {returnedAt184} (line 184)"); return returnedAt184; }
            Claim();
            _nview.GetZDO().Set(KeyMounted, true);
            _nview.GetZDO().Set(KeyLoaded, false);
            ModLog.Verbose("CannonMount: cannon mounted at " + transform.position.ToString("0.0"));
            p.Message(MessageHud.MessageType.TopLeft, ItemCatalog.Cannon.Name + " mounted");
            { if (RuleLog.On) ModLog.Every("CannonMount.Mount:190", 2d, "CannonMount.Mount: returns true (line 190)"); return true; }
        }

        bool Unmount(Player p, CannonStatus s)
        {
            string why = CannonStateRules.WhyNotUnmount(s);
            if (why != null || _loading) { var returnedAt196 = Tell(p, why ?? "Wait for the loading to finish"); if (RuleLog.On) ModLog.Every("CannonMount.Unmount:196", 2d, $"CannonMount.Unmount: returns {returnedAt196} (line 196)"); return returnedAt196; }
            if (CannonAim.Current == this) CannonAim.End("the cannon was taken off");
            Claim();
            _nview.GetZDO().Set(KeyMounted, false);
            _nview.GetZDO().Set(KeyLoaded, false);
            CannonInventory.Give(p, GunPowderedPrefabs.Cannon, 1);
            if (s.Loaded) GiveLoad(p);
            ModLog.Verbose("CannonMount: cannon taken off" + (s.Loaded ? " with its load" : ""));
            p.Message(MessageHud.MessageType.TopLeft, ItemCatalog.Cannon.Name + " taken off");
            { if (RuleLog.On) ModLog.Every("CannonMount.Unmount:205", 2d, "CannonMount.Unmount: returns true (line 205)"); return true; }
        }

        void GiveLoad(Player p)
        {
            if (RuleLog.On) ModLog.Every("CannonMount.GiveLoad", 2d, $"CannonMount.GiveLoad(p={p})");
            CannonInventory.Give(p, GunPowderedPrefabs.CannonBall, 1);
            CannonInventory.Give(p, GunPowderedPrefabs.Gunpowder, PowderPerShot());
        }

        bool Load(Player p, CannonStatus s)
        {
            if (RuleLog.On) ModLog.Every("CannonMount.Load", 2d, $"CannonMount.Load(p={p}, s={s})");
            Inventory inv = p.GetInventory();
            string why = CannonStateRules.WhyNotLoad(s, CannonInventory.Count(inv, GunPowderedPrefabs.Gunpowder), PowderPerShot(),
                CannonInventory.Count(inv, GunPowderedPrefabs.CannonBall));
            if (why != null) { var returnedAt221 = Tell(p, why); if (RuleLog.On) ModLog.Every("CannonMount.Load:221", 2d, $"CannonMount.Load: returns {returnedAt221} (line 221)"); return returnedAt221; }
            StartCoroutine(LoadAtMuzzle(p));
            { if (RuleLog.On) ModLog.Every("CannonMount.Load:223", 2d, "CannonMount.Load: returns true (line 223)"); return true; }
        }

        // D-053: 4 seconds at the muzzle with the cast ball in the player's hand; walking away or dying stops it, nothing is used up then
        IEnumerator LoadAtMuzzle(Player p)
        {
            _loading = true;
            float seconds = ModSettings.CannonLoadSeconds.Value, end = Time.time + seconds, nextAnim = Time.time + AnimEvery;
            bool shown = ShowBallInHand(p, seconds);
            ModLog.Verbose("CannonMount: loading for " + Fmt(seconds) + " s, ball in hand " + shown);
            while (Time.time < end)
            {
                if (p == null || p.IsDead() || !IsValid || Vector3.Distance(p.transform.position, transform.position) > UseRange)
                {
                    _loading = false;
                    if (p != null) p.Message(MessageHud.MessageType.Center, "Loading stopped");
                    ModLog.Verbose("CannonMount: loading stopped (the player left)");
                    yield break;
                }
                if (Time.time >= nextAnim)
                {
                    p.GetZAnim().SetTrigger("interact");
                    nextAnim += AnimEvery;
                }
                yield return null;
            }
            _loading = false;
            Inventory inv = p.GetInventory();
            string why = CannonStateRules.WhyNotLoad(Status(), CannonInventory.Count(inv, GunPowderedPrefabs.Gunpowder), PowderPerShot(),
                CannonInventory.Count(inv, GunPowderedPrefabs.CannonBall));
            if (why != null) { Tell(p, why); yield break; }
            CannonInventory.Take(inv, GunPowderedPrefabs.Gunpowder, PowderPerShot());
            CannonInventory.Take(inv, GunPowderedPrefabs.CannonBall, 1);
            Claim();
            _nview.GetZDO().Set(KeyLoaded, true);
            _nview.GetZDO().Set(KeyFuse, 0L);
            ModLog.Verbose("CannonMount: loaded");
            p.Message(MessageHud.MessageType.TopLeft, ItemCatalog.Cannon.Name + " loaded");
        }

        // the cast ball shows in the right hand for the loading time, for everybody (the game's own "use item" hand visual, as for a feast)
        static bool ShowBallInHand(Player p, float seconds)
        {
            GameObject ball = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(GunPowderedPrefabs.CannonBall) : null;
            if (UseHandVisual == null || ball == null) { if (RuleLog.On) ModLog.Every("CannonMount.ShowBallInHand:267", 2d, "CannonMount.ShowBallInHand: returns false (line 267)"); return false; }
            try
            {
                UseHandVisual.Invoke(p, new object[] { ball, seconds });
                { if (RuleLog.On) ModLog.Every("CannonMount.ShowBallInHand:271", 2d, "CannonMount.ShowBallInHand: returns true (line 271)"); return true; }
            }
            catch (Exception e)
            {
                ModLog.Warn("CannonMount: the ball could not be shown in the hand: " + (e.InnerException ?? e).Message);
                return false;
            }
        }

        /// <summary>
        /// Lights the fuse (the aiming player, torch in hand): the shot follows after the fuse seconds with the given launch. Expected outcome: null when
        /// lit, else the reason.
        /// </summary>
        internal string Light(Player p, Vector3 velocity, float targetFlat)
        {
            string why = CannonStateRules.WhyNotLight(Status(), CannonInventory.HoldsTorch(p));
            if (why != null || _fireQueued) { var returnedAt287 = why ?? CannonStateRules.FuseBurning; if (RuleLog.On) ModLog.Every("CannonMount.Light:287", 2d, $"CannonMount.Light: returns {returnedAt287} (line 287)"); return returnedAt287; }
            Claim();
            _nview.GetZDO().Set(KeyFuse, (long)(ZNet.instance.GetTimeSeconds() * 1000d));
            _fireQueued = true;
            StartCoroutine(FireAfterFuse(p, velocity, targetFlat));
            ModLog.Verbose("CannonMount: fuse lit, " + Fmt(ModSettings.CannonFuseSeconds.Value) + " s to the shot");
            return null;
        }

        // sparks and smoke out of the touch hole while the fuse burns, then the blast at the muzzle and the shot (D-052 addendum). The effects are
        // networked vanilla ones: this client makes them, the others see them.
        IEnumerator FireAfterFuse(Player p, Vector3 velocity, float targetFlat)
        {
            if (RuleLog.On) ModLog.Every("CannonMount.FireAfterFuse", 2d, $"CannonMount.FireAfterFuse(p={p}, velocity={velocity}, targetFlat={targetFlat})");
            float end = Time.time + ModSettings.CannonFuseSeconds.Value;
            while (Time.time < end)
            {
                if (_touchHole != null) CannonEffects.PlayFuse(_touchHole.position);
                yield return new WaitForSeconds(Mathf.Min(FuseSparkEvery, Mathf.Max(0.01f, end - Time.time)));
            }
            _fireQueued = false;
            if (!IsValid || !Status().Loaded) yield break;
            if (_muzzle != null) CannonEffects.PlayMuzzle(_muzzle.position, velocity);
            Fire(p, velocity, targetFlat);
        }

        void Fire(Player p, Vector3 velocity, float targetFlat)
        {
            Claim();
            _nview.GetZDO().Set(KeyLoaded, false);
            GameObject flightPrefab = CannonSetup.Projectile;
            if (flightPrefab == null || _muzzle == null) { ModLog.Warn("CannonMount: no ball flight prefab, the shot is lost"); return; }
            GameObject ball = Instantiate(flightPrefab, _muzzle.position, Quaternion.LookRotation(velocity));
            CannonBallFlight flight = ball.GetComponent<CannonBallFlight>();
            bool flies = flight != null && flight.Launch(p, transform, velocity, targetFlat);
            if (p != null && ModSettings.CannonRecoil.Value > 0f) p.ApplyPushback(-new Vector3(velocity.x, 0f, velocity.z).normalized, ModSettings.CannonRecoil.Value);
            ModLog.Verbose("CannonMount: fired, ball flies " + flies + ", velocity " + velocity.ToString("0.0") + ", aimed " + Fmt(targetFlat) + " m");
            if (CannonAim.Current == this) CannonAim.End("fired");
        }

        // broken or dismantled: the cannon (and its load) drop next to the stock's own wood and resin (D-052 addendum)
        void OnDestroyed()
        {
            try
            {
                if (!IsValid) { if (RuleLog.On) ModLog.Verbose("CannonMount.OnDestroyed: returns (line 332)"); return; }
                CannonStatus s = Status();
                Vector3 at = transform.position + (Vector3.up * 0.5f);
                if (s.Mounted) CannonInventory.Drop(GunPowderedPrefabs.Cannon, 1, at);
                if (s.Mounted && s.Loaded)
                {
                    CannonInventory.Drop(GunPowderedPrefabs.CannonBall, 1, at);
                    CannonInventory.Drop(GunPowderedPrefabs.Gunpowder, PowderPerShot(), at);
                }
                ModLog.Verbose("CannonMount: stock destroyed, dropped cannon " + s.Mounted + ", load " + (s.Mounted && s.Loaded));
            }
            catch (Exception e)
            {
                ModLog.Warn("CannonMount.OnDestroyed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        void Claim()
        {
            if (RuleLog.On) ModLog.Every("CannonMount.Claim", 2d, $"CannonMount.Claim()");
            if (!_nview.IsOwner()) _nview.ClaimOwnership();
        }

        static bool Tell(Player p, string why)
        {
            if (RuleLog.On) ModLog.Every("CannonMount.Tell", 2d, $"CannonMount.Tell(p={p}, why={why})");
            p.Message(MessageHud.MessageType.Center, why);
            { if (RuleLog.On) ModLog.Every("CannonMount.Tell:359", 2d, "CannonMount.Tell: returns false (line 359)"); return false; }
        }

        internal static int PowderPerShot() => Mathf.Max(1, Mathf.RoundToInt(ModSettings.CannonPowderPerShot.Value));

        static Transform Deep(Transform root, string name)
        {
            if (RuleLog.On) ModLog.Verbose($"CannonMount.Deep(root={root}, name={name})");
            if (root == null) { if (RuleLog.On) ModLog.Verbose("CannonMount.Deep: returns null (line 367)"); return null; }
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) { var returnedAt368 = t; if (RuleLog.On) ModLog.Verbose($"CannonMount.Deep: returns {returnedAt368} (line 368)"); return returnedAt368; }
            { if (RuleLog.On) ModLog.Verbose("CannonMount.Deep: returns null (line 369)"); return null; }
        }

        static ArcVector ToArc(Vector3 v) => new ArcVector(v.x, v.y, v.z);
        static string Fmt(float f) => f.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
