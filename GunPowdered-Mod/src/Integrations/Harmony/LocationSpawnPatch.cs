// kydl:read-only-observer - no creature is moved, commanded or spawned here; the patch only reads where locations were created
using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Features.Sulfur.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// When the world creates a location for the first time (SpawnMode.Full, on the machine that generates it), the mod adds its Sulfurous Mud around
    /// geysers (FireHole) and in the water next to Draugr piles outside sunken crypts (D-015). Cave guano (Troll caves, burial chambers, mountain caves)
    /// is placed when a player first goes in, by CaveEntryPatch (D-033).
    /// Found in game 1.0.21 (D-030): a Full call returns NO object - it creates the networked pieces and a location proxy, and the proxy spawns the visible
    /// location again in Client mode. So the patch works from the Full call's location, position and rotation, reads spots from the location prefab, and
    /// places a moment later, when the rooms (a Troll cave's interior is up in the sky, entered by teleport) and the terrain have their colliders.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "SpawnLocation")]
    internal static class LocationSpawnPatch
    {
        const string Geyser = "FireHole", SunkenCrypt = "SunkenCrypt", DraugrPile = "Spawner_DraugrPile";
        const float SettleSeconds = 2f;

        // __args: location, seed, position, rotation, mode, ghost objects, cheated (checked by type so a changed signature skips instead of breaking).
        // The asked mode is read in the Prefix, before the game runs.
        static void Prefix(object[] __args, ref int __state)
        {
            if (RuleLog.Due("LocationSpawnPatch.Prefix", 2d)) ModLog.Every("LocationSpawnPatch.Prefix", 2d, $"LocationSpawnPatch.Prefix(__args={RuleLog.Shown(__args)}, __state={RuleLog.Shown(__state)})");
            __state = __args != null && __args.Length > 4 && __args[4] is ZoneSystem.SpawnMode ? (int)(ZoneSystem.SpawnMode)__args[4] : -1;
        }

        static void Postfix(object[] __args, int __state)
        {
            try
            {
                if (__state != (int)ZoneSystem.SpawnMode.Full || __args == null || __args.Length < 5) { if (RuleLog.Due("LocationSpawnPatch.Postfix:41", 2d)) ModLog.Every("LocationSpawnPatch.Postfix:41", 2d, "LocationSpawnPatch.Postfix: returns (line 41)"); return; }
                var loc = __args[0] as ZoneSystem.ZoneLocation;
                if (loc == null || !(__args[1] is int) || !(__args[2] is Vector3) || !(__args[3] is Quaternion)) { if (RuleLog.Due("LocationSpawnPatch.Postfix:43", 2d)) ModLog.Every("LocationSpawnPatch.Postfix:43", 2d, "LocationSpawnPatch.Postfix: returns (line 43)"); return; }
                string name = loc.m_prefabName ?? "";
                int seed = (int)__args[1];
                var pos = (Vector3)__args[2];
                var rot = (Quaternion)__args[3];

                bool geyser = name == Geyser && ModSettings.FeatureOn(ModSettings.EnableSulfur);
                bool piles = !geyser && !name.StartsWith(SunkenCrypt) && ModSettings.FeatureOn(ModSettings.EnableSulfur);
                if (!geyser && !piles) { if (RuleLog.Due("LocationSpawnPatch.Postfix:51", 2d)) ModLog.Every("LocationSpawnPatch.Postfix:51", 2d, "LocationSpawnPatch.Postfix: returns (line 51)"); return; }

                List<Vector3> spots = piles ? Spots(loc, pos, rot) : null;
                if (piles && spots.Count == 0) { if (RuleLog.Due("LocationSpawnPatch.Postfix:54", 2d)) ModLog.Every("LocationSpawnPatch.Postfix:54", 2d, "LocationSpawnPatch.Postfix: returns (line 54)"); return; }                                   // most locations have no Draugr pile: nothing to do
                if (RuleLog.Due("loc-full", 10d)) ModLog.Every("loc-full", 10d, "LocationSpawnPatch: " + name + " generated at " + pos.ToString("0") + ", " + (spots != null ? spots.Count : 0) + " spot(s)");
                Plugin.Instance.StartCoroutine(PlaceLater(name, geyser, pos, spots, seed));
            }
            catch (Exception e)
            {
                ModLog.Warn("LocationSpawnPatch: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // The Draugr piles of the location prefab, turned into world positions of this copy.
        static List<Vector3> Spots(ZoneSystem.ZoneLocation loc, Vector3 pos, Quaternion rot)
        {
            if (RuleLog.Due("LocationSpawnPatch.Spots", 2d)) ModLog.Every("LocationSpawnPatch.Spots", 2d, $"LocationSpawnPatch.Spots(loc={RuleLog.Shown(loc)}, pos={RuleLog.Shown(pos)}, rot={RuleLog.Shown(rot)})");
            var spots = new List<Vector3>();
            if (loc.m_prefab == null || !loc.m_prefab.IsLoaded) { var returnedAt69 = spots; if (RuleLog.Due("LocationSpawnPatch.Spots:69", 2d)) ModLog.Every("LocationSpawnPatch.Spots:69", 2d, $"LocationSpawnPatch.Spots: returns {RuleLog.Shown(returnedAt69)} (line 69)"); return returnedAt69; }
            Transform root = loc.m_prefab.Asset.transform;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(DraugrPile)) spots.Add(pos + rot * root.InverseTransformPoint(t.position));
            { var returnedAt73 = spots; if (RuleLog.Due("LocationSpawnPatch.Spots:73", 2d)) ModLog.Every("LocationSpawnPatch.Spots:73", 2d, $"LocationSpawnPatch.Spots: returns {RuleLog.Shown(returnedAt73)} (line 73)"); return returnedAt73; }
        }

        static IEnumerator PlaceLater(string name, bool geyser, Vector3 pos, List<Vector3> spots, int seed)
        {
            yield return new WaitForSeconds(SettleSeconds);
            Physics.SyncTransforms();
            try
            {
                if (geyser)
                {
                    int placed = MudPlacer.AroundGeyser(pos, seed);
                    ModLog.Decision("geyser mud", placed + " deposit(s)", "at " + pos.ToString("0") + ", seed " + seed);
                }
                else
                {
                    int placed = 0;
                    for (int n = 0; n < spots.Count; n++) placed += MudPlacer.NearPile(spots[n], seed + n);
                    ModLog.Decision("draugr pile mud", placed + " deposit(s)", name + ", " + spots.Count + " pile(s)");
                }
            }
            catch (Exception e)
            {
                ModLog.Warn("LocationSpawnPatch.PlaceLater: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
