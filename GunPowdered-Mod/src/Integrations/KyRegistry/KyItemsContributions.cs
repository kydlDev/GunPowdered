using System.Collections.Generic;
using Jotunn.Managers;
using KydlCore.Common;
using KydlCore.Features.Items.Contracts;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Core;

namespace GunPowdered.Integrations.KyRegistry
{
    /// <summary>
    /// GunPowdered's items and pieces in KydlCore's KY-Items registry, which the item viewer lists (user, 2026-11-18: "all items from this mod should be visible
    /// in KydlCore's item viewer"). Each catalog entry is contributed as "gunpowdered:&lt;prefab in lowercase&gt;" with its recipe, once the prefabs exist. The mod still
    /// adds the prefabs to the game itself (Jotunn, RegisterInGame = false); this only tells the registry. Ingredients (bat guano, saltpeter, gunpowder, water) and the world
    /// deposits are contributed as KyKind.Material, which the viewer shows in its Materials tab (user, 2026-11-18). Items stay listed whatever the Enable switches say: they are always registered in the game (PROJECT_RULES 17).
    /// </summary>
    internal static class KyItemsContributions
    {
        static bool _hooked;

        internal static string IdOf(string prefab) => "gunpowdered:" + prefab.ToLowerInvariant();

        /// <summary>
        /// Contributes every catalog entry now and again when Jotunn has registered the prefabs. Expected outcome: true when the hook is in place; the entries
        /// are listed as soon as their prefabs exist.
        /// </summary>
        internal static bool Start()
        {
            if (!_hooked)
            {
                _hooked = true;
                PrefabManager.OnPrefabsRegistered += OnPrefabsRegistered;
            }
            int listed = ContributeAll();
            ModLog.Verbose("KyItemsContributions: started, " + listed + " of " + Wanted().Count + " entries listed (the rest wait for the prefabs)");
            return _hooked;
        }

        /// <summary>How many of the mod's items and pieces KY-Items lists now (test hook). Deposits and ingredients count too.</summary>
        internal static int ListedCount()
        {
            int n = 0;
            foreach (ItemSpec s in Wanted()) if (KyItems.FindById(IdOf(s.Id)) != null) n++;
            return n;
        }

        /// <summary>Does KY-Items list this prefab of the mod now (test hook).</summary>
        internal static bool IsListed(string prefab) => KyItems.FindById(IdOf(prefab)) != null;

        /// <summary>How many the mod wants listed (every catalog entry: items, pieces, ingredients and deposits).</summary>
        internal static int WantedCount() => Wanted().Count;

        static List<ItemSpec> Wanted()
        {
            var list = new List<ItemSpec>();
            foreach (ItemSpec s in ItemCatalog.All) list.Add(s);
            return list;
        }

        /// <summary>Ingredients (what recipes and stations consume) and gatherables / deposits: the Materials tab of the item viewer.</summary>
        internal static bool IsMaterial(ItemSpec s) => s.Kind == ItemKind.Deposit || s.Id == GunPowderedPrefabs.BatGuano || s.Id == GunPowderedPrefabs.Saltpeter
                                                      || s.Id == GunPowderedPrefabs.Gunpowder || s.Id == GunPowderedPrefabs.Water;

        /// <summary>"listed/wanted" of the materials KY-Items holds as KyKind.Material (test hook).</summary>
        internal static string MaterialsListed()
        {
            int listed = 0, wanted = 0;
            foreach (ItemSpec s in Wanted())
            {
                if (!IsMaterial(s)) continue;
                wanted++;
                KyEntry e = KyItems.FindById(IdOf(s.Id));
                if (e != null && e.Kind == KyKind.Material) listed++;
            }
            return listed + "/" + wanted;
        }

        static void OnPrefabsRegistered() => ModLog.Verbose("KyItemsContributions: prefabs registered, " + ContributeAll() + " entries listed");

        static int ContributeAll()
        {
            int listed = 0;
            foreach (ItemSpec s in Wanted()) if (TryContribute(s)) listed++;
            return listed;
        }

        static bool TryContribute(ItemSpec s)
        {
            string id = IdOf(s.Id);
            if (KyItems.FindById(id) != null) return true;
            GameObject prefab = PrefabManager.Instance != null ? PrefabManager.Instance.GetPrefab(s.Id) : null;
            if (prefab == null) return false;
            KyResult r = KyItems.Contribute(new KyContribution
            {
                Id = id,
                ModGuid = Plugin.ModGuid,
                DisplayName = s.Name,
                Prefab = prefab,
                Kind = IsMaterial(s) ? KyKind.Material : s.Kind == ItemKind.Piece ? KyKind.Piece : KyKind.Item,
                Recipe = RecipeOf(s),
                RegisterInGame = false,
                Tags = IsMaterial(s) ? new[] { "GunPowdered", s.Kind.ToString(), s.Kind == ItemKind.Deposit ? "deposit" : "ingredient" } : new[] { "GunPowdered", s.Kind.ToString() }
            });
            if (!r.Accepted) ModLog.Warn("KyItemsContributions: KY-Items refused " + id + ": " + r.Reason);
            return r.Accepted;
        }

        static KyRecipe RecipeOf(ItemSpec s)
        {
            if (!s.HasRecipe) return null;
            IList<CostLine> cost = Integrations.Items.ItemRegistry.CostsOf(s);
            var resources = new KyResource[cost.Count];
            for (int i = 0; i < resources.Length; i++) resources[i] = new KyResource(cost[i].Prefab, cost[i].Amount);
            return new KyRecipe { CraftingStation = s.Station, MinStationLevel = s.StationLevel, Amount = Integrations.Items.ItemRegistry.YieldOf(s), Resources = resources };
        }
    }
}