using System;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Grenades.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>The mouse on its way into the camera (Player.SetMouseLook): while a grenade is being aimed it moves the landing spot and the camera stays (D-050).</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
    internal static class GrenadeLookPatch
    {
        static void Prefix(Player __instance, ref Vector2 mouseLook)
        {
            try
            {
                GrenadeAim.FilterLook(__instance, ref mouseLook);
            }
            catch (Exception e)
            {
                ModLog.Warn("GrenadeLookPatch: " + e.GetType().FullName + " filtering the mouse look: " + e.Message + " - the camera turns as usual");
            }
        }
    }
}
