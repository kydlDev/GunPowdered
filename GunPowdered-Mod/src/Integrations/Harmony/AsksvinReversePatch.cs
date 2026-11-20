// kydl:read-only-observer - D-144: an Asksvin backed up by its rider or led by the chest rope; every other creature and the game's own riding are unchanged
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>D-144: the animal's owner drives an Asksvin that backs up (rider's S) or is led by the chest rope instead of its AI for that step.</summary>
    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    internal static class AsksvinReverseAiPatch
    {
        static bool Prefix(MonsterAI __instance, float dt, ref bool __result)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinReverseAiPatch.Prefix:in", 2d, "AsksvinReverseAiPatch.Prefix(" + RuleLog.Shown(__instance) + ")");
            AsksvinReverse r = AsksvinReverse.Of(__instance);
            if (r == null || !r.Drive(dt)) { if (RuleLog.IsOn()) ModLog.Every("AsksvinReverseAiPatch.Prefix:18", 2d, "AsksvinReverseAiPatch.Prefix: returns true (line 18)"); return true; }
            __result = true;
            if (RuleLog.IsOn()) ModLog.Every("AsksvinReverseAiPatch.Prefix", 2d, "AsksvinReverseAiPatch.Prefix: " + __instance.name + " driven by AsksvinReverse - returns false (AI skipped)");
            return false;
        }
    }

    /// <summary>D-144: the rider's S (and A / D while backing) reach the ridden Asksvin's owner; the game's own controls still run.</summary>
    [HarmonyPatch(typeof(Sadle), nameof(Sadle.ApplyControlls))]
    internal static class AsksvinReverseRidePatch
    {
        static void Prefix(Sadle __instance, Vector3 moveDir, bool run)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinReverseRidePatch.Prefix:in", 2d, "AsksvinReverseRidePatch.Prefix(moveDir=" + RuleLog.Shown(moveDir) + ", run=" + run + ")");
            AsksvinReverse r = __instance != null ? __instance.GetComponentInParent<AsksvinReverse>() : null;
            if (r != null) r.RiderInput(moveDir, run);
            if (RuleLog.IsOn()) ModLog.Every("AsksvinReverseRidePatch.Prefix", 2d, "AsksvinReverseRidePatch.Prefix: Asksvin " + (r != null) + " - returns");
        }
    }

    /// <summary>D-144: a backing Asksvin keeps facing ahead (the game turns every creature towards where it moves, which would turn it round).</summary>
    [HarmonyPatch(typeof(Character), "UpdateRotation")]
    internal static class AsksvinReverseTurnPatch
    {
        static void Prefix(Character __instance, out Vector3 __state)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinReverseTurnPatch.Prefix", 2d, "AsksvinReverseTurnPatch.Prefix(backing " + AsksvinReverse.Backing.Count + ")");
            __state = Vector3.zero;
            if (AsksvinReverse.Backing.Count == 0 || !AsksvinReverse.Backing.Contains(__instance)) { if (RuleLog.IsOn()) ModLog.Every("AsksvinReverseTurnPatch.Prefix:46", 2d, "AsksvinReverseTurnPatch.Prefix: returns (line 46)"); return; }
            __state = AsksvinReverse.MoveDir(__instance);
            AsksvinReverse.MoveDir(__instance) = Vector3.zero;          // the turn then follows the look direction (straight ahead)
        }

        static void Postfix(Character __instance, Vector3 __state)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinReverseTurnPatch.Postfix", 2d, "AsksvinReverseTurnPatch.Postfix(restore " + (__state != Vector3.zero) + ")");
            if (__state != Vector3.zero) AsksvinReverse.MoveDir(__instance) = __state;
        }
    }
}
