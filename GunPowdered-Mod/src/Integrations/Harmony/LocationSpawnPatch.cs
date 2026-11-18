// kydl:read-only-observer - no creature is moved, commanded or spawned here; the patch only reads where locations were created
using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Features.Guano.Domain;
using GunPowdered.Features.Guano.Infrastructure;
using GunPowdered.Features.Sulfur.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// When the world creates a location for the first time (SpawnMode.Full, on the machine that generates it), the mod adds its deposits: Bat Guano in
    /// Troll Caves (D-016), Sulfurous Mud around geysers (FireHole) and in the water next to Draugr piles outside sunken crypts (D-015). Burial chambers
    /// and mountain caves are dungeons and are handled by <see cref="DungeonSpawnPatch"/>.
    /// Found in game 1.0.21 (D-030): a Full call returns NO object - it creates the networked pieces and a location proxy, and the proxy spawns the visible
    /// location again in Client mode. So the patch works from the Full call's location, position and rotation, reads spots from the location prefab, and
    /// places a moment later, when the rooms (a Troll cave's interior is up in the sky, entered by teleport) and the terrain have their colliders.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "SpawnLocation")]
    internal static class LocationSpawnPatch
    {
        const string TrollCave = "TrollCave", Geyser = "FireHole", SunkenCrypt = "SunkenCrypt", DraugrPile = "Spawner_DraugrPile";
        const float SettleSeconds = 2f;

        // __args: location, seed, position, rotation, mode, ghost objects, cheated (checked by type so a changed signature skips instead of breaking).
        // The asked mode is read in the Prefix, before the game runs.
        static void Prefix(object[] __args, ref int __state)
        {
            __state = __args != null && __args.Length > 4 && __args[4] is ZoneSystem.SpawnMode ? (int)(ZoneSystem.SpawnMode)__args[4] : -1;
        }

        static void Postfix(object[] __args, int __state)
        {
            try
            {
                if (__state != (int)ZoneSystem.SpawnMode.Full || __args == null || __args.Length < 5) return;
                var loc = __args[0] as ZoneSystem.ZoneLocation;
                if (loc == null || !(__args[1] is int) || !(__args[2] is Vector3) || !(__args[3] is Quaternion)) return;
                string name = loc.m_prefabName ?? "";
                int seed = (int)__args[1];
                var pos = (Vector3)__args[2];
                var rot = (Quaternion)__args[3];

                bool troll = name.StartsWith(TrollCave) && ModSettings.FeatureOn(ModSettings.EnableGuano);
                bool geyser = name == Geyser && ModSettings.FeatureOn(ModSettings.EnableSulfur);
                bool piles = !troll && !geyser && !name.StartsWith(SunkenCrypt) && ModSettings.FeatureOn(ModSettings.EnableSulfur);
                if (!troll && !geyser && !piles) return;

                List<Vector3> spots = troll ? Spots(loc, pos, rot, true) : (piles ? Spots(loc, pos, rot, false) : null);
                if (piles && spots.Count == 0) return;                                   // most locations have no Draugr pile: nothing to do
                ModLog.Every("loc-full", 10d, "LocationSpawnPatch: " + name + " generated at " + pos.ToString("0") + ", " + (spots != null ? spots.Count : 0) + " spot(s)");
                Plugin.Instance.StartCoroutine(PlaceLater(name, troll, geyser, pos, spots, seed));
            }
            catch (Exception e)
            {
                ModLog.Warn("LocationSpawnPatch: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // Spots from the location prefab: the Troll cave's room pieces under "Interior", or its Draugr piles; turned into world positions of this copy.
        static List<Vector3> Spots(ZoneSystem.ZoneLocation loc, Vector3 pos, Quaternion rot, bool rooms)
        {
            var spots = new List<Vector3>();
            if (loc.m_prefab == null || !loc.m_prefab.IsLoaded) return spots;
            Transform root = loc.m_prefab.Asset.transform;
            Transform from = rooms ? root.Find("Interior") : root;
            if (from == null) return spots;
            foreach (Transform t in from.GetComponentsInChildren<Transform>(true))
            {
                bool take = rooms ? (t.name.StartsWith("room") || (t.parent != null && t.parent.name.StartsWith("room"))) : t.name.StartsWith(DraugrPile);
                if (take) spots.Add(pos + rot * root.InverseTransformPoint(t.position));
            }
            return spots;
        }

        static IEnumerator PlaceLater(string name, bool troll, bool geyser, Vector3 pos, List<Vector3> spots, int seed)
        {
            yield return new WaitForSeconds(SettleSeconds);
            Physics.SyncTransforms();
            try
            {
                if (troll)
                {
                    int placed = GuanoPlacer.Place(CaveKind.TrollCaveOrBurialChamber, spots, seed);
                    ModLog.Decision("troll cave guano", placed + " deposit(s)", name + " seed " + seed + ", " + spots.Count + " spots");
                }
                else if (geyser)
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
