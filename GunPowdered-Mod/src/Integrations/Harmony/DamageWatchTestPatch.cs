// kydl:read-only-observer - TEST ONLY: patched in at run time by the lab hook creatureDamageWatch; logs the hits one watched creature takes, changes nothing
using System;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// T-009l diagnostic (Claude-Agent-10): the dynamite charges hurt a lab Troll about 2.44x more than configured. This logs every hit the watched
    /// creature takes as it arrives (before the game's backstab / stagger / resistances) and its health right after, so a run shows how many hits there
    /// are and what each carried. Not a [HarmonyPatch] class: applied only when a lab test asks, never in normal play.
    /// </summary>
    internal static class DamageWatchTestPatch
    {
        static Func<Character, bool> _watch;
        static bool _applied;
        static int _hits;

        /// <summary>Patches Character.RPC_Damage once; <paramref name="watch"/> says which creature's hits are logged (null = none).</summary>
        internal static void Apply(Func<Character, bool> watch)
        {
            _watch = watch;
            _hits = 0;
            if (_applied) { if (RuleLog.Due("DamageWatchTestPatch.Apply:26", 2d)) ModLog.Every("DamageWatchTestPatch.Apply:26", 2d, "DamageWatchTestPatch.Apply: returns (line 26)"); return; }
            var h = new Harmony("com.kydldev.GunPowdered.damagewatch");
            var target = AccessTools.Method(typeof(Character), "RPC_Damage");
            h.Patch(target, prefix: new HarmonyMethod(typeof(DamageWatchTestPatch), nameof(Prefix)), postfix: new HarmonyMethod(typeof(DamageWatchTestPatch), nameof(Postfix)));
            _applied = true;
            ModLog.Info("DamageWatchTestPatch: Character.RPC_Damage watched for the lab");
        }

        internal static int Hits => _hits;

        static void Prefix(Character __instance, HitData hit)
        {
            if (_watch == null || hit == null || !_watch(__instance)) { if (RuleLog.Due("DamageWatchTestPatch.Prefix:38", 2d)) ModLog.Every("DamageWatchTestPatch.Prefix:38", 2d, "DamageWatchTestPatch.Prefix: returns (line 38)"); return; }
            _hits++;
            HitData.DamageTypes d = hit.m_damage;
            ModLog.Info("DamageWatch: hit " + _hits + " on " + __instance.name + " at " + Time.time.ToString("0.000") + " - blunt " + d.m_blunt.ToString("0.0") + " pierce " + d.m_pierce.ToString("0.0")
                + " chop " + d.m_chop.ToString("0.0") + " pickaxe " + d.m_pickaxe.ToString("0.0") + " fire " + d.m_fire.ToString("0.0") + ", backstab bonus " + hit.m_backstabBonus.ToString("0.0")
                + ", staggering " + __instance.IsStaggering() + ", collider " + (hit.m_hitCollider != null ? hit.m_hitCollider.name : "none") + ", health before " + __instance.GetHealth().ToString("0.0"));
        }

        static void Postfix(Character __instance, HitData hit)
        {
            if (_watch == null || hit == null || !_watch(__instance)) { if (RuleLog.Due("DamageWatchTestPatch.Postfix:48", 2d)) ModLog.Every("DamageWatchTestPatch.Postfix:48", 2d, "DamageWatchTestPatch.Postfix: returns (line 48)"); return; }
            ModLog.Info("DamageWatch: " + __instance.name + " health after " + __instance.GetHealth().ToString("0.0"));
        }
    }
}
