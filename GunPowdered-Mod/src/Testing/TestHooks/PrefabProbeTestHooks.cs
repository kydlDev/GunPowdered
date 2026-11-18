// kydl:read-only-observer - test-only: lists prefab names of the running game
using System.Collections.Generic;
using System.Text.RegularExpressions;
using KydlCore.Common;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>TestLab hook that lists the game's prefab names matching a pattern (to pick vanilla effects by their real names).</summary>
    internal static class PrefabProbeTestHooks
    {
        /// The names of all ZNetScene prefabs matching the regex (case-insensitive), comma separated, sorted; also written to the log.
        [TestHook("GunPowdered.prefabsLike")]
        static string PrefabsLike(string pattern)
        {
            if (ZNetScene.instance == null) return "no scene";
            var re = new Regex(pattern ?? ".", RegexOptions.IgnoreCase, System.TimeSpan.FromSeconds(1));
            var names = new List<string>();
            foreach (UnityEngine.GameObject go in ZNetScene.instance.m_prefabs) if (go != null && re.IsMatch(go.name)) names.Add(go.name);
            names.Sort(System.StringComparer.Ordinal);
            string all = names.Count + ": " + string.Join(", ", names.ToArray());
            ModLog.Info("PrefabProbeTestHooks: prefabs like '" + pattern + "' = " + all);
            return all;
        }
    }
}
