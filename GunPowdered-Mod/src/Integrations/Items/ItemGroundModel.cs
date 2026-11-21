using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Integrations.Items
{
    /// <summary>
    /// T-017 / T-019 (user 2026-11-21 02:51 and 12:25: cannon balls, then "spawning 1 dynamite I got 2 sticks on the ground"): every item of this mod that
    /// keeps a hand copy of its model was drawn twice on the ground - its own model (root "gp_model") and the hand copy under "attach" (the cannon balls,
    /// dynamite stick + pack, grenade, musket: their *Setup.AttachHandModel) (the game shows only the "attach" child in a hand, taken from the PREFAB). An item lying in the
    /// world is a whole instance of the prefab, so this hides the hand copy there; the prefab keeps it, so the item still shows in the hand. Added by ItemRegistry to every item that has both models.
    /// </summary>
    internal sealed class ItemGroundModel : MonoBehaviour
    {
        /// <summary>
        /// Adds the component when the item has BOTH its own model (root "gp_model") and a hand copy ("attach/gp_model") - an item whose only model is the
        /// hand copy must keep it on the ground. Expected outcome: true when the item has (now) the component.
        /// </summary>
        internal static bool AddIfBoth(GameObject item)
        {
            bool both = item != null && item.transform.Find("gp_model") != null && item.transform.Find("attach/gp_model") != null;
            if (both && item.GetComponent<ItemGroundModel>() == null) item.AddComponent<ItemGroundModel>();
            ModLog.Verbose("ItemGroundModel.AddIfBoth: " + (item != null ? item.name : "null") + " - own model and hand copy " + both + " - returns " + both);
            return both;
        }

        void Awake()
        {
            ZNetView view = GetComponent<ZNetView>();
            if (view == null || view.GetZDO() == null) { ModLog.Verbose("ItemGroundModel.Awake: " + name + " is not a world instance (prefab or ghost) - the hand copy stays - returns"); return; }
            Transform hand = transform.Find("attach/gp_model");
            int hidden = 0;
            if (hand != null) foreach (Renderer r in hand.GetComponentsInChildren<Renderer>(true)) { r.enabled = false; hidden++; }
            ModLog.Verbose("ItemGroundModel.Awake: " + name + " in the world - " + hidden + " hand-copy renderer(s) hidden, the item shows once - returns");
        }
    }
}
