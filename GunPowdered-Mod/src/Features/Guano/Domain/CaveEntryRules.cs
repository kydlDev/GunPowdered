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
            if (Contains(loc, "Sunken") || Contains(gen, "Sunken")) { if (RuleLog.Due("CaveEntryRules.KindOf:20", 2d)) RuleLog.Every("CaveEntryRules.KindOf:20", 2d, $"CaveEntryRules.KindOf: returns null because Contains(loc, \"Sunken\") || Contains(gen, \"Sunken\") (line 20)"); return null; }
            // D-166 (user 2026-11-23): Hildir's crypt (a special Burial Chamber) and Hildir's cave (a special mountain cave with bats) get guano like their kind;
            // Hildir's other locations (camp, fortress) are no caves.
            if (Contains(loc, "Hildir") || Contains(gen, "Hildir"))
            {
                CaveKind? hildir = Contains(loc, "crypt") || Contains(gen, "Crypt") ? CaveKind.TrollCaveOrBurialChamber
                    : Contains(loc, "cave") || Contains(gen, "Cave") ? CaveKind.MountainCave : (CaveKind?)null;
                if (RuleLog.Due("CaveEntryRules.KindOf:hildir", 2d)) RuleLog.Every("CaveEntryRules.KindOf:hildir", 2d, $"CaveEntryRules.KindOf: returns {RuleLog.Shown(hildir)} (Hildir)");
                return hildir;
            }
            if (loc.StartsWith("TrollCave", StringComparison.Ordinal)) { var returnedAt30 = CaveKind.TrollCaveOrBurialChamber; if (RuleLog.Due("CaveEntryRules.KindOf:30", 2d)) RuleLog.Every("CaveEntryRules.KindOf:30", 2d, $"CaveEntryRules.KindOf: returns {returnedAt30} because loc.StartsWith(\"TrollCave\", StringComparison.Ordinal) (line 30)"); return returnedAt30; }
            if (loc.StartsWith("Crypt", StringComparison.Ordinal)) { var returnedAt31 = CaveKind.TrollCaveOrBurialChamber; if (RuleLog.Due("CaveEntryRules.KindOf:31", 2d)) RuleLog.Every("CaveEntryRules.KindOf:31", 2d, $"CaveEntryRules.KindOf: returns {returnedAt31} because loc.StartsWith(\"Crypt\", StringComparison.Ordinal) (line 31)"); return returnedAt31; }
            if (loc.StartsWith("MountainCave", StringComparison.Ordinal)) { var returnedAt32 = CaveKind.MountainCave; if (RuleLog.Due("CaveEntryRules.KindOf:32", 2d)) RuleLog.Every("CaveEntryRules.KindOf:32", 2d, $"CaveEntryRules.KindOf: returns {returnedAt32} because loc.StartsWith(\"MountainCave\", StringComparison.Ordinal) (line 32)"); return returnedAt32; }
            if (gen.StartsWith("DG_ForestCrypt", StringComparison.Ordinal) || gen.StartsWith("DG_HalfBurried_ForestCrypt", StringComparison.Ordinal)) { var returnedAt33 = CaveKind.TrollCaveOrBurialChamber; if (RuleLog.Due("CaveEntryRules.KindOf:33", 2d)) RuleLog.Every("CaveEntryRules.KindOf:33", 2d, $"CaveEntryRules.KindOf: returns {returnedAt33} because gen.StartsWith(\"DG_ForestCrypt\", StringComparison.Ordinal) || gen.StartsWith(\"DG_HalfBurried_ForestCrypt\", StringComparison.Ordinal) (line 33)"); return returnedAt33; }
            if (gen.StartsWith("DG_Cave", StringComparison.Ordinal)) { var returnedAt34 = CaveKind.MountainCave; if (RuleLog.Due("CaveEntryRules.KindOf:34", 2d)) RuleLog.Every("CaveEntryRules.KindOf:34", 2d, $"CaveEntryRules.KindOf: returns {returnedAt34} because gen.StartsWith(\"DG_Cave\", StringComparison.Ordinal) (line 34)"); return returnedAt34; }
            return null;
        }

        /// <summary>Place on this entry only when the player goes IN (not out), the location is a guano cave, and nobody placed there before.</summary>
        public static bool ShouldPlace(bool goingIn, CaveKind? kind, bool alreadyPlaced) => goingIn && kind.HasValue && !alreadyPlaced;

        // Stryker disable once StringMethod : LastIndexOf finds the part exactly when IndexOf does (only whether it is found is used)
        static bool Contains(string text, string part) => text.IndexOf(part, StringComparison.Ordinal) >= 0;
    }
}
