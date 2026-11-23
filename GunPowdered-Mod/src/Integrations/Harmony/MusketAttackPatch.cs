// kydl:read-only-observer - Humanoid here is only the local player who uses the musket; no creature is moved, commanded or spawned (the shot is a projectile)
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Musket.Infrastructure;
using GunPowdered.Core;

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
                if (player == null || player != Player.m_localPlayer || MusketGun.Held(player) == null) { if (RuleLog.Due("MusketAttackPatch.Prefix:22", 2d)) ModLog.Every("MusketAttackPatch.Prefix:22", 2d, $"MusketAttackPatch.Prefix: returns true because player == null || player != Player.m_localPlayer || MusketGun.Held(player) == null (line 22)"); return true; }
                if (!secondaryAttack) MusketGun.Fire(player);     // the secondary attack does nothing with the musket: reload has its own key (D-100)
                __result = false;
                { if (RuleLog.Due("MusketAttackPatch.Prefix:25", 2d)) ModLog.Every("MusketAttackPatch.Prefix:25", 2d, $"MusketAttackPatch.Prefix: returns false (line 25)"); return false; }
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("MusketAttackPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual" + "\n" + e.StackTrace);
                return true;
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("MusketAttackPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual" + "\n" + e.StackTrace);
                return true;
            }
            catch (Exception e)
            {
                ModLog.Warn("MusketAttackPatch: " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual" + "\n" + e.StackTrace);
                return true;
            }
        }
    }
}
