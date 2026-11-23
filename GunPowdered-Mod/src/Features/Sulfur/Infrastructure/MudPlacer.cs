using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Sulfur.Domain;
using GunPowdered.Integrations.Valheim;
using KydlCore.Common;

namespace GunPowdered.Features.Sulfur.Infrastructure
{
    /// <summary>
    /// Places Sulfurous Mud deposits around a swamp geyser (FireHole) or in the water next to a Draugr pile (D-015). Each deposit gets its sulfur amount
    /// (1-3) now, so every client agrees on it.
    /// </summary>
    internal static class MudPlacer
    {
        const float GeyserMin = 3f, GeyserMax = 8f, PileMin = 3f, PileMax = 15f;   // D-168: piles sit ~5 m inside a 15-20 m wide grave; its water often starts at the edge, past 10 m (a18mud1)
        const int Tries = 8;

        /// <summary>How many Draugr piles asked for mud since the game started (read by the test hooks).</summary>
        internal static int PilesSeen { get; private set; }

        /// <summary>Around a geyser: on dry or wet ground, any floor. Expected outcome: deposits placed.</summary>
        internal static int AroundGeyser(Vector3 centre, int seed)
        {
            if (RuleLog.Due("MudPlacer.AroundGeyser", 2d)) ModLog.Every("MudPlacer.AroundGeyser", 2d, $"MudPlacer.AroundGeyser(centre={RuleLog.Shown(centre)}, seed={RuleLog.Shown(seed)})");
            { var returnedAt26 = Place(centre, seed, ModSettings.MudPerGeyser.Value, GeyserMin, GeyserMax, false); if (RuleLog.Due("MudPlacer.AroundGeyser:26", 2d)) ModLog.Every("MudPlacer.AroundGeyser:26", 2d, $"MudPlacer.AroundGeyser: returns {RuleLog.Shown(returnedAt26)} (line 26)"); return returnedAt26; }
        }

        /// <summary>Next to a Draugr pile: only where the water covers the ground. Expected outcome: deposits placed.</summary>
        internal static int NearPile(Vector3 centre, int seed)
        {
            if (RuleLog.Due("MudPlacer.NearPile", 2d)) ModLog.Every("MudPlacer.NearPile", 2d, $"MudPlacer.NearPile(centre={RuleLog.Shown(centre)}, seed={RuleLog.Shown(seed)})");
            PilesSeen++;
            { var returnedAt34 = Place(centre, seed, ModSettings.MudPerDraugrPile.Value, PileMin, PileMax, true); if (RuleLog.Due("MudPlacer.NearPile:34", 2d)) ModLog.Every("MudPlacer.NearPile:34", 2d, $"MudPlacer.NearPile: returns {RuleLog.Shown(returnedAt34)} (line 34)"); return returnedAt34; }
        }

        static int Place(Vector3 centre, int seed, int count, float minR, float maxR, bool needWater)
        {
            var rng = new SeededRandom(seed ^ 0x3C6EF372);
            if (needWater) { int wetPlaced = PlaceInWater(centre, seed, count, minR, maxR, rng); if (RuleLog.Due("MudPlacer.Place:water", 2d)) ModLog.Every("MudPlacer.Place:water", 2d, "MudPlacer.Place: returns " + wetPlaced + " (in water)"); return wetPlaced; }
            int placed = 0;
            for (int n = 0; n < count; n++)
            {
                for (int t = 0; t < Tries; t++)
                {
                    Vector3 floor;
                    Vector3 spot = DepositSpawner.OnRing(centre, minR, maxR, rng);
                    if (!DepositSpawner.Floor(spot, out floor))
                    {
                        KydlCore.Common.ModLog.Verbose("MudPlacer: no floor within 3 m above / 6 m below " + spot.ToString("0.0") + " (centre " + centre.ToString("0.0") + ")");
                        continue;
                    }
                    if (needWater && !UnderWater(floor)) continue;
                    int amount = SulfurRules.Amount(SulfurSource.SulfurousMud, () => rng.NextDouble());
                    if (DepositSpawner.Spawn(GunPowderedPrefabs.SulfurousMud, floor, amount, seed + n) != null) placed++;
                    break;
                }
            }
            { var returnedAt58 = placed; if (RuleLog.Due("MudPlacer.Place:58", 2d)) ModLog.Every("MudPlacer.Place:58", 2d, $"MudPlacer.Place: returns {RuleLog.Shown(returnedAt58)} (line 58)"); return returnedAt58; }
        }

        /// <summary>
        /// D-168 (lab a18spots3: 8 random tries per deposit hit dry ground 183 of 201 times - swamp ground lies just above the water, so half the piles by
        /// water got no mud): scans the whole ring (every 1.5 m out, every 15 degrees), keeps every floor under water, and places the deposits on wet spots
        /// picked from those, at least 2 m apart. A pile with no water within the ring gets none (dry piles stay dry). Expected outcome: deposits placed.
        /// </summary>
        static int PlaceInWater(Vector3 centre, int seed, int count, float minR, float maxR, SeededRandom rng)
        {
            var wet = new System.Collections.Generic.List<Vector3>();
            int scanned = 0;
            for (float r = minR; r <= maxR + 0.01f; r += 1.5f)
                for (int a = 0; a < 24; a++)
                {
                    float rad = a * 15f * Mathf.Deg2Rad;
                    Vector3 floor;
                    scanned++;
                    if (DepositSpawner.Floor(centre + new Vector3(Mathf.Cos(rad) * r, 0f, Mathf.Sin(rad) * r), out floor) && UnderWaterQuiet(floor)) wet.Add(floor);
                }
            int placed = 0;
            var used = new System.Collections.Generic.List<Vector3>();
            for (int n = 0; n < count && wet.Count > 0; n++)
            {
                int pick = (int)(rng.NextDouble() * wet.Count) % wet.Count;
                Vector3 floor = wet[pick];
                wet.RemoveAt(pick);
                bool tooClose = false;
                foreach (Vector3 u in used) if (Vector3.Distance(u, floor) < 2f) { tooClose = true; break; }
                if (tooClose) { n--; continue; }
                int amount = SulfurRules.Amount(SulfurSource.SulfurousMud, () => rng.NextDouble());
                if (DepositSpawner.Spawn(GunPowderedPrefabs.SulfurousMud, floor, amount, seed + n) != null) { placed++; used.Add(floor); }
            }
            KydlCore.Common.ModLog.Verbose("MudPlacer: pile at " + centre.ToString("0.0") + ": " + scanned + " ring points scanned, " + (wet.Count + used.Count) + " under water left/used - PlaceInWater returns " + placed + " (of " + count + ")");
            return placed;
        }

        static bool UnderWaterQuiet(Vector3 floor)
        {
            float sea = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : float.MinValue;
            bool under = Mathf.Max(Floating.GetLiquidLevel(floor, 1f, LiquidType.Water), sea) > floor.y + 0.1f;
            if (RuleLog.Due("MudPlacer.UnderWaterQuiet", 2d)) ModLog.Every("MudPlacer.UnderWaterQuiet", 2d, "MudPlacer.UnderWaterQuiet(floor=" + floor.ToString("0.0") + "): returns " + under + " (one ring point; the scan total is logged per pile)");
            return under;
        }

        // the local water volume's level, or the world's sea level (lakes, rivers, the sea lie at it): at the test world's lake shore the volume lookup
        // alone found no water (bucket, run core3), so the same fallback is used here
        static bool UnderWater(Vector3 floor)
        {
            float sea = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : float.MinValue;
            float surface = Mathf.Max(Floating.GetLiquidLevel(floor, 1f, LiquidType.Water), sea);
            bool under = surface > floor.y + 0.1f;
            KydlCore.Common.ModLog.Verbose("MudPlacer: floor " + floor.ToString("0.0") + ", water surface " + surface.ToString("0.0") + " -> under water " + under);
            return under;
        }
    }
}
