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
            bool bad = double.IsNaN(emptiedAt) || double.IsInfinity(emptiedAt) || days < 0 || double.IsNaN(days) || dayLengthSeconds <= 0 || double.IsNaN(dayLengthSeconds);
            return bad ? double.NaN : emptiedAt + days * dayLengthSeconds;
        }

        /// <summary>Expected outcome: true when the refill time has been reached (bad input is never ready).</summary>
        internal static bool IsFull(double emptiedAt, double now, double days, double dayLengthSeconds)
        {
            double at = RefillAt(emptiedAt, days, dayLengthSeconds);
            return !double.IsNaN(at) && !double.IsNaN(now) && now >= at;
        }

        /// <summary>Expected outcome: seconds until the refill, 0 when already full or the input is bad.</summary>
        internal static double SecondsLeft(double emptiedAt, double now, double days, double dayLengthSeconds)
        {
            double at = RefillAt(emptiedAt, days, dayLengthSeconds);
            return double.IsNaN(at) || double.IsNaN(now) ? 0d : Math.Max(0d, at - now);
        }
    }
}
