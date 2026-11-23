// kydl:read-only-observer - only reads whether the local player aims a cannon; no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Cannon.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// While the local player aims a mounted Hand Cannon, the attack button lights its fuse (torch in hand, D-053) instead of swinging the torch.
    /// Everyone else's attacks are untouched.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class CannonAttackPatch
    {
        static bool Prefix(Humanoid __instance, ref bool __result)
        {
            try
            {
                if (__instance != Player.m_localPlayer || CannonAim.Current == null) { if (RuleLog.Due("CannonAttackPatch.Prefix:21", 2d)) ModLog.Every("CannonAttackPatch.Prefix:21", 2d, $"CannonAttackPatch.Prefix: returns true because __instance != Player.m_localPlayer || CannonAim.Current == null (line 21)"); return true; }
                CannonAim.AttackPressed(Player.m_localPlayer);
                __result = false;
                { if (RuleLog.Due("CannonAttackPatch.Prefix:24", 2d)) ModLog.Every("CannonAttackPatch.Prefix:24", 2d, $"CannonAttackPatch.Prefix: returns false (line 24)"); return false; }
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("CannonAttackPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual" + "\n" + e.StackTrace);
                return true;
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("CannonAttackPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual" + "\n" + e.StackTrace);
                return true;
            }
            catch (Exception e)
            {
                ModLog.Warn("CannonAttackPatch: " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual" + "\n" + e.StackTrace);
                return true;
            }
        }
    }
}
