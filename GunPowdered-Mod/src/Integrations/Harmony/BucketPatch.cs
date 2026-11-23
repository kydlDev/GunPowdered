// kydl:read-only-observer - Humanoid here is only the player who uses the item; no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Water.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>Using a held Bucket (the attack button) fills or empties it instead of swinging it (D-008).</summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class BucketPatch
    {
        static bool Prefix(Humanoid __instance, ref bool __result)
        {
            try
            {
                Player player = __instance as Player;
                if (player == null || player != Player.m_localPlayer) { if (RuleLog.Due("BucketPatch.Prefix:19", 2d)) ModLog.Every("BucketPatch.Prefix:19", 2d, $"BucketPatch.Prefix: returns true because player == null || player != Player.m_localPlayer (line 19)"); return true; }
                if (!BucketUse.Use(player, player.GetCurrentWeapon())) { if (RuleLog.Due("BucketPatch.Prefix:20", 2d)) ModLog.Every("BucketPatch.Prefix:20", 2d, $"BucketPatch.Prefix: returns true because !BucketUse.Use(player, player.GetCurrentWeapon()) (line 20)"); return true; }
                __result = false;
                { if (RuleLog.Due("BucketPatch.Prefix:22", 2d)) ModLog.Every("BucketPatch.Prefix:22", 2d, $"BucketPatch.Prefix: returns false (line 22)"); return false; }
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("BucketPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                return true;
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("BucketPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                return true;
            }
            catch (Exception e)
            {
                ModLog.Warn("BucketPatch: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                return true;
            }
        }
    }
}
