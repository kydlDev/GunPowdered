// kydl:read-only-observer - test-only: remembers where an object stands and counts its saved records (ZDOs) there later; changes nothing
using System.Collections;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// User 2026-11-23: "check if zones exploded by grenades/dynamites, or destroyed by cannons are saved and loaded after a relog". After a relog every
    /// GameObject reference a hook kept is gone (cannonPlacedLeft would read 0 whether the rock is there or not), so these hooks remember a POSITION and
    /// count the saved records (ZDOs) of a prefab there - the world save itself, loaded or not.
    /// </summary>
    internal static class PersistTestHooks
    {
        static Vector3? _spot;

        /// <summary>Remembers the position of the <paramref name="prefab"/> object nearest to the player. Returns "remembered PREFAB at (x, y, z)" or "no PREFAB".</summary>
        [TestHook("GunPowdered.rememberSpotOf")]
        static string RememberSpotOf(string prefab)
        {
            ModLog.Verbose("PersistTestHooks.RememberSpotOf(prefab=" + prefab + ")");
            Player p = Player.m_localPlayer;
            ZNetView best = null;
            float bestD = float.MaxValue;
            if (p != null)
                foreach (ZNetView nv in Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
                {
                    if (nv == null || !nv.IsValid() || Utils.GetPrefabName(nv.gameObject) != prefab) continue;
                    float d = Vector3.Distance(nv.transform.position, p.transform.position);
                    if (d < bestD) { bestD = d; best = nv; }
                }
            if (best == null) { ModLog.Verbose("PersistTestHooks.RememberSpotOf: returns no " + prefab); return "no " + prefab; }
            _spot = best.transform.position;
            string s = "remembered " + prefab + " at " + _spot.Value.ToString("0.0");
            ModLog.Info("PersistTestHooks: " + s);
            return s;
        }

        /// <summary>How many saved records of <paramref name="prefab"/> lie within <paramref name="metres"/> of the remembered spot (-1: nothing remembered).</summary>
        [TestHook("GunPowdered.savedCountAtSpot")]
        static int SavedCountAtSpot(string prefab, double metres)
        {
            if (!_spot.HasValue) { ModLog.Verbose("PersistTestHooks.SavedCountAtSpot: returns -1 (nothing remembered)"); return -1; }
            var all = AccessTools.Field(typeof(ZDOMan), "m_objectsByID")?.GetValue(ZDOMan.instance) as IDictionary;
            int hash = prefab.GetStableHashCode(), n = 0;
            if (all != null)
                foreach (object o in all.Values)
                    if (o is ZDO z && z.GetPrefab() == hash && Vector3.Distance(z.GetPosition(), _spot.Value) <= metres) n++;
            ModLog.Info("PersistTestHooks: " + n + " saved " + prefab + " within " + metres + " m of " + _spot.Value.ToString("0.0"));
            return n;
        }

        /// <summary>Forgets the remembered spot (the arena step calls it, so no test reads an earlier test's spot).</summary>
        internal static void Forget() => _spot = null;
    }
}
