using System.Collections.Generic;
using GunPowdered.Core;

namespace GunPowdered.Features.Saltpeter.Domain
{
    /// <summary>
    /// The Leaching Station upgrades (D-129, user 2026-11-20). Each different extension piece built near the station is one step (0 = none, at most 5).
    /// A step makes the full batch one base unit bigger (base unit 10 Bat Guano + 10 Water + 4 Wood -> 5 Saltpeter) and the leaching 5 minutes shorter
    /// (30 / 25 / 20 / 15 / 10 / 5 minutes). The station holds at most the full batch of its step; one use fills half of it, or what the player has.
    /// D-132: whenever it is idle and holds at least one base unit it starts by itself (no button) and leaches the whole base units inside (up to the
    /// step's full batch); the rest stays in. The Saltpeter of a finished run piles up until a player takes it all; below one base unit it stops.
    /// The rules themselves are shared with the Powder Mill (D-137): this class gives them the Leaching Station's numbers and names (BatchStationRules).
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
        internal const double MinMinutes = BatchStationRules.MinMinutes;

        /// <summary>The Leaching Station as a batch station: 5 steps, inputs Bat Guano / Water / Wood.</summary>
        internal static readonly BatchStationSpec Spec = new BatchStationSpec(MaxStep, "Bat Guano", "Water", "Wood");

        internal static BatchAmounts ToBatch(LeachingVatRules.VatAmounts a) => new BatchAmounts(a.Guano, a.Water, a.Fuel);

        internal static LeachingVatRules.VatAmounts ToVat(BatchAmounts a) => new LeachingVatRules.VatAmounts(a.First, a.Second, a.Third);

        /// <summary>The station's step: the number of DIFFERENT extension kinds in range, at most <see cref="MaxStep"/>.</summary>
        internal static int Step(IEnumerable<string> kindsInRange) => BatchStationRules.Step(Spec, kindsInRange);

        /// <summary>How many base units a full batch holds at this step: step + 1.</summary>
        internal static int UnitsPerBatch(int step) => BatchStationRules.UnitsPerBatch(Spec, step);

        /// <summary>The full batch at this step: 10 / 10 / 4 at step 0, 60 / 60 / 24 at step 5 with the default unit.</summary>
        internal static LeachingVatRules.VatAmounts FullBatch(LeachingVatRules.VatAmounts unit, int step) => ToVat(BatchStationRules.FullBatch(Spec, ToBatch(unit), step));

        /// <summary>What one use puts in at most: half of the full batch, rounded up (5 / 5 / 2 at step 0).</summary>
        internal static LeachingVatRules.VatAmounts FillPerUse(LeachingVatRules.VatAmounts unit, int step) => ToVat(BatchStationRules.FillPerUse(Spec, ToBatch(unit), step));

        /// <summary>What one use takes from the player: per material the smallest of the fill per use, the room left and what the player has.</summary>
        internal static LeachingVatRules.VatAmounts Take(LeachingVatRules.VatAmounts have, LeachingVatRules.VatAmounts full, LeachingVatRules.VatAmounts perUse, LeachingVatRules.VatAmounts player)
            => ToVat(BatchStationRules.Take(ToBatch(have), ToBatch(full), ToBatch(perUse), ToBatch(player)));

        /// <summary>How many whole base units a run leaches (0 below one base unit or with a bad unit).</summary>
        internal static int Units(LeachingVatRules.VatAmounts have, LeachingVatRules.VatAmounts unit, int step) => BatchStationRules.Units(Spec, ToBatch(have), ToBatch(unit), step);

        /// <summary>What a run of this many units uses up.</summary>
        internal static LeachingVatRules.VatAmounts Used(LeachingVatRules.VatAmounts unit, int units) => ToVat(BatchStationRules.Used(ToBatch(unit), units));

        /// <summary>D-132: does an idle station start a run now - at least one whole base unit inside; a running station never starts a second one.</summary>
        internal static bool StartsNow(int state, LeachingVatRules.VatAmounts have, LeachingVatRules.VatAmounts unit, int step)
            => BatchStationRules.StartsNow(Spec, state == LeachingVatRules.StateIdle, ToBatch(have), ToBatch(unit), step);

        /// <summary>D-132: the Saltpeter waiting in the cauldron after a run ended: the pile plus the run.</summary>
        internal static int Pile(int ready, int runSaltpeter) => BatchStationRules.Pile(ready, runSaltpeter);

        /// <summary>What is missing for one base unit, e.g. "Needs 3 more Water and 2 more Wood"; empty when one base unit is inside.</summary>
        internal static string Missing(LeachingVatRules.VatAmounts have, LeachingVatRules.VatAmounts unit) => BatchStationRules.Missing(Spec, ToBatch(have), ToBatch(unit));

        /// <summary>Real minutes of one run at this step: 30 / 25 / 20 / 15 / 10 / 5 with the defaults, never below <see cref="MinMinutes"/>.</summary>
        internal static double Minutes(int step, double baseMinutes, double minutesPerStep) => BatchStationRules.Minutes(Spec, step, baseMinutes, minutesPerStep);

        /// <summary>The amounts as "guano / water / wood" for log lines and hover text.</summary>
        internal static string Text(LeachingVatRules.VatAmounts a) => $"{a.Guano}/{a.Water}/{a.Fuel}";
    }
}
