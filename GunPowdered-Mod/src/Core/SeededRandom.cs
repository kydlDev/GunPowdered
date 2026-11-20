using System;

namespace GunPowdered.Core
{
    /// <summary>
    /// A small seeded number generator (SplitMix64) for world placement. Placement must be the same on every client that fills the same cave or
    /// location, so it is seeded from the location; a cryptographically strong generator cannot be seeded and is not needed here (nothing secret
    /// depends on it). Replaces System.Random in the placers (Sonar S2245, D-043). Pure: no Unity / Valheim types.
    /// </summary>
    internal sealed class SeededRandom
    {
        private const double TwoPow53 = 9007199254740992d;   // 2^53: the 53 high bits of a draw become the fraction of NextDouble
        private ulong _state;

        /// <summary>Starts the sequence for a seed. The same seed always gives the same sequence.</summary>
        internal SeededRandom(int seed)
        {
            if (RuleLog.On) RuleLog.Every("SeededRandom.SeededRandom", 2d, $"SeededRandom.SeededRandom(seed={seed})");
            _state = unchecked((ulong)(long)seed * 0x9E3779B97F4A7C15UL);
        }

        /// <summary>The next raw 64-bit draw (SplitMix64 step).</summary>
        private ulong NextRaw()
        {
            if (RuleLog.On) RuleLog.Every("SeededRandom.NextRaw", 2d, $"SeededRandom.NextRaw()");
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                // Stryker disable once Bitwise : on a ulong >> and >>> are the same (logical) shift
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                // Stryker disable once Bitwise : on a ulong >> and >>> are the same (logical) shift
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                // Stryker disable once Bitwise : on a ulong >> and >>> are the same (logical) shift
                { var returnedAt35 = z ^ (z >> 31); if (RuleLog.On) RuleLog.Every("SeededRandom.NextRaw:35", 2d, $"SeededRandom.NextRaw: returns {returnedAt35} (line 35)"); return returnedAt35; }
            }
        }

        /// <summary>Expected outcome: a number in [0, 1).</summary>
        // Stryker disable once Bitwise : on a ulong >> and >>> are the same (logical) shift
        internal double NextDouble() => (NextRaw() >> 11) / TwoPow53;

        /// <summary>
        /// A whole number in [0, maxExclusive). Like System.Random.Next: 0 for maxExclusive 0, an ArgumentOutOfRangeException below 0.
        /// Integer maths (a 31-bit draw times maxExclusive, divided by 2^31), so no rounding can reach maxExclusive. Expected outcome: result is
        /// inside [0, maxExclusive) (0 when maxExclusive is 0).
        /// </summary>
        internal int Next(int maxExclusive)
        {
            if (RuleLog.On) RuleLog.Every("SeededRandom.Next", 2d, $"SeededRandom.Next(maxExclusive={maxExclusive})");
            if (maxExclusive < 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "must not be negative");
            // Stryker disable once Bitwise : on a ulong >> and >>> are the same (logical) shift
            ulong draw31 = NextRaw() >> 33;
            // Stryker disable once Bitwise : on a ulong >> and >>> are the same (logical) shift
            { var returnedAt55 = (int)((draw31 * (ulong)maxExclusive) >> 31); if (RuleLog.On) RuleLog.Every("SeededRandom.Next:55", 2d, $"SeededRandom.Next: returns {returnedAt55} (line 55)"); return returnedAt55; }
        }
    }
}
