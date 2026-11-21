using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Features.Cannon.Infrastructure
{
    /// <summary>The items the Hand Cannon takes from and gives back to an inventory, found by prefab name (the shown names are localised).</summary>
    internal static class CannonInventory
    {
        /// <summary>How many items of the prefab the inventory holds (all stacks). Expected outcome: 0 for a null inventory.</summary>
        internal static int Count(Inventory inv, string prefab)
        {
            if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Count", 2d, $"CannonInventory.Count(inv={RuleLog.Shown(inv)}, prefab={RuleLog.Shown(prefab)})");
            if (inv == null) { if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Count:14", 2d, "CannonInventory.Count: returns 0 (line 14)"); return 0; }
            int n = 0;
            foreach (ItemDrop.ItemData i in inv.GetAllItems()) if (Is(i, prefab)) n += i.m_stack;
            { var returnedAt17 = n; if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Count:17", 2d, $"CannonInventory.Count: returns {RuleLog.Shown(returnedAt17)} (line 17)"); return returnedAt17; }
        }

        /// <summary>How many of each cannon ball (CannonAmmoRules.Balls, in that order) the inventory holds (D-145).</summary>
        internal static int[] BallsCarried(Inventory inv)
        {
            if (RuleLog.IsOn()) ModLog.Every("CannonInventory.BallsCarried", 2d, $"CannonInventory.BallsCarried(inv={RuleLog.Shown(inv)})");
            string[] balls = GunPowdered.Features.Cannon.Domain.CannonAmmoRules.Balls;
            var counts = new int[balls.Length];
            for (int i = 0; i < balls.Length; i++) counts[i] = Count(inv, balls[i]);
            { var returnedAt27 = counts; if (RuleLog.IsOn()) ModLog.Every("CannonInventory.BallsCarried:27", 2d, $"CannonInventory.BallsCarried: returns {RuleLog.Shown(returnedAt27)} (line 27)"); return returnedAt27; }
        }

        /// <summary>Takes amount items of the prefab out of the inventory. Expected outcome: true when all of them were taken (nothing is taken when too few).</summary>
        internal static bool Take(Inventory inv, string prefab, int amount)
        {
            if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Take", 2d, $"CannonInventory.Take(inv={RuleLog.Shown(inv)}, prefab={RuleLog.Shown(prefab)}, amount={RuleLog.Shown(amount)})");
            if (amount <= 0) { if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Take:34", 2d, "CannonInventory.Take: returns true (line 34)"); return true; }
            if (Count(inv, prefab) < amount) { if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Take:35", 2d, "CannonInventory.Take: returns false (line 35)"); return false; }
            int left = amount;
            foreach (ItemDrop.ItemData i in inv.GetAllItems().ToArray())
            {
                if (left <= 0) break;
                if (!Is(i, prefab)) continue;
                int take = Mathf.Min(left, i.m_stack);
                inv.RemoveItem(i, take);
                left -= take;
            }
            { var returnedAt45 = left <= 0; if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Take:45", 2d, $"CannonInventory.Take: returns {RuleLog.Shown(returnedAt45)} (line 45)"); return returnedAt45; }
        }

        /// <summary>
        /// Gives amount items of the prefab to the player; what does not fit is dropped at their feet. Expected outcome: the number that went into the
        /// inventory (the rest lies on the ground).
        /// </summary>
        internal static int Give(Player p, string prefab, int amount)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (p == null || go == null || amount <= 0) { if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Give:55", 2d, "CannonInventory.Give: returns 0 (line 55)"); return 0; }
            int added = 0;
            for (int k = 0; k < amount; k++)
            {
                if (p.GetInventory().AddItem(go, 1)) added++;
                else Drop(prefab, 1, p.transform.position + (p.transform.forward * 0.6f) + Vector3.up);
            }
            if (added < amount) ModLog.Verbose("CannonInventory: " + (amount - added) + " " + prefab + " did not fit, dropped");
            return added;
        }

        /// <summary>Drops amount items of the prefab into the world at the position. Expected outcome: the number dropped.</summary>
        internal static int Drop(string prefab, int amount, Vector3 at)
        {
            if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Drop", 2d, $"CannonInventory.Drop(prefab={RuleLog.Shown(prefab)}, amount={RuleLog.Shown(amount)}, at={RuleLog.Shown(at)})");
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (go == null) { if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Drop:71", 2d, "CannonInventory.Drop: returns 0 (line 71)"); return 0; }
            for (int k = 0; k < amount; k++)
            {
                GameObject drop = Object.Instantiate(go, at + (Vector3.up * (0.15f * k)), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                ItemDrop item = drop.GetComponent<ItemDrop>();
                if (item != null) ItemDrop.OnCreateNew(item);
            }
            { var returnedAt78 = amount; if (RuleLog.IsOn()) ModLog.Every("CannonInventory.Drop:78", 2d, $"CannonInventory.Drop: returns {RuleLog.Shown(returnedAt78)} (line 78)"); return returnedAt78; }
        }

        /// <summary>Does the player hold a torch (a Torch type item equipped in either hand)?</summary>
        internal static bool HoldsTorch(Player p)
        {
            if (RuleLog.IsOn()) ModLog.Every("CannonInventory.HoldsTorch", 2d, $"CannonInventory.HoldsTorch(p={RuleLog.Shown(p)})");
            if (p == null) { if (RuleLog.IsOn()) ModLog.Every("CannonInventory.HoldsTorch:85", 2d, "CannonInventory.HoldsTorch: returns false (line 85)"); return false; }
            foreach (ItemDrop.ItemData i in p.GetInventory().GetAllItems())
                if (i.m_equipped && i.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch) { if (RuleLog.IsOn()) ModLog.Every("CannonInventory.HoldsTorch:87", 2d, "CannonInventory.HoldsTorch: returns true (line 87)"); return true; }
            { if (RuleLog.IsOn()) ModLog.Every("CannonInventory.HoldsTorch:88", 2d, "CannonInventory.HoldsTorch: returns false (line 88)"); return false; }
        }

        static bool Is(ItemDrop.ItemData i, string prefab) => i != null && i.m_dropPrefab != null && i.m_dropPrefab.name == prefab;
    }
}
