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
            if (RuleLog.Due("BucketUse.PrefabOf", 2d)) ModLog.Every("BucketUse.PrefabOf", 2d, $"BucketUse.PrefabOf(item={RuleLog.Shown(item)})");
            { var returnedAt25 = item != null && item.m_dropPrefab != null ? item.m_dropPrefab.name : ""; if (RuleLog.Due("BucketUse.PrefabOf:25", 2d)) ModLog.Every("BucketUse.PrefabOf:25", 2d, $"BucketUse.PrefabOf: returns {RuleLog.Shown(returnedAt25)} (line 25)"); return returnedAt25; }
        }

        /// <summary>
        /// Handles one use of the held item. Expected outcome: true when the held item is one of the buckets (the use was handled here and the normal attack
        /// must not run).
        /// </summary>
        internal static bool Use(Player player, ItemDrop.ItemData held)
        {
            if (RuleLog.Due("BucketUse.Use", 2d)) ModLog.Every("BucketUse.Use", 2d, $"BucketUse.Use(player={RuleLog.Shown(player)}, held={RuleLog.Shown(held)})");
            string prefab = PrefabOf(held);
            bool isEmpty = prefab == GunPowderedPrefabs.Bucket, isFull = prefab == GunPowderedPrefabs.BucketFull;
            if (!isEmpty && !isFull) { if (RuleLog.Due("BucketUse.Use:37", 2d)) ModLog.Every("BucketUse.Use:37", 2d, "BucketUse.Use: returns false (line 37)"); return false; }
            if (!ModSettings.FeatureOn(ModSettings.EnableWater)) { player.Message(MessageHud.MessageType.Center, "Water is switched off"); { if (RuleLog.Due("BucketUse.Use:38", 2d)) ModLog.Every("BucketUse.Use:38", 2d, "BucketUse.Use: returns true (line 38)"); return true; } }
            if (_busy) { if (RuleLog.Due("BucketUse.Use:39", 2d)) ModLog.Every("BucketUse.Use:39", 2d, "BucketUse.Use: returns true (line 39)"); return true; }

            if (isFull) Plugin.Instance.StartCoroutine(PourRoutine(player, held));
            else if (WaterAhead(player)) Plugin.Instance.StartCoroutine(FillThenPour(player, held));
            else player.Message(MessageHud.MessageType.Center, "Face open water to fill the bucket");
            { if (RuleLog.Due("BucketUse.Use:44", 2d)) ModLog.Every("BucketUse.Use:44", 2d, "BucketUse.Use: returns true (line 44)"); return true; }
        }

        // water within reach in front of the player: the surface lies above the ground there. The surface is the local water volume's level, or the
        // world's sea level (lakes, rivers and the sea all lie at it): at a lake shore the volume lookup alone found nothing (run core3)
        static bool WaterAhead(Player p)
        {
            float sea = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : float.MinValue;
            var probes = new System.Text.StringBuilder();
            for (float d = 1f; d <= ReachMetres; d += 0.75f)
            {
                Vector3 point = p.transform.position + p.transform.forward * d;
                float surface = Mathf.Max(Floating.GetLiquidLevel(point, 1f, LiquidType.Water), sea);
                float ground = ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(point) : point.y;
                probes.Append(d.ToString("0.0")).Append(" m: surface ").Append(surface.ToString("0.0")).Append(" ground ").Append(ground.ToString("0.0")).Append("; ");
                if (surface > ground + 0.1f)
                {
                    ModLog.Decision("bucket water check", "water ahead", "player at " + p.transform.position.ToString("0.0") + ", " + probes);
                    return surface > ground;
                }
            }
            ModLog.Decision("bucket water check", "no water ahead", "player at " + p.transform.position.ToString("0.0") + ", facing " + p.transform.forward.ToString("0.0") + ", sea level " + sea + ", " + probes);
            return sea < float.MinValue;
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
            if (RuleLog.Due("BucketUse.PourRoutine", 2d)) ModLog.Every("BucketUse.PourRoutine", 2d, $"BucketUse.PourRoutine(p={RuleLog.Shown(p)}, item={RuleLog.Shown(item)})");
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
                    { if (RuleLog.Due("BucketUse.Pour:101", 2d)) ModLog.Every("BucketUse.Pour:101", 2d, "BucketUse.Pour: returns (line 101)"); return; }
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
            if (RuleLog.Due("BucketUse.Turn", 2d)) ModLog.Every("BucketUse.Turn", 2d, $"BucketUse.Turn(p={RuleLog.Shown(p)}, item={RuleLog.Shown(item)}, toPrefab={RuleLog.Shown(toPrefab)})");
            GameObject to = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(toPrefab) : null;
            ItemDrop drop = to != null ? to.GetComponent<ItemDrop>() : null;
            if (drop == null) { if (RuleLog.Due("BucketUse.Turn:120", 2d)) ModLog.Every("BucketUse.Turn:120", 2d, "BucketUse.Turn: returns false (line 120)"); return false; }
            bool wasHeld = item.m_equipped;
            if (wasHeld) p.UnequipItem(item, false);
            item.m_dropPrefab = to;
            item.m_shared = drop.m_itemData.m_shared;
            if (wasHeld) p.EquipItem(item, false);
            { var returnedAt126 = PrefabOf(item) == toPrefab; if (RuleLog.Due("BucketUse.Turn:126", 2d)) ModLog.Every("BucketUse.Turn:126", 2d, $"BucketUse.Turn: returns {RuleLog.Shown(returnedAt126)} (line 126)"); return returnedAt126; }
        }
    }
}
