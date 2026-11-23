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

        static float _health = float.NaN;

        static ZDO RecordAtSpot(string prefab)
        {
            var all = AccessTools.Field(typeof(ZDOMan), "m_objectsByID")?.GetValue(ZDOMan.instance) as IDictionary;
            int hash = prefab.GetStableHashCode();
            if (all != null && _spot.HasValue)
                foreach (object o in all.Values)
                    if (o is ZDO z && z.GetPrefab() == hash && Vector3.Distance(z.GetPosition(), _spot.Value) <= 1f) { ModLog.Verbose("PersistTestHooks.RecordAtSpot: returns the record of " + prefab); return z; }
            ModLog.Verbose("PersistTestHooks.RecordAtSpot: returns null (no " + prefab + " record at the spot)");
            return null;
        }

        /// <summary>Remembers the saved health of the piece at the remembered spot. Returns "health H" (H below the piece's full health = it was damaged) or "no record".</summary>
        [TestHook("GunPowdered.rememberSavedHealth")]
        static string RememberSavedHealth(string prefab)
        {
            ZDO z = RecordAtSpot(prefab);
            if (z == null) { ModLog.Verbose("PersistTestHooks.RememberSavedHealth: returns no record"); return "no record"; }
            _health = z.GetFloat(ZDOVars.s_health, float.NaN);
            GameObject p = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            WearNTear w = p != null ? p.GetComponent<WearNTear>() : null;
            string s = "health " + _health.ToString("0.0") + (w != null && !float.IsNaN(_health) && _health < w.m_health ? " (damaged, full " + w.m_health.ToString("0") + ")" : " (not damaged)");
            ModLog.Info("PersistTestHooks: " + prefab + " " + s);
            return s;
        }

        /// <summary>"same health H" when the saved health equals the remembered one (within 0.1); otherwise "bad: ..." with both.</summary>
        [TestHook("GunPowdered.savedHealthSame")]
        static string SavedHealthSame(string prefab)
        {
            ZDO z = RecordAtSpot(prefab);
            float now = z != null ? z.GetFloat(ZDOVars.s_health, float.NaN) : float.NaN;
            string s = float.IsNaN(_health) ? "bad: nothing remembered" : z == null ? "bad: no record any more" : Mathf.Abs(now - _health) <= 0.1f ? "same health " + now.ToString("0.0") : "bad: health " + now.ToString("0.0") + ", was " + _health.ToString("0.0");
            ModLog.Info("PersistTestHooks: " + prefab + " " + s);
            return s;
        }

        /// <summary>Forgets the remembered spot (the arena step calls it, so no test reads an earlier test's spot).</summary>
        internal static void Forget() { _spot = null; _health = float.NaN; }
    }
}
