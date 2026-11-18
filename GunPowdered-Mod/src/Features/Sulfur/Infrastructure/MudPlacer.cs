using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Sulfur.Domain;
using GunPowdered.Integrations.Valheim;

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

        /// <summary>Around a geyser: on dry or wet ground, any floor. Expected outcome: deposits placed.</summary>
        internal static int AroundGeyser(Vector3 centre, int seed)
        {
            return Place(centre, seed, ModSettings.MudPerGeyser.Value, GeyserMin, GeyserMax, false);
        }

        /// <summary>Next to a Draugr pile: only where the water covers the ground. Expected outcome: deposits placed.</summary>
        internal static int NearPile(Vector3 centre, int seed)
        {
            return Place(centre, seed, ModSettings.MudPerDraugrPile.Value, PileMin, PileMax, true);
        }

        static int Place(Vector3 centre, int seed, int count, float minR, float maxR, bool needWater)
        {
            var rng = new System.Random(seed ^ 0x3C6EF372);
            int placed = 0;
            for (int n = 0; n < count; n++)
            {
                for (int t = 0; t < Tries; t++)
                {
                    Vector3 floor;
                    if (!DepositSpawner.Floor(DepositSpawner.OnRing(centre, minR, maxR, rng), out floor)) continue;
                    if (needWater && !UnderWater(floor)) continue;
                    int amount = SulfurRules.Amount(SulfurSource.SulfurousMud, () => rng.NextDouble());
                    if (DepositSpawner.Spawn(GunPowderedPrefabs.SulfurousMud, floor, amount, seed + n) != null) placed++;
                    break;
                }
            }
            return placed;
        }

        static bool UnderWater(Vector3 floor)
        {
            return Floating.GetLiquidLevel(floor, 1f, LiquidType.Water) > floor.y + 0.1f;
        }
    }
}
