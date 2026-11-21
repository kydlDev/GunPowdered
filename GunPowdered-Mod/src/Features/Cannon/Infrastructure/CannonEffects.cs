using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Features.Cannon.Infrastructure
{
    /// <summary>
    /// The Hand Cannon's looks and sounds, all vanilla effect prefabs (names checked in game 2026-11-18, probe run): sparks and smoke at the touch hole
    /// while the fuse burns (D-052 addendum), the muzzle blast, and the spark burst at every ball impact (D-054: pass-through and final; purely
    /// visual, nothing is set on fire). These vanilla effects carry a network view: ONE client (the one that lit the fuse / flies the ball) makes them and
    /// every other client sees them through the network, as the game does with its own hit effects.
    /// </summary>
    internal static class CannonEffects
    {
        // sparks (the torch's own hit sparks) and a small grey puff (stone dust): vfx_FireAddFuel was a flame burst, too much like fire (run cannonV)
        static readonly string[] Fuse = { "vfx_torch_hit", "vfx_RockHit", "sfx_FireAddFuel" };
        static readonly string[] Muzzle = { "fx_turret_fire", "vfx_arbalest_fire", "sfx_arbalest_fire" };
        static readonly string[] Impact = { "vfx_HitSparks", "vfx_sledge_iron_hit", "sfx_sledge_iron_hit" };
        static readonly HashSet<string> Refused = new HashSet<string>();

        /// <summary>How many effect bursts were played since the start (test hooks read them).</summary>
        internal static int FuseCount { get; private set; }
        internal static int MuzzleCount { get; private set; }
        internal static int ImpactCount { get; private set; }

        /// <summary>Sparks and smoke out of the touch hole. Expected outcome: the number of effect objects made (0 when none of the prefabs exists).</summary>
        internal static int PlayFuse(Vector3 at)
        {
            if (RuleLog.Due("CannonEffects.PlayFuse", 2d)) ModLog.Every("CannonEffects.PlayFuse", 2d, $"CannonEffects.PlayFuse(at={RuleLog.Shown(at)})");
            FuseCount++;
            { var returnedAt32 = Play(Fuse, at, Quaternion.LookRotation(Vector3.up)); if (RuleLog.Due("CannonEffects.PlayFuse:32", 2d)) ModLog.Every("CannonEffects.PlayFuse:32", 2d, $"CannonEffects.PlayFuse: returns {RuleLog.Shown(returnedAt32)} (line 32)"); return returnedAt32; }
        }

        /// <summary>The blast out of the muzzle, along the shot. Expected outcome: the number of effect objects made.</summary>
        internal static int PlayMuzzle(Vector3 at, Vector3 forward)
        {
            if (RuleLog.Due("CannonEffects.PlayMuzzle", 2d)) ModLog.Every("CannonEffects.PlayMuzzle", 2d, $"CannonEffects.PlayMuzzle(at={RuleLog.Shown(at)}, forward={RuleLog.Shown(forward)})");
            MuzzleCount++;
            { var returnedAt40 = Play(Muzzle, at, Quaternion.LookRotation(forward.sqrMagnitude > 0f ? forward : Vector3.forward)); if (RuleLog.Due("CannonEffects.PlayMuzzle:40", 2d)) ModLog.Every("CannonEffects.PlayMuzzle:40", 2d, $"CannonEffects.PlayMuzzle: returns {RuleLog.Shown(returnedAt40)} (line 40)"); return returnedAt40; }
        }

        /// <summary>The spark burst where the ball hits something. Expected outcome: the number of effect objects made.</summary>
        internal static int PlayImpact(Vector3 at, Vector3 normal)
        {
            if (RuleLog.Due("CannonEffects.PlayImpact", 2d)) ModLog.Every("CannonEffects.PlayImpact", 2d, $"CannonEffects.PlayImpact(at={RuleLog.Shown(at)}, normal={RuleLog.Shown(normal)})");
            ImpactCount++;
            { var returnedAt48 = Play(Impact, at, Quaternion.LookRotation(normal.sqrMagnitude > 0f ? normal : Vector3.up)); if (RuleLog.Due("CannonEffects.PlayImpact:48", 2d)) ModLog.Every("CannonEffects.PlayImpact:48", 2d, $"CannonEffects.PlayImpact: returns {RuleLog.Shown(returnedAt48)} (line 48)"); return returnedAt48; }
        }

        static int Play(string[] names, Vector3 at, Quaternion rotation)
        {
            if (ZNetScene.instance == null) { if (RuleLog.Due("CannonEffects.Play:53", 2d)) ModLog.Every("CannonEffects.Play:53", 2d, "CannonEffects.Play: returns 0 (line 53)"); return 0; }
            int made = 0;
            foreach (string n in names)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(n);
                if (prefab == null)
                {
                    if (Refused.Add(n)) ModLog.Warn("CannonEffects: effect " + n + " not found, left out");
                    continue;
                }
                Object.Instantiate(prefab, at, rotation);
                made++;
            }
            { var returnedAt66 = made; if (RuleLog.Due("CannonEffects.Play:66", 2d)) ModLog.Every("CannonEffects.Play:66", 2d, $"CannonEffects.Play: returns {RuleLog.Shown(returnedAt66)} (line 66)"); return returnedAt66; }
        }
    }
}
