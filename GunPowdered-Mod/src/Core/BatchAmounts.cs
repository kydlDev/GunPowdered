namespace GunPowdered.Core
{
    /// <summary>Three input amounts of a batch station (what it holds, what one base batch needs, what a player has).</summary>
    internal readonly struct BatchAmounts
    {
        internal BatchAmounts(int first, int second, int third)
        {
            First = first;
            Second = second;
            Third = third;
            if (RuleLog.IsOn()) RuleLog.Every("BatchAmounts", 2d, $"BatchAmounts({first}/{second}/{third})");
        }

        internal int First { get; }
        internal int Second { get; }
        internal int Third { get; }
    }
}
