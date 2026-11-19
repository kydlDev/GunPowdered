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

        /// <summary>A second camera drawn over the game camera, looking at the Asksvin's saddle (shared name with the musket's free camera, so freeCamOff removes it). Returns its position.</summary>
        [TestHook("GunPowdered.freeCamOnAsksvin")]
        static string FreeCamOnAsksvin(double azimuth, double elevation, double metres)
        {
            Camera main = Camera.main;
            if (_animal == null || main == null) return "no Asksvin or main camera";
            GameObject go = GameObject.Find("gp_freecam");
            if (go == null)
            {
                go = new GameObject("gp_freecam");
                go.AddComponent<Camera>();
            }
            Camera cam = go.GetComponent<Camera>();
            cam.CopyFrom(main);
            cam.depth = main.depth + 1f;
            Vector3 centre = _animal.transform.position + Vector3.up * 1.5f;
            Vector3 dir = Quaternion.AngleAxis((float)azimuth, Vector3.up) * Vector3.ProjectOnPlane(_animal.transform.forward, Vector3.up).normalized;
            Vector3 offset = Quaternion.AngleAxis(-(float)elevation, Vector3.Cross(Vector3.up, dir)) * dir;
            go.transform.position = centre + offset * (float)metres;
            go.transform.LookAt(centre);
            return "free camera at " + go.transform.position.ToString("0.0");
        }

        /// <summary>Where the saddle really sits on the Asksvin, in the Asksvin's own space (x right, y up, z forward): centre, size and the pitch of its up axis, for every renderer of the saddle. T-009i fit check.</summary>
        [TestHook("GunPowdered.asksvinSaddleFit")]
        static string SaddleFit()
        {
            if (_animal == null) return "no Asksvin";
            var sb = new System.Text.StringBuilder();
            foreach (Renderer r in _animal.GetComponentsInChildren<Renderer>(true))
            {
                string path = r.transform.name + "/" + (r.transform.parent != null ? r.transform.parent.name : "");
                if (r is ParticleSystemRenderer || r.GetComponentsInParent<Transform>(true).Any(t => t.name == "gp_harness_model")) continue;
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
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(AsksvinHarness.AnimalPrefab) : null;
            if (tow == null || tow.Ring == null || prefab == null) return "no cannon or no Asksvin prefab";
            Vector3 dir = Vector3.ProjectOnPlane(cannon.transform.forward, Vector3.up).normalized;
            Vector3 at = tow.Ring.position + dir * (-AsksvinHarness.HookLocal.z + 1f);
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h + 0.1f;
            _animal = Object.Instantiate(prefab, at, Quaternion.LookRotation(dir));
            Character c = _animal.GetComponent<Character>();
            if (c != null) c.SetTamed(true);
            _animal.AddComponent<AsksvinSpeedProbe>();
            return "asksvin at " + at.ToString("0.0") + ", tamed " + (c != null && c.IsTamed()) + ", harness state " + (_animal.GetComponent<AsksvinHarness>() != null);
        }

        /// <summary>Puts the game's saddle on the spawned Asksvin (the owner's ZDO flag and the SetSaddle RPC, as Tameable.RPC_AddSaddle does). Returns "saddled True".</summary>
        [TestHook("GunPowdered.asksvinSaddle")]
        static string Saddle()
        {
            ZNetView nview = _animal != null ? _animal.GetComponent<ZNetView>() : null;
            if (nview == null || !nview.IsValid()) return "no asksvin";
            nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, true);
            nview.InvokeRPC(ZNetView.Everybody, "SetSaddle", true);
            return "saddled " + nview.GetZDO().GetBool(ZDOVars.s_haveSaddleHash);
        }

        /// <summary>Uses the Asksvin Harness from the local player's inventory on the spawned Asksvin (Tameable.UseItem). Returns "harnessed H, harness items N".</summary>
        [TestHook("GunPowdered.asksvinHarnessOn")]
        static string HarnessOn()
        {
            Player p = Player.m_localPlayer;
            Tameable t = _animal != null ? _animal.GetComponent<Tameable>() : null;
            if (p == null || t == null) return "no player or no asksvin";
            ItemDrop.ItemData item = null;
            foreach (ItemDrop.ItemData i in p.GetInventory().GetAllItems())
                if (i.m_dropPrefab != null && i.m_dropPrefab.name == GunPowderedPrefabs.AsksvinHarness) item = i;
            if (item == null) return "no harness in the inventory";
            t.UseItem(p, item);
            return State();
        }

        /// <summary>"harnessed H, harness items N".</summary>
        [TestHook("GunPowdered.asksvinHarnessState")]
        static string State()
        {
            AsksvinHarness h = _animal != null ? _animal.GetComponent<AsksvinHarness>() : null;
            int items = 0;
            if (Player.m_localPlayer != null)
                foreach (ItemDrop.ItemData i in Player.m_localPlayer.GetInventory().GetAllItems())
                    if (i.m_dropPrefab != null && i.m_dropPrefab.name == GunPowderedPrefabs.AsksvinHarness) items += i.m_stack;
            return "harnessed " + (h != null && h.Harnessed) + ", harness items " + items;
        }

        /// <summary>Presses E on the placed cannon's tow ring as the local player. Returns "hitch True/False".</summary>
        [TestHook("GunPowdered.mobileCannonHitch")]
        static string Hitch()
        {
            Player p = Player.m_localPlayer;
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            if (p == null || tow == null) return "no player or no cannon";
            _cannonStart = cannon.transform.position;
            tow.ResetTiltStats();
            return "hitch " + tow.Use(p);
        }

        /// <summary>"towed T, joint J, model M, moved D m, max speed S m/s (asksvin A), gap G m, upright U".</summary>
        [TestHook("GunPowdered.mobileCannonTowState")]
        static string TowState()
        {
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            if (tow == null) return "no cannon";
            AsksvinHarness h = _animal != null ? _animal.GetComponent<AsksvinHarness>() : null;
            float moved = Vector3.Distance(Flat(cannon.transform.position), Flat(_cannonStart));
            string s = "towed " + tow.Towed + ", joint " + tow.Jointed + ", model " + (h != null && h.ModelShown) + ", moved " + moved.ToString("0.0") + " m, max speed "
                + tow.MaxSpeedSeen.ToString("0.0") + " m/s (asksvin " + tow.AnimalMaxSpeed.ToString("0.0") + "), gap " + (h != null && tow.Ring != null ? Vector3.Distance(tow.Ring.position, h.transform.TransformPoint(AsksvinHarness.HookLocal)).ToString("0.00") : "?") + " m, upright " + (cannon.transform.up.y > 0.7f) + ", tilt max roll " + tow.MaxRollSeen.ToString("0") + " pitch " + tow.MaxPitchSeen.ToString("0") + " spin " + tow.MaxSpinSeen.ToString("0.0") + ", holds " + tow.Holds + ", flips " + tow.Flips + ", cannon at " + cannon.transform.position.ToString("0.0") + (_animal != null ? ", asksvin at " + _animal.transform.position.ToString("0.0") : "") + (Player.m_localPlayer != null ? ", player at " + Player.m_localPlayer.transform.position.ToString("0.0") : "");
            ModLog.Info("AsksvinTowTestHooks: " + s + "; " + tow.ClevisReport(_animal != null ? _animal.transform : null));
            return s;
        }

        /// <summary>Sends the spawned Asksvin after the local player, who stands <paramref name="metres"/> from it on the far side from the cannon: it walks/runs there towing the cannon. Returns "following".</summary>
        [TestHook("GunPowdered.asksvinFollow")]
        static string Follow(double metres)
        {
            Player p = Player.m_localPlayer;
            MonsterAI ai = _animal != null ? _animal.GetComponent<MonsterAI>() : null;
            if (p == null || ai == null) return "no player or no asksvin";
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
            return "following, player " + metres.ToString("0") + " m ahead";
        }

        /// <summary>A sharp turn for the towing Asksvin: the player (its follow target) jumps <paramref name="metres"/> ahead of it, turned <paramref name="degrees"/> from its heading (+ right), and the animal runs there. Returns "turn D deg, player M m".</summary>
        [TestHook("GunPowdered.asksvinTurn")]
        static string Turn(double degrees, double metres)
        {
            Player p = Player.m_localPlayer;
            MonsterAI ai = _animal != null ? _animal.GetComponent<MonsterAI>() : null;
            if (p == null || ai == null) return "no player or no asksvin";
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
            if (_animal == null) return "nothing to remove";
            ZNetView nview = _animal.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid()) nview.Destroy();
            else Object.Destroy(_animal);
            _animal = null;
            return "removed";
        }

        /// <summary>Puts the local player beside the spawned Asksvin and mounts it through the game's saddle (Sadle.Interact). Returns "riding True/False".</summary>
        [TestHook("GunPowdered.asksvinMount")]
        static string Mount()
        {
            Player p = Player.m_localPlayer;
            Sadle sadle = _animal != null ? _animal.GetComponentInChildren<Sadle>(true) : null;
            if (p == null || sadle == null) return "no player or no saddle";
            Vector3 at = _animal.transform.position + _animal.transform.right * 1.8f;
            Rigidbody body = p.GetComponent<Rigidbody>();
            p.transform.position = at;
            if (body != null)
            {
                body.position = at;
                body.linearVelocity = Vector3.zero;
            }
            sadle.Interact(p, false, false);
            return "riding " + p.IsRiding();
        }

        /// <summary>
        /// Gives the ridden Asksvin the rider's controls through the game's own Sadle.ApplyControlls, straight away from the cannon: "run" (the saddle keeps
        /// running until the animal's stamina is gone) or "stop". Returns the control sent.
        /// </summary>
        [TestHook("GunPowdered.asksvinRide")]
        static string Ride(string how)
        {
            Player p = Player.m_localPlayer;
            Sadle sadle = _animal != null ? _animal.GetComponentInChildren<Sadle>(true) : null;
            GameObject cannon = MobileCannonTestHooks.Cannon;
            if (p == null || sadle == null || cannon == null) return "no player, saddle or cannon";
            Vector3 away = Vector3.ProjectOnPlane(_animal.transform.position - cannon.transform.position, Vector3.up).normalized;
            bool run = how == "run";
            if (run) _cannonStart = cannon.transform.position;
            AsksvinSpeedProbe probe = _animal.GetComponent<AsksvinSpeedProbe>();
            if (probe != null && run) probe.Top = 0f;
            sadle.ApplyControlls(run ? Vector3.forward : Vector3.back, away, run, false, false);
            return how + " sent, riding " + p.IsRiding();
        }

        /// <summary>Gets the local player off the Asksvin. Returns "riding False".</summary>
        [TestHook("GunPowdered.asksvinDismount")]
        static string Dismount()
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            p.StopDoodadControl();      // the game's own way off a saddle (it also ends the attach)
            p.AttachStop();
            return "riding " + p.IsRiding();
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
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            GameObject best = null;
            float bestDist = 40f;
            foreach (AsksvinHarness h in Object.FindObjectsByType<AsksvinHarness>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(h.transform.position, p.transform.position);
                if (d < bestDist) { best = h.gameObject; bestDist = d; }
            }
            _animal = best;
            return best == null ? "none" : "found at " + bestDist.ToString("0") + " m";
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
            if (owner == 0L) return "none";
            if (owner == me) return "mine";
            if (ZNet.instance != null)
                foreach (ZNetPeer peer in ZNet.instance.GetPeers())
                    if (peer.m_uid == owner) return peer.m_playerName.Contains("Player2") || peer.m_playerName.Contains("Bjornita") ? "Bjornita's" : peer.m_playerName + "'s";
            return "other";
        }

        // ---- T-009 missing cases: saddle off, death, joint break, slopes, trees ----

        /// <summary>Takes the saddle off the lab's Asksvin with the game's own DropSaddle (as the owner's "remove saddle"). Returns "saddle dropped True/False".</summary>
        [TestHook("GunPowdered.asksvinDropSaddle")]
        static string DropSaddle()
        {
            Tameable t = _animal != null ? _animal.GetComponent<Tameable>() : null;
            if (t == null || Player.m_localPlayer == null) return "no asksvin";
            _lastAnimalPos = t.transform.position;
            var m = HarmonyLib.AccessTools.Method(typeof(Tameable), "DropSaddle");
            object r = m.GetParameters().Length == 1 ? m.Invoke(t, new object[] { Player.m_localPlayer.transform.position }) : m.Invoke(t, new object[0]);
            ModLog.Info("AsksvinTowTestHooks: saddle dropped " + r);
            return "saddle dropped " + r;
        }

        /// <summary>Kills the lab's Asksvin (a hit with no attacker, so no tame protection is in the way). Returns "dead True".</summary>
        [TestHook("GunPowdered.asksvinKill")]
        static string Kill()
        {
            Character c = _animal != null ? _animal.GetComponent<Character>() : null;
            if (c == null) return "no asksvin";
            _lastAnimalPos = c.transform.position;
            var hit = new HitData { m_point = c.transform.position, m_dir = Vector3.down };
            hit.m_damage.m_blunt = c.GetHealth() * 10f + 10000f;
            c.Damage(hit);
            ModLog.Info("AsksvinTowTestHooks: asksvin hit for " + hit.m_damage.m_blunt.ToString("0") + ", dead " + c.IsDead());
            return "killed";
        }

        /// <summary>Asksvin Harness items lying within 8 m of where the lab's Asksvin is or was. Returns "harness items on the ground N".</summary>
        [TestHook("GunPowdered.harnessItemsNear")]
        static string HarnessItemsNear()
        {
            Vector3 at = _animal != null ? _animal.transform.position : _lastAnimalPos;
            int n = 0;
            foreach (ItemDrop d in Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None))
                if (d != null && Utils.GetPrefabName(d.gameObject) == GunPowderedPrefabs.AsksvinHarness && Vector3.Distance(d.transform.position, at) < 8f) n += d.m_itemData.m_stack;
            ModLog.Info("AsksvinTowTestHooks: harness items on the ground " + n);
            return "harness items on the ground " + n;
        }

        static Vector3 _lastAnimalPos;

        /// <summary>Sets the live tow joint's break force (N), so the next pull breaks it. Returns "break force N" or why not.</summary>
        [TestHook("GunPowdered.mobileCannonBreakForce")]
        static string BreakForce(double newtons)
        {
            GameObject cannon = MobileCannonTestHooks.Cannon;
            ConfigurableJoint j = cannon != null ? cannon.GetComponent<ConfigurableJoint>() : null;
            if (j == null) return "no tow joint";
            j.breakForce = (float)newtons;
            if (_animal != null) _lastAnimalPos = _animal.transform.position;
            return "break force " + newtons.ToString("0");
        }

        /// <summary>
        /// Sends the towing Asksvin onto the steepest ground near it: the player (its follow target) goes <paramref name="metres"/> away, uphill
        /// (<paramref name="mode"/> "up") or along the slope ("across", the side-slope case that tips carts). Returns "slope mode, rise R m over M m (G deg)".
        /// </summary>
        [TestHook("GunPowdered.asksvinFollowSlope")]
        static string FollowSlope(string mode, double metres)
        {
            Player p = Player.m_localPlayer;
            MonsterAI ai = _animal != null ? _animal.GetComponent<MonsterAI>() : null;
            if (p == null || ai == null || ZoneSystem.instance == null) return "no player or no asksvin";
            Vector3 from = _animal.transform.position;
            float h0, best = -1f;
            h0 = GroundY(from);
            Vector3 up = Vector3.forward, at;
            if (mode == "across")
            {
                // the local fall line from height differences 4 m around the animal; run along the contour (90 degrees to it)
                float hx1, hx0, hz1, hz0;
                hx1 = GroundY(from + Vector3.right * 4f); hx0 = GroundY(from - Vector3.right * 4f);
                hz1 = GroundY(from + Vector3.forward * 4f); hz0 = GroundY(from - Vector3.forward * 4f);
                Vector3 grad = new Vector3(hx1 - hx0, 0f, hz1 - hz0);
                best = grad.magnitude / 8f * 12f;
                up = grad.sqrMagnitude > 0.0001f ? grad.normalized : Vector3.forward;
                Vector3 along = Vector3.Cross(Vector3.up, up).normalized;
                if (Vector3.Dot(along, _animal.transform.forward) < 0f) along = -along;     // the contour way the animal already faces, not back past the cannon (a9slope5)
                at = from + along * (float)metres;
            }
            else
            {
                // the highest ground within 2x metres (the arena itself is flat): the animal runs there, uphill
                at = from + Vector3.forward * (float)metres;
                for (float r = 10f; r <= 2f * (float)metres; r += 5f)
                    for (int i = 0; i < 24; i++)
                    {
                        Vector3 d = Quaternion.Euler(0f, i * 15f, 0f) * Vector3.forward;
                        float h;
                        h = GroundY(from + d * r); if ((h - h0) / r * 12f > best) { best = (h - h0) / r * 12f; up = d; at = from + d * r; }
                    }
            }
            at.y = GroundY(at);
            Rigidbody body = p.GetComponent<Rigidbody>();
            p.transform.position = at;
            if (body != null) { body.position = at; body.linearVelocity = Vector3.zero; }
            GameObject cannon = MobileCannonTestHooks.Cannon;
            if (cannon != null) _cannonStart = cannon.transform.position;
            ai.SetFollowTarget(p.gameObject);
            string s = "slope " + mode + ", rise " + (at.y - h0).ToString("0.0") + " m over " + Vector3.Distance(Flat(at), Flat(from)).ToString("0") + " m, gradient as rise per 12 m " + best.ToString("0.0") + " m ("
                + (Mathf.Atan2(best, 12f) * Mathf.Rad2Deg).ToString("0") + " deg), asksvin " + from.ToString("0.0") + " facing " + _animal.transform.forward.ToString("0.0") + " -> player " + at.ToString("0.0");
            ModLog.Info("AsksvinTowTestHooks: " + s);
            return s;
        }

        /// <summary>The real (loaded) ground slope under the player, from heights 4 m around: "ground slope S deg". For checking goToSlope after the zone loaded.</summary>
        [TestHook("GunPowdered.groundSlopeHere")]
        static string GroundSlopeHere()
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            Vector3 c = p.transform.position;
            float dx = GroundY(c + Vector3.right * 4f) - GroundY(c - Vector3.right * 4f), dz = GroundY(c + Vector3.forward * 4f) - GroundY(c - Vector3.forward * 4f);
            float deg = Mathf.Atan(new Vector2(dx, dz).magnitude / 8f) * Mathf.Rad2Deg;
            ModLog.Info("AsksvinTowTestHooks: ground slope " + deg.ToString("0") + " deg at " + c.ToString("0"));
            return "ground slope " + deg.ToString("0") + " deg";
        }

        /// <summary>Diagnostics: ground heights in rings round the player by three methods (ZoneSystem.GetGroundHeight, a terrain raycast, the world generator). Logged; returns the summary.</summary>
        [TestHook("GunPowdered.groundProfile")]
        static string GroundProfile()
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            Vector3 o = p.transform.position;
            var sb = new System.Text.StringBuilder("ground profile at " + o.ToString("0") + ":");
            int mask = LayerMask.GetMask("terrain");
            for (float r = 0f; r <= 90f; r += 15f)
                for (int i = 0; i < (r == 0f ? 1 : 8); i++)
                {
                    Vector3 c = o + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * r;
                    float zs;
                    bool ok = ZoneSystem.instance.GetGroundHeight(c, out zs);
                    RaycastHit hit;
                    string ray = Physics.Raycast(new Vector3(c.x, 1000f, c.z), Vector3.down, out hit, 2000f, mask) ? hit.point.y.ToString("0.0") : "-";
                    sb.Append(" [r" + r.ToString("0") + " a" + (i * 45) + " zs " + (ok ? zs.ToString("0.0") : "-") + " ray " + ray + " gen " + WorldGenerator.instance.GetHeight(c.x, c.z).ToString("0.0") + "]");
                }
            ModLog.Info("AsksvinTowTestHooks: " + sb);
            return sb.ToString();
        }

        /// <summary>Ground height: the loaded ground where there is one, else the world generator's (GetGroundHeight answered false everywhere in a9slope1).</summary>
        static float GroundY(Vector3 at)
        {
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h) && h != 0f) return h;
            return WorldGenerator.instance != null ? WorldGenerator.instance.GetHeight(at.x, at.z) : at.y;
        }

        static GameObject _ramp;

        /// <summary>
        /// The lab world is levelled flat for a long way round the arena (groundProfile, a9slope4: 53.0 m everywhere), so the slope test builds its own hillside:
        /// a 14 x 90 m slab on the terrain layer, tilted <paramref name="degrees"/> sideways, its low edge in the ground, centred on the player, who stands on it. The cannon and the Asksvin placed next land on it (their placing uses the ground height). Returns "ramp D deg".
        /// </summary>
        [TestHook("GunPowdered.rampHere")]
        static string RampHere(double degrees)
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            RampRemove();
            Vector3 dir = Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized;
            float g = GroundY(p.transform.position), a = (float)degrees * Mathf.Deg2Rad;
            _ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _ramp.name = "gp_lab_ramp";
            _ramp.layer = LayerMask.NameToLayer("terrain");
            _ramp.transform.localScale = new Vector3(14f, 1f, 90f);          // 45 m each way: the Asksvin stands behind the player and runs away from the cannon (a9slope6)
            _ramp.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, (float)degrees);
            Vector3 centre = p.transform.position;
            centre.y = g - 0.2f + 7f * Mathf.Sin(a) - 0.5f * Mathf.Cos(a);
            _ramp.transform.position = centre;
            Physics.SyncTransforms();
            Vector3 stand = p.transform.position - dir * 0f;
            stand.y = GroundY(stand) + 0.3f;
            p.transform.position = stand;
            Rigidbody body = p.GetComponent<Rigidbody>();
            if (body != null) { body.position = stand; body.linearVelocity = Vector3.zero; }
            ModLog.Info("AsksvinTowTestHooks: ramp " + degrees.ToString("0") + " deg, centre " + centre.ToString("0.0") + ", player on it at height " + (stand.y - g).ToString("0.0") + " m");
            return "ramp " + degrees.ToString("0") + " deg";
        }

        /// <summary>Removes the lab ramp. Returns "ramp removed".</summary>
        [TestHook("GunPowdered.rampRemove")]
        static string RampRemove()
        {
            if (_ramp != null) Object.Destroy(_ramp);
            _ramp = null;
            return "ramp removed";
        }

        static readonly System.Collections.Generic.List<GameObject> Trees = new System.Collections.Generic.List<GameObject>();

        /// <summary>Spawns <paramref name="count"/> trees (Beech1) on both sides of the line from the Asksvin to the player, 1.2-1.8 m off it, so the cannon's wheels and trail brush them. Returns "trees N".</summary>
        [TestHook("GunPowdered.treesOnPath")]
        static string TreesOnPath(double count)
        {
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("Beech1") : null;
            if (p == null || _animal == null || prefab == null) return "no player, asksvin or tree prefab";
            Vector3 a = _animal.transform.position, b = p.transform.position;
            Vector3 side = Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(b - a, Vector3.up).normalized);
            int n = (int)count;
            for (int i = 0; i < n; i++)
            {
                Vector3 at = Vector3.Lerp(a, b, (i + 1f) / (n + 1f)) + side * ((i % 2 == 0 ? 1f : -1f) * (1.2f + 0.3f * (i % 3)));
                float h;
                if (ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h;
                Trees.Add(Object.Instantiate(prefab, at, Quaternion.identity));
            }
            ModLog.Info("AsksvinTowTestHooks: " + n + " trees beside the path");
            return "trees " + n;
        }

        /// <summary>Removes the trees treesOnPath spawned. Returns "trees removed N".</summary>
        [TestHook("GunPowdered.treesRemove")]
        static string TreesRemove()
        {
            int n = 0;
            foreach (GameObject t in Trees)
            {
                if (t == null) continue;
                ZNetView v = t.GetComponent<ZNetView>();
                if (v != null && v.IsValid()) { v.ClaimOwnership(); ZNetScene.instance.Destroy(t); n++; }
            }
            Trees.Clear();
            return "trees removed " + n;
        }

        /// <summary>Tames the Asksvin that asksvinFind took (one the lab spawned, so that it can be a lab target for the second client). Returns "tamed True".</summary>
        [TestHook("GunPowdered.asksvinTame")]
        static string Tame()
        {
            Character c = _animal != null ? _animal.GetComponent<Character>() : null;
            if (c == null) return "no asksvin";
            c.SetTamed(true);
            return "tamed " + c.IsTamed();
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
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
