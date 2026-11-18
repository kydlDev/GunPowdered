using System;

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
            string loc = locationName ?? string.Empty;
            string gen = generatorName ?? string.Empty;
            if (Contains(loc, "Sunken") || Contains(gen, "Sunken") || Contains(loc, "Hildir") || Contains(gen, "Hildir")) return null;
            if (loc.StartsWith("TrollCave", StringComparison.Ordinal)) return CaveKind.TrollCaveOrBurialChamber;
            if (loc.StartsWith("Crypt", StringComparison.Ordinal)) return CaveKind.TrollCaveOrBurialChamber;
            if (loc.StartsWith("MountainCave", StringComparison.Ordinal)) return CaveKind.MountainCave;
            if (gen.StartsWith("DG_ForestCrypt", StringComparison.Ordinal) || gen.StartsWith("DG_HalfBurried_ForestCrypt", StringComparison.Ordinal)) return CaveKind.TrollCaveOrBurialChamber;
            if (gen.StartsWith("DG_Cave", StringComparison.Ordinal)) return CaveKind.MountainCave;
            return null;
        }

        /// <summary>Place on this entry only when the player goes IN (not out), the location is a guano cave, and nobody placed there before.</summary>
        public static bool ShouldPlace(bool goingIn, CaveKind? kind, bool alreadyPlaced) => goingIn && kind.HasValue && !alreadyPlaced;

        static bool Contains(string text, string part) => text.IndexOf(part, StringComparison.Ordinal) >= 0;
    }
}
