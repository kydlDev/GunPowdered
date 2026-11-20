using System;

namespace GunPowdered.Core
{
    /// <summary>What differs between two batch stations: how many extension steps they have and how their three inputs are named.</summary>
    internal sealed class BatchStationSpec
    {
        internal BatchStationSpec(int maxStep, string firstName, string secondName, string thirdName)
        {
            MaxStep = Math.Max(0, maxStep);
            FirstName = firstName;
            SecondName = secondName;
            ThirdName = thirdName;
            if (RuleLog.IsOn()) RuleLog.Verbose($"BatchStationSpec(maxStep={MaxStep}, {firstName} / {secondName} / {thirdName})");
        }

        /// <summary>The last upgrade step (the number of different extensions that count).</summary>
        internal int MaxStep { get; }
        internal string FirstName { get; }
        internal string SecondName { get; }
        internal string ThirdName { get; }
    }
}
