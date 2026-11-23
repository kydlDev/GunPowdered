using System;
using System.Collections.Generic;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Guano.Domain;
using GunPowdered.Integrations.Valheim;
using KydlCore.Common;

namespace GunPowdered.Features.Guano.Infrastructure
{
    /// <summary>
    /// Rolls and places Bat Guano deposits in one cave (D-016): GuanoDepositRules decides how many deposits the cave gets and how many guano each gives,
    /// this class puts them on the floor at spots the caller found inside the cave. The roll is seeded by the cave's own seed, so the same cave always
    /// rolls the same way.
    /// </summary>
    internal static class GuanoPlacer
    {
        /// <summary>Expected outcome: the number of deposits that ended up in the world (0 when the roll gave none or no spot had a floor).</summary>
        internal static int Place(CaveKind kind, IList<Vector3> spots, int seed) => Place(kind, spots, seed, out _);

        /// <summary>Same, and says how many deposits the roll asked for (a planned deposit that found no floor is the difference).</summary>
        internal static int Place(CaveKind kind, IList<Vector3> spots, int seed, out int planned)
        {
            var rng = new SeededRandom(seed ^ 0x6A09E667);
            int[] plan = GuanoDepositRules.Plan(kind, () => rng.NextDouble());
            planned = plan.Length;
            if (plan.Length == 0 || spots.Count == 0) { if (RuleLog.Due("GuanoPlacer.Place:27", 2d)) ModLog.Every("GuanoPlacer.Place:27", 2d, $"GuanoPlacer.Place: returns 0 because plan.Length == 0 || spots.Count == 0 (line 27)"); return 0; }

            // every spot, dealt evenly over the cave's floors (D-151: lowest floor first, one per floor per round, random within a floor); a spot without a
            // floor under it (a wall piece, a gap) is skipped and the next one is tried (seen in game: 2 of 3 missed)
            int placed = 0, noFloor = 0;
            var heights = new List<double>(spots.Count);
            foreach (Vector3 p in spots) heights.Add(p.y);
            var order = new List<Vector3>(spots.Count);
            foreach (int i in GuanoSpreadRules.FloorOrder(heights, () => rng.NextDouble())) order.Add(spots[i]);
            for (int s = 0; s < order.Count && placed < plan.Length; s++)
            {
                Vector3 floor;
                if (!DepositSpawner.Floor(order[s], out floor)) { noFloor++; { if (RuleLog.Due("GuanoPlacer.Place:39", 2d)) ModLog.Every("GuanoPlacer.Place:39", 2d, $"GuanoPlacer.Place: skips to the next item (continue) because !DepositSpawner.Floor(order[s], out floor) (line 39)"); continue; } }
                if (DepositSpawner.Spawn(GunPowderedPrefabs.GuanoDeposit, floor, plan[placed], seed + placed) != null) placed++;
            }
            KydlCore.Common.ModLog.Verbose("GuanoPlacer: " + kind + " planned " + plan.Length + ", placed " + placed + ", no floor at " + noFloor + " spot(s)");
            { var returnedAt43 = placed; if (RuleLog.Due("GuanoPlacer.Place:43", 2d)) ModLog.Every("GuanoPlacer.Place:43", 2d, $"GuanoPlacer.Place: returns {returnedAt43} (line 43)"); return returnedAt43; }
        }
    }
}
