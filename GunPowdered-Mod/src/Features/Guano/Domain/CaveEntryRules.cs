using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Guano.Domain
{
    /// <summary>
    /// When guano goes into a cave (D-033, user 2026-11-18): the first time a player walks in through the entrance, while the teleport screen is up
    /// and the rooms below have their floors. Decides which locations count as which kind of cave and whether this entry should place.
    /// </summary>
    public static class CaveEntryRules
    {
        /// <summary>The cave kind of a location (by its prefab name) or of its dungeon generator; null when guano never goes there.</summary>
        public static CaveKind? KindOf(string locationName, string generatorName)
        {
            if (RuleLog.Due("CaveEntryRules.KindOf", 2d)) RuleLog.Every("CaveEntryRules.KindOf", 2d, $"CaveEntryRules.KindOf(locationName={RuleLog.Shown(locationName)}, generatorName={RuleLog.Shown(generatorName)})");
            // Stryker disable String : any fallback text without the cave words gives the same answer as an empty name
            string loc = locationName ?? string.Empty;
            string gen = generatorName ?? string.Empty;
            // Stryker restore String
            if (Contains(loc, "Sunken") || Contains(gen, "Sunken") || Contains(loc, "Hildir") || Contains(gen, "Hildir")) { if (RuleLog.Due("CaveEntryRules.KindOf:20", 2d)) RuleLog.Every("CaveEntryRules.KindOf:20", 2d, "CaveEntryRules.KindOf: returns null (line 20)"); return null; }
            if (loc.StartsWith("TrollCave", StringComparison.Ordinal)) { var returnedAt21 = CaveKind.TrollCaveOrBurialChamber; if (RuleLog.Due("CaveEntryRules.KindOf:21", 2d)) RuleLog.Every("CaveEntryRules.KindOf:21", 2d, $"CaveEntryRules.KindOf: returns {RuleLog.Shown(returnedAt21)} (line 21)"); return returnedAt21; }
            if (loc.StartsWith("Crypt", StringComparison.Ordinal)) { var returnedAt22 = CaveKind.TrollCaveOrBurialChamber; if (RuleLog.Due("CaveEntryRules.KindOf:22", 2d)) RuleLog.Every("CaveEntryRules.KindOf:22", 2d, $"CaveEntryRules.KindOf: returns {RuleLog.Shown(returnedAt22)} (line 22)"); return returnedAt22; }
            if (loc.StartsWith("MountainCave", StringComparison.Ordinal)) { var returnedAt23 = CaveKind.MountainCave; if (RuleLog.Due("CaveEntryRules.KindOf:23", 2d)) RuleLog.Every("CaveEntryRules.KindOf:23", 2d, $"CaveEntryRules.KindOf: returns {RuleLog.Shown(returnedAt23)} (line 23)"); return returnedAt23; }
            if (gen.StartsWith("DG_ForestCrypt", StringComparison.Ordinal) || gen.StartsWith("DG_HalfBurried_ForestCrypt", StringComparison.Ordinal)) { var returnedAt24 = CaveKind.TrollCaveOrBurialChamber; if (RuleLog.Due("CaveEntryRules.KindOf:24", 2d)) RuleLog.Every("CaveEntryRules.KindOf:24", 2d, $"CaveEntryRules.KindOf: returns {RuleLog.Shown(returnedAt24)} (line 24)"); return returnedAt24; }
            if (gen.StartsWith("DG_Cave", StringComparison.Ordinal)) { var returnedAt25 = CaveKind.MountainCave; if (RuleLog.Due("CaveEntryRules.KindOf:25", 2d)) RuleLog.Every("CaveEntryRules.KindOf:25", 2d, $"CaveEntryRules.KindOf: returns {RuleLog.Shown(returnedAt25)} (line 25)"); return returnedAt25; }
            { if (RuleLog.Due("CaveEntryRules.KindOf:26", 2d)) RuleLog.Every("CaveEntryRules.KindOf:26", 2d, "CaveEntryRules.KindOf: returns null (line 26)"); return null; }
        }

        /// <summary>Place on this entry only when the player goes IN (not out), the location is a guano cave, and nobody placed there before.</summary>
        public static bool ShouldPlace(bool goingIn, CaveKind? kind, bool alreadyPlaced) => goingIn && kind.HasValue && !alreadyPlaced;

        // Stryker disable once StringMethod : LastIndexOf finds the part exactly when IndexOf does (only whether it is found is used)
        static bool Contains(string text, string part) => text.IndexOf(part, StringComparison.Ordinal) >= 0;
    }
}
