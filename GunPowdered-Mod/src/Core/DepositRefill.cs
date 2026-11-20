using System;

namespace GunPowdered.Core
{
    /// <summary>
    /// When a gathered-out deposit (Bat Guano or Sulfurous Mud) holds its gatherables again: 3 in-game days after it was emptied (D-019).
    /// Pure: times are seconds of world time, the day length comes from the caller.
    /// </summary>
    internal static class DepositRefill
    {
        internal const double DefaultDays = 3d;
        internal const double DefaultDayLengthSeconds = 1800d;

        /// <summary>Expected outcome: world time (seconds) at which the deposit is full again; NaN when the input is bad.</summary>
        internal static double RefillAt(double emptiedAt, double days, double dayLengthSeconds)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"DepositRefill.RefillAt(emptiedAt={RuleLog.Shown(emptiedAt)}, days={RuleLog.Shown(days)}, dayLengthSeconds={RuleLog.Shown(dayLengthSeconds)})");
            bool bad = double.IsNaN(emptiedAt) || double.IsInfinity(emptiedAt) || days < 0 || double.IsNaN(days) || dayLengthSeconds <= 0 || double.IsNaN(dayLengthSeconds);
            { var returnedAt19 = bad ? double.NaN : emptiedAt + days * dayLengthSeconds; if (RuleLog.IsOn()) RuleLog.Verbose($"DepositRefill.RefillAt: returns {RuleLog.Shown(returnedAt19)} (line 19)"); return returnedAt19; }
        }

        /// <summary>Expected outcome: true when the refill time has been reached (bad input is never ready).</summary>
        internal static bool IsFull(double emptiedAt, double now, double days, double dayLengthSeconds)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"DepositRefill.IsFull(emptiedAt={RuleLog.Shown(emptiedAt)}, now={RuleLog.Shown(now)}, days={RuleLog.Shown(days)}, dayLengthSeconds={RuleLog.Shown(dayLengthSeconds)})");
            double at = RefillAt(emptiedAt, days, dayLengthSeconds);
            { var returnedAt27 = !double.IsNaN(at) && !double.IsNaN(now) && now >= at; if (RuleLog.IsOn()) RuleLog.Verbose($"DepositRefill.IsFull: returns {RuleLog.Shown(returnedAt27)} (line 27)"); return returnedAt27; }
        }

        /// <summary>Expected outcome: what a pick gives - the full amount from a full deposit, nothing from one that is not full (D-056 d: a deposit is hidden and gives nothing until its refill time).</summary>
        internal static int Gives(bool full, int amount) => full && amount > 0 ? amount : 0;

        /// <summary>Expected outcome: true when the deposit's model is shown (only while it is full).</summary>
        internal static bool ShowsModel(bool full) => full;

        /// <summary>Expected outcome: seconds until the refill, 0 when already full or the input is bad.</summary>
        internal static double SecondsLeft(double emptiedAt, double now, double days, double dayLengthSeconds)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"DepositRefill.SecondsLeft(emptiedAt={RuleLog.Shown(emptiedAt)}, now={RuleLog.Shown(now)}, days={RuleLog.Shown(days)}, dayLengthSeconds={RuleLog.Shown(dayLengthSeconds)})");
            double at = RefillAt(emptiedAt, days, dayLengthSeconds);
            { var returnedAt41 = double.IsNaN(at) || double.IsNaN(now) ? 0d : Math.Max(0d, at - now); if (RuleLog.IsOn()) RuleLog.Verbose($"DepositRefill.SecondsLeft: returns {RuleLog.Shown(returnedAt41)} (line 41)"); return returnedAt41; }
        }
    }
}
