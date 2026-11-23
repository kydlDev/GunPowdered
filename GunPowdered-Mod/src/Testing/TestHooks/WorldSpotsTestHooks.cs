// kydl:read-only-observer - test-only: reads the world's placed locations near the user's spot list; changes nothing
using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

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
            // user 2026-11-23, seed TestWorld (the lab world; KydlFlat is the template name)
            ("Bone pile dry", -1398, 640), ("Bone pile dry", -1538, 764), ("Bone pile dry", -1288, 1209),
            ("Bone pile wet", -2423, -11), ("Bone pile wet", -4427, -1073), ("Bone pile wet", -4613, -1141), ("Bone pile wet", -4718, -1272),
            ("Bone pile wet", -3839, 497), ("Bone pile wet", -3827, 833), ("Bone pile wet", -3520, 884), ("Bone pile wet", -3506, 820),
            ("FireHole", -3575, 971), ("FireHole", -3573, 872), ("FireHole", -3271, -745), ("FireHole", -4415, -964),
            ("FireHole", -4532, -1176), ("FireHole", -4632, -1196), ("FireHole", -4607, -1281), ("FireHole", -1538, -2663),
            ("TrollCave", -2169, -2883), ("TrollCave", -1913, -3142), ("TrollCave", -2172, -3261), ("TrollCave", -1657, -3330),
            ("TrollCave", -1157, -4860), ("TrollCave", 133, -3646), ("TrollCave", 1032, -1145), ("TrollCave", -261, -827),
            ("Crypt", -575, 323), ("Crypt", -770, 117), ("Crypt", -1090, 327), ("Hildir_crypt", 2299, 5374),
            ("Crypt", -1015, 67), ("Crypt", -1082, -71), ("Crypt", -1223, 118), ("Crypt", -1410, 201),
            ("MountainCave", -1216, 960), ("MountainCave", -1408, 960), ("Hildir_cave", -1344, 832), ("MountainCave", -1344, 768),
            ("MountainCave", -1408, 1216), ("MountainCave", -1600, 768), ("MountainCave", -1728, 896), ("MountainCave", -1472, 384),
            ("Bat swamp", 2362, 2322), ("Bat swamp", 1911, -4386), ("Bat swamp", 2580, -4208),
        };

        // the location name a kind should match (bone piles and swamp areas are not locations: they never count)
        static string Want(string kind) => kind.StartsWith("Bone") || kind.StartsWith("Bat") ? "\u0000" : kind == "Crypt" ? "Crypt" : kind;

        /// <summary>Logs one line per spot ("WorldSpots: KIND (x, z): nearest location NAME at (x, y, z) D m away, generator height H, biome B") and
        /// returns "world W seed S: N spots, M with the expected location within 20 m". Reads only.</summary>
        [TestHook("GunPowdered.surveySpots")]
        static string Survey()
        {
            ModLog.Verbose("WorldSpotsTestHooks.Survey()");
            var world = AccessTools.Field(typeof(ZNet), "m_world")?.GetValue(null) as World;
            string head = "world " + (world != null ? world.m_name : "?") + " seed " + (world != null ? world.m_seedName : "?");
            var zs = ZoneSystem.instance;
            var wg = WorldGenerator.instance;
            if (zs == null || wg == null) { var returnedAt47 = head + ": no zone system"; if (RuleLog.IsOn()) ModLog.Verbose($"WorldSpotsTestHooks.Survey: returns {returnedAt47} because zs == null || wg == null (line 47)"); return returnedAt47; }
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
                string name = best.m_location != null ? best.m_location.m_prefabName : "none";
                if (bestD <= 20f && name.IndexOf(Want(s.kind), System.StringComparison.OrdinalIgnoreCase) >= 0) near++;
                float low = float.MaxValue;                  // lowest generated ground within 40 m: below the water level (30 m) = water nearby
                for (float dx = -40f; dx <= 40f; dx += 4f)
                    for (float dz = -40f; dz <= 40f; dz += 4f)
                        if (dx * dx + dz * dz <= 1600f) low = Mathf.Min(low, wg.GetHeight(s.x + dx, s.z + dz));
                ModLog.Info("WorldSpots: " + s.kind + " (" + s.x + ", " + s.z + "): nearest location " + name + " at " + best.m_position.ToString("0") + " " + bestD.ToString("0") + " m away, generator height " + wg.GetHeight(s.x, s.z).ToString("0.0") + ", lowest within 40 m " + low.ToString("0.0") + ", biome " + wg.GetBiome(at));
            }
            string r = head + ": " + Spots.Length + " spots, " + near + " with the expected location within 20 m (of " + all.Count + " locations)";
            ModLog.Info("WorldSpotsTestHooks.Survey: returns " + r);
            return r;
        }
    }
}
