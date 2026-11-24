using GunPowdered.Core;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>D-183 / D-192 test hooks: the punch after the last stick was set down, and the bundle's own Flametal look. They call the real code paths.</summary>
    internal static class PlayTestFixHooks
    {
        /// <summary>
        /// Presses attack the way the game does (Humanoid.StartAttack) with whatever is in the hands and says whether the game started an attack:
        /// "attack started True/False, in attack True/False".
        /// </summary>
        [TestHook("GunPowdered.pressAttack")]
        static string PressAttack()
        {
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("PlayTestFixHooks.PressAttack: returns \"no player\" because p == null (line 18)"); return "no player"; }
            bool started = p.StartAttack(null, false);
            string s = "attack started " + started + ", in attack " + p.InAttack();
            ModLog.Info("PlayTestFixHooks: " + s);
            return s;
        }

        /// <summary>
        /// D-192: which material every part of a registered prefab's look uses: "flametal slots N of M: part=material; ..." where flametal slots are
        /// the bundle's own Flametal materials (flametal_mat, ext_flametal_mat, flametal_glow_mat, flametal_wheel_mat). Parts the mod switched off
        /// (the placeholder's own look) are left out.
        /// </summary>
        [TestHook("GunPowdered.flametalLook")]
        static string FlametalLook(string prefab)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (go == null) { ModLog.Verbose($"PlayTestFixHooks.FlametalLook: returns \"no prefab\" because {prefab} is not registered (line 34)"); return "no prefab"; }
            int slots = 0, flametal = 0;
            var seen = new System.Text.StringBuilder();
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) { if (RuleLog.Due("PlayTestFixHooks.FlametalLook:39", 2d)) ModLog.Every("PlayTestFixHooks.FlametalLook:39", 2d, $"PlayTestFixHooks.FlametalLook: skips to the next item (continue) because {r.name} is switched off (line 39)"); continue; }
                foreach (Material m in r.sharedMaterials)
                {
                    string n = m != null ? m.name.Replace(" (Instance)", "") : "<none>";
                    slots++;
                    if (System.Array.IndexOf(OwnFlametal, n) >= 0) flametal++;
                    seen.Append(r.name).Append('=').Append(n).Append("; ");
                }
            }
            string s = $"flametal slots {flametal} of {slots}: {seen}";
            ModLog.Info($"PlayTestFixHooks: {prefab} {s}");
            return s;
        }

        static readonly string[] OwnFlametal = { "flametal_mat", "ext_flametal_mat", "flametal_glow_mat", "flametal_wheel_mat" };
    }
}
