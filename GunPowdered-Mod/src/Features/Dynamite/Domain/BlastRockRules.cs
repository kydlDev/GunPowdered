using GunPowdered.Core;

namespace GunPowdered.Features.Dynamite.Domain
{
    /// <summary>
    /// D-182 (user 2026-11-24, shot-004: a pack on a copper rock left pieces standing 1 m from it): the game's MineRock5.Damage finds the rock pieces a blast
    /// reaches with a 128-collider buffer around the hit point. In a forest a 20 m blast finds more than 128 colliders (trees, bushes, terrain), the buffer
    /// is full and the pieces after it are never hit. The mod takes over only then; below the limit the game's own code runs unchanged. Pure, so the unit tests check it.
    /// </summary>
    internal static class BlastRockRules
    {
        /// <summary>The size of the game's buffer (MineRock5.m_tempColliders).</summary>
        internal const int GameBuffer = 128;

        /// <summary>Whether the game's own search would miss pieces: <paramref name="found"/> colliders in the blast fill (or overflow) its buffer.</summary>
        internal static bool GameMissesPieces(int found)
        {
            bool misses = found >= GameBuffer;
            if (RuleLog.Due("BlastRockRules.GameMissesPieces", 2d)) RuleLog.Every("BlastRockRules.GameMissesPieces", 2d, $"BlastRockRules.GameMissesPieces(found={found}): returns {misses} (game buffer {GameBuffer})");
            return misses;
        }
    }
}
