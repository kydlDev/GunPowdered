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
                // T-033 diagnosis (a18spots3: two real geysers got no mud and no placement log): every geyser / grave spawn, with its mode (Full is the
                // first generation, the only one that places mud), so a missing placement can be traced.
                if (__args != null && __args.Length > 2 && __args[0] is ZoneSystem.ZoneLocation zl0 && __args[2] is Vector3 p0 && (zl0.m_prefabName == Geyser || (zl0.m_prefabName ?? "").StartsWith("Grave")))
                    ModLog.Verbose("LocationSpawnPatch: " + zl0.m_prefabName + " spawn at " + p0.ToString("0") + ", mode " + (__state >= 0 ? ((ZoneSystem.SpawnMode)__state).ToString() : "?"));
                if (__state != (int)ZoneSystem.SpawnMode.Full || __args == null || __args.Length < 5) return;
                var loc = __args[0] as ZoneSystem.ZoneLocation;
                if (loc == null || !(__args[1] is int) || !(__args[2] is Vector3) || !(__args[3] is Quaternion)) { if (RuleLog.Due("LocationSpawnPatch.Postfix:47", 2d)) ModLog.Every("LocationSpawnPatch.Postfix:47", 2d, $"LocationSpawnPatch.Postfix: returns because loc == null || !(__args[1] is int) || !(__args[2] is Vector3) || !(__args[3] is Quaternion) (line 47)"); return; }
                string name = loc.m_prefabName ?? "";
                int seed = (int)__args[1];
                var pos = (Vector3)__args[2];
                var rot = (Quaternion)__args[3];

                // D-169: the mud is placed by LocationProxyMudPatch when the location first appears near a player (any spawn mode), not here:
                // a zone generated at the edge of the loaded area spawns its locations in Ghost mode, and those never got mud (a18mud1).
                if (RuleLog.Due("loc-full", 10d)) ModLog.Every("loc-full", 10d, "LocationSpawnPatch: " + name + " generated (Full) at " + pos.ToString("0") + ", seed " + seed + ", rotation " + rot.eulerAngles.y.ToString("0") + " - mud follows when it appears near a player");
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("LocationSpawnPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("LocationSpawnPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("LocationSpawnPatch: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// D-169: places the mod's mud for one location copy (geyser: around it; other locations except sunken crypts: next to their Draugr piles, in water).
        /// Expected outcome: true when a placement was started.
        /// </summary>
        internal static bool Plan(ZoneSystem.ZoneLocation loc, int seed, Vector3 pos, Quaternion rot, string why)
        {
            string name = loc != null ? loc.m_prefabName ?? "" : "";
            bool geyser = name == Geyser && ModSettings.FeatureOn(ModSettings.EnableSulfur);
            bool piles = !geyser && name.Length > 0 && !name.StartsWith(SunkenCrypt) && ModSettings.FeatureOn(ModSettings.EnableSulfur);
            if (!geyser && !piles) { if (RuleLog.Due("LocationSpawnPatch.Plan:none", 2d)) ModLog.Every("LocationSpawnPatch.Plan:none", 2d, "LocationSpawnPatch.Plan: returns false (" + name + ": no mud location)"); return false; }
            List<Vector3> spots = piles ? Spots(loc, pos, rot) : null;
            if (piles && spots.Count == 0) { if (RuleLog.Due("LocationSpawnPatch.Plan:nopile", 2d)) ModLog.Every("LocationSpawnPatch.Plan:nopile", 2d, "LocationSpawnPatch.Plan: returns false (" + name + ": no Draugr pile)"); return false; }
            Plugin.Instance.StartCoroutine(PlaceLater(name, geyser, pos, spots, seed));
            ModLog.Verbose("LocationSpawnPatch.Plan: " + name + " at " + pos.ToString("0") + " (" + why + "), " + (spots != null ? spots.Count : 0) + " pile(s) - returns true");
            return true;
        }

        // The Draugr piles of the location prefab, turned into world positions of this copy.
        static List<Vector3> Spots(ZoneSystem.ZoneLocation loc, Vector3 pos, Quaternion rot)
        {
            if (RuleLog.Due("LocationSpawnPatch.Spots", 2d)) ModLog.Every("LocationSpawnPatch.Spots", 2d, $"LocationSpawnPatch.Spots(loc={RuleLog.Shown(loc)}, pos={RuleLog.Shown(pos)}, rot={RuleLog.Shown(rot)})");
            var spots = new List<Vector3>();
            if (loc.m_prefab == null || !loc.m_prefab.IsLoaded) { var returnedAt93 = spots; if (RuleLog.Due("LocationSpawnPatch.Spots:93", 2d)) ModLog.Every("LocationSpawnPatch.Spots:93", 2d, $"LocationSpawnPatch.Spots: returns {returnedAt93} because loc.m_prefab == null || !loc.m_prefab.IsLoaded (line 93)"); return returnedAt93; }
            Transform root = loc.m_prefab.Asset.transform;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith(DraugrPile)) spots.Add(pos + rot * root.InverseTransformPoint(t.position));
            { var returnedAt97 = spots; if (RuleLog.Due("LocationSpawnPatch.Spots:97", 2d)) ModLog.Every("LocationSpawnPatch.Spots:97", 2d, $"LocationSpawnPatch.Spots: returns {returnedAt97} (line 97)"); return returnedAt97; }
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
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("LocationSpawnPatch.PlaceLater: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("LocationSpawnPatch.PlaceLater: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("LocationSpawnPatch.PlaceLater: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
    /// <summary>
    /// D-169 (lab a18mud1: geysers and swamp graves whose zone was generated at the edge of the loaded area spawned in Ghost mode and never got mud):
    /// the mud goes in when the location's proxy first shows the location near a player - whatever mode generated its zone - once per location
    /// (marker gp_mud_done on the proxy's ZDO), by the proxy's owner. A world that had the location before the mod gets its mud on the first visit.
    /// </summary>
    [HarmonyPatch(typeof(LocationProxy), "SpawnLocation")]
    internal static class LocationProxyMudPatch
    {
        internal const string DoneKey = "gp_mud_done";
        static readonly AccessTools.FieldRef<LocationProxy, ZNetView> View = AccessTools.FieldRefAccess<LocationProxy, ZNetView>("m_nview");

        // The proxy's ZDO keeps the location as the stable hash of its prefab name (what ZoneSystem stores in ZDOVars.s_location).
        static ZoneSystem.ZoneLocation ByHash(int hash)
        {
            if (ZoneSystem.instance != null)
                foreach (ZoneSystem.ZoneLocation l in ZoneSystem.instance.m_locations)
                    if (l != null && (l.m_prefabName ?? "").GetStableHashCode() == hash)
                    {
                        if (RuleLog.Due("LocationProxyMudPatch.ByHash", 2d)) ModLog.Every("LocationProxyMudPatch.ByHash", 2d, "LocationProxyMudPatch.ByHash(" + hash + "): returns " + l.m_prefabName);
                        return l;
                    }
            if (RuleLog.Due("LocationProxyMudPatch.ByHash:none", 2d)) ModLog.Every("LocationProxyMudPatch.ByHash:none", 2d, "LocationProxyMudPatch.ByHash(" + hash + "): returns null (no location with that hash)");
            return null;
        }

        static void Postfix(LocationProxy __instance, bool __result)
        {
            try
            {
                ZNetView nv = __instance != null ? View(__instance) : null;
                if (!__result || nv == null || !nv.IsValid() || !nv.IsOwner()) { if (RuleLog.Due("LocationProxyMudPatch:skip", 2d)) ModLog.Every("LocationProxyMudPatch:skip", 2d, "LocationProxyMudPatch.Postfix: returns (not shown yet, or not the owner)"); return; }
                ZDO zdo = nv.GetZDO();
                if (zdo.GetBool(DoneKey)) { if (RuleLog.Due("LocationProxyMudPatch:done", 2d)) ModLog.Every("LocationProxyMudPatch:done", 2d, "LocationProxyMudPatch.Postfix: returns (mud already handled)"); return; }
                ZoneSystem.ZoneLocation loc = ByHash(zdo.GetInt(ZDOVars.s_location));
                if (loc == null || loc.m_prefab == null || !loc.m_prefab.IsLoaded) { if (RuleLog.Due("LocationProxyMudPatch:noloc", 2d)) ModLog.Every("LocationProxyMudPatch:noloc", 2d, "LocationProxyMudPatch.Postfix: returns (location prefab not loaded yet - tried again on the next show)"); return; }
                zdo.Set(DoneKey, true);
                bool started = LocationSpawnPatch.Plan(loc, zdo.GetInt(ZDOVars.s_seed), __instance.transform.position, __instance.transform.rotation, "shown near a player");
                ModLog.Verbose("LocationProxyMudPatch: " + loc.m_prefabName + " at " + __instance.transform.position.ToString("0") + " marked done, placement started " + started);
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("LocationProxyMudPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("LocationProxyMudPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("LocationProxyMudPatch: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
