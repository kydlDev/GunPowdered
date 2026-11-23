using System.Linq;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>Asksvin tow test hooks, second half (G04 split).</summary>
    internal static partial class AsksvinTowTestHooks
    {
        /// <summary>Removes the spawned Asksvin (and its harness flag with it). Returns "removed".</summary>
        [TestHook("GunPowdered.asksvinRemove")]
        static string Remove()
        {
            if (RuleLog.Due("AsksvinTowTestHooks.Remove", 2d)) ModLog.Every("AsksvinTowTestHooks.Remove", 2d, $"AsksvinTowTestHooks.Remove()");
            if (_animal == null) return "nothing to remove";
            ZNetView nview = _animal.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid()) nview.Destroy();
            else Object.Destroy(_animal);
            _animal = null;
            _driveTo = null;
            { if (RuleLog.Due("AsksvinTowTestHooks.Remove:242", 2d)) ModLog.Every("AsksvinTowTestHooks.Remove:242", 2d, $"AsksvinTowTestHooks.Remove: returns \"removed\" (line 242)"); return "removed"; }
        }

        /// <summary>Puts the local player beside the spawned Asksvin and mounts it through the game's saddle (Sadle.Interact). Returns "riding True/False".</summary>
        [TestHook("GunPowdered.asksvinMount")]
        static string Mount()
        {
            if (RuleLog.Due("AsksvinTowTestHooks.Mount", 2d)) ModLog.Every("AsksvinTowTestHooks.Mount", 2d, $"AsksvinTowTestHooks.Mount()");
            Player p = Player.m_localPlayer;
            Sadle sadle = _animal != null ? _animal.GetComponentInChildren<Sadle>(true) : null;
            if (p == null || sadle == null) { if (RuleLog.Due("AsksvinTowTestHooks.Mount:252", 2d)) ModLog.Every("AsksvinTowTestHooks.Mount:252", 2d, $"AsksvinTowTestHooks.Mount: returns \"no player or no saddle\" because p == null || sadle == null (line 252)"); return "no player or no saddle"; }
            Vector3 at = _animal.transform.position + _animal.transform.right * 1.8f;
            Rigidbody body = p.GetComponent<Rigidbody>();
            p.transform.position = at;
            if (body != null)
            {
                body.position = at;
                body.linearVelocity = Vector3.zero;
            }
            sadle.Interact(p, false, false);
            { var returnedAt262 = "riding " + p.IsRiding(); if (RuleLog.Due("AsksvinTowTestHooks.Mount:262", 2d)) ModLog.Every("AsksvinTowTestHooks.Mount:262", 2d, $"AsksvinTowTestHooks.Mount: returns {returnedAt262} (line 262)"); return returnedAt262; }
        }

        /// <summary>
        /// Gives the ridden Asksvin the rider's controls through the game's own Sadle.ApplyControlls, straight away from the cannon: "run" (the saddle keeps
        /// running until the animal's stamina is gone) or "stop". Returns the control sent.
        /// </summary>
        [TestHook("GunPowdered.asksvinRide")]
        static string Ride(string how)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Ride(how={how})");
            Player p = Player.m_localPlayer;
            Sadle sadle = _animal != null ? _animal.GetComponentInChildren<Sadle>(true) : null;
            GameObject cannon = MobileCannonTestHooks.Cannon;
            if (p == null || sadle == null || cannon == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Ride: returns \"no player, saddle or cannon\" because p == null || sadle == null || cannon == null (line 276)"); return "no player, saddle or cannon"; }
            Vector3 away = Vector3.ProjectOnPlane(_animal.transform.position - cannon.transform.position, Vector3.up).normalized;
            bool run = how == "run", walk = how == "walk";                    // walk: D-144 tests line the cannon up behind the Asksvin before backing
            if (run) _cannonStart = cannon.transform.position;
            AsksvinSpeedProbe probe = _animal.GetComponent<AsksvinSpeedProbe>();
            if (probe != null && run) probe.Top = 0f;
            sadle.ApplyControlls(run || walk ? Vector3.forward : Vector3.back, away, run, false, false);
            { var returnedAt283 = how + " sent, riding " + p.IsRiding(); if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Ride: returns {returnedAt283} (line 283)"); return returnedAt283; }
        }

        /// <summary>Gets the local player off the Asksvin. Returns "riding False".</summary>
        [TestHook("GunPowdered.asksvinDismount")]
        static string Dismount()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Dismount()");
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Dismount: returns \"no player\" because p == null (line 292)"); return "no player"; }
            p.StopDoodadControl();      // the game's own way off a saddle (it also ends the attach)
            p.AttachStop();
            { var returnedAt295 = "riding " + p.IsRiding(); if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Dismount: returns {returnedAt295} (line 295)"); return returnedAt295; }
        }

        /// <summary>The spawned Asksvin's top speed since the last run order (m/s, horizontal): "asksvin top S m/s".</summary>
        [TestHook("GunPowdered.asksvinTopSpeed")]
        static string TopSpeed()
        {
            AsksvinSpeedProbe probe = _animal != null ? _animal.GetComponent<AsksvinSpeedProbe>() : null;
            string s = probe == null ? "no asksvin" : "asksvin top " + probe.Top.ToString("0.0") + " m/s";
            ModLog.Info("AsksvinTowTestHooks: " + s);
            return s;
        }

        /// <summary>After a relog: takes the Asksvin nearest to the local player (within 40 m) as the lab's Asksvin. Returns "found at D m" or "none".</summary>
        [TestHook("GunPowdered.asksvinFind")]
        static string Find()
        {
            if (RuleLog.Due("AsksvinTowTestHooks.Find", 2d)) ModLog.Every("AsksvinTowTestHooks.Find", 2d, $"AsksvinTowTestHooks.Find()");
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.Due("AsksvinTowTestHooks.Find:314", 2d)) ModLog.Every("AsksvinTowTestHooks.Find:314", 2d, $"AsksvinTowTestHooks.Find: returns \"no player\" because p == null (line 314)"); return "no player"; }
            GameObject best = null;
            float bestDist = 40f;
            foreach (AsksvinHarness h in Object.FindObjectsByType<AsksvinHarness>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(h.transform.position, p.transform.position);
                if (d < bestDist) { best = h.gameObject; bestDist = d; }
            }
            _driveTo = null;
            _animal = best;
            { var returnedAt324 = best == null ? "none" : "found at " + bestDist.ToString("0") + " m"; if (RuleLog.Due("AsksvinTowTestHooks.Find:324", 2d)) ModLog.Every("AsksvinTowTestHooks.Find:324", 2d, $"AsksvinTowTestHooks.Find: returns {returnedAt324} (line 324)"); return returnedAt324; }
        }

        /// <summary>
        /// Two-client diagnostics: "cannon owner C, animal owner A, me M, cannon is X, animal is Y" (X/Y = mine / Bjornita's / other / none) plus every peer's
        /// uid and reference position and its distance to the animal (the server takes back objects whose owner's reference position is not near them). Logged.
        /// </summary>
        [TestHook("GunPowdered.mobileCannonOwners")]
        static string Owners()
        {
            GameObject cannon = MobileCannonTestHooks.Cannon;
            ZNetView cv = cannon != null ? cannon.GetComponent<ZNetView>() : null;
            ZNetView av = _animal != null ? _animal.GetComponent<ZNetView>() : null;
            long me = ZDOMan.GetSessionID();
            long c = cv != null && cv.IsValid() ? cv.GetZDO().GetOwner() : 0L, a = av != null && av.IsValid() ? av.GetZDO().GetOwner() : 0L;
            string s = "cannon owner " + c + ", animal owner " + a + ", me " + me + ", cannon is " + Who(c, me) + ", animal is " + Who(a, me);
            if (ZNet.instance != null)
                foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                    s += "; peer " + peer.m_playerName + " uid " + peer.m_uid + " ref " + peer.GetRefPos().ToString("0") + (_animal != null ? " (" + Vector3.Distance(peer.GetRefPos(), _animal.transform.position).ToString("0") + " m from the animal)" : "");
            AsksvinHarness h = _animal != null ? _animal.GetComponent<AsksvinHarness>() : null;
            if (h != null) s += "; " + h.SkinReport();
            ModLog.Info("AsksvinTowTestHooks: " + s);
            return s;
        }

        static string Who(long owner, long me)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Who(owner={owner}, me={me})");
            if (owner == 0L) return "none";
            if (owner == me) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Who: returns \"mine\" because owner == me (line 353)"); return "mine"; }
            if (ZNet.instance != null)
                foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                    if (peer.m_uid == owner) { var returnedAt356 = peer.m_playerName.Contains("Player2") || peer.m_playerName.Contains("Bjornita") ? "Bjornita's" : peer.m_playerName + "'s"; if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Who: returns {returnedAt356} because peer.m_uid == owner (line 356)"); return returnedAt356; }
            { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Who: returns \"other\" (line 357)"); return "other"; }
        }

        /// <summary>Sends the towing Asksvin to a world point (x, z) on the ground: the player, its follow target, goes there. "top" / "foot" variants are the cliff's.
        /// Returns "going to (x, z), ground y".</summary>
        [TestHook("GunPowdered.asksvinFollowTo")]
        static string FollowTo(double x, double z)
        {
            Player p = Player.m_localPlayer;
            MonsterAI ai = _animal != null ? _animal.GetComponent<MonsterAI>() : null;
            if (p == null || ai == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.FollowTo: returns \"no player or no asksvin\" because p == null || ai == null (line 367)"); return "no player or no asksvin"; }
            Vector3 at = new Vector3((float)x, 0f, (float)z);
            at.y = TowGroundTestHooks.GroundY(new Vector3(at.x, TowGroundTestHooks.MountainCliffTop.y + 5f, at.z));
            Rigidbody body = p.GetComponent<Rigidbody>();
            p.transform.position = at;
            if (body != null) { body.position = at; body.linearVelocity = Vector3.zero; }
            GameObject cannon = MobileCannonTestHooks.Cannon;
            if (cannon != null) _cannonStart = cannon.transform.position;
            ai.SetFollowTarget(p.gameObject);
            string s = "going to (" + x.ToString("0") + ", " + z.ToString("0") + "), ground y " + at.y.ToString("0.0");
            ModLog.Info("AsksvinTowTestHooks: " + s + ", asksvin at " + _animal.transform.position.ToString("0.0"));
            return s;
        }
        static Vector3? _driveTo;

        /// <summary>
        /// Like asksvinFollowTo, but the Asksvin runs a straight line to (x, z) instead of asking the pathfinder: on the template's 39 degree cliff the
        /// pathfinder found no way at all in two of three runs (it stood still) and a long diagonal in the third (a9cliffrep, a9g16g). A rider steers
        /// straight too. Test-only: the MonsterAI patch (Integrations/Harmony/AsksvinTestDrivePatch) is applied on the first call and does nothing without a drive. Returns "driving to (x, z), ground y".
        /// </summary>
        [TestHook("GunPowdered.asksvinDriveTo")]
        static string DriveTo(double x, double z)
        {
            string s = FollowTo(x, z);
            if (s.StartsWith("no ")) { var returnedAt391 = s; if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.DriveTo: returns {returnedAt391} because s.StartsWith(\"no \") (line 391)"); return returnedAt391; }
            GunPowdered.Integrations.HarmonyPatches.AsksvinTestDrivePatch.Apply(DriveTarget);
            _driveTo = Player.m_localPlayer.transform.position;
            ModLog.Info("AsksvinTowTestHooks: straight drive to " + _driveTo.Value.ToString("0.0"));
            return s.Replace("going to", "driving to");
        }

        /// <summary>Ends a drive (cleanArena-safe; the patch stays but idles).</summary>
        [TestHook("GunPowdered.asksvinDriveStop")]
        static string DriveStop() { _driveTo = null; return "drive stopped"; }

        /// <summary>Where the drive steers this creature: the drive target for the lab's Asksvin during a drive, else null (the game's AI runs).</summary>
        static Vector3? DriveTarget(MonsterAI ai) => _driveTo != null && _animal != null && ai != null && ai.gameObject == _animal ? _driveTo : null;

        /// <summary>Tames the Asksvin that asksvinFind took (one the lab spawned, so that it can be a lab target for the second client). Returns "tamed True".</summary>
        [TestHook("GunPowdered.asksvinTame")]
        static string Tame()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Tame()");
            Character c = _animal != null ? _animal.GetComponent<Character>() : null;
            if (c == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Tame: returns \"no asksvin\" because c == null (line 411)"); return "no asksvin"; }
            c.SetTamed(true);
            { var returnedAt413 = "tamed " + c.IsTamed(); if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Tame: returns {returnedAt413} (line 413)"); return returnedAt413; }
        }

        internal static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
