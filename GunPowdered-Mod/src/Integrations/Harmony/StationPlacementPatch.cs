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
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("StationPlacementGhostPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().FullName + ": " + e.Message + " - the ghost keeps the game's verdict" + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("StationPlacementGhostPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().FullName + ": " + e.Message + " - the ghost keeps the game's verdict" + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("StationPlacementGhostPatch: " + e.GetType().FullName + ": " + e.Message + " - the ghost keeps the game's verdict" + "\n" + e.StackTrace);
            }
        }
    }
}
