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
    internal static partial class AsksvinTowTestHooks
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
