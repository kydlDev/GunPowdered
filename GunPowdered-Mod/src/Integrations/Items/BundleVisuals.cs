using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Core;

namespace GunPowdered.Integrations.Items
{
    /// <summary>
    /// Gives a prefab cloned from a vanilla placeholder (D-013) the look of this mod's AssetBundle (D-027): the clone keeps all its game behaviour
    /// (item data, piece, pickable, network view), its own meshes and solid colliders are switched off, and the bundle's model (the child "model" of the
    /// bundle prefab with the same name as the prefab id) is added in their place together with its colliders, on the clone's physics layer.
    /// Without a bundle, or without the prefab in it, the clone keeps the placeholder's look and one warning is logged.
    /// </summary>
    internal static class BundleVisuals
    {
        const string ModelChild = "gp_model";
        const string CollidersChild = "colliders";

        /// <summary>Swaps the look of a cloned prefab for the bundle model. Expected outcome: true when the bundle model was added; false (look unchanged) when there is no bundle or no such prefab.</summary>
        internal static bool Apply(string id, GameObject target)
        {
            GameObject source = Plugin.Bundle != null ? Plugin.Bundle.LoadAsset<GameObject>(id) : null;
            Transform model = source != null ? source.transform.Find("model") : null;
            if (target == null || model == null)
            {
                ModLog.Every("visual-missing-" + id, 600d, "BundleVisuals: no bundle model for " + id + ", the placeholder look stays");
                return false;
            }
            if (target.transform.Find(ModelChild) != null) { if (RuleLog.Due("BundleVisuals.Apply:29", 2d)) ModLog.Every("BundleVisuals.Apply:29", 2d, $"BundleVisuals.Apply: returns true because target.transform.Find(ModelChild) != null (line 29)"); return true; }           // already swapped

            foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (Collider c in target.GetComponentsInChildren<Collider>(true)) if (!c.isTrigger) c.enabled = false;

            GameObject inst = Object.Instantiate(model.gameObject, target.transform);
            inst.name = ModelChild;
            inst.transform.localPosition = model.localPosition;            // a model that is not at the root's origin (the Hand Cannon lies on its barrel) keeps its offset
            inst.transform.localRotation = model.localRotation;
            inst.transform.localScale = Vector3.one;
            SetLayer(inst, target.layer);              // D-192: the flametal parts carry the bundle's own Flametal look (no runtime swap any more)

            // a bundle prefab may carry its solid shapes as a child "colliders" (the Cannon Stock: a base and two trestles instead of one box)
            Transform shapes = source.transform.Find(CollidersChild);
            if (shapes != null)
            {
                GameObject solid = Object.Instantiate(shapes.gameObject, target.transform);
                solid.name = "gp_colliders";
                solid.transform.localPosition = shapes.localPosition;
                solid.transform.localRotation = shapes.localRotation;
                SetLayer(solid, target.layer);
            }

            BoxCollider box = source.GetComponent<BoxCollider>();
            if (box != null)
            {
                BoxCollider mine = target.AddComponent<BoxCollider>();
                mine.center = box.center;
                mine.size = box.size;
            }
            // a round item (the Cannon Ball) carries a sphere instead of a box; without it the dropped item would fall through the ground
            SphereCollider sphere = source.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                SphereCollider mine = target.AddComponent<SphereCollider>();
                mine.center = sphere.center;
                mine.radius = sphere.radius;
            }
            ModLog.Verbose("BundleVisuals: " + id + " now uses the bundle model, layer " + LayerMask.LayerToName(target.layer));
            return true;
        }

        /// <summary>
        /// Copies another named child of a bundle prefab onto the target (the Cannon Stock's "mounted_barrel": the cannon as it lies on the stock), on the
        /// target's layer, keeping its local position, rotation and active state. Expected outcome: the copy (named newName), or null when the bundle or
        /// the child is missing (one warning is logged).
        /// </summary>
        internal static GameObject CopyChild(string id, GameObject target, string child, string newName)
        {
            GameObject source = Plugin.Bundle != null ? Plugin.Bundle.LoadAsset<GameObject>(id) : null;
            Transform part = source != null ? source.transform.Find(child) : null;
            if (target == null || part == null)
            {
                ModLog.Warn("BundleVisuals: no child '" + child + "' in the bundle prefab " + id);
                return null;
            }
            Transform existing = target.transform.Find(newName);
            if (existing != null) { var returnedAt86 = existing.gameObject; if (RuleLog.IsOn()) ModLog.Verbose($"BundleVisuals.CopyChild: returns {returnedAt86} because existing != null (line 86)"); return returnedAt86; }
            GameObject copy = Object.Instantiate(part.gameObject, target.transform);
            copy.name = newName;
            copy.transform.localPosition = part.localPosition;
            copy.transform.localRotation = part.localRotation;
            copy.transform.localScale = part.localScale;
            copy.SetActive(part.gameObject.activeSelf);
            SetLayer(copy, target.layer);
            ModLog.Verbose("BundleVisuals: " + id + " got the bundle child " + child + " as " + newName + " at " + copy.transform.localPosition.ToString("0.000"));
            return copy;
        }

        /// <summary>The bundle's inventory / build menu icon of a prefab id. Expected outcome: the sprite, or null when there is no bundle or no such icon.</summary>
        internal static Sprite Icon(string id)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"BundleVisuals.Icon(id={RuleLog.Shown(id)})");
            return Plugin.Bundle != null ? Plugin.Bundle.LoadAsset<Sprite>(IconName(id)) : null;
        }

        /// <summary>The icon file name (without extension) of a prefab id: the id without GP_, except the Leaching Station.</summary>
        internal static string IconName(string id)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"BundleVisuals.IconName(id={RuleLog.Shown(id)})");
            if (id == GunPowderedPrefabs.LeachingVat) return "LeachingStation";
            { var returnedAt110 = id.StartsWith("GP_") ? id.Substring(3) : id; if (RuleLog.Due("BundleVisuals.IconName:110", 2d)) ModLog.Every("BundleVisuals.IconName:110", 2d, $"BundleVisuals.IconName: returns {returnedAt110} (line 110)"); return returnedAt110; }
        }

        static void SetLayer(GameObject go, int layer)
        {
            if (RuleLog.Due("BundleVisuals.SetLayer", 2d)) ModLog.Every("BundleVisuals.SetLayer", 2d, $"BundleVisuals.SetLayer(go={RuleLog.Shown(go)}, layer={RuleLog.Shown(layer)})");
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }
    }
}
