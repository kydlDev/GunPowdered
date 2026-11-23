using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Gunpowder.Domain
{
    /// <summary>
    /// The Powder Mill base batch (D-014, automatic since D-137): saltpeter + charcoal + sulfur make gunpowder. Pure arithmetic for batch counts and yield, so the
    /// station, the recipe display and the API agree. The numbers come from settings (Tunables); these are the starting values.
    /// </summary>
    internal static class GunpowderRules
    {
        internal const int DefaultSaltpeter = 3, DefaultCharcoal = 1, DefaultSulfur = 1, DefaultYield = 5;

        /// <summary>How many full batches the loaded ingredients make. Expected outcome: 0 or more; limited by the scarcest ingredient; 0 for a bad (below 1) recipe amount.</summary>
        internal static int Batches(int saltpeter, int charcoal, int sulfur, int saltpeterPer, int charcoalPer, int sulfurPer)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"GunpowderRules.Batches(saltpeter={RuleLog.Shown(saltpeter)}, charcoal={RuleLog.Shown(charcoal)}, sulfur={RuleLog.Shown(sulfur)}, saltpeterPer={RuleLog.Shown(saltpeterPer)}, charcoalPer={RuleLog.Shown(charcoalPer)}, sulfurPer={RuleLog.Shown(sulfurPer)})");
            if (saltpeterPer < 1 || charcoalPer < 1 || sulfurPer < 1) return 0;
            { var returnedAt19 = Math.Max(0, Math.Min(saltpeter / saltpeterPer, Math.Min(charcoal / charcoalPer, sulfur / sulfurPer))); if (RuleLog.IsOn()) RuleLog.Verbose($"GunpowderRules.Batches: returns {returnedAt19} (line 19)"); return returnedAt19; }   // a negative amount (bad input) gives 0
        }

        /// <summary>Degrees per second the sweep arm turns while the mill grinds (D-026, D-137).</summary>
        internal const float TurnDegreesPerSecond = 40f;

        /// <summary>Gunpowder for a number of batches. Expected outcome: batches x yieldPerBatch, 0 for no batches or a bad setting.</summary>
        internal static int Output(int batches, int yieldPerBatch)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"GunpowderRules.Output(batches={RuleLog.Shown(batches)}, yieldPerBatch={RuleLog.Shown(yieldPerBatch)})");
            return batches < 1 || yieldPerBatch < 1 ? 0 : batches * yieldPerBatch;
        }
    }
}
