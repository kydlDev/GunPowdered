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
        /// The station's states (D-024): waiting for a load, burning for the in-game day, saltpeter ready to collect.
        internal const int StateIdle = 0, StateBurning = 1, StateDone = 2;

        /// <summary>Can the hearth be lit: the barrel is loaded and the hearth holds enough wood. Expected outcome: false for no load, a missing or negative fuel need, or too little wood.</summary>
        internal static bool CanStart(bool barrelLoaded, int fuel, int fuelPerBatch)
        {
            if (RuleLog.On) RuleLog.Every("LeachingVatRules.CanStart", 2d, $"LeachingVatRules.CanStart(barrelLoaded={RuleLog.Shown(barrelLoaded)}, fuel={RuleLog.Shown(fuel)}, fuelPerBatch={RuleLog.Shown(fuelPerBatch)})");
            { var returnedAt24 = barrelLoaded && fuelPerBatch >= 1 && fuel >= fuelPerBatch; if (RuleLog.On) RuleLog.Every("LeachingVatRules.CanStart:24", 2d, $"LeachingVatRules.CanStart: returns {RuleLog.Shown(returnedAt24)} (line 24)"); return returnedAt24; }
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
            if (RuleLog.On) RuleLog.Verbose($"VatAmounts.Accept(prefab={RuleLog.Shown(prefab)}, idle={RuleLog.Shown(idle)}, have={RuleLog.Shown(have)}, need={RuleLog.Shown(need)})");
            if (!idle || string.IsNullOrEmpty(prefab)) { var returnedAt46 = Feed.Refused; if (RuleLog.On) RuleLog.Verbose($"VatAmounts.Accept: returns {RuleLog.Shown(returnedAt46)} (line 46)"); return returnedAt46; }
            if (prefab == "GP_BatGuano") { var returnedAt47 = have.Guano < need.Guano ? Feed.Guano : Feed.Refused; if (RuleLog.On) RuleLog.Verbose($"VatAmounts.Accept: returns {RuleLog.Shown(returnedAt47)} (line 47)"); return returnedAt47; }
            if (prefab == "GP_Water") { var returnedAt48 = have.Water < need.Water ? Feed.Water : Feed.Refused; if (RuleLog.On) RuleLog.Verbose($"VatAmounts.Accept: returns {RuleLog.Shown(returnedAt48)} (line 48)"); return returnedAt48; }
            if (prefab == "Wood") { var returnedAt49 = have.Fuel < need.Fuel ? Feed.Wood : Feed.Refused; if (RuleLog.On) RuleLog.Verbose($"VatAmounts.Accept: returns {RuleLog.Shown(returnedAt49)} (line 49)"); return returnedAt49; }
            { var returnedAt50 = Feed.Refused; if (RuleLog.On) RuleLog.Verbose($"VatAmounts.Accept: returns {RuleLog.Shown(returnedAt50)} (line 50)"); return returnedAt50; }
        }

        /// <summary>The barrel is ready when it holds a whole batch of guano and of water. Expected outcome: false for any shortfall or a bad setting.</summary>
        internal static bool BarrelFull(int guano, int water, int guanoNeed, int waterNeed) =>
            guanoNeed >= 1 && waterNeed >= 1 && guano >= guanoNeed && water >= waterNeed;

        /// <summary>How many full batches the loaded guano and water make. Expected outcome: 0 or more; never more than either input allows.</summary>
        internal static int Batches(int guano, int water, int guanoPerBatch, int waterPerBatch)
        {
            if (RuleLog.On) RuleLog.Verbose($"VatAmounts.Batches(guano={RuleLog.Shown(guano)}, water={RuleLog.Shown(water)}, guanoPerBatch={RuleLog.Shown(guanoPerBatch)}, waterPerBatch={RuleLog.Shown(waterPerBatch)})");
            if (guanoPerBatch < 1 || waterPerBatch < 1) { if (RuleLog.On) RuleLog.Verbose("VatAmounts.Batches: returns 0 (line 61)"); return 0; }
            { var returnedAt62 = Math.Max(0, Math.Min(guano / guanoPerBatch, water / waterPerBatch)); if (RuleLog.On) RuleLog.Verbose($"VatAmounts.Batches: returns {RuleLog.Shown(returnedAt62)} (line 62)"); return returnedAt62; }   // a negative amount (bad input) gives 0
        }

        /// <summary>Saltpeter for a number of batches. Expected outcome: batches x perBatch, 0 for no batches or a negative setting.</summary>
        internal static int Saltpeter(int batches, int saltpeterPerBatch)
        {
            if (RuleLog.On) RuleLog.Every("VatAmounts.Saltpeter", 2d, $"VatAmounts.Saltpeter(batches={RuleLog.Shown(batches)}, saltpeterPerBatch={RuleLog.Shown(saltpeterPerBatch)})");
            { var returnedAt69 = batches < 1 || saltpeterPerBatch < 1 ? 0 : batches * saltpeterPerBatch; if (RuleLog.On) RuleLog.Every("VatAmounts.Saltpeter:69", 2d, $"VatAmounts.Saltpeter: returns {RuleLog.Shown(returnedAt69)} (line 69)"); return returnedAt69; }
        }

        /// <summary>The game-clock second at which a batch started at startSeconds is done. Expected outcome: startSeconds + days x dayLengthSeconds.</summary>
        internal static double ReadyAt(double startSeconds, double dayLengthSeconds, double days)
        {
            if (RuleLog.On) RuleLog.Every("VatAmounts.ReadyAt", 2d, $"VatAmounts.ReadyAt(startSeconds={RuleLog.Shown(startSeconds)}, dayLengthSeconds={RuleLog.Shown(dayLengthSeconds)}, days={RuleLog.Shown(days)})");
            { var returnedAt76 = startSeconds + days * dayLengthSeconds; if (RuleLog.On) RuleLog.Every("VatAmounts.ReadyAt:76", 2d, $"VatAmounts.ReadyAt: returns {RuleLog.Shown(returnedAt76)} (line 76)"); return returnedAt76; }
        }

        /// <summary>Is the batch done at nowSeconds. A clock that went backwards (a reloaded older save) means not done yet.</summary>
        internal static bool IsReady(double startSeconds, double nowSeconds, double dayLengthSeconds, double days)
        {
            if (RuleLog.On) RuleLog.Every("VatAmounts.IsReady", 2d, $"VatAmounts.IsReady(startSeconds={RuleLog.Shown(startSeconds)}, nowSeconds={RuleLog.Shown(nowSeconds)}, dayLengthSeconds={RuleLog.Shown(dayLengthSeconds)}, days={RuleLog.Shown(days)})");
            { var returnedAt83 = nowSeconds >= ReadyAt(startSeconds, dayLengthSeconds, days); if (RuleLog.On) RuleLog.Every("VatAmounts.IsReady:83", 2d, $"VatAmounts.IsReady: returns {RuleLog.Shown(returnedAt83)} (line 83)"); return returnedAt83; }
        }

        /// <summary>Seconds left until the batch is done, never below 0. Expected outcome: 0 exactly when <see cref="IsReady"/>.</summary>
        internal static double SecondsLeft(double startSeconds, double nowSeconds, double dayLengthSeconds, double days)
        {
            if (RuleLog.On) RuleLog.Verbose($"VatAmounts.SecondsLeft(startSeconds={RuleLog.Shown(startSeconds)}, nowSeconds={RuleLog.Shown(nowSeconds)}, dayLengthSeconds={RuleLog.Shown(dayLengthSeconds)}, days={RuleLog.Shown(days)})");
            double left = ReadyAt(startSeconds, dayLengthSeconds, days) - nowSeconds;
            { var returnedAt91 = Math.Max(0d, left); if (RuleLog.On) RuleLog.Verbose($"VatAmounts.SecondsLeft: returns {RuleLog.Shown(returnedAt91)} (line 91)"); return returnedAt91; }
        }
    }
}
