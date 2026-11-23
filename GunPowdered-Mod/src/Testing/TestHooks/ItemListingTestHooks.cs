// kydl:read-only-observer - test-only: reads the recipe the game really has and what KydlCore's KY-Items registry lists
using System.Globalization;
using System.Text;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Integrations.KyRegistry;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>TestLab hooks that read the real game: the recipe of an item (ObjectDB) and the mod's entries in the KY-Items registry (the item viewer's list).</summary>
    internal static class ItemListingTestHooks
    {
        /// The game's recipe for a prefab: "piece_workbench level 3, makes 5: GP_Gunpowder x3, Obsidian x2, ..." or why there is none.
        [TestHook("GunPowdered.recipeOf")]
        static string RecipeOf(string prefab)
        {
            if (ObjectDB.instance == null) { if (RuleLog.Due("ItemListingTestHooks.RecipeOf:19", 2d)) ModLog.Every("ItemListingTestHooks.RecipeOf:19", 2d, $"ItemListingTestHooks.RecipeOf: returns \"no ObjectDB\" because ObjectDB.instance == null (line 19)"); return "no ObjectDB"; }
            foreach (Recipe r in ObjectDB.instance.m_recipes)
            {
                if (r == null || r.m_item == null || r.m_item.name != prefab) { if (RuleLog.Due("ItemListingTestHooks.RecipeOf:22", 2d)) ModLog.Every("ItemListingTestHooks.RecipeOf:22", 2d, $"ItemListingTestHooks.RecipeOf: skips to the next item (continue) because r == null || r.m_item == null || r.m_item.name != prefab (line 22)"); continue; }
                var sb = new StringBuilder();
                sb.Append(r.m_craftingStation != null ? r.m_craftingStation.name : "no station").Append(" level ").Append(r.m_minStationLevel.ToString(CultureInfo.InvariantCulture));
                sb.Append(", makes ").Append(r.m_amount.ToString(CultureInfo.InvariantCulture)).Append(", enabled ").Append(r.m_enabled).Append(':');
                for (int i = 0; i < r.m_resources.Length; i++) sb.Append(i == 0 ? " " : ", ").Append(r.m_resources[i].m_resItem.name).Append(" x").Append(r.m_resources[i].m_amount.ToString(CultureInfo.InvariantCulture));
                ModLog.Verbose("ItemListingTestHooks: recipe of " + prefab + " = " + sb);
                return sb.ToString();
            }
            { var returnedAt30 = "no recipe for " + prefab; if (RuleLog.Due("ItemListingTestHooks.RecipeOf:30", 2d)) ModLog.Every("ItemListingTestHooks.RecipeOf:30", 2d, $"ItemListingTestHooks.RecipeOf: returns {returnedAt30} (line 30)"); return returnedAt30; }
        }

        /// The game's build piece of a prefab: "category Misc, station piece_workbench: Wood x10, Resin x4, in KY-Items True" or why there is none.
        [TestHook("GunPowdered.pieceOf")]
        static string PieceOf(string prefab)
        {
            GameObject go = PrefabManager.Instance != null ? PrefabManager.Instance.GetPrefab(prefab) : null;
            Piece p = go != null ? go.GetComponent<Piece>() : null;
            if (p == null) { var returnedAt39 = "no piece " + prefab; if (RuleLog.IsOn()) ModLog.Verbose($"ItemListingTestHooks.PieceOf: returns {returnedAt39} because p == null (line 39)"); return returnedAt39; }
            var sb = new StringBuilder();
            sb.Append("category ").Append(p.m_category == PieceManager.Instance.GetPieceCategory(KyCategory.Name) ? KyCategory.Name : p.m_category.ToString()).Append(", station ").Append(p.m_craftingStation != null ? p.m_craftingStation.name : "none").Append(", enabled ").Append(p.m_enabled).Append(':');
            for (int i = 0; i < p.m_resources.Length; i++) sb.Append(i == 0 ? " " : ", ").Append(p.m_resources[i].m_resItem.name).Append(" x").Append(p.m_resources[i].m_amount.ToString(CultureInfo.InvariantCulture));
            sb.Append(", in KY-Items ").Append(KyItemsContributions.IsListed(prefab));
            ModLog.Verbose("ItemListingTestHooks: piece " + prefab + " = " + sb);
            return sb.ToString();
        }

        /// The item data of a prefab: "stack 1, weight 20, type Material" (what the inventory enforces).
        [TestHook("GunPowdered.itemOf")]
        static string ItemOf(string prefab)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ItemListingTestHooks.ItemOf(prefab={prefab})");
            GameObject go = PrefabManager.Instance != null ? PrefabManager.Instance.GetPrefab(prefab) : null;
            ItemDrop drop = go != null ? go.GetComponent<ItemDrop>() : null;
            if (drop == null) { var returnedAt55 = "no item " + prefab; if (RuleLog.IsOn()) ModLog.Verbose($"ItemListingTestHooks.ItemOf: returns {returnedAt55} because drop == null (line 55)"); return returnedAt55; }
            ItemDrop.ItemData.SharedData s = drop.m_itemData.m_shared;
            { var returnedAt57 = "stack " + s.m_maxStackSize.ToString(CultureInfo.InvariantCulture) + ", weight " + s.m_weight.ToString("0.##", CultureInfo.InvariantCulture) + ", type " + s.m_itemType; if (RuleLog.IsOn()) ModLog.Verbose($"ItemListingTestHooks.ItemOf: returns {returnedAt57} (line 57)"); return returnedAt57; }
        }

        /// "all N listed" when KY-Items lists every item and piece of the mod, else "M of N listed".
        [TestHook("GunPowdered.kyItemsListed")]
        static string KyItemsListed()
        {
            int listed = KyItemsContributions.ListedCount(), wanted = KyItemsContributions.WantedCount();
            ModLog.Verbose("ItemListingTestHooks: KY-Items lists " + listed + " of " + wanted);
            return listed == wanted ? "all " + wanted + " listed" : listed + " of " + wanted + " listed";
        }

        /// "all N materials" when every ingredient and deposit of the mod is in KY-Items as a Material (the viewer's Materials tab), else "listed/wanted".
        [TestHook("GunPowdered.kyMaterialsListed")]
        static string KyMaterialsListed()
        {
            string r = KyItemsContributions.MaterialsListed();
            string[] parts = r.Split('/');
            ModLog.Verbose("ItemListingTestHooks: KY-Items holds " + r + " materials");
            return parts[0] == parts[1] ? "all " + parts[1] + " materials" : r + " materials";
        }
    }
}