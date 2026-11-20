using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Gunpowder.Domain
{
    /// <summary>
    /// The Powder Mill recipe (D-014): saltpeter + charcoal + sulfur make gunpowder. Pure arithmetic for batch counts and yield, so the
    /// station, the recipe display and the API agree. The numbers come from settings (Tunables); these are the starting values.
    /// </summary>
    internal static class GunpowderRules
    {
        internal const int DefaultSaltpeter = 3, DefaultCharcoal = 1, DefaultSulfur = 1, DefaultYield = 5;

        /// <summary>How many full batches the loaded ingredients make. Expected outcome: 0 or more; limited by the scarcest ingredient; 0 for a bad (below 1) recipe amount.</summary>
        internal static int Batches(int saltpeter, int charcoal, int sulfur, int saltpeterPer, int charcoalPer, int sulfurPer)
        {
            if (RuleLog.On) RuleLog.Verbose($"GunpowderRules.Batches(saltpeter={RuleLog.Shown(saltpeter)}, charcoal={RuleLog.Shown(charcoal)}, sulfur={RuleLog.Shown(sulfur)}, saltpeterPer={RuleLog.Shown(saltpeterPer)}, charcoalPer={RuleLog.Shown(charcoalPer)}, sulfurPer={RuleLog.Shown(sulfurPer)})");
            if (saltpeterPer < 1 || charcoalPer < 1 || sulfurPer < 1) { if (RuleLog.On) RuleLog.Verbose("GunpowderRules.Batches: returns 0 (line 18)"); return 0; }
            { var returnedAt19 = Math.Max(0, Math.Min(saltpeter / saltpeterPer, Math.Min(charcoal / charcoalPer, sulfur / sulfurPer))); if (RuleLog.On) RuleLog.Verbose($"GunpowderRules.Batches: returns {RuleLog.Shown(returnedAt19)} (line 19)"); return returnedAt19; }   // a negative amount (bad input) gives 0
        }

        /// Degrees per second the sweep arm turns while the mill works, and how long after the last "in use" stamp it keeps turning (D-026).
        internal const float TurnDegreesPerSecond = 40f;
        internal const double TurnGraceSeconds = 3d;

        /// <summary>Is the mill turning: somebody used it within the grace time. Expected outcome: false for no stamp (0 or less), a stamp in the future of a clock that went back, or a stamp older than the grace.</summary>
        internal static bool IsTurning(double lastUseSeconds, double nowSeconds, double graceSeconds)
        {
            if (RuleLog.On) RuleLog.Every("GunpowderRules.IsTurning", 2d, $"GunpowderRules.IsTurning(lastUseSeconds={RuleLog.Shown(lastUseSeconds)}, nowSeconds={RuleLog.Shown(nowSeconds)}, graceSeconds={RuleLog.Shown(graceSeconds)})");
            if (lastUseSeconds <= 0d || nowSeconds < lastUseSeconds) { if (RuleLog.On) RuleLog.Every("GunpowderRules.IsTurning:30", 2d, "GunpowderRules.IsTurning: returns false (line 30)"); return false; }
            { var returnedAt31 = nowSeconds - lastUseSeconds <= graceSeconds; if (RuleLog.On) RuleLog.Every("GunpowderRules.IsTurning:31", 2d, $"GunpowderRules.IsTurning: returns {RuleLog.Shown(returnedAt31)} (line 31)"); return returnedAt31; }
        }

        /// <summary>Gunpowder for a number of batches. Expected outcome: batches x yieldPerBatch, 0 for no batches or a bad setting.</summary>
        internal static int Output(int batches, int yieldPerBatch)
        {
            if (RuleLog.On) RuleLog.Verbose($"GunpowderRules.Output(batches={RuleLog.Shown(batches)}, yieldPerBatch={RuleLog.Shown(yieldPerBatch)})");
            return batches < 1 || yieldPerBatch < 1 ? 0 : batches * yieldPerBatch;
        }
    }
}
