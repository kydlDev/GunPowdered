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
            if (target.transform.Find(ModelChild) != null) return true;           // already swapped

            foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            foreach (Collider c in target.GetComponentsInChildren<Collider>(true)) if (!c.isTrigger) c.enabled = false;

            GameObject inst = Object.Instantiate(model.gameObject, target.transform);
            inst.name = ModelChild;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            SetLayer(inst, target.layer);

            BoxCollider box = source.GetComponent<BoxCollider>();
            if (box != null)
            {
                BoxCollider mine = target.AddComponent<BoxCollider>();
                mine.center = box.center;
                mine.size = box.size;
            }
            ModLog.Verbose("BundleVisuals: " + id + " now uses the bundle model, layer " + LayerMask.LayerToName(target.layer));
            return true;
        }

        /// <summary>The bundle's inventory / build menu icon of a prefab id. Expected outcome: the sprite, or null when there is no bundle or no such icon.</summary>
        internal static Sprite Icon(string id)
        {
            return Plugin.Bundle != null ? Plugin.Bundle.LoadAsset<Sprite>(IconName(id)) : null;
        }

        /// <summary>The icon file name (without extension) of a prefab id: the id without GP_, except the Leaching Station.</summary>
        internal static string IconName(string id)
        {
            if (id == GunPowderedPrefabs.LeachingVat) return "LeachingStation";
            return id.StartsWith("GP_") ? id.Substring(3) : id;
        }

        static void SetLayer(GameObject go, int layer)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }
    }
}
