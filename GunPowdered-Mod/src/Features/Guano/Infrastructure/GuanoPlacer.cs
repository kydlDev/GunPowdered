using System;
using System.Collections.Generic;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Guano.Domain;
using GunPowdered.Integrations.Valheim;

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
        internal static int Place(CaveKind kind, IList<Vector3> spots, int seed)
        {
            var rng = new System.Random(seed ^ 0x6A09E667);
            int[] plan = GuanoDepositRules.Plan(kind, () => rng.NextDouble());
            if (plan.Length == 0 || spots.Count == 0) return 0;

            // every spot in a random order: a spot without a floor under it (a wall piece, a gap) is skipped and the next one is tried (seen in game: 2 of 3 missed)
            int placed = 0, noFloor = 0;
            List<Vector3> order = DepositSpawner.Pick(spots, spots.Count, rng);
            for (int s = 0; s < order.Count && placed < plan.Length; s++)
            {
                Vector3 floor;
                if (!DepositSpawner.Floor(order[s], out floor)) { noFloor++; continue; }
                if (DepositSpawner.Spawn(GunPowderedPrefabs.GuanoDeposit, floor, plan[placed], seed + placed) != null) placed++;
            }
            KydlCore.Common.ModLog.Verbose("GuanoPlacer: " + kind + " planned " + plan.Length + ", placed " + placed + ", no floor at " + noFloor + " spot(s)");
            return placed;
        }
    }
}
