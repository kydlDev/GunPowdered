namespace GunPowdered.Core
{
    /// <summary>
    /// Dice for the pure rules. The caller draws one number in [0, 1) per decision (the game adapter passes a seeded generator, tests pass fixed
    /// numbers), so every branch of a rule is reproducible. Pure: no Unity / Valheim types.
    /// </summary>
    internal static class Roll
    {
        /// <summary>True when the drawn number falls inside the probability. Expected outcome: 0 never hits, 1 always hits.</summary>
        internal static bool Chance(double probability, double roll01)
        {
            if (RuleLog.IsOn()) RuleLog.Every("Roll.Chance", 2d, $"Roll.Chance(probability={RuleLog.Shown(probability)}, roll01={RuleLog.Shown(roll01)})");
            { var returnedAt13 = roll01 < probability; if (RuleLog.IsOn()) RuleLog.Every("Roll.Chance:13", 2d, $"Roll.Chance: returns {RuleLog.Shown(returnedAt13)} (line 13)"); return returnedAt13; }
        }

        /// <summary>
        /// A whole number between min and max, both inclusive, spread evenly. A draw of exactly 1 (or more) lands on max, a draw below 0 on min,
        /// so a generator that returns an edge value cannot leave the range. Expected outcome: result is inside [min, max].
        /// </summary>
        internal static int IntInclusive(int min, int max, double roll01)
        {
            if (RuleLog.IsOn()) RuleLog.Every("Roll.IntInclusive", 2d, $"Roll.IntInclusive(min={RuleLog.Shown(min)}, max={RuleLog.Shown(max)}, roll01={RuleLog.Shown(roll01)})");
            // Stryker disable once Equality : max == min gives min on both paths (the formula adds (int)(r * 1) = 0 for every r below 1)
            if (max <= min) { var returnedAt24 = min; if (RuleLog.IsOn()) RuleLog.Every("Roll.IntInclusive:24", 2d, $"Roll.IntInclusive: returns {RuleLog.Shown(returnedAt24)} (line 24)"); return returnedAt24; }
            // Stryker disable once Equality : roll01 == 0 maps to 0 on both paths
            double clamped = roll01 < 0d ? 0d : roll01 >= 1d ? 0.9999999999d : roll01;
            { var returnedAt27 = min + (int)(clamped * (max - min + 1)); if (RuleLog.IsOn()) RuleLog.Every("Roll.IntInclusive:27", 2d, $"Roll.IntInclusive: returns {RuleLog.Shown(returnedAt27)} (line 27)"); return returnedAt27; }
        }
    }
}
