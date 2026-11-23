// kydl:read-only-observer - only changes the aim DIRECTION the local player's own throw gets; no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Grenades.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// The direction the game's Attack throws along (Humanoid.GetAimDir, called with the projectile's spawn point): for the local player's grenade throw
    /// it becomes the direction that lands on the chosen spot (D-050). Everything else keeps the look direction.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAimDir))]
    internal static class GrenadeAimDirPatch
    {
        static void Postfix(Humanoid __instance, Vector3 fromPoint, ref Vector3 __result)
        {
            try
            {
                if (__instance != Player.m_localPlayer) { if (RuleLog.Due("GrenadeAimDirPatch.Postfix:22", 2d)) ModLog.Every("GrenadeAimDirPatch.Postfix:22", 2d, $"GrenadeAimDirPatch.Postfix: returns because __instance != Player.m_localPlayer (line 22)"); return; }
                ItemDrop.ItemData weapon = Player.m_localPlayer.GetCurrentWeapon();
                if (!GrenadeSetup.IsGrenade(weapon)) { if (RuleLog.Due("GrenadeAimDirPatch.Postfix:24", 2d)) ModLog.Every("GrenadeAimDirPatch.Postfix:24", 2d, $"GrenadeAimDirPatch.Postfix: returns because !GrenadeSetup.IsGrenade(weapon) (line 24)"); return; }
                Vector3 aim;
                if (GrenadeAim.AimFor(fromPoint, weapon.m_shared.m_attack.m_launchAngle, out aim)) __result = aim;
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("GrenadeAimDirPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().FullName + " aiming the grenade: " + e.Message + " - the grenade goes along the look direction" + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("GrenadeAimDirPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().FullName + " aiming the grenade: " + e.Message + " - the grenade goes along the look direction" + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("GrenadeAimDirPatch: " + e.GetType().FullName + " aiming the grenade: " + e.Message + " - the grenade goes along the look direction" + "\n" + e.StackTrace);
            }
        }
    }
}
