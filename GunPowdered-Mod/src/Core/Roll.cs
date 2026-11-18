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
            return roll01 < probability;
        }

        /// <summary>
        /// A whole number between min and max, both inclusive, spread evenly. A draw of exactly 1 (or more) lands on max, a draw below 0 on min,
        /// so a generator that returns an edge value cannot leave the range. Expected outcome: result is inside [min, max].
        /// </summary>
        internal static int IntInclusive(int min, int max, double roll01)
        {
            if (max <= min) return min;
            double clamped = roll01 < 0d ? 0d : roll01 >= 1d ? 0.9999999999d : roll01;
            return min + (int)(clamped * (max - min + 1));
        }
    }
}
