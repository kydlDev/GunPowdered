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
                + tow.MaxSpeedSeen.ToString("0.0") + " m/s (asksvin " + tow.AnimalMaxSpeed.ToString("0.0") + "), gap " + (h != null && tow.Ring != null ? Vector3.Distance(tow.Ring.position, h.transform.TransformPoint(AsksvinHarness.HookLocal)).ToString("0.00") : "?") + " m, upright " + (cannon.transform.up.y > 0.7f);
            ModLog.Info("AsksvinTowTestHooks: " + s);
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
