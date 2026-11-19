// kydl:read-only-observer - Humanoid here is only the local player who sets dynamite down; no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Dynamite.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// With Dynamite in hand, the local player's attack button sets one stick down on the ground in front of them (D-100) instead of swinging it; the
    /// secondary attack does nothing. Every other item and every other character attacks as usual.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class DynamiteAttackPatch
    {
        static bool Prefix(Humanoid __instance, bool secondaryAttack, ref bool __result)
        {
            try
            {
                Player player = __instance as Player;
                if (player == null || player != Player.m_localPlayer || DynamiteSetup.Held(player) == null) return true;
                if (!secondaryAttack) DynamiteSetup.Place(player);
                __result = false;
                return false;
            }
            catch (Exception e)
            {
                ModLog.Warn("DynamiteAttackPatch: " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual");
                return true;
            }
        }
    }
}
