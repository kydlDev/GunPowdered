// kydl:read-only-observer - TEST ONLY: no creature is moved or commanded here; while a test armed SpawnBlock, the game's own spawn near the player is skipped
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Testing.TestHooks;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// D-185: lets a TestLab test switch the game's spawns off around the player (SpawnBlock) - creatures and the Ashlands' falling lava rocks. Inert unless
    /// a test armed it: the prefix asks SpawnBlock, which answers "spawn" at once when it is not armed.
    /// </summary>
    [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
    internal static class SpawnBlockPatch
    {
        static bool Prefix(SpawnSystem.SpawnData critter, Vector3 spawnPoint)
        {
            if (!SpawnBlock.Active) { if (RuleLog.Due("SpawnBlockPatch.Prefix:19", 5d)) ModLog.Every("SpawnBlockPatch.Prefix:19", 5d, $"SpawnBlockPatch.Prefix: returns {!SpawnBlock.Active} because !SpawnBlock.Active (line 19)"); return !SpawnBlock.Active; }
            string prefab = critter != null && critter.m_prefab != null ? critter.m_prefab.name : "<none>";
            { var returnedAt21 = SpawnBlock.Allows(prefab, spawnPoint); if (RuleLog.IsOn()) ModLog.Verbose($"SpawnBlockPatch.Prefix: returns {returnedAt21} (line 21)"); return returnedAt21; }
        }
    }
}
