using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Sulfur.Domain
{
    /// <summary>Where the new sulfur comes from (D-015). Amounts are the user's (Project description, D-146).</summary>
    public enum SulfurSource
    {
        /// Sulfurous Mud deposit mined with a pickaxe: 1-3.
        SulfurousMud,
        /// Swamp Blob (prefab Blob): 1-2.
        Blob,
        /// Oozer (prefab BlobElite): 1-2.
        Oozer,
        /// Writhan (Ashlands, prefab Writhan): 3-5 (D-146).
        Writhan
    }

    /// <summary>How much sulfur one deposit or kill gives. Pure: the caller hands in a source of numbers in [0, 1).</summary>
    internal static class SulfurRules
    {
        internal const int Min = 1;

        /// Chance that a killed Blob or Oozer drops sulfur at all: 10 % each (D-016).
        internal const double KillDropChance = 0.10;

        /// Chance that a killed Writhan drops sulfur at all: 20 % (D-146).
        internal const double WrithanDropChance = 0.20;

        /// <summary>Expected outcome: true when the roll is a hit. Mud deposits always give sulfur; only kills use this.</summary>
        internal static bool KillDrops(double roll01)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"SulfurRules.KillDrops(roll01={RuleLog.Shown(roll01)})");
            { var returnedAt34 = Roll.Chance(KillDropChance, roll01); if (RuleLog.IsOn()) RuleLog.Verbose($"SulfurRules.KillDrops: returns {RuleLog.Shown(returnedAt34)} (line 34)"); return returnedAt34; }
        }

        /// <summary>
        /// The chance a kill of this source drops sulfur (D-146): the Writhan's own chance, the Blob / Oozer chance for the others. The two chances come in
        /// from the settings so the rule stays pure. Expected outcome: a probability in [0, 1] (mud deposits always give sulfur: 1).
        /// </summary>
        internal static double KillChance(SulfurSource source, double blobOozerChance, double writhanChance)
        {
            if (RuleLog.IsOn()) RuleLog.Every("SulfurRules.KillChance", 2d, $"SulfurRules.KillChance(source={RuleLog.Shown(source)}, blobOozerChance={RuleLog.Shown(blobOozerChance)}, writhanChance={RuleLog.Shown(writhanChance)})");
            double chance = source == SulfurSource.SulfurousMud ? 1d : source == SulfurSource.Writhan ? writhanChance : blobOozerChance;
            { var returnedAt45 = Math.Max(0d, Math.Min(1d, chance)); if (RuleLog.IsOn()) RuleLog.Every("SulfurRules.KillChance:45", 2d, $"SulfurRules.KillChance: returns {RuleLog.Shown(returnedAt45)} (line 45)"); return returnedAt45; }
        }

        /// <summary>Expected outcome: a whole number from <see cref="MinOf"/> to <see cref="Max"/> of the source, every amount equally likely.</summary>
        internal static int Amount(SulfurSource source, Func<double> next01)
        {
            if (RuleLog.IsOn()) RuleLog.Every("SulfurRules.Amount", 2d, $"SulfurRules.Amount(source={RuleLog.Shown(source)}, next01={RuleLog.Shown(next01)})");
            { var returnedAt52 = Roll.IntInclusive(MinOf(source), Max(source), next01()); if (RuleLog.IsOn()) RuleLog.Every("SulfurRules.Amount:52", 2d, $"SulfurRules.Amount: returns {RuleLog.Shown(returnedAt52)} (line 52)"); return returnedAt52; }
        }

        /// <summary>The fewest sulfur one drop of the source gives: 3 for the Writhan (D-146), else 1.</summary>
        internal static int MinOf(SulfurSource source)
        {
            if (RuleLog.IsOn()) RuleLog.Every("SulfurRules.MinOf", 2d, $"SulfurRules.MinOf(source={RuleLog.Shown(source)})");
            { var returnedAt59 = source == SulfurSource.Writhan ? 3 : Min; if (RuleLog.IsOn()) RuleLog.Every("SulfurRules.MinOf:59", 2d, $"SulfurRules.MinOf: returns {RuleLog.Shown(returnedAt59)} (line 59)"); return returnedAt59; }
        }

        internal static int Max(SulfurSource source)
        {
            if (RuleLog.IsOn()) RuleLog.Every("SulfurRules.Max", 2d, $"SulfurRules.Max(source={RuleLog.Shown(source)})");
            { var returnedAt65 = source == SulfurSource.Writhan ? 5 : source == SulfurSource.SulfurousMud ? 3 : 2; if (RuleLog.IsOn()) RuleLog.Every("SulfurRules.Max:65", 2d, $"SulfurRules.Max: returns {RuleLog.Shown(returnedAt65)} (line 65)"); return returnedAt65; }
        }
    }
}
