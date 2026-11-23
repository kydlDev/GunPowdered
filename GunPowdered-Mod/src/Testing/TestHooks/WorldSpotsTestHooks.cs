// kydl:read-only-observer - test-only: reads the world's placed locations near the user's spot list; changes nothing
using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// User 2026-11-23: real cave / chamber / geyser / bone pile spots of the lab world (given as X, Z), so cave tests use the world's own locations
    /// instead of generating caves next to an arena spot that moves (enter-burial-chambers: 7 of 8 entrances). This survey checks each spot against the
    /// world's placed location list (ZoneSystem location instances: name, position with ground height) and the world generator's height and biome.
    /// </summary>
    internal static class WorldSpotsTestHooks
    {
        static readonly (string kind, float x, float z)[] Spots =
        {
            ("Mountain cave", 3136, 1856), ("Mountain cave", 3008, 1536), ("Mountain cave", 2816, 1472), ("Mountain cave", 2816, 1728),
            ("Mountain cave", 1152, 3584), ("Mountain cave", 1344, 3712), ("Mountain cave", 1408, 3904), ("Mountain cave", 960, 3712),
            ("Burial chamber", 519, -904), ("Burial chamber", 317, -763), ("Burial chamber", 693, -828), ("Burial chamber", 10, -1153),
            ("Burial chamber", -128, -1147), ("Burial chamber", -192, -1219), ("Burial chamber", -2, -1353), ("Burial chamber", 837, -438),
            ("Troll cave", 1152, 59), ("Troll cave", 504, -579), ("Troll cave", 770, -836), ("Troll cave", 1160, -574),
            ("Troll cave", 1533, -446), ("Troll cave", 1786, -261), ("Troll cave", 2233, -639), ("Troll cave", 2049, -1025),
            ("Bat swamp", 2569, 2676), ("Bat swamp", 4891, 3258),
            ("FireHole", 2100, 1088), ("FireHole", 252, 3265), ("FireHole", -83, 3110), ("FireHole", 390, 2808),
            ("FireHole", 1776, 3802), ("FireHole", 1775, 3695), ("FireHole", -949, 4571), ("FireHole", -841, 4646),
            ("Evil bone pile", -79, 3462), ("Evil bone pile", -8, 3378), ("Evil bone pile", 48, 3335), ("Evil bone pile", 108, 3279),
            ("Evil bone pile", 193, 3324), ("Evil bone pile", 199, 3244), ("Evil bone pile", 111, 2759), ("Evil bone pile", 516, 2894),
        };

        /// <summary>Logs one line per spot ("WorldSpots: KIND (x, z): nearest location NAME at (x, y, z) D m away, generator height H, biome B") and
        /// returns "world W seed S: N spots, M with a location within 20 m". Reads only.</summary>
        [TestHook("GunPowdered.surveySpots")]
        static string Survey()
        {
            ModLog.Verbose("WorldSpotsTestHooks.Survey()");
            var world = AccessTools.Field(typeof(ZNet), "m_world")?.GetValue(null) as World;
            string head = "world " + (world != null ? world.m_name : "?") + " seed " + (world != null ? world.m_seedName : "?");
            var zs = ZoneSystem.instance;
            var wg = WorldGenerator.instance;
            if (zs == null || wg == null) return head + ": no zone system";
            var all = new List<ZoneSystem.LocationInstance>(zs.GetLocationList());
            int near = 0;
            foreach (var s in Spots)
            {
                var at = new Vector3(s.x, 0f, s.z);
                ZoneSystem.LocationInstance best = default;
                float bestD = float.MaxValue;
                foreach (var li in all)
                {
                    float d = Vector2.Distance(new Vector2(li.m_position.x, li.m_position.z), new Vector2(s.x, s.z));
                    if (d < bestD) { bestD = d; best = li; }
                }
                if (bestD <= 20f) near++;
                string name = best.m_location != null ? best.m_location.m_prefabName : "none";
                ModLog.Info("WorldSpots: " + s.kind + " (" + s.x + ", " + s.z + "): nearest location " + name + " at " + best.m_position.ToString("0") + " " + bestD.ToString("0") + " m away, generator height " + wg.GetHeight(s.x, s.z).ToString("0.0") + ", biome " + wg.GetBiome(at));
            }
            string r = head + ": " + Spots.Length + " spots, " + near + " with a location within 20 m (of " + all.Count + " locations)";
            ModLog.Info("WorldSpotsTestHooks.Survey: returns " + r);
            return r;
        }
    }
}
