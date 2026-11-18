using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Grenades.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// The player's buttons on their way into the game (Player.SetControls): with a Spiked Grenade in hand the left button is held back until it is
    /// let go (then one attack press = the throw) and the block is off while aiming, so the right button cancels (D-050). The rules are GrenadeAim's.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    internal static class GrenadeControlsPatch
    {
        static void Prefix(Player __instance, ref bool attack, ref bool attackHold, ref bool block, ref bool blockHold)
        {
            try
            {
                GrenadeAim.FilterControls(__instance, ref attack, ref attackHold, ref block, ref blockHold);
            }
            catch (Exception e)
            {
                ModLog.Warn("GrenadeControlsPatch: " + e.GetType().FullName + " filtering the grenade controls: " + e.Message + " - the buttons go through unchanged");
            }
        }
    }
}
