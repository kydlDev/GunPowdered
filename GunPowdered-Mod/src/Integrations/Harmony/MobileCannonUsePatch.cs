using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Infrastructure;
using HarmonyLib;
using KydlCore.Common;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// D-182 (user 2026-11-24, shot-007): the cart's "[E] Use" (grab and push) shows and works only at the Mobile Cannon's handle. Looking anywhere else on
    /// the cannon shows the muzzle's loading text and E loads it. Other carts are untouched.
    /// </summary>
    [HarmonyPatch(typeof(Vagon), nameof(Vagon.GetHoverText))]
    internal static class MobileCannonHoverPatch
    {
        static bool Prefix(Vagon __instance, ref string __result)
        {
            MobileCannonGun gun = __instance != null ? __instance.GetComponent<MobileCannonGun>() : null;
            if (gun == null || MobileCannonLook.AtHandle(__instance)) { if (RuleLog.Due("MobileCannonHoverPatch.Prefix:game", 2d)) ModLog.Every("MobileCannonHoverPatch.Prefix:game", 2d, "MobileCannonHoverPatch.Prefix: returns true (the cart's own Use) because gun == null || MobileCannonLook.AtHandle(__instance) (line 18)"); return true; }
            __result = gun.LoadHoverText();
            if (RuleLog.Due("MobileCannonHoverPatch.Prefix:load", 2d)) ModLog.Every("MobileCannonHoverPatch.Prefix:load", 2d, "MobileCannonHoverPatch.Prefix: returns false - not at the handle, shows the loading text (line 20)");
            return false;
        }
    }

    /// <summary>D-182: E on the Mobile Cannon away from its handle loads it (the muzzle's action) instead of grabbing the cart.</summary>
    [HarmonyPatch(typeof(Vagon), nameof(Vagon.Interact))]
    [HarmonyPriority(Priority.High)]       // before MobileCannonTowedGrabPatch, which skips itself when this one already answered
    internal static class MobileCannonUsePatch
    {
        static bool Prefix(Vagon __instance, Humanoid character, bool hold, ref bool __result)
        {
            MobileCannonGun gun = __instance != null ? __instance.GetComponent<MobileCannonGun>() : null;
            if (gun == null || hold || MobileCannonLook.AtHandle(__instance)) { if (RuleLog.Due("MobileCannonUsePatch.Prefix:game", 2d)) ModLog.Every("MobileCannonUsePatch.Prefix:game", 2d, "MobileCannonUsePatch.Prefix: returns true (the cart's own grab) because gun == null || hold || MobileCannonLook.AtHandle(__instance) (line 32)"); return true; }
            __result = character is Player p && gun.Load(p);
            ModLog.Decision("mobile cannon E", "load", $"not at the handle - load started {__result}");
            return false;
        }
    }
}
