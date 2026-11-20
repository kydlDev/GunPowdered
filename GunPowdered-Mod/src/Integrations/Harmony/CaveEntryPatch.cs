// kydl:read-only-observer - the teleport itself is the game's; the patch only notes that the local player went into a cave
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Guano.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>A player who uses a cave entrance (Teleport) to go IN triggers the cave's one-time guano placement (D-033).</summary>
    [HarmonyPatch(typeof(Teleport), nameof(Teleport.Interact))]
    internal static class CaveEntryPatch
    {
        static void Prefix(Humanoid character, ref bool __state)
        {
            if (RuleLog.On) ModLog.Every("CaveEntryPatch.Prefix", 2d, $"CaveEntryPatch.Prefix(character={RuleLog.Shown(character)}, __state={RuleLog.Shown(__state)})");
            __state = character != null && !character.InInterior();     // outside before the teleport = going in
        }

        static void Postfix(Teleport __instance, Humanoid character, bool __result, bool __state)
        {
            try
            {
                if (!__result || __instance.m_targetPoint == null) { if (RuleLog.On) ModLog.Every("CaveEntryPatch.Postfix:24", 2d, "CaveEntryPatch.Postfix: returns (line 24)"); return; }
                CaveEntryPlacer.OnEntered(character as Player, __instance.GetComponentInParent<Location>(), __instance.m_targetPoint.transform.position, __state);
            }
            catch (Exception e)
            {
                ModLog.Warn("CaveEntryPatch: " + e.GetType().FullName + " after a cave teleport: " + e.Message + " - no guano placed this time");
            }
        }
    }
}
