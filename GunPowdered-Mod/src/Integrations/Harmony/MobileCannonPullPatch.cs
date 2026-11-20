// kydl:read-only-observer - only the player hitched to a Mobile Cannon is slowed and turned; no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Features.MobileCannon.Domain;
using GunPowdered.Features.MobileCannon.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>D-105: while hitched to a Mobile Cannon the player walks at most MobileCannonPullSpeed (after the game's own speed modifiers).</summary>
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ApplyStatusEffectSpeedMods))]
    internal static class MobileCannonSpeedPatch
    {
        static readonly AccessTools.FieldRef<SEMan, Character> CharacterRef = AccessTools.FieldRefAccess<SEMan, Character>("m_character");

        static void Postfix(SEMan __instance, ref float speed)
        {
            try
            {
                Character c = CharacterRef(__instance);
                if (c == null || !c.IsPlayer() || MobileCannonPull.PulledBy(c) == null) { if (RuleLog.On) ModLog.Every("MobileCannonSpeedPatch.Postfix:24", 2d, "MobileCannonSpeedPatch.Postfix: returns (line 24)"); return; }
                speed = MobileCannonPullRules.CapSpeed(speed, ModSettings.MobileCannonPullSpeed.Value);
            }
            catch (NullReferenceException e)
            {
                ModLog.Warn("MobileCannonSpeedPatch: " + e.Message + " - speed unchanged");
            }
        }
    }

    /// <summary>D-106: the hitched player keeps facing the cannon (turns to it instead of to the walking direction), so walking back pulls and walking on pushes.</summary>
    [HarmonyPatch(typeof(Player), "AlwaysRotateCamera")]
    internal static class MobileCannonFacePatch
    {
        static void Postfix(Player __instance, ref bool __result)
        {
            if (RuleLog.On) ModLog.Every("MobileCannonFacePatch.Postfix", 2d, $"MobileCannonFacePatch.Postfix(__instance={RuleLog.Shown(__instance)}, __result={RuleLog.Shown(__result)})");
            if (!__result && MobileCannonPull.PulledBy(__instance) != null) __result = true;
        }
    }

    /// <summary>D-106: for the hitched player the turning target (the look yaw) is the cannon during the turn, then the real look yaw comes back.</summary>
    [HarmonyPatch(typeof(Character), "UpdateRotation")]
    internal static class MobileCannonTurnPatch
    {
        static readonly AccessTools.FieldRef<Character, Quaternion> LookYawRef = AccessTools.FieldRefAccess<Character, Quaternion>("m_lookYaw");

        static void Prefix(Character __instance, ref float turnSpeed, out Quaternion? __state)
        {
            if (RuleLog.On) ModLog.Every("MobileCannonTurnPatch.Prefix", 2d, $"MobileCannonTurnPatch.Prefix(__instance={RuleLog.Shown(__instance)}, turnSpeed={RuleLog.Shown(turnSpeed)})");
            __state = null;
            MobileCannonPull pull = __instance.IsPlayer() ? MobileCannonPull.PulledBy(__instance) : null;
            if (pull == null) { if (RuleLog.On) ModLog.Every("MobileCannonTurnPatch.Prefix:56", 2d, "MobileCannonTurnPatch.Prefix: returns (line 56)"); return; }
            turnSpeed = Mathf.Max(turnSpeed, __instance.m_turnSpeed);   // the game passes the turn speed through the same speed mods, so the walking cap would make turning crawl
            __state = LookYawRef(__instance);
            LookYawRef(__instance) = pull.FacingFor(__instance);
        }

        static void Postfix(Character __instance, Quaternion? __state)
        {
            if (RuleLog.On) ModLog.Every("MobileCannonTurnPatch.Postfix", 2d, $"MobileCannonTurnPatch.Postfix(__instance={RuleLog.Shown(__instance)}, __state={RuleLog.Shown(__state)})");
            if (__state.HasValue) LookYawRef(__instance) = __state.Value;
        }
    }
}
