using System;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Integrations.Valheim;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>D-140: after the game chose the closest snap pair, only the allowed pairs stay (StationPlacement.FilterSnap).</summary>
    [HarmonyPatch(typeof(Player), "FindClosestSnapPoints")]
    internal static class StationSnapPatch
    {
        static void Postfix(Player __instance, Transform ghost, float maxSnapDistance, ref Transform a, ref Transform b, ref bool __result)
        {
            try
            {
                StationPlacement.FilterSnap(__instance, ghost, maxSnapDistance, ref a, ref b, ref __result);
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("StationSnapPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().FullName + ": " + e.Message + " - the game's snap stays" + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("StationSnapPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().FullName + ": " + e.Message + " - the game's snap stays" + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("StationSnapPatch: " + e.GetType().FullName + ": " + e.Message + " - the game's snap stays" + "\n" + e.StackTrace);
            }
        }
    }
}
