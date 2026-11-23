// kydl:read-only-observer - test-only: re-keys KydlCore's lab terrain template after a relog; touches no game object
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// a18kneel3 (D-172): KydlCore's TestTerrainRestore keeps the template terrain data per terrain compiler keyed by its ZDOID (AppDomain key
    /// <see cref="TestTerrainRestore.SnapshotKey"/>). A relog loads the world again and the ZDOIDs come back shuffled - an old id now names ANOTHER zone's
    /// compiler (log: "(128, 0, 0) copy 15419 bytes" was the (192, 0, 0) data) - so the next test start wrote one zone's terrain into another and the arena
    /// dropped 1.5 m (visual-dynamite-kneel: no ground 6 m ahead, after relog-dynamite-hole). Until KydlCore keys it by position, the arena step keeps a
    /// position -> data copy of that template while the ids are right, and after a relog puts it back under the new ids. Test-only; KydlCore's own copy
    /// logic is unchanged.
    /// </summary>
    internal static class TerrainTemplateKeys
    {
        static readonly Dictionary<Vector2Int, byte[]> ByPosition = new Dictionary<Vector2Int, byte[]>();
        static ZNet _world;

        static Vector2Int Key(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z));

        /// <summary>Called by the arena step before KydlCore's cleanup. Expected outcome: "terrain template: ..." (what was remembered or put back).</summary>
        internal static string Sync()
        {
            if (ZNet.instance == null) { if (RuleLog.IsOn()) ModLog.Verbose($"TerrainTemplateKeys.Sync: returns \"terrain template: no world\" because ZNet.instance == null (line 27)"); return "terrain template: no world"; }
            bool relogged = _world != null && !ReferenceEquals(_world, ZNet.instance);
            _world = ZNet.instance;      // also before KydlCore's first copy exists, so a relog in the very first test is seen
            if (!(System.AppDomain.CurrentDomain.GetData(TestTerrainRestore.SnapshotKey) is Dictionary<ZDOID, byte[]> copy))
                { if (RuleLog.IsOn()) ModLog.Verbose($"TerrainTemplateKeys.Sync: returns \"terrain template: none yet\" (line 31)"); return "terrain template: none yet"; }
            var current = new Dictionary<Vector2Int, ZDOID>();
            foreach (TerrainComp tc in Object.FindObjectsByType<TerrainComp>(FindObjectsSortMode.None))
            {
                ZNetView v = tc.GetComponent<ZNetView>();
                if (v != null && v.IsValid()) current[Key(tc.transform.position)] = v.GetZDO().m_uid;
            }
            if (!relogged)
            {
                int kept = 0;
                foreach (var kv in current)
                    if (copy.TryGetValue(kv.Value, out byte[] data) && !ByPosition.ContainsKey(kv.Key)) { ByPosition[kv.Key] = data; kept++; }
                string s1 = "terrain template: " + kept + " new area(s) remembered by position (" + ByPosition.Count + " in all)";
                ModLog.Verbose("TerrainTemplateKeys.Sync: " + s1);
                return s1;
            }
            int before = copy.Count, put = 0;
            copy.Clear();      // every old id may now name another area
            foreach (var kv in current)
                if (ByPosition.TryGetValue(kv.Key, out byte[] data)) { copy[kv.Value] = data; put++; }
            string s = "terrain template: relog - " + before + " old id(s) dropped, " + put + " area(s) put back under their new ids";
            ModLog.Info("TerrainTemplateKeys.Sync: " + s);
            return s;
        }
    }
}
