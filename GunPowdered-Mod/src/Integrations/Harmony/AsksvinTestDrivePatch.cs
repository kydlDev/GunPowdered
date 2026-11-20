// kydl:read-only-observer - TEST ONLY: patched in at run time by the lab hook asksvinDriveTo; steers only the lab's own Asksvin during a drive, every other creature keeps the game's AI
using System;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// The straight drive of the TestLab slope test (D-124, Agent-9): the Asksvin's pathfinder found no way down the template cliff in 2 of 3 runs, so
    /// the test steers it in a straight line. Not a [HarmonyPatch] class on purpose: it is applied only when a lab test asks (AsksvinTowTestHooks.DriveTo),
    /// never in normal play. Moved here from the test hooks by Claude-Agent-10 (MIGRATION_DECISIONS A5: Harmony patches live in Integrations/Harmony).
    /// </summary>
    internal static class AsksvinTestDrivePatch
    {
        static Func<MonsterAI, Vector3?> _target;
        static bool _applied;

        /// <summary>Patches MonsterAI.UpdateAI once; <paramref name="target"/> answers where a creature is driven (null = the game's AI runs).</summary>
        internal static void Apply(Func<MonsterAI, Vector3?> target)
        {
            _target = target;
            if (_applied) { if (RuleLog.IsOn()) ModLog.Every("AsksvinTestDrivePatch.Apply:24", 2d, "AsksvinTestDrivePatch.Apply: returns (line 24)"); return; }
            var h = new Harmony("com.kydldev.GunPowdered.testdrive");
            h.Patch(AccessTools.Method(typeof(MonsterAI), nameof(MonsterAI.UpdateAI)), prefix: new HarmonyMethod(typeof(AsksvinTestDrivePatch), nameof(Prefix)));
            _applied = true;
            ModLog.Info("AsksvinTestDrivePatch: MonsterAI.UpdateAI patched for the lab's straight drive");
        }

        static bool Prefix(MonsterAI __instance, ref bool __result)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinTestDrivePatch.Prefix", 2d, $"AsksvinTestDrivePatch.Prefix(__instance={RuleLog.Shown(__instance)}, __result={RuleLog.Shown(__result)})");
            Vector3? to = _target != null ? _target(__instance) : null;
            if (to == null) { if (RuleLog.IsOn()) ModLog.Every("AsksvinTestDrivePatch.Prefix:35", 2d, "AsksvinTestDrivePatch.Prefix: returns true (line 35)"); return true; }
            Vector3 d = to.Value - __instance.transform.position;
            d.y = 0f;
            if (d.magnitude < 2f) __instance.StopMoving();
            else __instance.MoveTowards(d.normalized, true);
            __result = true;
            { if (RuleLog.IsOn()) ModLog.Every("AsksvinTestDrivePatch.Prefix:41", 2d, "AsksvinTestDrivePatch.Prefix: returns false (line 41)"); return false; }
        }
    }
}
