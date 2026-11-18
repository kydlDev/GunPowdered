using System;

namespace GunPowdered.Features.Saltpeter.Domain
{
    /// <summary>
    /// The Leaching Vat (D-004, D-014): guano and water go in, saltpeter comes out after one in-game day. Pure: the game adapter passes in
    /// the game's own clock (seconds) and the length of a game day, so the timer works across save / load and on the server.
    /// </summary>
    internal static class LeachingVatRules
    {
        internal const int DefaultGuanoPerBatch = 10, DefaultWaterPerBatch = 10, DefaultSaltpeterPerBatch = 5;
        /// Valheim's default day length in seconds (EnvMan.m_dayLengthSec); the adapter passes the live value.
        internal const double DefaultDayLengthSeconds = 1800d;
        /// Wood the hearth burns for one batch (D-024).
        internal const int DefaultFuelPerBatch = 4;
        /// The station's states (D-024): waiting for a load, burning for the in-game day, saltpeter ready to collect.
        internal const int StateIdle = 0, StateBurning = 1, StateDone = 2;

        /// <summary>Can the hearth be lit: the barrel is loaded and the hearth holds enough wood. Expected outcome: false for no load, a missing or negative fuel need, or too little wood.</summary>
        internal static bool CanStart(bool barrelLoaded, int fuel, int fuelPerBatch)
        {
            return barrelLoaded && fuelPerBatch >= 1 && fuel >= fuelPerBatch;
        }

        /// <summary>Guano and water in the barrel and wood in the hearth (what the station holds, or what one batch needs).</summary>
        internal readonly struct VatAmounts
        {
            internal VatAmounts(int guano, int water, int fuel) { Guano = guano; Water = water; Fuel = fuel; }
            internal int Guano { get; }
            internal int Water { get; }
            internal int Fuel { get; }
        }

        /// <summary>What one fed item is (D-035, like the game's Smelter: one item per request, the owner checks the item and the room).</summary>
        internal enum Feed { Refused, Guano, Water, Wood }

        /// <summary>
        /// Does the station take one item of this prefab now. Expected outcome: Guano / Water / Wood when the station is idle and that slot still has room,
        /// Refused for any other item, a full slot or a busy station.
        /// </summary>
        internal static Feed Accept(string prefab, bool idle, VatAmounts have, VatAmounts need)
        {
            if (!idle || string.IsNullOrEmpty(prefab)) return Feed.Refused;
            if (prefab == "GP_BatGuano") return have.Guano < need.Guano ? Feed.Guano : Feed.Refused;
            if (prefab == "GP_Water") return have.Water < need.Water ? Feed.Water : Feed.Refused;
            if (prefab == "Wood") return have.Fuel < need.Fuel ? Feed.Wood : Feed.Refused;
            return Feed.Refused;
        }

        /// <summary>The barrel is ready when it holds a whole batch of guano and of water. Expected outcome: false for any shortfall or a bad setting.</summary>
        internal static bool BarrelFull(int guano, int water, int guanoNeed, int waterNeed) =>
            guanoNeed >= 1 && waterNeed >= 1 && guano >= guanoNeed && water >= waterNeed;

        /// <summary>How many full batches the loaded guano and water make. Expected outcome: 0 or more; never more than either input allows.</summary>
        internal static int Batches(int guano, int water, int guanoPerBatch, int waterPerBatch)
        {
            if (guanoPerBatch < 1 || waterPerBatch < 1) return 0;
            return Math.Max(0, Math.Min(guano / guanoPerBatch, water / waterPerBatch));   // a negative amount (bad input) gives 0
        }

        /// <summary>Saltpeter for a number of batches. Expected outcome: batches x perBatch, 0 for no batches or a negative setting.</summary>
        internal static int Saltpeter(int batches, int saltpeterPerBatch)
        {
            return batches < 1 || saltpeterPerBatch < 1 ? 0 : batches * saltpeterPerBatch;
        }

        /// <summary>The game-clock second at which a batch started at startSeconds is done. Expected outcome: startSeconds + days x dayLengthSeconds.</summary>
        internal static double ReadyAt(double startSeconds, double dayLengthSeconds, double days)
        {
            return startSeconds + days * dayLengthSeconds;
        }

        /// <summary>Is the batch done at nowSeconds. A clock that went backwards (a reloaded older save) means not done yet.</summary>
        internal static bool IsReady(double startSeconds, double nowSeconds, double dayLengthSeconds, double days)
        {
            return nowSeconds >= ReadyAt(startSeconds, dayLengthSeconds, days);
        }

        /// <summary>Seconds left until the batch is done, never below 0. Expected outcome: 0 exactly when <see cref="IsReady"/>.</summary>
        internal static double SecondsLeft(double startSeconds, double nowSeconds, double dayLengthSeconds, double days)
        {
            double left = ReadyAt(startSeconds, dayLengthSeconds, days) - nowSeconds;
            return Math.Max(0d, left);
        }
    }
}
