// kydl:read-only-observer - Humanoid here is only the local player who sets dynamite down; no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Dynamite.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// With Dynamite in hand, the local player's attack button sets one stick down on the ground in front of them (D-100) instead of swinging it; the
    /// secondary attack does nothing. Every other item and every other character attacks as usual.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class DynamiteAttackPatch
    {
        /// <summary>When the local player last set a charge down (D-183: the press that set the last one down must not become a punch).</summary>
        static float _placedAt = -100f;
        static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> RightItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_rightItem");
        static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> LeftItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_leftItem");

        static bool Prefix(Humanoid __instance, bool secondaryAttack, ref bool __result)
        {
            try
            {
                Player player = __instance as Player;
                if (player != null && player == Player.m_localPlayer && DynamiteSetup.Held(player) == null
                    && GunPowdered.Features.Dynamite.Domain.DynamiteRules.SwallowsPunch(UnityEngine.Time.time - _placedAt, RightItem(player) == null && LeftItem(player) == null))
                {
                    __result = false;
                    ModLog.Decision("attack after setting a charge down", "swallowed", $"empty hands {UnityEngine.Time.time - _placedAt:0.00} s after the charge was set down - no punch (D-183)");
                    return false;
                }
                if (player == null || player != Player.m_localPlayer || DynamiteSetup.Held(player) == null) { if (RuleLog.Due("DynamiteAttackPatch.Prefix:22", 2d)) ModLog.Every("DynamiteAttackPatch.Prefix:22", 2d, $"DynamiteAttackPatch.Prefix: returns true because player == null || player != Player.m_localPlayer || DynamiteSetup.Held(player) == null (line 22)"); return true; }
                if (!secondaryAttack) { DynamiteSetup.Place(player); _placedAt = UnityEngine.Time.time; }
                __result = false;
                { if (RuleLog.Due("DynamiteAttackPatch.Prefix:25", 2d)) ModLog.Every("DynamiteAttackPatch.Prefix:25", 2d, $"DynamiteAttackPatch.Prefix: returns false (line 25)"); return false; }
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("DynamiteAttackPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual" + "\n" + e.StackTrace);
                return true;
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("DynamiteAttackPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual" + "\n" + e.StackTrace);
                return true;
            }
            catch (Exception e)
            {
                ModLog.Warn("DynamiteAttackPatch: " + e.GetType().FullName + ": " + e.Message + " - the attack goes on as usual" + "\n" + e.StackTrace);
                return true;
            }
        }
    }
}
