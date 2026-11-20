using System;
using System.Collections.Generic;
using System.Reflection;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// Puts the ground of the lab back after a test dug into it. The KydlFlat arena is itself a terrain modification (the template's baked levelling), so
    /// clearing a terrain piece's modifications drops the ground by metres and the player falls through it (lab a8dyn1, a11full1: mud-draugr-lake cleared
    /// everything within 80 m). <see cref="Mark"/> copies the modification arrays of every terrain piece near a spot before the test digs;
    /// <see cref="Restore"/> writes those copies back and clears only the pieces that did not exist at the mark (made by the test's digging).
    /// </summary>
    internal static class TerrainSnapshot
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static readonly string[] Fields = { "m_modifiedHeight", "m_levelDelta", "m_smoothDelta", "m_modifiedPaint", "m_paintMask" };
        static readonly Dictionary<TerrainComp, object[]> Saved = new Dictionary<TerrainComp, object[]>();

        /// <summary>Copies the modifications of every terrain piece within <paramref name="radius"/> m of <paramref name="at"/> (flat distance). Returns how many were copied.</summary>
        internal static int Mark(Vector3 at, float radius)
        {
            Saved.Clear();
            foreach (TerrainComp tc in UnityEngine.Object.FindObjectsByType<TerrainComp>(FindObjectsSortMode.None))
            {
                if (!Near(tc, at, radius)) continue;
                var copy = new object[Fields.Length];
                for (int i = 0; i < Fields.Length; i++) copy[i] = (Field(Fields[i])?.GetValue(tc) as Array)?.Clone();
                Saved[tc] = copy;
            }
            ModLog.Info($"TerrainSnapshot: marked {Saved.Count} terrain piece(s) within {radius} m of ({at.x:0}, {at.z:0})");
            return Saved.Count;
        }

        /// <summary>
        /// Writes the marked modifications back into the terrain pieces near <paramref name="at"/>; a piece that was not there at the mark is cleared (only the test
        /// made it). Nothing marked: nothing is touched. Returns "reset N restored, M cleared".
        /// </summary>
        internal static string Restore(Vector3 at, float radius)
        {
            if (Saved.Count == 0)
            {
                ModLog.Warn("TerrainSnapshot: restore asked with nothing marked - the ground is left as it is (a blind clear would remove the arena's own levelling)");
                return "reset 0 restored, 0 cleared (nothing marked)";
            }
            int restored = 0, cleared = 0;
            foreach (TerrainComp tc in UnityEngine.Object.FindObjectsByType<TerrainComp>(FindObjectsSortMode.None))
            {
                if (!Near(tc, at, radius)) continue;
                ZNetView view = tc.GetComponent<ZNetView>();
                if (view != null && view.IsValid() && !view.IsOwner()) view.ClaimOwnership();
                bool known = Saved.TryGetValue(tc, out object[] saved);
                for (int i = 0; i < Fields.Length; i++)
                {
                    var now = Field(Fields[i])?.GetValue(tc) as Array;
                    if (now == null) continue;
                    if (known && saved[i] is Array before && before.Length == now.Length) Array.Copy(before, now, now.Length);
                    else Array.Clear(now, 0, now.Length);
                }
                typeof(TerrainComp).GetMethod("Save", Flags)?.Invoke(tc, new object[] { false });
                Poke(Field("m_hmap")?.GetValue(tc) as Heightmap);
                if (known) restored++; else cleared++;
                ModLog.Verbose($"TerrainSnapshot: terrain piece at ({tc.transform.position.x:0}, {tc.transform.position.z:0}) {(known ? "restored from the mark" : "cleared (made after the mark)")}");
            }
            ModLog.Info($"TerrainSnapshot: reset near ({at.x:0}, {at.z:0}) - {restored} restored from the mark, {cleared} cleared");
            return "reset " + restored + " restored, " + cleared + " cleared";
        }

        static bool Near(TerrainComp tc, Vector3 at, float radius)
        {
            Vector3 p = tc.transform.position;
            return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(at.x, at.z)) <= radius;
        }

        static FieldInfo Field(string name) => typeof(TerrainComp).GetField(name, Flags);

        static void Poke(Heightmap hm)
        {
            MethodInfo poke = typeof(Heightmap).GetMethod("Poke", Flags);
            if (hm == null || poke == null) { ModLog.Verbose("TerrainSnapshot.Poke: no heightmap or no Poke method - the ground updates on its own later"); return; }
            var args = new object[poke.GetParameters().Length];
            for (int i = 0; i < args.Length; i++) args[i] = Type.Missing;
            poke.Invoke(hm, BindingFlags.OptionalParamBinding | Flags, null, args, null);
        }
    }
}
