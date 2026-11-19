// kydl:read-only-observer - Humanoid here is only the local player who uses the musket; no creature is moved, commanded or spawned (the shot is a projectile)
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Musket.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// With the Iron Musket in hand, the local player's attack button fires one loaded cartridge (D-095); the secondary attack does nothing with the musket
    /// (the reload has its own key, [Musket] Reload Key, D-100). Every other weapon and every other character attacks as usual.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class MusketAttackPatch
    {
        static bool Prefix(Humanoid __instance, bool secondaryAttack, ref bool __result)
        {
            try
            {
                Player player = __instance as Player;
                if (player == null || player != Player.m_localPlayer || MusketGun.Held(player) == null) return true;
                if (!secondaryAttack) MusketGun.Fire(player);     // the secondary attack does nothing with the musket: reload has its own key (D-100)
                __result = false;
                return false;
            }
            catch (Exception e)
            {
                ModLog.Warn("MusketAttackPatch: " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual");
                return true;
            }
        }
    }
}
