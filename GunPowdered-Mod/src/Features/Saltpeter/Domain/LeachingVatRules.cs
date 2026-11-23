using System;
using GunPowdered.Core;

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
        /// The station's states: idle (less than one base batch inside) or burning (a run is going, D-132). StateDone is only found in saves from before
        /// D-132 (the old 'done, wait for a player' state): the owner moves its Saltpeter onto the pile and goes on as idle.
        internal const int StateIdle = 0, StateBurning = 1, StateDone = 2;

        /// <summary>Can the hearth be lit: the barrel is loaded and the hearth holds enough wood. Expected outcome: false for no load, a missing or negative fuel need, or too little wood.</summary>
        internal static bool CanStart(bool barrelLoaded, int fuel, int fuelPerBatch)
        {
            if (RuleLog.Due("LeachingVatRules.CanStart", 2d)) RuleLog.Every("LeachingVatRules.CanStart", 2d, $"LeachingVatRules.CanStart(barrelLoaded={RuleLog.Shown(barrelLoaded)}, fuel={RuleLog.Shown(fuel)}, fuelPerBatch={RuleLog.Shown(fuelPerBatch)})");
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
        /// Does the station take one item of this prefab now (D-132: at any time, also during a run, up to what it can hold). Expected outcome: Guano /
        /// Water / Wood while that slot is below <paramref name="need"/>, Refused for any other item or a full slot.
        /// </summary>
        internal static Feed Accept(string prefab, VatAmounts have, VatAmounts need)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"VatAmounts.Accept(prefab={RuleLog.Shown(prefab)}, have={RuleLog.Shown(have)}, need={RuleLog.Shown(need)})");
            if (string.IsNullOrEmpty(prefab)) return Feed.Refused;
            if (prefab == "GP_BatGuano") { var returnedAt48 = have.Guano < need.Guano ? Feed.Guano : Feed.Refused; if (RuleLog.IsOn()) RuleLog.Verbose($"VatAmounts.Accept: returns {returnedAt48} because prefab == \"GP_BatGuano\" (line 48)"); return returnedAt48; }
            if (prefab == "GP_Water") { var returnedAt49 = have.Water < need.Water ? Feed.Water : Feed.Refused; if (RuleLog.IsOn()) RuleLog.Verbose($"VatAmounts.Accept: returns {returnedAt49} because prefab == \"GP_Water\" (line 49)"); return returnedAt49; }
            if (prefab == "Wood") { var returnedAt50 = have.Fuel < need.Fuel ? Feed.Wood : Feed.Refused; if (RuleLog.IsOn()) RuleLog.Verbose($"VatAmounts.Accept: returns {returnedAt50} because prefab == \"Wood\" (line 50)"); return returnedAt50; }
            { var returnedAt51 = Feed.Refused; if (RuleLog.IsOn()) RuleLog.Verbose($"VatAmounts.Accept: returns {returnedAt51} (line 51)"); return returnedAt51; }
        }

        /// <summary>The barrel is ready when it holds a whole batch of guano and of water. Expected outcome: false for any shortfall or a bad setting.</summary>
        internal static bool BarrelFull(int guano, int water, int guanoNeed, int waterNeed) =>
            guanoNeed >= 1 && waterNeed >= 1 && guano >= guanoNeed && water >= waterNeed;

        /// <summary>How many full batches the loaded guano and water make. Expected outcome: 0 or more; never more than either input allows.</summary>
        internal static int Batches(int guano, int water, int guanoPerBatch, int waterPerBatch)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"VatAmounts.Batches(guano={RuleLog.Shown(guano)}, water={RuleLog.Shown(water)}, guanoPerBatch={RuleLog.Shown(guanoPerBatch)}, waterPerBatch={RuleLog.Shown(waterPerBatch)})");
            if (guanoPerBatch < 1 || waterPerBatch < 1) return 0;
            { var returnedAt63 = Math.Max(0, Math.Min(guano / guanoPerBatch, water / waterPerBatch)); if (RuleLog.IsOn()) RuleLog.Verbose($"VatAmounts.Batches: returns {returnedAt63} (line 63)"); return returnedAt63; }   // a negative amount (bad input) gives 0
        }

        /// <summary>Saltpeter for a number of batches. Expected outcome: batches x perBatch, 0 for no batches or a negative setting.</summary>
        internal static int Saltpeter(int batches, int saltpeterPerBatch)
        {
            if (RuleLog.Due("VatAmounts.Saltpeter", 2d)) RuleLog.Every("VatAmounts.Saltpeter", 2d, $"VatAmounts.Saltpeter(batches={RuleLog.Shown(batches)}, saltpeterPerBatch={RuleLog.Shown(saltpeterPerBatch)})");
            return batches < 1 || saltpeterPerBatch < 1 ? 0 : batches * saltpeterPerBatch;
        }

        /// <summary>The game-clock second at which a batch started at startSeconds is done. Expected outcome: startSeconds + days x dayLengthSeconds.</summary>
        internal static double ReadyAt(double startSeconds, double dayLengthSeconds, double days)
        {
            if (RuleLog.Due("VatAmounts.ReadyAt", 2d)) RuleLog.Every("VatAmounts.ReadyAt", 2d, $"VatAmounts.ReadyAt(startSeconds={RuleLog.Shown(startSeconds)}, dayLengthSeconds={RuleLog.Shown(dayLengthSeconds)}, days={RuleLog.Shown(days)})");
            return startSeconds + days * dayLengthSeconds;
        }

        /// <summary>Is the batch done at nowSeconds. A clock that went backwards (a reloaded older save) means not done yet.</summary>
        internal static bool IsReady(double startSeconds, double nowSeconds, double dayLengthSeconds, double days)
        {
            if (RuleLog.Due("VatAmounts.IsReady", 2d)) RuleLog.Every("VatAmounts.IsReady", 2d, $"VatAmounts.IsReady(startSeconds={RuleLog.Shown(startSeconds)}, nowSeconds={RuleLog.Shown(nowSeconds)}, dayLengthSeconds={RuleLog.Shown(dayLengthSeconds)}, days={RuleLog.Shown(days)})");
            return nowSeconds >= ReadyAt(startSeconds, dayLengthSeconds, days);
        }

        /// <summary>Seconds left until the batch is done, never below 0. Expected outcome: 0 exactly when <see cref="IsReady"/>.</summary>
        internal static double SecondsLeft(double startSeconds, double nowSeconds, double dayLengthSeconds, double days)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"VatAmounts.SecondsLeft(startSeconds={RuleLog.Shown(startSeconds)}, nowSeconds={RuleLog.Shown(nowSeconds)}, dayLengthSeconds={RuleLog.Shown(dayLengthSeconds)}, days={RuleLog.Shown(days)})");
            double left = ReadyAt(startSeconds, dayLengthSeconds, days) - nowSeconds;
            { var returnedAt92 = Math.Max(0d, left); if (RuleLog.Due("VatAmounts.SecondsLeft:92", 2d)) RuleLog.Every("VatAmounts.SecondsLeft:92", 2d, $"VatAmounts.SecondsLeft: returns {returnedAt92} (line 92)"); return returnedAt92; }
        }
    }
}
