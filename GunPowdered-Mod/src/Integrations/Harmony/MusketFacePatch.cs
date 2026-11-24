using GunPowdered.Core;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Features.Musket.Infrastructure;
using HarmonyLib;
using KydlCore.Common;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// D-182: a player with the musket in both hands always turns with the camera (the game's own "always rotate" path, used while blocking or drawing a
    /// bow): the body faces the aim and the direction keys walk sideways / backwards, so the musket shoots where it points (<see cref="MusketPoseRules.FacesAim"/>).
    /// </summary>
    [HarmonyPatch(typeof(Player), "AlwaysRotateCamera")]
    internal static class MusketFacePatch
    {
        static void Postfix(Player __instance, ref bool __result)
        {
            if (__result || __instance == null) { if (RuleLog.Due("MusketFacePatch.Postfix:skip", 2d)) ModLog.Every("MusketFacePatch.Postfix:skip", 2d, "MusketFacePatch.Postfix: returns because __result || __instance == null (line 18)"); return; }
            MusketRig rig = __instance.GetComponent<MusketRig>();
            bool faces = MusketPoseRules.FacesAim(rig != null && MusketGun.Held(__instance) != null, rig != null && rig.Raised);
            __result = faces;
            if (RuleLog.Due("MusketFacePatch.Postfix", 2d)) ModLog.Every("MusketFacePatch.Postfix", 2d, $"MusketFacePatch.Postfix: {__instance.GetPlayerName()} faces the aim {faces} - returns (line 22)");
        }
    }
}
