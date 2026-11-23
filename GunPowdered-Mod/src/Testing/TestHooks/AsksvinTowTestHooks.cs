using System.Linq;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the Asksvin harness and towing (D-116). The harness goes on through the game's own Tameable.UseItem (our patch), hitching
    /// through the tow ring's E; the Asksvin moves by the game's own follow AI, so the tow joint is tested with a real moving animal.
    /// </summary>
    internal static class AsksvinTowTestHooks
    {
        static GameObject _animal;

        /// <summary>The lab's Asksvin (asksvinSpawn / asksvinFind), for the ground and obstacle hooks in TowGroundTestHooks.</summary>
        internal static GameObject Animal => _animal;

        /// <summary>Where the lab's cannon stood when a follow / drive started (the towing checks measure from it).</summary>
        internal static Vector3 CannonStart { get => _cannonStart; set => _cannonStart = value; }

        /// <summary>A second camera drawn over the game camera, looking at the Asksvin's saddle (shared name with the musket's free camera, so freeCamOff removes it). Returns its position.</summary>
        [TestHook("GunPowdered.freeCamOnAsksvin")]
        internal static string FreeCamOnAsksvin(double azimuth, double elevation, double metres)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.FreeCamOnAsksvin(azimuth={azimuth}, elevation={elevation}, metres={metres})");
            Camera main = Camera.main;
            if (_animal == null || main == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.FreeCamOnAsksvin: returns \"no Asksvin or main camera\" because _animal == null || main == null (line 29)"); return "no Asksvin or main camera"; }
            GameObject go = GameObject.Find("gp_freecam");
            if (go == null)
            {
                go = new GameObject("gp_freecam");
                go.AddComponent<Camera>();
            }
            HitchCamFollow still = go.GetComponent<HitchCamFollow>();
            if (still != null) still.enabled = false;                         // a still camera again (freeCamOnHitch turns the follower on after this)
            Camera cam = go.GetComponent<Camera>();
            cam.CopyFrom(main);
            cam.depth = main.depth + 1f;
            Vector3 centre = _animal.transform.position + Vector3.up * 1.5f;
            Vector3 dir = Quaternion.AngleAxis((float)azimuth, Vector3.up) * Vector3.ProjectOnPlane(_animal.transform.forward, Vector3.up).normalized;
            Vector3 offset = Quaternion.AngleAxis(-(float)elevation, Vector3.Cross(Vector3.up, dir)) * dir;
            go.transform.position = centre + offset * (float)metres;
            go.transform.LookAt(centre);
            { var returnedAt46 = "free camera at " + go.transform.position.ToString("0.0"); if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.FreeCamOnAsksvin: returns {returnedAt46} (line 46)"); return returnedAt46; }
        }

        /// <summary>Where the saddle really sits on the Asksvin, in the Asksvin's own space (x right, y up, z forward): centre, size and the pitch of its up axis, for every renderer of the saddle. T-009i fit check.</summary>
        [TestHook("GunPowdered.asksvinSaddleFit")]
        static string SaddleFit()
        {
            if (_animal == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.SaddleFit: returns \"no Asksvin\" because _animal == null (line 53)"); return "no Asksvin"; }
            var sb = new System.Text.StringBuilder();
            foreach (Renderer r in _animal.GetComponentsInChildren<Renderer>(true))
            {
                string path = r.transform.name + "/" + (r.transform.parent != null ? r.transform.parent.name : "");
                if (r is ParticleSystemRenderer || r.GetComponentsInParent<Transform>(true).Any(t => t.name == "gp_harness_model")) { ModLog.Verbose("AsksvinTowTestHooks.SaddleFit: skips renderer " + r.name + " (particles or the harness model)"); continue; }
                Bounds b = r.bounds;
                Vector3 c = _animal.transform.InverseTransformPoint(b.center);
                Vector3 up = _animal.transform.InverseTransformDirection(r.transform.up);
                Vector3 lp = _animal.transform.InverseTransformPoint(r.transform.position);
                sb.Append(path).Append(": bounds centre ").Append(c.ToString("0.00")).Append(" world size ").Append(b.size.ToString("0.00"))
                  .Append(", origin ").Append(lp.ToString("0.00")).Append(", up ").Append(up.ToString("0.00")).Append("; ");
            }
            SkinnedMeshRenderer saddle = null;
            foreach (SkinnedMeshRenderer s in _animal.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (s.name == "Sadel") saddle = s;
            if (saddle != null)
            {
                var baked = new Mesh();
                saddle.BakeMesh(baked, true);
                Matrix4x4 toAnimal = _animal.transform.worldToLocalMatrix * Matrix4x4.TRS(saddle.transform.position, saddle.transform.rotation, Vector3.one);
                Vector3 lo = new Vector3(9f, 9f, 9f), hi = -lo;
                float[] zs = { -1.25f, -1.1f, -0.9f, -0.6f, -0.3f, -0.1f };
                var xmax = new float[zs.Length];
                var ytop = new float[zs.Length];
                foreach (Vector3 v in baked.vertices)
                {
                    Vector3 p = toAnimal.MultiplyPoint3x4(v);
                    lo = Vector3.Min(lo, p);
                    hi = Vector3.Max(hi, p);
                    for (int i = 0; i < zs.Length; i++)
                        if (Mathf.Abs(p.z - zs[i]) < 0.06f) { xmax[i] = Mathf.Max(xmax[i], Mathf.Abs(p.x)); ytop[i] = Mathf.Max(ytop[i], p.y); }
                }
                sb.Append(" BAKED saddle in Asksvin space: min ").Append(lo.ToString("0.00")).Append(" max ").Append(hi.ToString("0.00")).Append("; half-width / top by z:");
                for (int i = 0; i < zs.Length; i++) sb.Append(" z ").Append(zs[i].ToString("0.00")).Append(" -> x ").Append(xmax[i].ToString("0.00")).Append(" y ").Append(ytop[i].ToString("0.00")).Append(";");
                Object.Destroy(baked);
            }
            string answer = sb.Length == 0 ? "no saddle renderer" : sb.ToString();
            ModLog.Info("AsksvinTowTestHooks: saddle fit - " + answer);
            return answer;
        }

        static Vector3 _cannonStart;

        /// <summary>Spawns a tame Asksvin (no saddle) in line behind the placed cannon's trail, its tow hook about 1 m from the tow ring, facing away. Returns "asksvin at ...".</summary>
        [TestHook("GunPowdered.asksvinSpawn")]
        static string Spawn()
        {
            if (RuleLog.Due("AsksvinTowTestHooks.Spawn", 2d)) ModLog.Every("AsksvinTowTestHooks.Spawn", 2d, $"AsksvinTowTestHooks.Spawn()");
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(AsksvinHarness.AnimalPrefab) : null;
            if (tow == null || tow.Ring == null || prefab == null) { if (RuleLog.Due("AsksvinTowTestHooks.Spawn:104", 2d)) ModLog.Every("AsksvinTowTestHooks.Spawn:104", 2d, $"AsksvinTowTestHooks.Spawn: returns \"no cannon or no Asksvin prefab\" because tow == null || tow.Ring == null || prefab == null (line 104)"); return "no cannon or no Asksvin prefab"; }
            Vector3 dir = Vector3.ProjectOnPlane(cannon.transform.forward, Vector3.up).normalized;
            Vector3 at = tow.Ring.position + dir * (-AsksvinHarness.HookLocal.z + 1.5f);      // hook 1.5 m from the ring (body 5 m, inside the 6 m hitch range; 2.5 put the body at 6.0 m: refused, a9dmg2)
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h + 0.1f;
            _driveTo = null;      // a failed slope test must not steer the next Asksvin
            _animal = Object.Instantiate(prefab, at, Quaternion.LookRotation(dir));
            Character c = _animal.GetComponent<Character>();
            if (c != null) c.SetTamed(true);
            _animal.AddComponent<AsksvinSpeedProbe>();
            { var returnedAt114 = "asksvin at " + at.ToString("0.0") + ", tamed " + (c != null && c.IsTamed()) + ", harness state " + (_animal.GetComponent<AsksvinHarness>() != null); if (RuleLog.Due("AsksvinTowTestHooks.Spawn:114", 2d)) ModLog.Every("AsksvinTowTestHooks.Spawn:114", 2d, $"AsksvinTowTestHooks.Spawn: returns {returnedAt114} (line 114)"); return returnedAt114; }
        }

        /// <summary>Puts the game's saddle on the spawned Asksvin (the owner's ZDO flag and the SetSaddle RPC, as Tameable.RPC_AddSaddle does). Returns "saddled True".</summary>
        [TestHook("GunPowdered.asksvinSaddle")]
        static string Saddle()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Saddle()");
            ZNetView nview = _animal != null ? _animal.GetComponent<ZNetView>() : null;
            if (nview == null || !nview.IsValid()) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Saddle: returns \"no asksvin\" because nview == null || !nview.IsValid() (line 123)"); return "no asksvin"; }
            nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, true);
            nview.InvokeRPC(ZNetView.Everybody, "SetSaddle", true);
            { var returnedAt126 = "saddled " + nview.GetZDO().GetBool(ZDOVars.s_haveSaddleHash); if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Saddle: returns {returnedAt126} (line 126)"); return returnedAt126; }
        }

        /// <summary>Uses the Asksvin Harness from the local player's inventory on the spawned Asksvin (Tameable.UseItem). Returns "harnessed H, harness items N".</summary>
        [TestHook("GunPowdered.asksvinHarnessOn")]
        static string HarnessOn()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.HarnessOn()");
            Player p = Player.m_localPlayer;
            Tameable t = _animal != null ? _animal.GetComponent<Tameable>() : null;
            if (p == null || t == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.HarnessOn: returns \"no player or no asksvin\" because p == null || t == null (line 136)"); return "no player or no asksvin"; }
            ItemDrop.ItemData item = null;
            foreach (ItemDrop.ItemData i in p.GetInventory().GetAllItems())
                if (i.m_dropPrefab != null && i.m_dropPrefab.name == GunPowderedPrefabs.AsksvinHarness) item = i;
            if (item == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.HarnessOn: returns \"no harness in the inventory\" because item == null (line 140)"); return "no harness in the inventory"; }
            t.UseItem(p, item);
            { var returnedAt142 = State(); if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.HarnessOn: returns {returnedAt142} (line 142)"); return returnedAt142; }
        }

        /// <summary>"harnessed H, harness items N".</summary>
        [TestHook("GunPowdered.asksvinHarnessState")]
        static string State()
        {
            if (RuleLog.Due("AsksvinTowTestHooks.State", 2d)) ModLog.Every("AsksvinTowTestHooks.State", 2d, $"AsksvinTowTestHooks.State()");
            AsksvinHarness h = _animal != null ? _animal.GetComponent<AsksvinHarness>() : null;
            int items = 0;
            if (Player.m_localPlayer != null)
                foreach (ItemDrop.ItemData i in Player.m_localPlayer.GetInventory().GetAllItems())
                    if (i.m_dropPrefab != null && i.m_dropPrefab.name == GunPowderedPrefabs.AsksvinHarness) items += i.m_stack;
            { var returnedAt155 = "harnessed " + (h != null && h.Harnessed) + ", harness items " + items; if (RuleLog.Due("AsksvinTowTestHooks.State:155", 2d)) ModLog.Every("AsksvinTowTestHooks.State:155", 2d, $"AsksvinTowTestHooks.State: returns {returnedAt155} (line 155)"); return returnedAt155; }
        }

        /// <summary>Presses E on the placed cannon's tow ring as the local player. Returns "hitch True/False".</summary>
        [TestHook("GunPowdered.mobileCannonHitch")]
        static string Hitch()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Hitch()");
            Player p = Player.m_localPlayer;
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            if (p == null || tow == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Hitch: returns \"no player or no cannon\" because p == null || tow == null (line 166)"); return "no player or no cannon"; }
            _cannonStart = cannon.transform.position;
            tow.ResetTiltStats();
            { var returnedAt169 = "hitch " + tow.Use(p); if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Hitch: returns {returnedAt169} (line 169)"); return returnedAt169; }
        }

        /// <summary>"towed T, joint J, model M, moved D m, max speed S m/s (asksvin A), gap G m, upright U".</summary>
        [TestHook("GunPowdered.mobileCannonTowState")]
        static string TowState()
        {
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            if (tow == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.TowState: returns \"no cannon\" because tow == null (line 178)"); return "no cannon"; }
            AsksvinHarness h = _animal != null ? _animal.GetComponent<AsksvinHarness>() : null;
            float moved = Vector3.Distance(Flat(cannon.transform.position), Flat(_cannonStart));
            string s = "towed " + tow.Towed + ", joint " + tow.Jointed + ", model " + (h != null && h.ModelShown) + ", moved " + moved.ToString("0.0") + " m, max speed "
                + tow.MaxSpeedSeen.ToString("0.0") + " m/s (asksvin " + tow.AnimalMaxSpeed.ToString("0.0") + "), gap " + (h != null && tow.Ring != null ? Vector3.Distance(tow.Ring.position, h.transform.TransformPoint(tow.HookNow)).ToString("0.00") : "?") + " m, upright " + (cannon.transform.up.y > 0.7f) + ", tilt max roll " + tow.MaxRollSeen.ToString("0") + " pitch " + tow.MaxPitchSeen.ToString("0") + " spin " + tow.MaxSpinSeen.ToString("0.0") + ", holds " + tow.Holds + ", flips " + tow.Flips + ", cannon at " + cannon.transform.position.ToString("0.0") + (_animal != null ? ", asksvin at " + _animal.transform.position.ToString("0.0") : "") + (Player.m_localPlayer != null ? ", player at " + Player.m_localPlayer.transform.position.ToString("0.0") : "");
            ModLog.Info("AsksvinTowTestHooks: " + s + "; " + tow.ClevisReport(_animal != null ? _animal.transform : null));
            return s;
        }

        /// <summary>Sends the spawned Asksvin after the local player, who stands <paramref name="metres"/> from it on the far side from the cannon: it walks/runs there towing the cannon. Returns "following".</summary>
        [TestHook("GunPowdered.asksvinFollow")]
        static string Follow(double metres)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Follow(metres={metres})");
            Player p = Player.m_localPlayer;
            MonsterAI ai = _animal != null ? _animal.GetComponent<MonsterAI>() : null;
            if (p == null || ai == null) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Follow: returns \"no player or no asksvin\" because p == null || ai == null (line 194)"); return "no player or no asksvin"; }
            GameObject cannon = MobileCannonTestHooks.Cannon;
            Vector3 away = cannon != null ? Vector3.ProjectOnPlane(_animal.transform.position - cannon.transform.position, Vector3.up).normalized : _animal.transform.forward;
            Vector3 at = _animal.transform.position + away * (float)metres;          // straight away from the cannon, so the animal pulls it, not runs through it
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h;
            Rigidbody body = p.GetComponent<Rigidbody>();
            p.transform.position = at;
            if (body != null)
            {
                body.position = at;
                body.linearVelocity = Vector3.zero;
            }
            if (cannon != null) _cannonStart = cannon.transform.position;           // after the hitch snap: only the towing counts
            ai.SetFollowTarget(p.gameObject);
            { var returnedAt209 = "following, player " + metres.ToString("0") + " m ahead"; if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Follow: returns {returnedAt209} (line 209)"); return returnedAt209; }
        }

        /// <summary>A sharp turn for the towing Asksvin: the player (its follow target) jumps <paramref name="metres"/> ahead of it, turned <paramref name="degrees"/> from its heading (+ right), and the animal runs there. Returns "turn D deg, player M m".</summary>
        [TestHook("GunPowdered.asksvinTurn")]
        static string Turn(double degrees, double metres)
        {
            Player p = Player.m_localPlayer;
            MonsterAI ai = _animal != null ? _animal.GetComponent<MonsterAI>() : null;
            if (p == null || ai == null) { if (RuleLog.Due("AsksvinTowTestHooks.Turn:218", 2d)) ModLog.Every("AsksvinTowTestHooks.Turn:218", 2d, $"AsksvinTowTestHooks.Turn: returns \"no player or no asksvin\" because p == null || ai == null (line 218)"); return "no player or no asksvin"; }
            Vector3 dir = Quaternion.Euler(0f, (float)degrees, 0f) * Vector3.ProjectOnPlane(_animal.transform.forward, Vector3.up).normalized;
            Vector3 at = _animal.transform.position + dir * (float)metres;
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h;
            Rigidbody body = p.GetComponent<Rigidbody>();
            p.transform.position = at;
            if (body != null) { body.position = at; body.linearVelocity = Vector3.zero; }
            ai.SetFollowTarget(p.gameObject);
            ModLog.Info("AsksvinTowTestHooks: turn " + degrees.ToString("0") + " deg, player " + metres.ToString("0") + " m ahead of the asksvin");
            return "turn " + degrees.ToString("0") + " deg, player " + metres.ToString("0") + " m";
        }

        /// <summary>Removes the spawned Asksvin (and its harness flag with it). Returns "removed".</summary>
        [TestHook("GunPowdered.asksvinRemove")]
        static string Remove()
        {
            if (RuleLog.Due("AsksvinTowTestHooks.Remove", 2d)) ModLog.Every("AsksvinTowTestHooks.Remove", 2d, $"AsksvinTowTestHooks.Remove()");
            if (_animal == null) { if (RuleLog.Due("AsksvinTowTestHooks.Remove:236", 2d)) ModLog.Every("AsksvinTowTestHooks.Remove:236", 2d, $"AsksvinTowTestHooks.Remove: returns \"nothing to remove\" because _animal == null (line 236)"); return "nothing to remove"; }
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
            if (owner == 0L) { if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinTowTestHooks.Who: returns \"none\" because owner == 0L (line 352)"); return "none"; }
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

    /// <summary>Test only: the top horizontal speed of the lab's Asksvin.</summary>
    internal sealed class AsksvinSpeedProbe : MonoBehaviour
    {
        internal float Top;
        Rigidbody _body;

        void Awake() => _body = GetComponent<Rigidbody>();

        void FixedUpdate()
        {
            if (_body != null) Top = Mathf.Max(Top, Vector3.ProjectOnPlane(_body.linearVelocity, Vector3.up).magnitude);
            ModLog.Every("asksvin-probe-" + GetInstanceID(), 10d, "AsksvinSpeedProbe: top " + Top.ToString("0.0") + " m/s");
        }
    }
}
