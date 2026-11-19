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
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.MobileCannon) : null;
            Vagon vagon = prefab != null ? prefab.GetComponent<Vagon>() : null;
            if (vagon == null) return "no cart in " + GunPowderedPrefabs.MobileCannon;
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
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.MobileCannon) : null;
            if (p == null || prefab == null) return "no player or no prefab";
            Vector3 at = p.transform.position + p.transform.forward * (float)metres;
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h + 0.05f;
            _cannon = Object.Instantiate(prefab, at, Quaternion.LookRotation(-p.transform.forward));
            _placedAt = at;
            return "placed at " + at.ToString("0.0");
        }

        /// <summary>Puts the local player at the trail end, facing away from the cannon, and presses E on the cart (the game attaches next frame). Returns "grab asked".</summary>
        [TestHook("GunPowdered.mobileCannonGrab")]
        static string Grab()
        {
            Player p = Player.m_localPlayer;
            Vagon vagon = _cannon != null ? _cannon.GetComponent<Vagon>() : null;
            if (p == null || vagon == null) return "no player or no cannon";
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
            return "grab asked at " + p.transform.position.ToString("0.0");
        }

        /// <summary>True while the local player is hitched to the placed cannon.</summary>
        [TestHook("GunPowdered.mobileCannonAttached")]
        static bool Attached()
        {
            Vagon vagon = _cannon != null ? _cannon.GetComponent<Vagon>() : null;
            return vagon != null && Player.m_localPlayer != null && vagon.IsAttached(Player.m_localPlayer);
        }

        /// <summary>Walks the local player <paramref name="metres"/> straight on over <paramref name="seconds"/> (physics steps). Returns "walking".</summary>
        [TestHook("GunPowdered.mobileCannonWalk")]
        static string Walk(double metres, double seconds)
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            p.StartCoroutine(WalkRoutine(p, (float)metres, (float)seconds));
            return "walking " + metres.ToString("0.0") + " m in " + seconds.ToString("0.0") + " s";
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
            if (_cannon == null) return "no cannon";
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
            if (_cannon == null) return "nothing to remove";
            ZNetView view = _cannon.GetComponent<ZNetView>();
            if (view != null && view.IsValid()) view.Destroy(); else Object.Destroy(_cannon);
            _cannon = null;
            return "removed";
        }
        /// <summary>"max speed S m/s, stamina used U, facing A deg": the hitched local player's fastest speed and stamina since the hitch, and how far they look away from the cannon.</summary>
        [TestHook("GunPowdered.mobileCannonPullStats")]
        static string PullStats()
        {
            Player p = Player.m_localPlayer;
            MobileCannonPull pull = _cannon != null ? _cannon.GetComponent<MobileCannonPull>() : null;
            if (p == null || pull == null) return "no player or no cannon";
            float facing = Quaternion.Angle(Quaternion.LookRotation(Vector3.ProjectOnPlane(p.transform.forward, Vector3.up)), pull.FacingFor(p));
            string s = "max speed " + pull.MaxSpeedSeen.ToString("0.0") + " m/s, stamina used " + pull.StaminaUsed.ToString("0.0") + ", facing " + facing.ToString("0") + " deg, hands on handle " + pull.HandsOnHandle;
            ModLog.Info("MobileCannonTestHooks: " + s);
            return s;
        }
        /// <summary>Turns the placed cannon's crank by <paramref name="notches"/> wheel notches through the same RPC as the mouse wheel. Returns "turned N".</summary>
        [TestHook("GunPowdered.mobileCannonCrank")]
        static string Crank(int notches)
        {
            MobileCannonAim aim = _cannon != null ? _cannon.GetComponent<MobileCannonAim>() : null;
            if (aim == null) return "no cannon";
            return aim.Turn(notches) ? "turned " + notches : "not sent";
        }

        /// <summary>"pitch P deg, barrel B deg, hover H": the stored elevation, the barrel's real tilt (+ = muzzle up) and whether a crank handle with a hover text exists.</summary>
        [TestHook("GunPowdered.mobileCannonPitch")]
        static string PitchInfo()
        {
            MobileCannonAim aim = _cannon != null ? _cannon.GetComponent<MobileCannonAim>() : null;
            if (aim == null || aim.BarrelPart == null) return "no cannon";
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
            Player p = Player.m_localPlayer;
            MobileCannonGun gun = _cannon != null ? _cannon.GetComponent<MobileCannonGun>() : null;
            if (p == null || gun == null || gun.MuzzlePoint == null) return "no player or no cannon";
            Vector3 outward = Vector3.ProjectOnPlane(gun.MuzzlePoint.forward, Vector3.up).normalized;
            StandAt(p, gun.MuzzlePoint.position + outward * 1.2f, -outward);
            return gun.Load(p) ? "loading" : "refused";
        }

        /// <summary>"loaded L, lit F, loading G".</summary>
        [TestHook("GunPowdered.mobileCannonGunState")]
        static string GunState()
        {
            MobileCannonGun gun = _cannon != null ? _cannon.GetComponent<MobileCannonGun>() : null;
            return gun == null ? "no cannon" : "loaded " + gun.Loaded + ", lit " + gun.Lit + ", loading " + gun.Loading;
        }

        /// <summary>Stands the local player beside the touch hole and lights the fuse with the torch in hand. Returns "lit" or the reason.</summary>
        [TestHook("GunPowdered.mobileCannonLight")]
        static string Light()
        {
            Player p = Player.m_localPlayer;
            MobileCannonGun gun = _cannon != null ? _cannon.GetComponent<MobileCannonGun>() : null;
            if (p == null || gun == null || gun.TouchHolePoint == null) return "no player or no cannon";
            Vector3 side = _cannon.transform.right;
            StandAt(p, gun.TouchHolePoint.position + side * 1.3f, -side);
            string why = gun.Light(p);
            return why ?? "lit";
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
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            GameObject best = null;
            float bestDist = 40f;
            foreach (MobileCannonGun gun in Object.FindObjectsByType<MobileCannonGun>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(gun.transform.position, p.transform.position);
                if (d < bestDist) { best = gun.gameObject; bestDist = d; }
            }
            _cannon = best;
            return best == null ? "none" : "found at " + bestDist.ToString("0") + " m";
        }
    }
}
