using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Sulfur.Domain
{
    /// <summary>Where the new sulfur comes from (D-015). Amounts are the user's (Project description).</summary>
    public enum SulfurSource
    {
        /// Sulfurous Mud deposit mined with a pickaxe: 1-3.
        SulfurousMud,
        /// Swamp Blob (prefab Blob): 1-2.
        Blob,
        /// Oozer (prefab BlobElite): 1-2.
        Oozer
    }

    /// <summary>How much sulfur one deposit or kill gives. Pure: the caller hands in a source of numbers in [0, 1).</summary>
    internal static class SulfurRules
    {
        internal const int Min = 1;

        /// Chance that a killed Blob or Oozer drops sulfur at all: 10 % each (D-016).
        internal const double KillDropChance = 0.10;

        /// <summary>Expected outcome: true when the roll is a hit. Mud deposits always give sulfur; only kills use this.</summary>
        internal static bool KillDrops(double roll01)
        {
            if (RuleLog.On) RuleLog.Verbose($"SulfurRules.KillDrops(roll01={roll01})");
            { var returnedAt29 = Roll.Chance(KillDropChance, roll01); if (RuleLog.On) RuleLog.Verbose($"SulfurRules.KillDrops: returns {returnedAt29} (line 29)"); return returnedAt29; }
        }

        /// <summary>Expected outcome: a whole number from <see cref="Min"/> to <see cref="Max"/> of the source.</summary>
        internal static int Amount(SulfurSource source, Func<double> next01)
        {
            if (RuleLog.On) RuleLog.Every("SulfurRules.Amount", 2d, $"SulfurRules.Amount(source={source}, next01={next01})");
            { var returnedAt36 = Roll.IntInclusive(Min, Max(source), next01()); if (RuleLog.On) RuleLog.Every("SulfurRules.Amount:36", 2d, $"SulfurRules.Amount: returns {returnedAt36} (line 36)"); return returnedAt36; }
        }

        internal static int Max(SulfurSource source)
        {
            if (RuleLog.On) RuleLog.Every("SulfurRules.Max", 2d, $"SulfurRules.Max(source={source})");
            { var returnedAt42 = source == SulfurSource.SulfurousMud ? 3 : 2; if (RuleLog.On) RuleLog.Every("SulfurRules.Max:42", 2d, $"SulfurRules.Max: returns {returnedAt42} (line 42)"); return returnedAt42; }
        }
    }
}
