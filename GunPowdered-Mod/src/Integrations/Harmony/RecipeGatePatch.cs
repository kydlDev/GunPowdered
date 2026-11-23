// kydl:read-only-observer - no creature is moved, commanded or spawned here; the patch only switches mod recipes on when their world key is set
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Integrations.Items;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// D-108: some recipes wait for a world key (the Deep North Dynamite Pack for "defeated_fader"). The game only discovers and lists enabled recipes
    /// (Player.UpdateKnownRecipesList, Player.GetAvailableRecipes), so right before either the keyed recipes get the key's current state.
    /// </summary>
    [HarmonyPatch]
    internal static class RecipeGatePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "UpdateKnownRecipesList")]
        static void BeforeDiscover() => Refresh();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.GetAvailableRecipes))]
        static void BeforeList() => Refresh();

        static void Refresh()
        {
            try { ItemRegistry.RefreshKeyed(); }
            catch (System.NullReferenceException e) { ModLog.Warn("RecipeGatePatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().FullName + ": " + e.Message + " - keyed recipes keep their state" + "\n" + e.StackTrace); }
            catch (UnityEngine.MissingReferenceException e) { ModLog.Warn("RecipeGatePatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().FullName + ": " + e.Message + " - keyed recipes keep their state" + "\n" + e.StackTrace); }
            catch (Exception e) { ModLog.Warn("RecipeGatePatch: " + e.GetType().FullName + ": " + e.Message + " - keyed recipes keep their state" + "\n" + e.StackTrace); }
        }
    }
}
