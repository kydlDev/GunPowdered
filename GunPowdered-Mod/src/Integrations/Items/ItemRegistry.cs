using System;
using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Cannon.Infrastructure;
using GunPowdered.Features.Grenades.Infrastructure;
using GunPowdered.Features.Gunpowder.Infrastructure;
using GunPowdered.Features.Musket.Infrastructure;
using GunPowdered.Features.Saltpeter.Infrastructure;
using GunPowdered.Features.Water.Infrastructure;
using GunPowdered.Integrations.KyRegistry;
using GunPowdered.Integrations.Valheim;

namespace GunPowdered.Integrations.Items
{
    /// <summary>
    /// Turns the pure <see cref="ItemCatalog"/> entries into Jotunn items, pieces and deposit prefabs, cloned from vanilla placeholders (D-013).
    /// Everything is registered once, whatever the switches say, because saves and worlds store these prefabs; a feature switched off only disables
    /// the recipes and pieces (<see cref="SetEnabled"/>), so a config change applies at once (PROJECT_RULES 17).
    /// </summary>
    internal static class ItemRegistry
    {
        sealed class FeatureSet
        {
            internal readonly List<ItemSpec> Specs = new List<ItemSpec>();
            internal readonly List<Recipe> Recipes = new List<Recipe>();
            internal readonly List<Piece> Pieces = new List<Piece>();
            internal bool Created;
        }

        static readonly Dictionary<string, FeatureSet> Sets = new Dictionary<string, FeatureSet>();
        /// <summary>Recipes that wait for a world key (D-108), with their feature and the key.</summary>
        static readonly List<KeyValuePair<Recipe, KeyValuePair<string, string>>> Keyed = new List<KeyValuePair<Recipe, KeyValuePair<string, string>>>();
        static bool _hooked;

        /// <summary>Remembers what a feature adds; the game objects are created when the vanilla prefabs are available. Expected outcome: the feature's specs are queued.</summary>
        internal static bool Register(string featureId, params ItemSpec[] specs)
        {
            FeatureSet set;
            if (!Sets.TryGetValue(featureId, out set)) { set = new FeatureSet(); Sets[featureId] = set; }
            set.Specs.AddRange(specs);
            if (!_hooked)
            {
                PrefabManager.OnVanillaPrefabsAvailable += CreateAll;
                _hooked = true;
            }
            ModLog.Verbose("ItemRegistry: " + featureId + " queued " + specs.Length + " entr" + (specs.Length == 1 ? "y" : "ies"));
            return set.Specs.Count >= specs.Length;
        }

        /// <summary>Turns a feature's recipes and pieces on or off. Expected outcome: every recipe and piece of the feature has the asked state.</summary>
        internal static bool SetEnabled(string featureId, bool on)
        {
            FeatureSet set;
            if (!Sets.TryGetValue(featureId, out set)) { var returnedAt59 = set == null; if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.SetEnabled: returns {RuleLog.Shown(returnedAt59)} (line 59)"); return returnedAt59; }
            foreach (var r in set.Recipes) r.m_enabled = RecipeGate.IsOn(on, KeyOf(r), HasKey);
            foreach (var p in set.Pieces) p.m_enabled = on;
            bool state = set.Recipes.TrueForAll(r => r.m_enabled == RecipeGate.IsOn(on, KeyOf(r), HasKey)) && set.Pieces.TrueForAll(p => p.m_enabled == on);
            ModLog.Verbose("ItemRegistry: " + featureId + " recipes and pieces " + (on ? "on" : "off") + ", state ok " + state);
            return state;
        }

        /// <summary>
        /// Puts the world-key state on the recipes that wait for a key (D-108). Called before the game discovers or lists recipes (RecipeGatePatch), so the
        /// recipe shows up once Fader is dead and not a moment before. Expected outcome: the number of keyed recipes that are on.
        /// </summary>
        internal static int RefreshKeyed()
        {
            int on = 0;
            foreach (var k in Keyed)
            {
                if (k.Key == null) continue;
                bool want = RecipeGate.IsOn(IsOn(k.Value.Key), k.Value.Value, HasKey);
                if (k.Key.m_enabled != want)
                {
                    k.Key.m_enabled = want;
                    ModLog.Info("ItemRegistry: recipe " + (k.Key.m_item != null ? k.Key.m_item.name : "?") + " now " + (want ? "on" : "off") + " (world key " + k.Value.Value + " " + HasKey(k.Value.Value) + ")");
                }
                if (want) on++;
            }
            { var returnedAt85 = on; if (RuleLog.IsOn()) ModLog.Every("FeatureSet.RefreshKeyed:85", 2d, $"FeatureSet.RefreshKeyed: returns {RuleLog.Shown(returnedAt85)} (line 85)"); return returnedAt85; }
        }

        static string KeyOf(Recipe r)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.KeyOf(r={RuleLog.Shown(r)})");
            foreach (var k in Keyed) if (k.Key == r) { var returnedAt91 = k.Value.Value; if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.KeyOf: returns {RuleLog.Shown(returnedAt91)} (line 91)"); return returnedAt91; }
            { if (RuleLog.IsOn()) ModLog.Verbose("FeatureSet.KeyOf: returns null (line 92)"); return null; }
        }

        static bool HasKey(string key) => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key);

        static void CreateAll()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= CreateAll;
            foreach (var pair in Sets)
            {
                try
                {
                    // items first (the guano deposit points at the Bat Guano item), then pieces, then deposits
                    foreach (var s in pair.Value.Specs) if (s.Kind == ItemKind.Material || s.Kind == ItemKind.Tool || s.Kind == ItemKind.Weapon) CreateItem(s, pair.Value);
                }
                catch (Exception e) { ModLog.Warn("ItemRegistry: creating the items of " + pair.Key + " failed (" + e.GetType().Name + "): " + e.Message); }
            }
            foreach (var pair in Sets)
            {
                try
                {
                    foreach (var s in pair.Value.Specs) if (s.Kind == ItemKind.Piece) CreatePiece(s, pair.Value);
                    foreach (var s in pair.Value.Specs) if (s.Kind == ItemKind.Deposit) CreateDeposit(s);
                    pair.Value.Created = true;
                }
                catch (Exception e) { ModLog.Warn("ItemRegistry: creating the pieces of " + pair.Key + " failed (" + e.GetType().Name + "): " + e.Message); }
            }
            foreach (var pair in Sets) SetEnabled(pair.Key, IsOn(pair.Key));
            KyItemsContributions.Start();          // the item viewer lists every item and piece of the mod
        }

        static string FeatureOf(FeatureSet set)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.FeatureOf(set={RuleLog.Shown(set)})");
            foreach (var pair in Sets) if (pair.Value == set) { var returnedAt126 = pair.Key; if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.FeatureOf: returns {RuleLog.Shown(returnedAt126)} (line 126)"); return returnedAt126; }
            { if (RuleLog.IsOn()) ModLog.Verbose("FeatureSet.FeatureOf: returns null (line 127)"); return null; }
        }

        // The Enable switch of each feature, read at call time.
        static bool IsOn(string featureId)
        {
            if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn", 2d, $"FeatureSet.IsOn(featureId={RuleLog.Shown(featureId)})");
            switch (featureId)
            {
                case "guano": { var returnedAt136 = ModSettings.FeatureOn(ModSettings.EnableGuano); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:136", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt136)} (line 136)"); return returnedAt136; }
                case "water": { var returnedAt137 = ModSettings.FeatureOn(ModSettings.EnableWater); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:137", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt137)} (line 137)"); return returnedAt137; }
                case "saltpeter": { var returnedAt138 = ModSettings.FeatureOn(ModSettings.EnableSaltpeter); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:138", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt138)} (line 138)"); return returnedAt138; }
                case "sulfur": { var returnedAt139 = ModSettings.FeatureOn(ModSettings.EnableSulfur); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:139", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt139)} (line 139)"); return returnedAt139; }
                case "gunpowder": { var returnedAt140 = ModSettings.FeatureOn(ModSettings.EnableGunpowder); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:140", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt140)} (line 140)"); return returnedAt140; }
                case "grenades": { var returnedAt141 = ModSettings.FeatureOn(ModSettings.EnableGrenades); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:141", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt141)} (line 141)"); return returnedAt141; }
                case "cannon": { var returnedAt142 = ModSettings.FeatureOn(ModSettings.EnableCannon); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:142", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt142)} (line 142)"); return returnedAt142; }
                case "musket": { var returnedAt143 = ModSettings.FeatureOn(ModSettings.EnableMusket); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:143", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt143)} (line 143)"); return returnedAt143; }
                case "arrows": { var returnedAt144 = ModSettings.FeatureOn(ModSettings.EnableExplosiveArrows); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:144", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt144)} (line 144)"); return returnedAt144; }
                case "dynamite": { var returnedAt145 = ModSettings.FeatureOn(ModSettings.EnableDynamite); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:145", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt145)} (line 145)"); return returnedAt145; }
                case "mobilecannon": { var returnedAt146 = ModSettings.FeatureOn(ModSettings.EnableCannon); if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:146", 2d, $"FeatureSet.IsOn: returns {RuleLog.Shown(returnedAt146)} (line 146)"); return returnedAt146; }   // one switch for both cannon types
                default: { if (RuleLog.IsOn()) ModLog.Every("FeatureSet.IsOn:147", 2d, "FeatureSet.IsOn: returns false (line 147)"); return false; }
            }
        }

        static void CreateItem(ItemSpec s, FeatureSet set)
        {
            var cost = Costs(s);
            var cfg = new ItemConfig
            {
                Name = s.Name,
                Description = s.Description,
                Weight = s.Weight,
                CraftingStation = s.HasRecipe ? s.Station : null,
                MinStationLevel = s.StationLevel,
                Amount = Yield(s),
                Requirements = ToRequirements(cost)
            };
            var item = new CustomItem(s.Id, s.CloneOf, cfg);
            if (!ItemManager.Instance.AddItem(item)) { ModLog.Warn("ItemRegistry: Jotunn refused item " + s.Id); return; }

            var shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_maxStackSize = s.Stack;
            BundleVisuals.Apply(s.Id, item.ItemPrefab);
            Sprite icon = BundleVisuals.Icon(s.Id);
            if (icon != null) shared.m_icons = new[] { icon };
            if (item.Recipe != null && item.Recipe.Recipe != null)
            {
                set.Recipes.Add(item.Recipe.Recipe);
                if (s.RequiredGlobalKey != null)
                    Keyed.Add(new KeyValuePair<Recipe, KeyValuePair<string, string>>(item.Recipe.Recipe, new KeyValuePair<string, string>(FeatureOf(set), s.RequiredGlobalKey)));
            }
            if (s.Id == GunPowderedPrefabs.SpikedGrenade) GrenadeSetup.Configure(item.ItemPrefab);
            if (s.Id == GunPowderedPrefabs.CannonBall) CannonSetup.ConfigureBall(item.ItemPrefab);
            if (s.Id == GunPowderedPrefabs.Musket) MusketSetup.Configure(item.ItemPrefab);
            if (s.Id == GunPowderedPrefabs.Dynamite) GunPowdered.Features.Dynamite.Infrastructure.DynamiteSetup.Configure(item.ItemPrefab);
            if (s.Id == GunPowderedPrefabs.DeepNorthPack) GunPowdered.Features.Dynamite.Infrastructure.DynamiteSetup.ConfigurePack(item.ItemPrefab);
            if (s.Id == GunPowderedPrefabs.ExplosiveArrow) GunPowdered.Features.ExplosiveArrows.Infrastructure.ExplosiveArrowSetup.Configure(item.ItemPrefab);
            ModLog.Verbose("ItemRegistry: item " + s.Id + " cloned from " + s.CloneOf + ", stack " + s.Stack + ", recipe " + (item.Recipe != null) + " at station level " + s.StationLevel);
        }

        static void CreatePiece(ItemSpec s, FeatureSet set)
        {
            var cfg = new PieceConfig
            {
                Name = s.Name,
                Description = s.Description,
                PieceTable = "Hammer",
                Category = s.Id == GunPowderedPrefabs.CannonStock || s.Id == GunPowderedPrefabs.MobileCannon ? KyCategory.For(s.Id) : s.Id == GunPowderedPrefabs.RainBarrel ? "Misc" : "Crafting",
                CraftingStation = s.HasRecipe ? s.Station : ItemCatalog.Workbench,           // the Mobile Cannon is built beside a Forge, the other pieces at a Workbench
                Requirements = ToRequirements(Costs(s))
            };
            var piece = new CustomPiece(s.Id, s.CloneOf, cfg);
            if (!PieceManager.Instance.AddPiece(piece)) { ModLog.Warn("ItemRegistry: Jotunn refused piece " + s.Id); return; }

            var station = piece.PiecePrefab.GetComponent<CraftingStation>();
            if (station != null) station.m_name = s.Name;          // the station's own name must differ from the workbench or recipes mix up
            AddBehaviour(s, piece.PiecePrefab);
            BundleVisuals.Apply(s.Id, piece.PiecePrefab);
            if (s.Id == GunPowderedPrefabs.CannonStock) CannonSetup.ConfigureStock(piece.PiecePrefab);     // needs the bundle look in place
            if (s.Id == GunPowderedPrefabs.MobileCannon) GunPowdered.Features.MobileCannon.Infrastructure.MobileCannonSetup.Configure(piece.PiecePrefab);   // cart clone, D-104
            Sprite icon = BundleVisuals.Icon(s.Id);
            if (icon != null) piece.Piece.m_icon = icon;
            set.Pieces.Add(piece.Piece);
            ModLog.Verbose("ItemRegistry: piece " + s.Id + " cloned from " + s.CloneOf);
        }

        // What the piece does in the world. Pieces cloned from the chest lose its container (they take items by interacting, not through a window).
        static void AddBehaviour(ItemSpec s, GameObject prefab)
        {
            if (s.Id == GunPowderedPrefabs.LeachingVat)
            {
                Container chest = prefab.GetComponent<Container>();
                if (chest != null) UnityEngine.Object.DestroyImmediate(chest);
                prefab.AddComponent<LeachingVat>();
            }
            else if (s.Id == GunPowderedPrefabs.PowderMill)
            {
                // D-137: the mill grinds by itself (PowderMill, a batch station like the Leaching Station) - the workbench's crafting station goes, so there is
                // no Craft menu and no roof rule (D-040: it works in the open); the arm turns while it grinds (PowderMillSpin)
                CraftingStation station = prefab.GetComponent<CraftingStation>();
                if (station != null) UnityEngine.Object.DestroyImmediate(station);
                prefab.AddComponent<PowderMill>();
                prefab.AddComponent<PowderMillSpin>();
                ModLog.Verbose("ItemRegistry: Powder Mill is automatic (crafting station " + (station != null ? "removed" : "was missing") + ")");
            }
            else if (s.Id == GunPowderedPrefabs.CannonStock || s.Id == GunPowderedPrefabs.MobileCannon)
            {
                // cloned from the chest: the stock holds no items, the cannon is mounted by interacting (CannonMount, added by CannonSetup after the bundle look);
                // the Mobile Cannon holds none either (its behaviour comes with the T-009f stages)
                Container chest = prefab.GetComponent<Container>();
                if (chest != null) UnityEngine.Object.DestroyImmediate(chest);
            }
            else if (StationOf(s.Id) != null)
            {
                // D-129 / D-137: cloned from the chest for its piece and network view; an extension holds no items, it only counts for its station near it
                Container chest = prefab.GetComponent<Container>();
                if (chest != null) UnityEngine.Object.DestroyImmediate(chest);
                var extension = prefab.AddComponent<BatchStationExtension>();
                extension.Station = StationOf(s.Id);
                extension.Kind = s.Id;
                // D-138: the two flametal ovens show a fire behind their panes while their station runs
                if (s.Id == GunPowderedPrefabs.FlametalHeater || s.Id == GunPowderedPrefabs.MillOven) prefab.AddComponent<ExtensionFire>();
                ModLog.Verbose("ItemRegistry: " + s.Id + " is an extension of " + extension.Station);
            }
            else if (s.Id == GunPowderedPrefabs.RainBarrel)
            {
                Container chest = prefab.GetComponent<Container>();
                if (chest != null) UnityEngine.Object.DestroyImmediate(chest);
                prefab.AddComponent<RainBarrel>();
            }
        }

        // the station an extension piece belongs to (its prefab name), or null for a piece that is no extension
        static string StationOf(string id)
        {
            foreach (ItemSpec e in ItemCatalog.LeachingExtensions)
            {
                if (e.Id == id) { if (RuleLog.IsOn()) ModLog.Every("ItemRegistry.StationOf:vat", 2d, $"ItemRegistry.StationOf({id}): returns the Leaching Station"); return GunPowderedPrefabs.LeachingVat; }
            }
            foreach (ItemSpec e in ItemCatalog.MillExtensions)
            {
                if (e.Id == id) { if (RuleLog.IsOn()) ModLog.Every("ItemRegistry.StationOf:mill", 2d, $"ItemRegistry.StationOf({id}): returns the Powder Mill"); return GunPowderedPrefabs.PowderMill; }
            }
            if (RuleLog.IsOn()) ModLog.Every("ItemRegistry.StationOf:none", 2d, $"ItemRegistry.StationOf({id}): returns null");
            return null;
        }

        // Deposits are Pickables: the vanilla Pickable respawns by itself, so the 3-day refill (D-019) is its respawn time.
        static void CreateDeposit(ItemSpec s)
        {
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(s.Id, s.CloneOf);
            if (prefab == null) { ModLog.Warn("ItemRegistry: could not clone " + s.CloneOf + " for " + s.Id); return; }
            // the vanilla sulfur rock can also be smashed for its drops; a deposit of ours is only picked, with the tool the PickablePatch asks for
            var smash = prefab.GetComponent<Destructible>();
            if (smash != null) UnityEngine.Object.DestroyImmediate(smash);
            var smashDrops = prefab.GetComponent<DropOnDestroyed>();
            if (smashDrops != null) UnityEngine.Object.DestroyImmediate(smashDrops);
            var pick = prefab.GetComponent<Pickable>();
            if (pick != null)
            {
                string yields = s.Id == GunPowderedPrefabs.GuanoDeposit ? GunPowderedPrefabs.BatGuano : ItemCatalog.Sulfur;
                var item = ItemManager.Instance.GetItem(yields);
                GameObject drop = item != null ? item.ItemPrefab : PrefabManager.Instance.GetPrefab(yields);
                if (drop != null) pick.m_itemPrefab = drop;
                pick.m_amount = 1;
                pick.m_overrideName = s.Name;          // else the hover still says "Sulfur Rock"
                // the vanilla sulfur rock also drops extra Sulfur from its own table: a mud pick gave 4 instead of 2 and a guano pick dropped Sulfur (run core1)
                int extra = pick.m_extraDrops != null && pick.m_extraDrops.m_drops != null ? pick.m_extraDrops.m_drops.Count : 0;
                pick.m_extraDrops = new DropTable();
                ModLog.Verbose("ItemRegistry: deposit " + s.Id + " - " + extra + " extra drop entr" + (extra == 1 ? "y" : "ies") + " of " + s.CloneOf + " removed");
                pick.m_respawnTimeMinutes = (float)(DepositRefill.RefillAt(0d, ModSettings.DepositRefillDays.Value, DepositRefill.DefaultDayLengthSeconds) / 60d);
                pick.m_hideWhenPicked = null;
            }
            BundleVisuals.Apply(s.Id, prefab);
            // a deposit is hidden while it is not full (D-056 d): the Pickable hides its model when picked and shows it again at the refill
            Transform model = prefab.transform.Find("gp_model");
            if (pick != null && model != null) pick.m_hideWhenPicked = model.gameObject;
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, true));
            ModLog.Verbose("ItemRegistry: deposit " + s.Id + " cloned from " + s.CloneOf + ", respawn " + (pick != null ? pick.m_respawnTimeMinutes.ToString("0.#") + " min" : "no Pickable"));
        }

        // Costs and yields come from the catalog (the Powder Mill's Gunpowder has no Craft-menu recipe since D-137).
        internal static IList<CostLine> CostsOf(ItemSpec s) => Costs(s);

        static IList<CostLine> Costs(ItemSpec s)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.Costs({s.Id}): returns {s.Cost.Count} line(s)");
            return s.Cost;
        }

        internal static int YieldOf(ItemSpec s) => Yield(s);

        static int Yield(ItemSpec s)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.Yield({s.Id}): returns {s.Yield}");
            return s.Yield;
        }

        static RequirementConfig[] ToRequirements(IList<CostLine> cost)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.ToRequirements(cost={RuleLog.Shown(cost)})");
            var list = new RequirementConfig[cost.Count];
            for (int i = 0; i < list.Length; i++) list[i] = new RequirementConfig { Item = cost[i].Prefab, Amount = cost[i].Amount, Recover = true };
            { var returnedAt308 = list; if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.ToRequirements: returns {RuleLog.Shown(returnedAt308)} (line 308)"); return returnedAt308; }
        }
    }
}
