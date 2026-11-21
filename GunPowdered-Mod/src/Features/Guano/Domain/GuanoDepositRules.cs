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
                if (roll < upTo) { var returnedAt38 = count; if (RuleLog.Due("GuanoDepositRules.DepositCount:38", 2d)) RuleLog.Every("GuanoDepositRules.DepositCount:38", 2d, $"GuanoDepositRules.DepositCount: returns {RuleLog.Shown(returnedAt38)} (line 38)"); return returnedAt38; }
            }
            // a kind whose chances add up to 1 (mountain caves, D-151) always gets a count: a roll past the rounded sum takes the last one, never 0
            if (upTo >= 0.999999d) { var last = chances.Length - 1; if (RuleLog.Due("GuanoDepositRules.DepositCount:sum1", 2d)) RuleLog.Every("GuanoDepositRules.DepositCount:sum1", 2d, $"GuanoDepositRules.DepositCount: returns {last} (the chances add up to 1)"); return last; }
            { if (RuleLog.Due("GuanoDepositRules.DepositCount:40", 2d)) RuleLog.Every("GuanoDepositRules.DepositCount:40", 2d, "GuanoDepositRules.DepositCount: returns 0 (line 40)"); return 0; }
        }

        /// <summary>Expected outcome: a whole number from 1 to <see cref="MaxGatherables"/> of the kind.</summary>
        internal static int GatherablesPerDeposit(CaveKind kind, Func<double> next01)
        {
            if (RuleLog.Due("GuanoDepositRules.GatherablesPerDeposit", 2d)) RuleLog.Every("GuanoDepositRules.GatherablesPerDeposit", 2d, $"GuanoDepositRules.GatherablesPerDeposit(kind={RuleLog.Shown(kind)}, next01={RuleLog.Shown(next01)})");
            { var returnedAt47 = Roll.IntInclusive(1, MaxGatherables(kind), next01()); if (RuleLog.Due("GuanoDepositRules.GatherablesPerDeposit:47", 2d)) RuleLog.Every("GuanoDepositRules.GatherablesPerDeposit:47", 2d, $"GuanoDepositRules.GatherablesPerDeposit: returns {RuleLog.Shown(returnedAt47)} (line 47)"); return returnedAt47; }
        }

        /// <summary>The whole plan for one cave: one entry per deposit holding its gatherables. Expected outcome: length = deposit count, every entry inside the kind's range.</summary>
        internal static int[] Plan(CaveKind kind, Func<double> next01)
        {
            if (RuleLog.Due("GuanoDepositRules.Plan", 2d)) RuleLog.Every("GuanoDepositRules.Plan", 2d, $"GuanoDepositRules.Plan(kind={RuleLog.Shown(kind)}, next01={RuleLog.Shown(next01)})");
            int deposits = DepositCount(kind, next01);
            var plan = new int[deposits];
            for (int i = 0; i < deposits; i++) plan[i] = GatherablesPerDeposit(kind, next01);
            { var returnedAt57 = plan; if (RuleLog.Due("GuanoDepositRules.Plan:57", 2d)) RuleLog.Every("GuanoDepositRules.Plan:57", 2d, $"GuanoDepositRules.Plan: returns {RuleLog.Shown(returnedAt57)} (line 57)"); return returnedAt57; }
        }

        /// <summary>Most gatherables one deposit holds: 1-4 in mountain caves, 1-2 elsewhere.</summary>
        internal static int MaxGatherables(CaveKind kind)
        {
            if (RuleLog.Due("GuanoDepositRules.MaxGatherables", 2d)) RuleLog.Every("GuanoDepositRules.MaxGatherables", 2d, $"GuanoDepositRules.MaxGatherables(kind={RuleLog.Shown(kind)})");
            { var returnedAt64 = kind == CaveKind.MountainCave ? 4 : 2; if (RuleLog.Due("GuanoDepositRules.MaxGatherables:64", 2d)) RuleLog.Every("GuanoDepositRules.MaxGatherables:64", 2d, $"GuanoDepositRules.MaxGatherables: returns {RuleLog.Shown(returnedAt64)} (line 64)"); return returnedAt64; }
        }

        /// <summary>Most deposits one cave can get: 2 in Troll Caves / Burial Chambers, 10 in mountain caves (D-151), 3 in swamp bat areas.</summary>
        internal static int MaxDeposits(CaveKind kind)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"GuanoDepositRules.MaxDeposits(kind={RuleLog.Shown(kind)})");
            { var returnedAt71 = CountChances(kind).Length - 1; if (RuleLog.IsOn()) RuleLog.Verbose($"GuanoDepositRules.MaxDeposits: returns {RuleLog.Shown(returnedAt71)} (line 71)"); return returnedAt71; }
        }

        static double[] CountChances(CaveKind kind)
        {
            if (RuleLog.Due("GuanoDepositRules.CountChances", 2d)) RuleLog.Every("GuanoDepositRules.CountChances", 2d, $"GuanoDepositRules.CountChances(kind={RuleLog.Shown(kind)})");
            switch (kind)
            {
                case CaveKind.MountainCave: { var returnedAt79 = MountainCounts; if (RuleLog.Due("GuanoDepositRules.CountChances:79", 2d)) RuleLog.Every("GuanoDepositRules.CountChances:79", 2d, $"GuanoDepositRules.CountChances: returns {RuleLog.Shown(returnedAt79)} (line 79)"); return returnedAt79; }
                case CaveKind.SwampBatArea: { var returnedAt80 = SwampCounts; if (RuleLog.Due("GuanoDepositRules.CountChances:80", 2d)) RuleLog.Every("GuanoDepositRules.CountChances:80", 2d, $"GuanoDepositRules.CountChances: returns {RuleLog.Shown(returnedAt80)} (line 80)"); return returnedAt80; }
                default: { var returnedAt81 = TrollOrBurialCounts; if (RuleLog.Due("GuanoDepositRules.CountChances:81", 2d)) RuleLog.Every("GuanoDepositRules.CountChances:81", 2d, $"GuanoDepositRules.CountChances: returns {RuleLog.Shown(returnedAt81)} (line 81)"); return returnedAt81; }
            }
        }
    }
}
