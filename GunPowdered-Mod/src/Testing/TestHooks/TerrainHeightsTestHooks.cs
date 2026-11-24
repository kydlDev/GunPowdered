// kydl:read-only-observer - test-only: reads ground heights around the player; changes nothing in the world
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// D-175 (user 2026-11-24: "world tearing, blue spots showing up in between green land masses"): after the relog tests KydlCore's terrain restore wrote
    /// one zone's levelling into another, so the lab world showed hills, holes, floating slabs and torn edges - and no test noticed. These hooks make the
    /// ground itself the evidence: <see cref="Mark"/> remembers the ground height on a 4 m grid within 100 m of the player (keyed by world position, so a
    /// relog does not matter), <see cref="Same"/> compares the ground with those marks.
    /// </summary>
    internal static class TerrainHeightsTestHooks
    {
        const float Radius = 100f, Step = 4f, Tolerance = 0.05f;
        static readonly Dictionary<Vector2Int, float> Marked = new Dictionary<Vector2Int, float>();

        /// <summary>Expected outcome: "marked N ground heights within 100 m". Fails with "no player" before the world is loaded.</summary>
        [TestHook("GunPowdered.terrainHeightsMark")]
        static string Mark()
        {
            if (RuleLog.IsOn()) ModLog.Verbose("TerrainHeightsTestHooks.Mark()");
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.IsOn()) ModLog.Verbose("TerrainHeightsTestHooks.Mark: returns \"no player\" because p == null (line 26)"); return "no player"; }
            Marked.Clear();
            Vector3 c = p.transform.position;
            int notLoaded = 0;
            for (float dx = -Radius; dx <= Radius; dx += Step)
                for (float dz = -Radius; dz <= Radius; dz += Step)
                    if (dx * dx + dz * dz <= Radius * Radius)
                    {
                        var key = new Vector2Int(Mathf.RoundToInt(c.x + dx), Mathf.RoundToInt(c.z + dz));
                        if (Heightmap.GetHeight(new Vector3(key.x, 0f, key.y), out float h)) Marked[key] = h;
                        else notLoaded++;
                    }
            string r = "marked " + Marked.Count + " ground heights within " + Radius.ToString("0") + " m of (" + c.x.ToString("0") + ", " + c.z.ToString("0") + "), " + notLoaded + " not loaded";
            ModLog.Info("TerrainHeights: " + r);
            if (RuleLog.IsOn()) ModLog.Verbose("TerrainHeightsTestHooks.Mark: returns " + r + " (line 40)");
            return r;
        }

        /// <summary>
        /// Expected outcome: "same as marked (N points, largest change X m)". Otherwise "DIFFERS: K of N points moved more than 0.05 m (largest D m at (x, z):
        /// was A, now B), M not loaded" - the first numbers name where to look on a picture; "nothing marked" when <see cref="Mark"/> did not run.
        /// </summary>
        [TestHook("GunPowdered.terrainHeightsSame")]
        static string Same()
        {
            if (RuleLog.IsOn()) ModLog.Verbose("TerrainHeightsTestHooks.Same()");
            if (Marked.Count == 0) { if (RuleLog.IsOn()) ModLog.Verbose("TerrainHeightsTestHooks.Same: returns \"nothing marked\" because Marked.Count == 0 (line 52)"); return "nothing marked"; }
            int moved = 0, notLoaded = 0;
            float largest = 0f, was = 0f, now = 0f;
            Vector2Int where = default;
            foreach (var kv in Marked)
            {
                bool loaded = Heightmap.GetHeight(new Vector3(kv.Key.x, 0f, kv.Key.y), out float h);
                float d = loaded ? Mathf.Abs(h - kv.Value) : 0f;
                if (!loaded) notLoaded++;
                if (d > Tolerance) moved++;
                if (d > largest) { largest = d; where = kv.Key; was = kv.Value; now = h; }
            }
            string r = moved == 0 && notLoaded == 0
                ? "same as marked (" + Marked.Count + " points, largest change " + largest.ToString("0.000") + " m)"
                : "DIFFERS: " + moved + " of " + Marked.Count + " points moved more than " + Tolerance + " m (largest " + largest.ToString("0.00") + " m at (" + where.x + ", " + where.y + "): was " + was.ToString("0.00") + ", now " + now.ToString("0.00") + "), " + notLoaded + " not loaded";
            ModLog.Info("TerrainHeights: " + r);
            if (RuleLog.IsOn()) ModLog.Verbose("TerrainHeightsTestHooks.Same: returns " + r + " (line 68)");
            return r;
        }
    }
}