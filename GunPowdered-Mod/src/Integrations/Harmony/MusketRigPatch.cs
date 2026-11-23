// kydl:read-only-observer - no creature is moved, commanded or spawned here; the patch only adds the musket pose component to each player
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Musket.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>Every player (the local one and the others) gets the musket rig when it wakes, so everyone sees the musket poses (D-092).</summary>
    [HarmonyPatch(typeof(Player), "Awake")]
    internal static class MusketRigPatch
    {
        static void Postfix(Player __instance)
        {
            try
            {
                MusketRig rig = MusketRig.AttachTo(__instance);
                ModLog.Verbose("MusketRigPatch: " + __instance.name + " rig " + (rig != null));
            }
            catch (UnityEngine.MissingComponentException e)
            {
                ModLog.Warn("MusketRigPatch: " + e.Message + " - this player keeps the crossbow hold");
            }
            catch (Exception e)
            {
                ModLog.Warn("MusketRigPatch: " + e.GetType().FullName + ": " + e.Message + " - this player keeps the crossbow hold" + "\n" + e.StackTrace);
            }
        }
    }
}
