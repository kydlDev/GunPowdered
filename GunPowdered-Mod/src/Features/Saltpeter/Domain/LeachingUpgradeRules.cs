using System;
using System.Collections.Generic;
using GunPowdered.Core;

namespace GunPowdered.Features.Saltpeter.Domain
{
    /// <summary>
    /// The Leaching Station upgrades (D-129, user 2026-11-20). Each different extension piece built near the station is one step (0 = none, at most 5).
    /// A step makes the full batch one base unit bigger (base unit 10 Bat Guano + 10 Water + 4 Wood -> 5 Saltpeter) and the leaching 5 minutes shorter
    /// (30 / 25 / 20 / 15 / 10 / 5 minutes). One use fills half of the full batch, or what the player has. A run leaches only whole base units (up to the
    /// step's full batch); the rest stays in for the next run. The station does not start with less than one base unit.
    /// Pure: the game adapter passes in the amounts and the extension kinds it found, so every branch is unit-tested.
    /// </summary>
    internal static class LeachingUpgradeRules
    {
        /// <summary>The last step (the Flametal Heater, D-129).</summary>
        internal const int MaxStep = 5;
        /// <summary>Real minutes of one run without extensions (one in-game day, D-129).</summary>
        internal const double DefaultBaseMinutes = 30d;
        /// <summary>Minutes each step takes off the run (D-129).</summary>
        internal const double DefaultMinutesPerStep = 5d;
        /// <summary>A run never gets shorter than this, whatever the settings say.</summary>
        internal const double MinMinutes = 1d;

        /// <summary>The station's step: the number of DIFFERENT extension kinds in range, at most <see cref="MaxStep"/>. Expected outcome: 0 for none, null or empty kinds; duplicates count once.</summary>
        internal static int Step(IEnumerable<string> kindsInRange)
        {
            if (kindsInRange == null) { if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.Step:null", 2d, "LeachingUpgradeRules.Step: returns 0 (no kinds)"); return 0; }
            var distinct = new HashSet<string>(StringComparer.Ordinal);
            foreach (string kind in kindsInRange)
            {
                if (!string.IsNullOrEmpty(kind)) distinct.Add(kind);
            }
            int step = Math.Min(distinct.Count, MaxStep);
            if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.Step", 2d, $"LeachingUpgradeRules.Step: {distinct.Count} kind(s) in range, returns {step}");
            return step;
        }

        /// <summary>How many base units a full batch holds at this step. Expected outcome: step + 1, the step clamped to 0..<see cref="MaxStep"/>.</summary>
        internal static int UnitsPerBatch(int step)
        {
            int units = Math.Max(0, Math.Min(step, MaxStep)) + 1;
            if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.UnitsPerBatch", 2d, $"LeachingUpgradeRules.UnitsPerBatch(step={step}): returns {units}");
            return units;
        }

        /// <summary>The full batch at this step: the base unit times <see cref="UnitsPerBatch"/>. Expected outcome: 10 / 10 / 4 at step 0, 60 / 60 / 24 at step 5 with the default unit.</summary>
        internal static LeachingVatRules.VatAmounts FullBatch(LeachingVatRules.VatAmounts unit, int step)
        {
            int n = UnitsPerBatch(step);
            var full = new LeachingVatRules.VatAmounts(Math.Max(0, unit.Guano) * n, Math.Max(0, unit.Water) * n, Math.Max(0, unit.Fuel) * n);
            if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.FullBatch", 2d, $"LeachingUpgradeRules.FullBatch(step={step}): returns {Text(full)}");
            return full;
        }

        /// <summary>What one use puts in at most: half of the full batch, rounded up. Expected outcome: 5 / 5 / 2 at step 0, 30 / 30 / 12 at step 5 with the default unit.</summary>
        internal static LeachingVatRules.VatAmounts FillPerUse(LeachingVatRules.VatAmounts unit, int step)
        {
            LeachingVatRules.VatAmounts full = FullBatch(unit, step);
            var half = new LeachingVatRules.VatAmounts((full.Guano + 1) / 2, (full.Water + 1) / 2, (full.Fuel + 1) / 2);
            if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.FillPerUse", 2d, $"LeachingUpgradeRules.FillPerUse(step={step}): returns {Text(half)}");
            return half;
        }

        /// <summary>
        /// What one use takes from the player: per material the smallest of the fill per use, the room left in the station and what the player has.
        /// Expected outcome: never negative, never more than any of the three.
        /// </summary>
        internal static LeachingVatRules.VatAmounts Take(LeachingVatRules.VatAmounts have, LeachingVatRules.VatAmounts full, LeachingVatRules.VatAmounts perUse, LeachingVatRules.VatAmounts player)
        {
            var take = new LeachingVatRules.VatAmounts(
                One(have.Guano, full.Guano, perUse.Guano, player.Guano),
                One(have.Water, full.Water, perUse.Water, player.Water),
                One(have.Fuel, full.Fuel, perUse.Fuel, player.Fuel));
            if (RuleLog.On) RuleLog.Verbose($"LeachingUpgradeRules.Take(have={Text(have)}, full={Text(full)}, perUse={Text(perUse)}, player={Text(player)}): returns {Text(take)}");
            return take;
        }

        static int One(int have, int full, int perUse, int player)
        {
            int room = Math.Max(0, full - Math.Max(0, have));
            int one = Math.Max(0, Math.Min(Math.Min(room, perUse), player));
            if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.One", 2d, $"LeachingUpgradeRules.One(have={have}, full={full}, perUse={perUse}, player={player}): returns {one}");
            return one;
        }

        /// <summary>
        /// How many whole base units a run leaches: the fewest of the three materials allow, at most <see cref="UnitsPerBatch"/> of the step.
        /// Expected outcome: 0 below one base unit or with a bad unit (any part below 1).
        /// </summary>
        internal static int Units(LeachingVatRules.VatAmounts have, LeachingVatRules.VatAmounts unit, int step)
        {
            if (unit.Guano < 1 || unit.Water < 1 || unit.Fuel < 1) { if (RuleLog.On) RuleLog.Verbose($"LeachingUpgradeRules.Units: returns 0 (bad unit {Text(unit)})"); return 0; }
            int units = Math.Min(Math.Min(have.Guano / unit.Guano, have.Water / unit.Water), have.Fuel / unit.Fuel);
            units = Math.Max(0, Math.Min(units, UnitsPerBatch(step)));
            if (RuleLog.On) RuleLog.Verbose($"LeachingUpgradeRules.Units(have={Text(have)}, unit={Text(unit)}, step={step}): returns {units}");
            return units;
        }

        /// <summary>What a run of this many units uses up. Expected outcome: the unit times the count (0 for none).</summary>
        internal static LeachingVatRules.VatAmounts Used(LeachingVatRules.VatAmounts unit, int units)
        {
            int n = Math.Max(0, units);
            var used = new LeachingVatRules.VatAmounts(unit.Guano * n, unit.Water * n, unit.Fuel * n);
            if (RuleLog.On) RuleLog.Verbose($"LeachingUpgradeRules.Used(units={units}): returns {Text(used)}");
            return used;
        }

        /// <summary>The station starts by itself once it holds the whole full batch. Expected outcome: true only when every material reached the full batch.</summary>
        internal static bool IsFull(LeachingVatRules.VatAmounts have, LeachingVatRules.VatAmounts full)
        {
            bool isFull = full.Guano > 0 && have.Guano >= full.Guano && have.Water >= full.Water && have.Fuel >= full.Fuel;
            if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.IsFull", 2d, $"LeachingUpgradeRules.IsFull(have={Text(have)}, full={Text(full)}): returns {isFull}");
            return isFull;
        }

        /// <summary>
        /// What is missing for one base unit, e.g. "Needs 3 more Water and 2 more Wood". Expected outcome: empty when the station holds at least one base unit.
        /// </summary>
        internal static string Missing(LeachingVatRules.VatAmounts have, LeachingVatRules.VatAmounts unit)
        {
            var parts = new List<string>(3);
            if (have.Guano < unit.Guano) parts.Add($"{unit.Guano - Math.Max(0, have.Guano)} more Bat Guano");
            if (have.Water < unit.Water) parts.Add($"{unit.Water - Math.Max(0, have.Water)} more Water");
            if (have.Fuel < unit.Fuel) parts.Add($"{unit.Fuel - Math.Max(0, have.Fuel)} more Wood");
            string text = parts.Count == 0 ? string.Empty : "Needs " + Join(parts);
            if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.Missing", 2d, $"LeachingUpgradeRules.Missing(have={Text(have)}, unit={Text(unit)}): returns '{text}'");
            return text;
        }

        static string Join(List<string> parts)
        {
            if (parts.Count == 1) { if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.Join:1", 2d, "LeachingUpgradeRules.Join: returns one part"); return parts[0]; }
            string text = string.Join(", ", parts.GetRange(0, parts.Count - 1)) + " and " + parts[parts.Count - 1];
            if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.Join", 2d, $"LeachingUpgradeRules.Join: returns '{text}'");
            return text;
        }

        /// <summary>
        /// Real minutes of one run at this step: base minus minutes per step times the step, never below <see cref="MinMinutes"/>.
        /// Expected outcome: 30 / 25 / 20 / 15 / 10 / 5 with the defaults.
        /// </summary>
        internal static double Minutes(int step, double baseMinutes, double minutesPerStep)
        {
            int s = Math.Max(0, Math.Min(step, MaxStep));
            double minutes = Math.Max(MinMinutes, baseMinutes - Math.Max(0d, minutesPerStep) * s);
            if (RuleLog.On) RuleLog.Every("LeachingUpgradeRules.Minutes", 2d, $"LeachingUpgradeRules.Minutes(step={step}, base={baseMinutes}, perStep={minutesPerStep}): returns {minutes}");
            return minutes;
        }

        /// <summary>The amounts as "guano / water / wood" for log lines and hover text.</summary>
        internal static string Text(LeachingVatRules.VatAmounts a) => $"{a.Guano}/{a.Water}/{a.Fuel}";
    }
}
