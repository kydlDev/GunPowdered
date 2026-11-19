// kydl:read-only-observer - Humanoid here is only the local player who uses the musket; no creature is moved, commanded or spawned (the shot is a projectile)
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Musket.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// With the Iron Musket in hand, the local player's attack button fires one loaded cartridge and the secondary attack (the reload key, D-087) loads
    /// cartridges from the backpack (D-095). Every other weapon and every other character attacks as usual.
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
                if (secondaryAttack) MusketReloader.Begin(player);
                else MusketGun.Fire(player);
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
