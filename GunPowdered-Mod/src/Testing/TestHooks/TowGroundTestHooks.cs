using System.Linq;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// Test only: the ground and the obstacles of the Mobile Cannon towing tests - slope and height readings, the KydlFlat template's mountain cliff
    /// (there and back), and trees beside the tow path. Split from AsksvinTowTestHooks (class size, PROJECT_RULES 7); the Asksvin itself stays there.
    /// </summary>
    internal static class TowGroundTestHooks
    {
        /// <summary>
        /// Sends the towing Asksvin onto the steepest ground near it: the player (its follow target) goes <paramref name="metres"/> away, uphill
        /// (<paramref name="mode"/> "up") or along the slope ("across", the side-slope case that tips carts). Returns "slope mode, rise R m over M m (G deg)".
        /// </summary>
        [TestHook("GunPowdered.asksvinFollowSlope")]
        static string FollowSlope(string mode, double metres)
        {
            Player p = Player.m_localPlayer;
            MonsterAI ai = AsksvinTowTestHooks.Animal != null ? AsksvinTowTestHooks.Animal.GetComponent<MonsterAI>() : null;
            if (p == null || ai == null || ZoneSystem.instance == null) return "no player or no asksvin";
            Vector3 from = AsksvinTowTestHooks.Animal.transform.position;
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
                if (Vector3.Dot(along, AsksvinTowTestHooks.Animal.transform.forward) < 0f) along = -along;     // the contour way the animal already faces, not back past the cannon (a9slope5)
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
            if (cannon != null) AsksvinTowTestHooks.CannonStart = cannon.transform.position;
            ai.SetFollowTarget(p.gameObject);
            string s = "slope " + mode + ", rise " + (at.y - h0).ToString("0.0") + " m over " + Vector3.Distance(AsksvinTowTestHooks.Flat(at), AsksvinTowTestHooks.Flat(from)).ToString("0") + " m, gradient as rise per 12 m " + best.ToString("0.0") + " m ("
                + (Mathf.Atan2(best, 12f) * Mathf.Rad2Deg).ToString("0") + " deg), asksvin " + from.ToString("0.0") + " facing " + AsksvinTowTestHooks.Animal.transform.forward.ToString("0.0") + " -> player " + at.ToString("0.0");
            ModLog.Info("TowGroundTestHooks: " + s);
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
            ModLog.Info("TowGroundTestHooks: ground slope " + deg.ToString("0") + " deg at " + c.ToString("0"));
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
            ModLog.Info("TowGroundTestHooks: " + sb);
            return sb.ToString();
        }

        /// <summary>Ground height: the loaded ground where there is one, else the world generator's (GetGroundHeight answered false everywhere in a9slope1).</summary>
        internal static float GroundY(Vector3 at)
        {
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h) && h != 0f) return h;
            return WorldGenerator.instance != null ? WorldGenerator.instance.GetHeight(at.x, at.z) : at.y;
        }

        // ---- slopes: the KydlFlat template's baked cliffs ----
        // The test world is levelled on purpose (KydlFlat, Claude\shared\TestLab\templates\README.md: "tests do not build terrain ... they use the flat ground
        // and the baked cliffs"). a9slope5..8 built a tilted slab instead: the game's grass threw ~800 NullReferenceExceptions on it (ClutterSystem.GetGroundInfo
        // wants a Heightmap under every terrain hit). The slope test now uses the template's mountain cliff: bottom (200, -116), top (200, -145), 20 m rise over a
        // 25 m run (about 39 degrees), flat top 20 m wide (bake.txt).
        internal static readonly Vector3 MountainCliffBottom = new Vector3(200f, 53f, -116f), MountainCliffTop = new Vector3(200f, 73f, -145f);

        /// <summary>
        /// Teleports the player onto the mountain cliff's flat top, <paramref name="metres"/> back from its edge (z -145). The cannon is always placed south of the
        /// player and the Asksvin north of it, facing north (a9cliff1/2: the teleport's facing is not used), so on the top the Asksvin faces the edge: the test
        /// first tows DOWN the 39 degree face, turns round on the flat ground below and tows back UP. Returns "on the mountain cliff top, M m from the edge".
        /// </summary>
        [TestHook("GunPowdered.goToMountainCliffTop")]
        static string GoToMountainCliffTop(double metres)
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            _arenaSpot = p.transform.position;
            _arenaRot = p.transform.rotation;
            Vector3 at = MountainCliffTop + Vector3.back * (float)metres;
            p.TeleportTo(at + Vector3.up * 0.5f, p.transform.rotation, true);
            ModLog.Info("TowGroundTestHooks: teleport to " + at.ToString("0") + ", " + metres.ToString("0") + " m behind the mountain cliff's edge");
            return "on the mountain cliff top, " + metres.ToString("0") + " m from the edge";
        }
        /// <summary>True when the lab's cannon stands between <paramref name="min"/> and <paramref name="max"/> m height (for "wait until" on the cliff: the Asksvin picks its own way down, a fixed wait caught it half way, a9g16g).</summary>
        [TestHook("GunPowdered.cannonHeightBetween")]
        static bool CannonHeightBetween(double min, double max)
        {
            GameObject cannon = MobileCannonTestHooks.Cannon;
            return cannon != null && cannon.transform.position.y >= min && cannon.transform.position.y <= max;
        }

        static Vector3 _arenaSpot;
        static Quaternion _arenaRot = Quaternion.identity;

        /// <summary>
        /// Back from the cliff to where goToMountainCliffTop started (the arena spot), with the game's own distant teleport, which waits for the ground there.
        /// Without it the next test's arena jump (270 m, instant) landed before the arena ground had loaded: the player and the cannon fell through (a9g16e:
        /// mobilecannon-tow-break after tow-slope, the cannon 23 m off, upright False). Pair it with a "wait until" on groundUnderPlayer. Returns "back to the arena spot".
        /// </summary>
        [TestHook("GunPowdered.backFromCliff")]
        static string BackFromCliff()
        {
            Player p = Player.m_localPlayer;
            if (p == null || _arenaSpot == Vector3.zero) return "no player or no arena spot";
            p.TeleportTo(_arenaSpot + Vector3.up * 0.3f, _arenaRot, true);
            ModLog.Info("TowGroundTestHooks: back to the arena spot " + _arenaSpot.ToString("0"));
            return "back to the arena spot";
        }

        /// <summary>True when the player is not teleporting and loaded ground (terrain layer) lies within 3 m under them.</summary>
        [TestHook("GunPowdered.groundUnderPlayer")]
        static bool GroundUnderPlayer()
        {
            Player p = Player.m_localPlayer;
            if (p == null || p.IsTeleporting()) return false;
            return Physics.Raycast(p.transform.position + Vector3.up * 1f, Vector3.down, 4f, LayerMask.GetMask("terrain"));
        }


        static readonly System.Collections.Generic.List<GameObject> Trees = new System.Collections.Generic.List<GameObject>();

        /// <summary>Spawns <paramref name="count"/> trees (Beech1) on both sides of the line from the Asksvin to the player, 1.2-1.8 m off it, so the cannon's wheels and trail brush them. Returns "trees N".</summary>
        [TestHook("GunPowdered.treesOnPath")]
        static string TreesOnPath(double count)
        {
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("Beech1") : null;
            if (p == null || AsksvinTowTestHooks.Animal == null || prefab == null) return "no player, asksvin or tree prefab";
            Vector3 a = AsksvinTowTestHooks.Animal.transform.position, b = p.transform.position;
            Vector3 side = Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(b - a, Vector3.up).normalized);
            int n = (int)count;
            for (int i = 0; i < n; i++)
            {
                Vector3 at = Vector3.Lerp(a, b, (i + 1f) / (n + 1f)) + side * ((i % 2 == 0 ? 1f : -1f) * (1.2f + 0.3f * (i % 3)));
                float h;
                if (ZoneSystem.instance.GetGroundHeight(at, out h)) at.y = h;
                Trees.Add(Object.Instantiate(prefab, at, Quaternion.identity));
            }
            ModLog.Info("TowGroundTestHooks: " + n + " trees beside the path");
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

    }
}
