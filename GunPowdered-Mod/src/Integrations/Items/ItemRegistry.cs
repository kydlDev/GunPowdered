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
using GunPowdered.Features.Saltpeter.Infrastructure;
using GunPowdered.Features.Water.Infrastructure;
using GunPowdered.Integrations.KyRegistry;

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
            if (!Sets.TryGetValue(featureId, out set)) return set == null;
            foreach (var r in set.Recipes) r.m_enabled = on;
            foreach (var p in set.Pieces) p.m_enabled = on;
            bool state = set.Recipes.TrueForAll(r => r.m_enabled == on) && set.Pieces.TrueForAll(p => p.m_enabled == on);
            ModLog.Verbose("ItemRegistry: " + featureId + " recipes and pieces " + (on ? "on" : "off") + ", state ok " + state);
            return state;
        }

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

        // The Enable switch of each feature, read at call time.
        static bool IsOn(string featureId)
        {
            switch (featureId)
            {
                case "guano": return ModSettings.FeatureOn(ModSettings.EnableGuano);
                case "water": return ModSettings.FeatureOn(ModSettings.EnableWater);
                case "saltpeter": return ModSettings.FeatureOn(ModSettings.EnableSaltpeter);
                case "sulfur": return ModSettings.FeatureOn(ModSettings.EnableSulfur);
                case "gunpowder": return ModSettings.FeatureOn(ModSettings.EnableGunpowder);
                case "grenades": return ModSettings.FeatureOn(ModSettings.EnableGrenades);
                case "cannon": return ModSettings.FeatureOn(ModSettings.EnableCannon);
                case "musket": return ModSettings.FeatureOn(ModSettings.EnableMusket);
                case "arrows": return ModSettings.FeatureOn(ModSettings.EnableExplosiveArrows);
                case "dynamite": return ModSettings.FeatureOn(ModSettings.EnableDynamite);
                case "mobilecannon": return ModSettings.FeatureOn(ModSettings.EnableMobileCannon);
                default: return false;
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
            if (item.Recipe != null && item.Recipe.Recipe != null) set.Recipes.Add(item.Recipe.Recipe);
            if (s.Id == GunPowderedPrefabs.SpikedGrenade) GrenadeSetup.Configure(item.ItemPrefab);
            if (s.Id == GunPowderedPrefabs.CannonBall) CannonSetup.ConfigureBall(item.ItemPrefab);
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
            Sprite icon = BundleVisuals.Icon(s.Id);
            if (icon != null) piece.Piece.m_icon = icon;
            set.Pieces.Add(piece.Piece);
            ModLog.Verbose("ItemRegistry: piece " + s.Id + " cloned from " + s.CloneOf);
        }

        // What the piece does in the world. The Leaching Vat keeps the chest's container (its inventory is where guano and water go); the Rain Barrel
        // loses it, because the barrel hands out water by interacting instead of opening a window.
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
                prefab.AddComponent<PowderMillSpin>();
                // cloned from the workbench, which needs a roof; the Powder Mill works in the open (user 2026-11-18, D-040)
                CraftingStation station = prefab.GetComponent<CraftingStation>();
                if (station != null) station.m_craftRequireRoof = false;
                ModLog.Verbose("ItemRegistry: Powder Mill needs no roof (station " + (station != null ? "found" : "missing") + ")");
            }
            else if (s.Id == GunPowderedPrefabs.CannonStock || s.Id == GunPowderedPrefabs.MobileCannon)
            {
                // cloned from the chest: the stock holds no items, the cannon is mounted by interacting (CannonMount, added by CannonSetup after the bundle look);
                // the Mobile Cannon holds none either (its behaviour comes with the T-009f stages)
                Container chest = prefab.GetComponent<Container>();
                if (chest != null) UnityEngine.Object.DestroyImmediate(chest);
            }
            else if (s.Id == GunPowderedPrefabs.RainBarrel)
            {
                Container chest = prefab.GetComponent<Container>();
                if (chest != null) UnityEngine.Object.DestroyImmediate(chest);
                prefab.AddComponent<RainBarrel>();
            }
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

        // The Powder Mill reads its numbers from the Tunables (D-014); everything else uses the catalog values.
        internal static IList<CostLine> CostsOf(ItemSpec s) => Costs(s);

        static IList<CostLine> Costs(ItemSpec s)
        {
            if (s.Id != GunPowderedPrefabs.Gunpowder) return s.Cost;
            return new[]
            {
                new CostLine(GunPowderedPrefabs.Saltpeter, ModSettings.MillSaltpeter.Value),
                new CostLine(ItemCatalog.Charcoal, ModSettings.MillCharcoal.Value),
                new CostLine(ItemCatalog.Sulfur, ModSettings.MillSulfur.Value)
            };
        }

        internal static int YieldOf(ItemSpec s) => Yield(s);

        static int Yield(ItemSpec s)
        {
            return s.Id == GunPowderedPrefabs.Gunpowder ? ModSettings.MillYield.Value : s.Yield;
        }

        static RequirementConfig[] ToRequirements(IList<CostLine> cost)
        {
            var list = new RequirementConfig[cost.Count];
            for (int i = 0; i < list.Length; i++) list[i] = new RequirementConfig { Item = cost[i].Prefab, Amount = cost[i].Amount, Recover = true };
            return list;
        }
    }
}
