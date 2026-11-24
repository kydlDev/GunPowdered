using GunPowdered.Core;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Integrations.Items
{
    /// <summary>
    /// D-182 (user 2026-11-24, shot-008: "any and all Flametal textures have to be replaced with the FlametalNew ingot visual", the blue-ish ingot): every
    /// flametal part of the bundle models (Flametal Cannon Ball and its flight, the Mobile Cannon's barrel, bands and tyres, the Flametal Heater, the drying
    /// oven) gets the game's own FlametalNew ingot material at runtime. Nothing of the game is copied into the bundle; a game update that changes the ingot
    /// changes these parts with it.
    /// </summary>
    internal static class FlametalLook
    {
        /// <summary>The bundle's flametal materials (GunPowdered-Unity: Assets/GunPowdered_Art/Materials).</summary>
        static readonly string[] BundleMaterials = { "flametal_mat", "flametal_glow_mat", "ext_flametal_mat" };

        const string IngotPrefab = "FlametalNew";
        static Material _ingot;

        /// <summary>D-191: the prefab ids whose look got the ingot material, so FlametalIcons renders their icons again from the new look.</summary>
        internal static readonly System.Collections.Generic.HashSet<string> Changed = new System.Collections.Generic.HashSet<string>();

        /// <summary>The game's FlametalNew ingot material (its "model" renderer), or null when the prefab is not loaded yet.</summary>
        internal static Material Ingot
        {
            get
            {
                if (_ingot != null) return _ingot;
                GameObject ingot = PrefabManager.Cache.GetPrefab<GameObject>(IngotPrefab);
                Transform model = ingot != null ? ingot.transform.Find("model") : null;
                MeshRenderer r = model != null ? model.GetComponent<MeshRenderer>() : null;
                _ingot = r != null ? r.sharedMaterial : null;
                ModLog.Verbose($"FlametalLook.Ingot: {IngotPrefab} {(ingot != null ? "found" : "not loaded")}, material {(_ingot != null ? _ingot.name + " / " + _ingot.shader.name : "<none>")}");
                return _ingot;
            }
        }

        /// <summary>Whether a bundle material is one of the flametal ones (its name, an instance's "(Instance)" suffix ignored).</summary>
        internal static bool IsFlametal(Material m)
        {
            if (m == null) { if (RuleLog.Due("FlametalLook.IsFlametal:null", 2d)) ModLog.Every("FlametalLook.IsFlametal:null", 2d, "FlametalLook.IsFlametal: returns false because m == null (line 43)"); return false; }
            string n = m.name.Replace(" (Instance)", "");
            bool flametal = System.Array.IndexOf(BundleMaterials, n) >= 0;
            { if (RuleLog.Due("FlametalLook.IsFlametal", 2d)) ModLog.Every("FlametalLook.IsFlametal", 2d, $"FlametalLook.IsFlametal: returns {flametal} because {n} is {(flametal ? "" : "not ")}one of the bundle's flametal materials (line 46)"); return flametal; }
        }

        /// <summary>Puts the ingot material on every flametal slot under <paramref name="model"/>. Expected outcome: the number of slots changed (0 when there are none or the ingot is not loaded).</summary>
        internal static int Apply(GameObject model, string id)
        {
            Material ingot = Ingot;
            if (model == null || ingot == null) { ModLog.Every("flametal-look-" + id, 600d, $"FlametalLook: {id} keeps its own flametal look (model {model != null}, ingot material {ingot != null})"); return 0; }
            int changed = 0;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                bool any = false;
                for (int i = 0; i < mats.Length; i++) if (IsFlametal(mats[i])) { mats[i] = ingot; any = true; changed++; }
                if (any) r.sharedMaterials = mats;
            }
            if (changed > 0)
            {
                Changed.Add(id);
                ModLog.Info($"FlametalLook: {id} - {changed} flametal slot(s) now use the game's {IngotPrefab} material ({ingot.name})");
            }
            { if (RuleLog.Due("FlametalLook.Apply", 2d)) ModLog.Every("FlametalLook.Apply", 2d, $"FlametalLook.Apply: returns {changed} for {id} (line 67)"); return changed; }
        }
    }
}
