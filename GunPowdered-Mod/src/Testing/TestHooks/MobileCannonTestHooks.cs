using System.Collections;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the Mobile Cannon (D-104). The cannon is placed, grabbed and pulled through the game's own cart code (Vagon.Interact and its
    /// joint); only the walking is driven by the hook, because the lab has no walk step.
    /// </summary>
    internal static class MobileCannonTestHooks
    {
        static GameObject _cannon;
        static Vector3 _placedAt;

        internal static GameObject Cannon => _cannon;

        /// <summary>The built prefab: "cart True, pull (x, y, z), colliders N, model True, wheels W".</summary>
        [TestHook("GunPowdered.mobileCannonInfo")]
        static string Info()
        {
            if (RuleLog.Due("MobileCannonTestHooks.Info", 2d)) ModLog.Every("MobileCannonTestHooks.Info", 2d, $"MobileCannonTestHooks.Info()");
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.MobileCannon) : null;
            Vagon vagon = prefab != null ? prefab.GetComponent<Vagon>() : null;
            if (vagon == null) { var returnedAt27 = "no cart in " + GunPowderedPrefabs.MobileCannon; if (RuleLog.Due("MobileCannonTestHooks.Info:27", 2d)) ModLog.Every("MobileCannonTestHooks.Info:27", 2d, $"MobileCannonTestHooks.Info: returns {returnedAt27} because vagon == null (line 27)"); return returnedAt27; }
            int colliders = 0;
            foreach (Collider c in prefab.GetComponentsInChildren<Collider>(true)) if (c.enabled && !c.isTrigger) colliders++;
            bool model = prefab.transform.Find(MobileCannonSetup.ModelName) != null;
            return "cart True, pull " + vagon.m_attachPoint.localPosition.ToString("0.00") + ", colliders " + colliders + ", model " + model
                + ", wheels " + (vagon.m_wheels != null ? vagon.m_wheels.Length : 0) + ", storage " + (prefab.GetComponentInChildren<Container>(true) != null);
        }

        /// <summary>Builds a Mobile Cannon <paramref name="metres"/> in front of the local player with its trail towards the player. Returns "placed at (x, y, z)".</summary>
        [TestHook("GunPowdered.mobileCannonPlace")]
        static string Place(double metres)
        {
            if (RuleLog.Due("MobileCannonTestHooks.Place", 2d)) ModLog.Every("MobileCannonTestHooks.Place", 2d, $"MobileCannonTestHooks.Place(metres={metres})");
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.MobileCannon) : null;
            if (p == null || prefab == null) { if (RuleLog.Due("MobileCannonTestHooks.Place:42", 2d)) ModLog.Every("MobileCannonTestHooks.Place:42", 2d, $"MobileCannonTestHooks.Place: returns \"no player or no prefab\" because p == null || prefab == null (line 42)"); return "no player or no prefab"; }
            Vector3 at = p.transform.position + p.transform.forward * (float)metres;
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h + 0.05f;
            _cannon = Object.Instantiate(prefab, at, Quaternion.LookRotation(-p.transform.forward));
            _placedAt = at;
            { var returnedAt48 = "placed at " + at.ToString("0.0"); if (RuleLog.Due("MobileCannonTestHooks.Place:48", 2d)) ModLog.Every("MobileCannonTestHooks.Place:48", 2d, $"MobileCannonTestHooks.Place: returns {returnedAt48} (line 48)"); return returnedAt48; }
        }

        /// <summary>Puts the local player at the trail end, facing away from the cannon, and presses E on the cart (the game attaches next frame). Returns "grab asked".</summary>
        [TestHook("GunPowdered.mobileCannonGrab")]
        static string Grab()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.Grab()");
            Player p = Player.m_localPlayer;
            Vagon vagon = _cannon != null ? _cannon.GetComponent<Vagon>() : null;
            if (p == null || vagon == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.Grab: returns \"no player or no cannon\" because p == null || vagon == null (line 58)"); return "no player or no cannon"; }
            Vector3 pull = vagon.m_attachPoint.position - vagon.m_attachOffset;
            Vector3 away = _cannon.transform.forward;
            Rigidbody body = p.GetComponent<Rigidbody>();
            p.transform.position = pull + away * 0.1f;
            p.transform.rotation = Quaternion.LookRotation(away);
            if (body != null)
            {
                body.position = p.transform.position;           // the body, or the next physics step puts the player back
                body.rotation = p.transform.rotation;
                body.linearVelocity = Vector3.zero;
            }
            p.SetLookDir(away);
            vagon.Interact(p, false, false);                  // the game's cart always answers false; it attaches in its next Update
            { var returnedAt72 = "grab asked at " + p.transform.position.ToString("0.0"); if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.Grab: returns {returnedAt72} (line 72)"); return returnedAt72; }
        }

        /// <summary>True while the local player is hitched to the placed cannon.</summary>
        [TestHook("GunPowdered.mobileCannonAttached")]
        static bool Attached()
        {
            if (RuleLog.Due("MobileCannonTestHooks.Attached", 2d)) ModLog.Every("MobileCannonTestHooks.Attached", 2d, $"MobileCannonTestHooks.Attached()");
            Vagon vagon = _cannon != null ? _cannon.GetComponent<Vagon>() : null;
            { var returnedAt81 = vagon != null && Player.m_localPlayer != null && vagon.IsAttached(Player.m_localPlayer); if (RuleLog.Due("MobileCannonTestHooks.Attached:81", 2d)) ModLog.Every("MobileCannonTestHooks.Attached:81", 2d, $"MobileCannonTestHooks.Attached: returns {returnedAt81} (line 81)"); return returnedAt81; }
        }

        /// <summary>Walks the local player <paramref name="metres"/> straight on over <paramref name="seconds"/> (physics steps). Returns "walking".</summary>
        [TestHook("GunPowdered.mobileCannonWalk")]
        static string Walk(double metres, double seconds)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.Walk(metres={metres}, seconds={seconds})");
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.Walk: returns \"no player\" because p == null (line 90)"); return "no player"; }
            p.StartCoroutine(WalkRoutine(p, (float)metres, (float)seconds));
            { var returnedAt92 = "walking " + metres.ToString("0.0") + " m in " + seconds.ToString("0.0") + " s"; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.Walk: returns {returnedAt92} (line 92)"); return returnedAt92; }
        }

        static IEnumerator WalkRoutine(Player p, float metres, float seconds)
        {
            Rigidbody body = p.GetComponent<Rigidbody>();
            Vector3 dir = p.transform.forward;
            float done = 0f, speed = metres / Mathf.Max(0.1f, seconds);
            var wait = new WaitForFixedUpdate();
            while (done < metres && p != null && body != null)
            {
                float step = speed * Time.fixedDeltaTime;
                Vector3 next = body.position + dir * step;
                float h;
                if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(next, out h)) next.y = h;
                body.MovePosition(next);
                done += step;
                yield return wait;
            }
            ModLog.Info("MobileCannonTestHooks: walked " + done.ToString("0.0") + " m");
        }

        /// <summary>"moved D m, wheels W deg, attached A": how far the placed cannon has rolled from where it was built and how far its wheels turned.</summary>
        [TestHook("GunPowdered.mobileCannonState")]
        static string State()
        {
            if (_cannon == null) { if (RuleLog.Due("MobileCannonTestHooks.State:118", 2d)) ModLog.Every("MobileCannonTestHooks.State:118", 2d, $"MobileCannonTestHooks.State: returns \"no cannon\" because _cannon == null (line 118)"); return "no cannon"; }
            MobileCannonWheels wheels = _cannon.GetComponent<MobileCannonWheels>();
            float moved = Vector3.Distance(new Vector3(_cannon.transform.position.x, 0f, _cannon.transform.position.z), new Vector3(_placedAt.x, 0f, _placedAt.z));
            string s = "moved " + moved.ToString("0.0") + " m, wheels " + (wheels != null ? wheels.TurnedDegrees.ToString("0") : "?") + " deg, attached " + Attached();
            ModLog.Info("MobileCannonTestHooks: " + s);
            return s;
        }

        /// <summary>Lets go of the cannon and removes it. Returns "removed".</summary>
        [TestHook("GunPowdered.mobileCannonRemove")]
        static string Remove()
        {
            if (RuleLog.Due("MobileCannonTestHooks.Remove", 2d)) ModLog.Every("MobileCannonTestHooks.Remove", 2d, $"MobileCannonTestHooks.Remove()");
            if (_cannon == null) return "nothing to remove";
            ZNetView view = _cannon.GetComponent<ZNetView>();
            if (view != null && view.IsValid()) view.Destroy(); else Object.Destroy(_cannon);
            _cannon = null;
            { if (RuleLog.Due("MobileCannonTestHooks.Remove:135", 2d)) ModLog.Every("MobileCannonTestHooks.Remove:135", 2d, $"MobileCannonTestHooks.Remove: returns \"removed\" (line 135)"); return "removed"; }
        }
        /// <summary>"max speed S m/s, stamina used U, facing A deg": the hitched local player's fastest speed and stamina since the hitch, and how far they look away from the cannon.</summary>
        [TestHook("GunPowdered.mobileCannonPullStats")]
        static string PullStats()
        {
            Player p = Player.m_localPlayer;
            MobileCannonPull pull = _cannon != null ? _cannon.GetComponent<MobileCannonPull>() : null;
            if (p == null || pull == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.PullStats: returns \"no player or no cannon\" because p == null || pull == null (line 143)"); return "no player or no cannon"; }
            float facing = Quaternion.Angle(Quaternion.LookRotation(Vector3.ProjectOnPlane(p.transform.forward, Vector3.up)), pull.FacingFor(p));
            string s = "max speed " + pull.MaxSpeedSeen.ToString("0.0") + " m/s, stamina used " + pull.StaminaUsed.ToString("0.0") + ", facing " + facing.ToString("0") + " deg, hands on handle " + pull.HandsOnHandle;
            ModLog.Info("MobileCannonTestHooks: " + s);
            return s;
        }
        /// <summary>Turns the placed cannon's crank by <paramref name="notches"/> wheel notches through the same RPC as the mouse wheel. Returns "turned N".</summary>
        [TestHook("GunPowdered.mobileCannonCrank")]
        static string Crank(int notches)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.Crank(notches={notches})");
            MobileCannonAim aim = _cannon != null ? _cannon.GetComponent<MobileCannonAim>() : null;
            if (aim == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.Crank: returns \"no cannon\" because aim == null (line 155)"); return "no cannon"; }
            { var returnedAt156 = aim.Turn(notches) ? "turned " + notches : "not sent"; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.Crank: returns {returnedAt156} (line 156)"); return returnedAt156; }
        }

        /// <summary>"pitch P deg, barrel B deg, hover H": the stored elevation, the barrel's real tilt (+ = muzzle up) and whether a crank handle with a hover text exists.</summary>
        [TestHook("GunPowdered.mobileCannonPitch")]
        static string PitchInfo()
        {
            MobileCannonAim aim = _cannon != null ? _cannon.GetComponent<MobileCannonAim>() : null;
            if (aim == null || aim.BarrelPart == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.PitchInfo: returns \"no cannon\" because aim == null || aim.BarrelPart == null (line 164)"); return "no cannon"; }
            Vector3 muzzleDir = -_cannon.transform.forward;                       // the model is turned: the muzzle points to the cart's back
            Vector3 barrelDir = aim.BarrelPart.forward;
            float tilt = Mathf.Asin(Mathf.Clamp(Vector3.Dot(barrelDir, _cannon.transform.up), -1f, 1f)) * Mathf.Rad2Deg;     // against the carriage
            float carriage = Mathf.Asin(Mathf.Clamp(Vector3.Dot(muzzleDir, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;           // the carriage's own lean, muzzle side up = +
            MobileCannonCrank crank = _cannon.GetComponentInChildren<MobileCannonCrank>();
            string s = "pitch " + aim.Pitch.ToString("0.0") + " deg, barrel " + tilt.ToString("0.0") + " deg, carriage " + carriage.ToString("0.0") + " deg, muzzle back " + (Vector3.Dot(Vector3.ProjectOnPlane(barrelDir, Vector3.up).normalized, muzzleDir) > 0.9f)
                + ", hover " + (crank != null ? crank.GetHoverText().Split('\n')[0] : "none");
            ModLog.Info("MobileCannonTestHooks: " + s);
            return s;
        }
        /// <summary>Stands the local player next to the placed cannon's muzzle (1.2 m in front of it, facing it) and starts loading there. Returns "loading" or the reason.</summary>
        [TestHook("GunPowdered.mobileCannonLoad")]
        static string Load()
        {
            if (RuleLog.Due("MobileCannonTestHooks.Load", 2d)) ModLog.Every("MobileCannonTestHooks.Load", 2d, $"MobileCannonTestHooks.Load()");
            Player p = Player.m_localPlayer;
            MobileCannonGun gun = _cannon != null ? _cannon.GetComponent<MobileCannonGun>() : null;
            if (p == null || gun == null || gun.MuzzlePoint == null) { if (RuleLog.Due("MobileCannonTestHooks.Load:182", 2d)) ModLog.Every("MobileCannonTestHooks.Load:182", 2d, $"MobileCannonTestHooks.Load: returns \"no player or no cannon\" because p == null || gun == null || gun.MuzzlePoint == null (line 182)"); return "no player or no cannon"; }
            Vector3 outward = Vector3.ProjectOnPlane(gun.MuzzlePoint.forward, Vector3.up).normalized;
            StandAt(p, gun.MuzzlePoint.position + outward * 1.2f, -outward);
            { var returnedAt185 = gun.Load(p) ? "loading" : "refused"; if (RuleLog.Due("MobileCannonTestHooks.Load:185", 2d)) ModLog.Every("MobileCannonTestHooks.Load:185", 2d, $"MobileCannonTestHooks.Load: returns {returnedAt185} (line 185)"); return returnedAt185; }
        }

        /// <summary>"loaded L, lit F, loading G".</summary>
        [TestHook("GunPowdered.mobileCannonGunState")]
        static string GunState()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.GunState()");
            MobileCannonGun gun = _cannon != null ? _cannon.GetComponent<MobileCannonGun>() : null;
            { var returnedAt194 = gun == null ? "no cannon" : "loaded " + gun.Loaded + ", lit " + gun.Lit + ", loading " + gun.Loading; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.GunState: returns {returnedAt194} (line 194)"); return returnedAt194; }
        }

        /// <summary>Stands the local player beside the touch hole and lights the fuse with the torch in hand. Returns "lit" or the reason.</summary>
        [TestHook("GunPowdered.mobileCannonLight")]
        static string Light()
        {
            if (RuleLog.Due("MobileCannonTestHooks.Light", 2d)) ModLog.Every("MobileCannonTestHooks.Light", 2d, $"MobileCannonTestHooks.Light()");
            Player p = Player.m_localPlayer;
            MobileCannonGun gun = _cannon != null ? _cannon.GetComponent<MobileCannonGun>() : null;
            if (p == null || gun == null || gun.TouchHolePoint == null) { if (RuleLog.Due("MobileCannonTestHooks.Light:204", 2d)) ModLog.Every("MobileCannonTestHooks.Light:204", 2d, $"MobileCannonTestHooks.Light: returns \"no player or no cannon\" because p == null || gun == null || gun.TouchHolePoint == null (line 204)"); return "no player or no cannon"; }
            Vector3 side = _cannon.transform.right;
            StandAt(p, gun.TouchHolePoint.position + side * 1.3f, -side);
            string why = gun.Light(p);
            { var returnedAt208 = why ?? "lit"; if (RuleLog.Due("MobileCannonTestHooks.Light:208", 2d)) ModLog.Every("MobileCannonTestHooks.Light:208", 2d, $"MobileCannonTestHooks.Light: returns {returnedAt208} (line 208)"); return returnedAt208; }
        }

        static GameObject _target;

        /// <summary>D-107: puts a vanilla piece (an Ashlands fortress wall / gate) on the ground <paramref name="metres"/> in front of the Mobile Cannon's muzzle,
        /// facing it. Returns "target P at D m, health H".</summary>
        [TestHook("GunPowdered.mobileCannonTargetAhead")]
        static string TargetAhead(string prefab, double metres)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.TargetAhead(prefab={prefab}, metres={metres})");
            MobileCannonGun gun = _cannon != null ? _cannon.GetComponent<MobileCannonGun>() : null;
            GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (gun == null || gun.MuzzlePoint == null || source == null) { var returnedAt221 = "no cannon or no prefab " + prefab; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.TargetAhead: returns {returnedAt221} because gun == null || gun.MuzzlePoint == null || source == null (line 221)"); return returnedAt221; }
            Vector3 dir = Vector3.ProjectOnPlane(-_cannon.transform.forward, Vector3.up).normalized;      // the muzzle points to the cart's back
            Vector3 at = gun.MuzzlePoint.position + dir * (float)metres;
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h;
            _target = Object.Instantiate(source, at, Quaternion.LookRotation(-dir));
            { var returnedAt227 = "target " + prefab + " at " + metres.ToString("0") + " m, health " + TargetHealth(); if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.TargetAhead: returns {returnedAt227} (line 227)"); return returnedAt227; }
        }

        /// <summary>The target's health (WearNTear / Destructible / MineRock5), e.g. "6000". "none" without a target.</summary>
        [TestHook("GunPowdered.mobileCannonTargetHealth")]
        static string TargetHealth()
        {
            if (_target == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.TargetHealth: returns \"none\" because _target == null (line 234)"); return "none"; }
            ZNetView v = _target.GetComponent<ZNetView>();
            WearNTear w = _target.GetComponent<WearNTear>();
            float max = w != null ? w.m_health : 0f;
            float now = v != null && v.IsValid() ? v.GetZDO().GetFloat(ZDOVars.s_health, max) : max;
            string s = now.ToString("0") + " of " + max.ToString("0");
            ModLog.Info("MobileCannonTestHooks: target " + _target.name + " health " + s);
            return s;
        }

        /// <summary>Removes the target piece. Returns "target removed".</summary>
        [TestHook("GunPowdered.mobileCannonTargetRemove")]
        static string TargetRemove()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.TargetRemove()");
            if (_target != null) { ZNetView v = _target.GetComponent<ZNetView>(); if (v != null && v.IsValid()) { v.ClaimOwnership(); ZNetScene.instance.Destroy(_target); } }
            _target = null;
            { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.TargetRemove: returns \"target removed\" (line 251)"); return "target removed"; }
        }

        /// <summary>Burning on the nearest creature of a prefab within 40 m (D-107): "burning True, per tick T, left L" (the game's SE_Burning: 1 tick a second
        /// for 5 s), "burning False" or "no P".</summary>
        [TestHook("GunPowdered.burningOn")]
        static string BurningOn(string prefab)
        {
            Player p = Player.m_localPlayer;
            Character best = null;
            float bestDist = 40f;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c == p || Utils.GetPrefabName(c.gameObject) != prefab) { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.BurningOn: skips to the next item (continue) because c == null || c == p || Utils.GetPrefabName(c.gameObject) != prefab (line 264)"); continue; }
                float d = p != null ? Vector3.Distance(c.transform.position, p.transform.position) : 0f;
                if (d < bestDist) { best = c; bestDist = d; }
            }
            if (best == null) { var returnedAt268 = "no " + prefab; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.BurningOn: returns {returnedAt268} because best == null (line 268)"); return returnedAt268; }
            StatusEffect se = best.GetSEMan() != null ? best.GetSEMan().GetStatusEffect(SEMan.s_statusEffectBurning) : null;
            if (se == null) { var returnedAt270 = "burning False" + (best.IsDead() ? " (dead)" : ""); if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTestHooks.BurningOn: returns {returnedAt270} because se == null (line 270)"); return returnedAt270; }
            float perHit = (float)HarmonyLib.AccessTools.Field(se.GetType(), "m_fireDamagePerHit").GetValue(se);
            float left = (float)HarmonyLib.AccessTools.Field(se.GetType(), "m_fireDamageLeft").GetValue(se);
            string s = "burning True, per tick " + perHit.ToString("0.0") + ", left " + left.ToString("0.0");
            ModLog.Info("MobileCannonTestHooks: " + prefab + " " + s);
            return s;
        }
        /// <summary>The last cannon ball's flight report (CannonBallFlight): "flew D m, stopped at ...".</summary>
        [TestHook("GunPowdered.mobileCannonLastShot")]
        static string LastShot()
        {
            string s = "flew " + GunPowdered.Features.Cannon.Infrastructure.CannonBallFlight.LastDistance.ToString("0") + " m, " + GunPowdered.Features.Cannon.Infrastructure.CannonBallFlight.LastReport;
            ModLog.Info("MobileCannonTestHooks: " + s);
            return s;
        }

        static void StandAt(Player p, Vector3 at, Vector3 facing)
        {
            if (RuleLog.Due("MobileCannonTestHooks.StandAt", 2d)) ModLog.Every("MobileCannonTestHooks.StandAt", 2d, $"MobileCannonTestHooks.StandAt(p={p}, at={at}, facing={facing})");
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h;
            Rigidbody body = p.GetComponent<Rigidbody>();
            p.transform.position = at;
            p.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(facing, Vector3.up).normalized);
            if (body != null)
            {
                body.position = at;
                body.rotation = p.transform.rotation;
                body.linearVelocity = Vector3.zero;
            }
        }
        /// <summary>After a relog: takes the Mobile Cannon nearest to the local player (within 40 m) as the lab's cannon. Returns "found at D m" or "none".</summary>
        [TestHook("GunPowdered.mobileCannonFind")]
        static string Find()
        {
            if (RuleLog.Due("MobileCannonTestHooks.Find", 2d)) ModLog.Every("MobileCannonTestHooks.Find", 2d, $"MobileCannonTestHooks.Find()");
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.Due("MobileCannonTestHooks.Find:307", 2d)) ModLog.Every("MobileCannonTestHooks.Find:307", 2d, $"MobileCannonTestHooks.Find: returns \"no player\" because p == null (line 307)"); return "no player"; }
            GameObject best = null;
            float bestDist = 40f;
            foreach (MobileCannonGun gun in Object.FindObjectsByType<MobileCannonGun>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(gun.transform.position, p.transform.position);
                if (d < bestDist) { best = gun.gameObject; bestDist = d; }
            }
            _cannon = best;
            { var returnedAt316 = best == null ? "none" : "found at " + bestDist.ToString("0") + " m"; if (RuleLog.Due("MobileCannonTestHooks.Find:316", 2d)) ModLog.Every("MobileCannonTestHooks.Find:316", 2d, $"MobileCannonTestHooks.Find: returns {returnedAt316} (line 316)"); return returnedAt316; }
        }
    }
}
