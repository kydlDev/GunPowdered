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
        // D-151 (user 2026-11-21 12:25): every mountain cave 6-10 deposits - always 6, then 40 % nothing more, 30 % +1, 15 % +2, 10 % +3, 5 % +4
        static readonly double[] MountainCounts = { 0d, 0d, 0d, 0d, 0d, 0d, 0.40d, 0.30d, 0.15d, 0.10d, 0.05d };
        static readonly double[] SwampCounts = { 0d, 0.10d / 3d, 0.10d / 3d, 0.10d / 3d };   // 10 % of areas, 1-3 deposits spread evenly

        /// <summary>Expected outcome: 0 or more deposits; never more than <see cref="MaxDeposits"/> of the kind.</summary>
        internal static int DepositCount(CaveKind kind, Func<double> next01)
        {
            if (RuleLog.Due("GuanoDepositRules.DepositCount", 2d)) RuleLog.Every("GuanoDepositRules.DepositCount", 2d, $"GuanoDepositRules.DepositCount(kind={RuleLog.Shown(kind)}, next01={RuleLog.Shown(next01)})");
            double[] chances = CountChances(kind);
            double roll = next01();
            double upTo = 0d;
            for (int count = 1; count < chances.Length; count++)
            {
                upTo += chances[count];
                if (roll < upTo) { var returnedAt39 = count; if (RuleLog.Due("GuanoDepositRules.DepositCount:39", 2d)) RuleLog.Every("GuanoDepositRules.DepositCount:39", 2d, $"GuanoDepositRules.DepositCount: returns {returnedAt39} because roll < upTo (line 39)"); return returnedAt39; }
            }
            // a kind whose chances add up to 1 (mountain caves, D-151) always gets a count: a roll past the rounded sum takes the last one, never 0
            if (upTo >= 0.999999d) { var last = chances.Length - 1; if (RuleLog.Due("GuanoDepositRules.DepositCount:sum1", 2d)) RuleLog.Every("GuanoDepositRules.DepositCount:sum1", 2d, $"GuanoDepositRules.DepositCount: returns {last} (the chances add up to 1)"); return last; }
            { if (RuleLog.Due("GuanoDepositRules.DepositCount:43", 2d)) RuleLog.Every("GuanoDepositRules.DepositCount:43", 2d, $"GuanoDepositRules.DepositCount: returns 0 (line 43)"); return 0; }
        }

        /// <summary>Expected outcome: a whole number from 1 to <see cref="MaxGatherables"/> of the kind.</summary>
        internal static int GatherablesPerDeposit(CaveKind kind, Func<double> next01)
        {
            if (RuleLog.Due("GuanoDepositRules.GatherablesPerDeposit", 2d)) RuleLog.Every("GuanoDepositRules.GatherablesPerDeposit", 2d, $"GuanoDepositRules.GatherablesPerDeposit(kind={RuleLog.Shown(kind)}, next01={RuleLog.Shown(next01)})");
            return Roll.IntInclusive(1, MaxGatherables(kind), next01());
        }

        /// <summary>The whole plan for one cave: one entry per deposit holding its gatherables. Expected outcome: length = deposit count, every entry inside the kind's range.</summary>
        internal static int[] Plan(CaveKind kind, Func<double> next01)
        {
            if (RuleLog.Due("GuanoDepositRules.Plan", 2d)) RuleLog.Every("GuanoDepositRules.Plan", 2d, $"GuanoDepositRules.Plan(kind={RuleLog.Shown(kind)}, next01={RuleLog.Shown(next01)})");
            int deposits = DepositCount(kind, next01);
            var plan = new int[deposits];
            for (int i = 0; i < deposits; i++) plan[i] = GatherablesPerDeposit(kind, next01);
            { var returnedAt60 = plan; if (RuleLog.Due("GuanoDepositRules.Plan:60", 2d)) RuleLog.Every("GuanoDepositRules.Plan:60", 2d, $"GuanoDepositRules.Plan: returns {returnedAt60} (line 60)"); return returnedAt60; }
        }

        /// <summary>Most gatherables one deposit holds: 1-4 in mountain caves, 1-2 elsewhere.</summary>
        internal static int MaxGatherables(CaveKind kind)
        {
            if (RuleLog.Due("GuanoDepositRules.MaxGatherables", 2d)) RuleLog.Every("GuanoDepositRules.MaxGatherables", 2d, $"GuanoDepositRules.MaxGatherables(kind={RuleLog.Shown(kind)})");
            return kind == CaveKind.MountainCave ? 4 : 2;
        }

        /// <summary>Most deposits one cave can get: 2 in Troll Caves / Burial Chambers, 10 in mountain caves (D-151), 3 in swamp bat areas.</summary>
        internal static int MaxDeposits(CaveKind kind)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"GuanoDepositRules.MaxDeposits(kind={RuleLog.Shown(kind)})");
            return CountChances(kind).Length - 1;
        }

        static double[] CountChances(CaveKind kind)
        {
            if (RuleLog.Due("GuanoDepositRules.CountChances", 2d)) RuleLog.Every("GuanoDepositRules.CountChances", 2d, $"GuanoDepositRules.CountChances(kind={RuleLog.Shown(kind)})");
            switch (kind)
            {
                case CaveKind.MountainCave: { var returnedAt82 = MountainCounts; if (RuleLog.Due("GuanoDepositRules.CountChances:82", 2d)) RuleLog.Every("GuanoDepositRules.CountChances:82", 2d, $"GuanoDepositRules.CountChances: returns {returnedAt82} (line 82)"); return returnedAt82; }
                case CaveKind.SwampBatArea: { var returnedAt83 = SwampCounts; if (RuleLog.Due("GuanoDepositRules.CountChances:83", 2d)) RuleLog.Every("GuanoDepositRules.CountChances:83", 2d, $"GuanoDepositRules.CountChances: returns {returnedAt83} (line 83)"); return returnedAt83; }
                default: { var returnedAt84 = TrollOrBurialCounts; if (RuleLog.Due("GuanoDepositRules.CountChances:84", 2d)) RuleLog.Every("GuanoDepositRules.CountChances:84", 2d, $"GuanoDepositRules.CountChances: returns {returnedAt84} (line 84)"); return returnedAt84; }
            }
        }
    }
}
