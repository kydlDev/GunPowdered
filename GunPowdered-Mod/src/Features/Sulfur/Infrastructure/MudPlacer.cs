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
        const float GeyserMin = 3f, GeyserMax = 8f, PileMin = 3f, PileMax = 10f;
        const int Tries = 8;

        /// <summary>How many Draugr piles asked for mud since the game started (read by the test hooks).</summary>
        internal static int PilesSeen { get; private set; }

        /// <summary>Around a geyser: on dry or wet ground, any floor. Expected outcome: deposits placed.</summary>
        internal static int AroundGeyser(Vector3 centre, int seed)
        {
            if (RuleLog.IsOn()) ModLog.Every("MudPlacer.AroundGeyser", 2d, $"MudPlacer.AroundGeyser(centre={RuleLog.Shown(centre)}, seed={RuleLog.Shown(seed)})");
            { var returnedAt26 = Place(centre, seed, ModSettings.MudPerGeyser.Value, GeyserMin, GeyserMax, false); if (RuleLog.IsOn()) ModLog.Every("MudPlacer.AroundGeyser:26", 2d, $"MudPlacer.AroundGeyser: returns {RuleLog.Shown(returnedAt26)} (line 26)"); return returnedAt26; }
        }

        /// <summary>Next to a Draugr pile: only where the water covers the ground. Expected outcome: deposits placed.</summary>
        internal static int NearPile(Vector3 centre, int seed)
        {
            if (RuleLog.IsOn()) ModLog.Every("MudPlacer.NearPile", 2d, $"MudPlacer.NearPile(centre={RuleLog.Shown(centre)}, seed={RuleLog.Shown(seed)})");
            PilesSeen++;
            { var returnedAt34 = Place(centre, seed, ModSettings.MudPerDraugrPile.Value, PileMin, PileMax, true); if (RuleLog.IsOn()) ModLog.Every("MudPlacer.NearPile:34", 2d, $"MudPlacer.NearPile: returns {RuleLog.Shown(returnedAt34)} (line 34)"); return returnedAt34; }
        }

        static int Place(Vector3 centre, int seed, int count, float minR, float maxR, bool needWater)
        {
            var rng = new SeededRandom(seed ^ 0x3C6EF372);
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
            { var returnedAt58 = placed; if (RuleLog.IsOn()) ModLog.Every("MudPlacer.Place:58", 2d, $"MudPlacer.Place: returns {RuleLog.Shown(returnedAt58)} (line 58)"); return returnedAt58; }
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
