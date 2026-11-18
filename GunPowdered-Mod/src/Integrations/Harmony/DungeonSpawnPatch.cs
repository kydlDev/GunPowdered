// kydl:read-only-observer - no creature is moved, commanded or spawned here; the patch only reads the rooms of a finished dungeon
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

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// Burial Chambers (dungeon DG_ForestCrypt, D-016: 10 % one deposit, 5 % two) and mountain caves (dungeon DG_Cave: 15 % of caves, 1-3 deposits) get their
    /// Bat Guano when the dungeon is generated. The rooms may still be loading when Generate returns, so the rooms are read once their number stops changing.
    /// Sunken crypts and the Hildir dungeons are left alone.
    /// </summary>
    [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Generate), new[] { typeof(int), typeof(ZoneSystem.SpawnMode) })]
    internal static class DungeonSpawnPatch
    {
        static void Postfix(DungeonGenerator __instance, int seed, ZoneSystem.SpawnMode mode)
        {
            try
            {
                if (mode != ZoneSystem.SpawnMode.Full || !ModSettings.FeatureOn(ModSettings.EnableGuano)) return;
                CaveKind? kind = KindOf(__instance.name);
                if (kind == null) return;
                Plugin.Instance.StartCoroutine(WhenRoomsAreThere(__instance, kind.Value, seed));
            }
            catch (Exception e)
            {
                ModLog.Warn("DungeonSpawnPatch: " + e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>Which kind of cave a dungeon prefab is, or null when it gets no guano (sunken crypts, Hildir dungeons, anything else).</summary>
        internal static CaveKind? KindOf(string dungeonName)
        {
            if (dungeonName.Contains("Hildir") || dungeonName.Contains("Sunken")) return null;
            if (dungeonName.StartsWith("DG_ForestCrypt") || dungeonName.StartsWith("DG_HalfBurried_ForestCrypt")) return CaveKind.TrollCaveOrBurialChamber;
            if (dungeonName.StartsWith("DG_Cave")) return CaveKind.MountainCave;
            return null;
        }

        static IEnumerator WhenRoomsAreThere(DungeonGenerator dungeon, CaveKind kind, int seed)
        {
            int last = -1;
            for (int i = 0; i < 30 && dungeon != null; i++)
            {
                int count = dungeon.GetComponentsInChildren<Room>().Length;
                if (count > 0 && count == last) break;
                last = count;
                yield return new WaitForSeconds(1f);
            }
            if (dungeon == null) yield break;

            var spots = new List<Vector3>();
            foreach (Room room in dungeon.GetComponentsInChildren<Room>()) spots.Add(room.transform.position);
            int placed = GuanoPlacer.Place(kind, spots, seed);
            ModLog.Decision("cave guano", placed + " deposit(s)", dungeon.name + " " + kind + ", seed " + seed + ", " + spots.Count + " rooms");
        }
    }
}
