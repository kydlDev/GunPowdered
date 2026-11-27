using System.Collections.Generic;
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
    /// adds the items and pieces to the game through KY-Items itself (RegisterInGame = true, ItemRegistry); the deposits are listed only (their mod adds them through KyPrefabs). Ingredients (bat guano, saltpeter, gunpowder, water) and the world
    /// deposits are contributed as KyKind.Material, which the viewer shows in its Materials tab (user, 2026-11-18). Items stay listed whatever the Enable switches say: they are always registered in the game (PROJECT_RULES 17).
    /// </summary>
    internal static class KyItemsContributions
    {
        static bool _hooked;

        internal static string IdOf(string prefab) => "gunpowdered:" + prefab.ToLowerInvariant();

        /// <summary>
        /// Lists every catalog entry now and again when the prefabs are in the network scene (KyPrefabs). Expected outcome: true when the hook is in place; the entries
        /// are listed as soon as their prefabs exist.
        /// </summary>
        internal static bool Start()
        {
            if (!_hooked)
            {
                _hooked = true;
                KyPrefabs.PrefabsRegistered += OnPrefabsRegistered;
            }
            int listed = ContributeAll();
            ModLog.Verbose("KyItemsContributions: started, " + listed + " of " + Wanted().Count + " entries listed (the rest wait for the prefabs)");
            return _hooked;
        }

        /// <summary>How many of the mod's items and pieces KY-Items lists now (test hook). Deposits and ingredients count too.</summary>
        internal static int ListedCount()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"KyItemsContributions.ListedCount()");
            int n = 0;
            foreach (ItemSpec s in Wanted()) if (KyItems.FindById(IdOf(s.Id)) != null) n++;
            { var returnedAt45 = n; if (RuleLog.IsOn()) ModLog.Verbose($"KyItemsContributions.ListedCount: returns {returnedAt45} (line 45)"); return returnedAt45; }
        }

        /// <summary>Does KY-Items list this prefab of the mod now (test hook).</summary>
        internal static bool IsListed(string prefab) => KyItems.FindById(IdOf(prefab)) != null;

        /// <summary>How many the mod wants listed (every catalog entry: items, pieces, ingredients and deposits).</summary>
        internal static int WantedCount() => Wanted().Count;

        static List<ItemSpec> Wanted()
        {
            if (RuleLog.Due("KyItemsContributions.Wanted", 2d)) ModLog.Every("KyItemsContributions.Wanted", 2d, $"KyItemsContributions.Wanted()");
            var list = new List<ItemSpec>();
            foreach (ItemSpec s in ItemCatalog.All) list.Add(s);
            { var returnedAt59 = list; if (RuleLog.Due("KyItemsContributions.Wanted:59", 2d)) ModLog.Every("KyItemsContributions.Wanted:59", 2d, $"KyItemsContributions.Wanted: returns {returnedAt59} (line 59)"); return returnedAt59; }
        }

        /// <summary>Ingredients (what recipes and stations consume) and gatherables / deposits: the Materials tab of the item viewer.</summary>
        internal static bool IsMaterial(ItemSpec s) => s.Kind == ItemKind.Deposit || s.Id == GunPowderedPrefabs.BatGuano || s.Id == GunPowderedPrefabs.Saltpeter
                                                      || s.Id == GunPowderedPrefabs.Gunpowder || s.Id == GunPowderedPrefabs.Water;

        /// <summary>"listed/wanted" of the materials KY-Items holds as KyKind.Material (test hook).</summary>
        internal static string MaterialsListed()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"KyItemsContributions.MaterialsListed()");
            int listed = 0, wanted = 0;
            foreach (ItemSpec s in Wanted())
            {
                if (!IsMaterial(s)) { if (RuleLog.IsOn()) ModLog.Verbose($"KyItemsContributions.MaterialsListed: skips to the next item (continue) because !IsMaterial(s) (line 73)"); continue; }
                wanted++;
                KyEntry e = KyItems.FindById(IdOf(s.Id));
                if (e != null && e.Kind == KyKind.Material) listed++;
            }
            { var returnedAt78 = listed + "/" + wanted; if (RuleLog.IsOn()) ModLog.Verbose($"KyItemsContributions.MaterialsListed: returns {returnedAt78} (line 78)"); return returnedAt78; }
        }

        static void OnPrefabsRegistered() => ModLog.Verbose("KyItemsContributions: prefabs registered, " + ContributeAll() + " entries listed");

        static int ContributeAll()
        {
            if (RuleLog.Due("KyItemsContributions.ContributeAll", 2d)) ModLog.Every("KyItemsContributions.ContributeAll", 2d, $"KyItemsContributions.ContributeAll()");
            int listed = 0;
            foreach (ItemSpec s in Wanted()) if (TryContribute(s)) listed++;
            { var returnedAt88 = listed; if (RuleLog.Due("KyItemsContributions.ContributeAll:88", 2d)) ModLog.Every("KyItemsContributions.ContributeAll:88", 2d, $"KyItemsContributions.ContributeAll: returns {returnedAt88} (line 88)"); return returnedAt88; }
        }

        static bool TryContribute(ItemSpec s)
        {
            string id = IdOf(s.Id);
            if (KyItems.FindById(id) != null) { if (RuleLog.Due("KyItemsContributions.TryContribute:94", 2d)) ModLog.Every("KyItemsContributions.TryContribute:94", 2d, $"KyItemsContributions.TryContribute: returns true because KyItems.FindById(id) != null (line 94)"); return true; }
            GameObject prefab = KyPrefabs.Get(s.Id);
            if (prefab == null) { if (RuleLog.Due("KyItemsContributions.TryContribute:96", 2d)) ModLog.Every("KyItemsContributions.TryContribute:96", 2d, $"KyItemsContributions.TryContribute: returns false because prefab == null (line 96)"); return false; }
            { var returnedAt101 = Contribute(s, prefab, false); if (RuleLog.Due("KyItemsContributions.TryContribute:101", 2d)) ModLog.Every("KyItemsContributions.TryContribute:101", 2d, $"KyItemsContributions.TryContribute: returns {returnedAt101} (line 101)"); return returnedAt101; }
        }

        /// <summary>
        /// Gives a finished prefab of the catalog to KydlCore's KY-Items. With <paramref name="registerInGame"/> KydlCore also puts it into the game (items and
        /// their recipes into ObjectDB, pieces into the Hammer's KY-Items category); a deposit is no item, its mod adds it to the network scene (KyPrefabs).
        /// Expected outcome: true when KY-Items accepted it (or knew it already).
        /// </summary>
        internal static bool Contribute(ItemSpec s, GameObject prefab, bool registerInGame)
        {
            string id = IdOf(s.Id);
            if (KyItems.FindById(id) != null) { if (RuleLog.Due("KyItemsContributions.Contribute:known", 2d)) ModLog.Every("KyItemsContributions.Contribute:known", 2d, $"KyItemsContributions.Contribute: {id} is listed already - returns true"); return true; }
            KyResult r = KyItems.Contribute(new KyContribution
            {
                Id = id,
                ModGuid = Plugin.ModGuid,
                DisplayName = s.Name,
                Prefab = prefab,
                Kind = IsMaterial(s) ? KyKind.Material : s.Kind == ItemKind.Piece ? KyKind.Piece : KyKind.Item,
                Recipe = RecipeOf(s),
                RegisterInGame = registerInGame,
                Tags = IsMaterial(s) ? new[] { "GunPowdered", s.Kind.ToString(), s.Kind == ItemKind.Deposit ? "deposit" : "ingredient" } : new[] { "GunPowdered", s.Kind.ToString() }
            });
            if (!r.Accepted) ModLog.Warn("KyItemsContributions: KY-Items refused " + id + ": " + r.Reason);
            { var returnedAt109 = r.Accepted; if (RuleLog.Due("KyItemsContributions.Contribute:109", 2d)) ModLog.Every("KyItemsContributions.Contribute:109", 2d, $"KyItemsContributions.Contribute: returns {returnedAt109} (line 109)"); return returnedAt109; }
        }

        static KyRecipe RecipeOf(ItemSpec s)
        {
            if (RuleLog.Due("KyItemsContributions.RecipeOf", 2d)) ModLog.Every("KyItemsContributions.RecipeOf", 2d, $"KyItemsContributions.RecipeOf(s={RuleLog.Shown(s)})");
            if (!s.HasRecipe && s.Kind != ItemKind.Piece) return null;          // a piece always has a recipe: its requirements and a station (the workbench when it names none, as before)
            string station = s.HasRecipe ? s.Station : ItemCatalog.Workbench;
            IList<CostLine> cost = Integrations.Items.ItemRegistry.CostsOf(s);
            var resources = new KyResource[cost.Count];
            for (int i = 0; i < resources.Length; i++) resources[i] = new KyResource(cost[i].Prefab, cost[i].Amount);
            { var returnedAt119 = new KyRecipe { CraftingStation = station, MinStationLevel = s.StationLevel, Amount = Integrations.Items.ItemRegistry.YieldOf(s), Resources = resources }; if (RuleLog.Due("KyItemsContributions.RecipeOf:119", 2d)) ModLog.Every("KyItemsContributions.RecipeOf:119", 2d, $"KyItemsContributions.RecipeOf: returns {returnedAt119} (line 119)"); return returnedAt119; }
        }
    }
}