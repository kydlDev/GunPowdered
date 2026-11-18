using System;
using System.Collections;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;

namespace GunPowdered.Features.Water.Infrastructure
{
    /// <summary>
    /// What happens when the player uses a Bucket (D-008): hold the empty bucket, face water, use it. The bucket turns into the full bucket (visible), and after a
    /// short moment the water lands in the inventory and the bucket is empty again. Runs on the player's own client; the items are the player's, so no
    /// other client is involved.
    /// </summary>
    internal static class BucketUse
    {
        const float ReachMetres = 2.5f;
        static bool _busy;

        /// <summary>The name of the prefab an item was made from ("" when unknown).</summary>
        internal static string PrefabOf(ItemDrop.ItemData item)
        {
            return item != null && item.m_dropPrefab != null ? item.m_dropPrefab.name : "";
        }

        /// <summary>
        /// Handles one use of the held item. Expected outcome: true when the held item is one of the buckets (the use was handled here and the normal attack
        /// must not run).
        /// </summary>
        internal static bool Use(Player player, ItemDrop.ItemData held)
        {
            string prefab = PrefabOf(held);
            bool isEmpty = prefab == GunPowderedPrefabs.Bucket, isFull = prefab == GunPowderedPrefabs.BucketFull;
            if (!isEmpty && !isFull) return false;
            if (!ModSettings.FeatureOn(ModSettings.EnableWater)) { player.Message(MessageHud.MessageType.Center, "Water is switched off"); return true; }
            if (_busy) return true;

            if (isFull) Plugin.Instance.StartCoroutine(PourRoutine(player, held));
            else if (WaterAhead(player)) Plugin.Instance.StartCoroutine(FillThenPour(player, held));
            else player.Message(MessageHud.MessageType.Center, "Face open water to fill the bucket");
            return true;
        }

        // water within reach in front of the player: the surface lies above the ground there
        static bool WaterAhead(Player p)
        {
            for (float d = 1f; d <= ReachMetres; d += 0.75f)
            {
                Vector3 point = p.transform.position + p.transform.forward * d;
                float surface = Floating.GetLiquidLevel(point, 1f, LiquidType.Water);
                float ground = ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(point) : point.y;
                if (surface > ground + 0.1f) return true;
            }
            return false;
        }

        static IEnumerator FillThenPour(Player p, ItemDrop.ItemData item)
        {
            _busy = true;
            bool turned = Turn(p, item, GunPowderedPrefabs.BucketFull);
            ModLog.State("bucket", "empty", turned ? "full" : "fill failed", "used facing water");
            if (turned)
            {
                yield return new WaitForSeconds(ModSettings.BucketFillSeconds.Value);
                Pour(p, item);
            }
            _busy = false;
        }

        static IEnumerator PourRoutine(Player p, ItemDrop.ItemData item)
        {
            _busy = true;
            Pour(p, item);
            _busy = false;
            yield break;
        }

        // full bucket -> Water into the inventory, bucket back to empty (stays full when the inventory has no room)
        static void Pour(Player p, ItemDrop.ItemData item)
        {
            try
            {
                GameObject water = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.Water) : null;
                int amount = ModSettings.WaterPerBucket.Value;
                if (water == null || !p.GetInventory().CanAddItem(water, amount))
                {
                    p.Message(MessageHud.MessageType.Center, "Your inventory is full");
                    return;
                }
                p.GetInventory().AddItem(water, amount);
                bool emptied = Turn(p, item, GunPowderedPrefabs.Bucket);
                p.Message(MessageHud.MessageType.TopLeft, "Took " + amount + " Water");
                ModLog.State("bucket", "full", emptied ? "empty" : "empty failed", amount + " Water added to the inventory");
            }
            catch (Exception e)
            {
                ModLog.Warn("BucketUse.Pour: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // The same inventory item becomes the other bucket (so it keeps its slot) and is held again so the hand model follows.
        static bool Turn(Player p, ItemDrop.ItemData item, string toPrefab)
        {
            GameObject to = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(toPrefab) : null;
            ItemDrop drop = to != null ? to.GetComponent<ItemDrop>() : null;
            if (drop == null) return false;
            bool wasHeld = item.m_equipped;
            if (wasHeld) p.UnequipItem(item, false);
            item.m_dropPrefab = to;
            item.m_shared = drop.m_itemData.m_shared;
            if (wasHeld) p.EquipItem(item, false);
            return PrefabOf(item) == toPrefab;
        }
    }
}
