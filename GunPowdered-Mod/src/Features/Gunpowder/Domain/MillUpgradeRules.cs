using System.Collections.Generic;
using GunPowdered.Core;

namespace GunPowdered.Features.Gunpowder.Domain
{
    /// <summary>
    /// The automatic Powder Mill (D-137, user 2026-11-20): it works like the Leaching Station (BatchStationRules) with its own numbers. Base batch
    /// 3 Saltpeter + 1 Charcoal + 1 Sulfur -> 5 Gunpowder; each DIFFERENT extension within range (Obsidian Grindstone, Dvergr Powder Sieve, Flametal
    /// Drying Oven: Mountains, Mistlands, Ashlands) is one step that adds a base batch to the full batch and takes 2.5 minutes off the 10-minute grinding
    /// (10 / 7.5 / 5 / 2.5 min).
    /// </summary>
    internal static class MillUpgradeRules
    {
        /// <summary>The last step (three extensions, D-137).</summary>
        internal const int MaxStep = 3;
        /// <summary>Real minutes of one grinding without extensions (D-137).</summary>
        internal const double DefaultBaseMinutes = 10d;
        /// <summary>Minutes each step takes off the grinding (D-137).</summary>
        internal const double DefaultMinutesPerStep = 2.5d;

        /// <summary>The Powder Mill as a batch station: 3 steps, inputs Saltpeter / Charcoal / Sulfur.</summary>
        internal static readonly BatchStationSpec Spec = new BatchStationSpec(MaxStep, "Saltpeter", "Charcoal", "Sulfur");

        /// <summary>The mill's step from the extension kinds near it.</summary>
        internal static int Step(IEnumerable<string> kindsInRange) => BatchStationRules.Step(Spec, kindsInRange);

        /// <summary>Real minutes of one grinding at this step: 10 / 7.5 / 5 / 2.5 with the defaults.</summary>
        internal static double Minutes(int step, double baseMinutes, double minutesPerStep) => BatchStationRules.Minutes(Spec, step, baseMinutes, minutesPerStep);
    }
}
