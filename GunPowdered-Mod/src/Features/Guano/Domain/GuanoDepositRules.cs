using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Guano.Domain
{
    /// <summary>Where bat guano can be found (D-004, D-015). Each kind rolls its own chance and size.</summary>
    public enum CaveKind
    {
        /// Troll Caves and Burial Chambers: rare.
        TrollCaveOrBurialChamber,
        MountainCave,
        /// The swamp areas in which Bat_Swamp can spawn.
        SwampBatArea
    }

    /// <summary>
    /// How many guano deposits one cave gets and how much each holds. Pure: the caller hands in a source of numbers in [0, 1), so every branch is
    /// tested. The percentages are the user's (Project description); the even split of one chance over several counts is an assumption written
    /// down in DECISIONS D-016.
    /// </summary>
    internal static class GuanoDepositRules
    {
        // chance of exactly N deposits, index = N (index 0 unused: the chance of none is what is left over)
        static readonly double[] TrollOrBurialCounts = { 0d, 0.10d, 0.05d };
        static readonly double[] MountainCounts = { 0d, 0.05d, 0.05d, 0.05d };               // 15 % of caves, 1-3 deposits spread evenly
        static readonly double[] SwampCounts = { 0d, 0.10d / 3d, 0.10d / 3d, 0.10d / 3d };   // 10 % of areas, 1-3 deposits spread evenly

        /// <summary>Expected outcome: 0 or more deposits; never more than <see cref="MaxDeposits"/> of the kind.</summary>
        internal static int DepositCount(CaveKind kind, Func<double> next01)
        {
            double[] chances = CountChances(kind);
            double roll = next01();
            double upTo = 0d;
            for (int count = 1; count < chances.Length; count++)
            {
                upTo += chances[count];
                if (roll < upTo) return count;
            }
            return 0;
        }

        /// <summary>Expected outcome: a whole number from 1 to <see cref="MaxGatherables"/> of the kind.</summary>
        internal static int GatherablesPerDeposit(CaveKind kind, Func<double> next01)
        {
            return Roll.IntInclusive(1, MaxGatherables(kind), next01());
        }

        /// <summary>The whole plan for one cave: one entry per deposit holding its gatherables. Expected outcome: length = deposit count, every entry inside the kind's range.</summary>
        internal static int[] Plan(CaveKind kind, Func<double> next01)
        {
            int deposits = DepositCount(kind, next01);
            var plan = new int[deposits];
            for (int i = 0; i < deposits; i++) plan[i] = GatherablesPerDeposit(kind, next01);
            return plan;
        }

        /// <summary>Most gatherables one deposit holds: 1-4 in mountain caves, 1-2 elsewhere.</summary>
        internal static int MaxGatherables(CaveKind kind)
        {
            return kind == CaveKind.MountainCave ? 4 : 2;
        }

        /// <summary>Most deposits one cave can get: 2 in Troll Caves / Burial Chambers, 3 elsewhere.</summary>
        internal static int MaxDeposits(CaveKind kind)
        {
            return CountChances(kind).Length - 1;
        }

        static double[] CountChances(CaveKind kind)
        {
            switch (kind)
            {
                case CaveKind.MountainCave: return MountainCounts;
                case CaveKind.SwampBatArea: return SwampCounts;
                default: return TrollOrBurialCounts;
            }
        }
    }
}
