using System;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Integrations.Valheim;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>D-140: after the game placed the build ghost, an extension standing on a batch station turns red (StationPlacement.CheckGhost).</summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    internal static class StationPlacementGhostPatch
    {
        static void Postfix(Player __instance)
        {
            try
            {
                StationPlacement.CheckGhost(__instance);
            }
            catch (Exception e)
            {
                ModLog.Warn("StationPlacementGhostPatch: " + e.GetType().FullName + ": " + e.Message + " - the ghost keeps the game's verdict");
            }
        }
    }
}
