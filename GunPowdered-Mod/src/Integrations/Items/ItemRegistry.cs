using System;
using System.Collections.Generic;
using KydlCore.Common;
using KydlCore.Features.Items.Contracts;
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
    /// Turns the pure <see cref="ItemCatalog"/> entries into KydlCore items (KyItems), pieces and deposit prefabs (KyPrefabs), cloned from vanilla placeholders (D-013).
    /// Everything is registered once, whatever the switches say, because saves and worlds store these prefabs; a feature switched off only disables
    /// the recipes and pieces (<see cref="SetEnabled"/>), so a config change applies at once (PROJECT_RULES 17).
    /// </summary>
    internal static class ItemRegistry
    {
        sealed class FeatureSet
        {
            internal readonly List<ItemSpec> Specs = new List<ItemSpec>();
            internal readonly List<string> RecipeIds = new List<string>();
            internal readonly List<Piece> Pieces = new List<Piece>();
            internal bool Created;
        }

        static readonly Dictionary<string, FeatureSet> Sets = new Dictionary<string, FeatureSet>();
        /// <summary>Recipes that wait for a world key (D-108): item id -> key.</summary>
        static readonly Dictionary<string, string> Keyed = new Dictionary<string, string>();
        static bool _hooked, _created;
        static ObjectDB _recipeDb;
        static readonly Dictionary<string, Recipe> RecipeCache = new Dictionary<string, Recipe>();

        /// <summary>Remembers what a feature adds; the game objects are created when the vanilla prefabs are available. Expected outcome: the feature's specs are queued.</summary>
        internal static bool Register(string featureId, params ItemSpec[] specs)
        {
            FeatureSet set;
            if (!Sets.TryGetValue(featureId, out set)) { set = new FeatureSet(); Sets[featureId] = set; }
            set.Specs.AddRange(specs);
            if (!_hooked)
            {
                KyPrefabs.WhenVanillaAvailable(CreateAll);          // runs now when the vanilla prefabs are there already, and again on every later ObjectDB copy (CreateAll skips then)
                _hooked = true;
            }
            ModLog.Verbose("ItemRegistry: " + featureId + " queued " + specs.Length + " entr" + (specs.Length == 1 ? "y" : "ies"));
            return set.Specs.Count >= specs.Length;
        }

        /// <summary>Turns a feature's recipes and pieces on or off. Expected outcome: every recipe and piece of the feature has the asked state.</summary>
        internal static bool SetEnabled(string featureId, bool on)
        {
            FeatureSet set;
            if (!Sets.TryGetValue(featureId, out set)) { var returnedAt60 = set == null; if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.SetEnabled: returns {returnedAt60} because !Sets.TryGetValue(featureId, out set) (line 60)"); return returnedAt60; }
            foreach (string id in set.RecipeIds) { Recipe r = RecipeOf(id); if (r != null) r.m_enabled = RecipeGate.IsOn(on, KeyOf(id), HasKey); }
            foreach (var p in set.Pieces) p.m_enabled = on;
            bool state = set.RecipeIds.TrueForAll(id => { Recipe r = RecipeOf(id); return r == null || r.m_enabled == RecipeGate.IsOn(on, KeyOf(id), HasKey); }) && set.Pieces.TrueForAll(p => p.m_enabled == on);
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
            foreach (var pair in Sets)
            {
                bool featureOn = IsOn(pair.Key);
                foreach (string id in pair.Value.RecipeIds)
                {
                    Recipe recipe = RecipeOf(id);
                    if (recipe == null) { if (RuleLog.Due("FeatureSet.RefreshKeyed:77", 2d)) ModLog.Every("FeatureSet.RefreshKeyed:77", 2d, $"FeatureSet.RefreshKeyed: skips to the next item (continue) because the recipe of {id} is not in ObjectDB (line 77)"); continue; }
                    string key = KeyOf(id);
                    bool want = RecipeGate.IsOn(featureOn, key, HasKey);
                    if (recipe.m_enabled != want)
                    {
                        recipe.m_enabled = want;       // ObjectDB makes its recipes anew with every world: the switch state goes on again before the game lists them
                        if (key != null) ModLog.Info("ItemRegistry: recipe " + id + " now " + (want ? "on" : "off") + " (world key " + key + " " + HasKey(key) + ")");
                    }
                    if (want && key != null) on++;
                }
            }
            { var returnedAt92 = on; if (RuleLog.Due("FeatureSet.RefreshKeyed:86", 2d)) ModLog.Every("FeatureSet.RefreshKeyed:92", 2d, $"FeatureSet.RefreshKeyed: returns {returnedAt92} (line 92)"); return returnedAt92; }
        }

        static string KeyOf(string id)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.KeyOf(id={RuleLog.Shown(id)})");
            string key;
            if (Keyed.TryGetValue(id, out key)) { var returnedAt99 = key; if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.KeyOf: returns {returnedAt99} because the recipe waits for a world key (line 99)"); return returnedAt99; }
            { if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.KeyOf: returns null (line 100)"); return null; }
        }

        /// <summary>The recipe KydlCore made for an item (KyItems.Contribute: "Recipe_" + prefab name) in the ObjectDB that is awake now; null before it is there.</summary>
        static Recipe RecipeOf(string id)
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null) { if (RuleLog.Due("ItemRegistry.RecipeOf:nodb", 2d)) ModLog.Every("ItemRegistry.RecipeOf:nodb", 2d, $"ItemRegistry.RecipeOf({id}): returns null (no ObjectDB)"); return null; }
            if (!ReferenceEquals(db, _recipeDb)) { RecipeCache.Clear(); _recipeDb = db; }
            Recipe cached;
            if (RecipeCache.TryGetValue(id, out cached) && cached != null) { var returnedAt110 = cached; if (RuleLog.Due("FeatureSet.RecipeOf:110", 2d)) ModLog.Every("FeatureSet.RecipeOf:110", 2d, $"FeatureSet.RecipeOf: returns {returnedAt110} because it is cached (line 110)"); return returnedAt110; }
            string name = "Recipe_" + id;
            foreach (Recipe r in db.m_recipes)
                if (r != null && r.name == name) { RecipeCache[id] = r; { var returnedAt113 = r; if (RuleLog.Due("FeatureSet.RecipeOf:113", 2d)) ModLog.Every("FeatureSet.RecipeOf:113", 2d, $"FeatureSet.RecipeOf: returns {returnedAt113} because the name matches (line 113)"); return returnedAt113; } }
            { if (RuleLog.Due("ItemRegistry.RecipeOf:none", 2d)) ModLog.Every("ItemRegistry.RecipeOf:none", 2d, $"ItemRegistry.RecipeOf({id}): returns null (not in ObjectDB yet)"); return null; }
        }

        static bool HasKey(string key) => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key);

        static void CreateAll()
        {
            if (_created) { ModLog.Verbose("ItemRegistry.CreateAll: the prefabs exist already - returns"); return; }       // WhenVanillaAvailable runs again on every later ObjectDB copy
            _created = true;
            foreach (var pair in Sets)
            {
                try
                {
                    // items first (the guano deposit points at the Bat Guano item), then pieces, then deposits
                    foreach (var s in pair.Value.Specs) if (s.Kind == ItemKind.Material || s.Kind == ItemKind.Tool || s.Kind == ItemKind.Weapon) CreateItem(s, pair.Value);
                }
                catch (Exception e) { ModLog.Warn("ItemRegistry: creating the items of " + pair.Key + " failed (" + e.GetType().Name + "): " + e.Message + "\n" + e.StackTrace); }      // foreign code: another mod's listener ran here - its exception types are not ours to know, so the catch-all is the only catch
            }
            foreach (var pair in Sets)
            {
                try
                {
                    foreach (var s in pair.Value.Specs) if (s.Kind == ItemKind.Piece) CreatePiece(s, pair.Value);
                    foreach (var s in pair.Value.Specs) if (s.Kind == ItemKind.Deposit) CreateDeposit(s);
                    pair.Value.Created = true;
                }
                catch (Exception e) { ModLog.Warn("ItemRegistry: creating the pieces of " + pair.Key + " failed (" + e.GetType().Name + "): " + e.Message + "\n" + e.StackTrace); }      // foreign code: another mod's listener ran here - its exception types are not ours to know, so the catch-all is the only catch
            }
            foreach (var pair in Sets) SetEnabled(pair.Key, IsOn(pair.Key));
            KyItemsContributions.Start();          // the item viewer lists every item and piece of the mod
        }

        static string FeatureOf(FeatureSet set)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.FeatureOf(set={RuleLog.Shown(set)})");
            foreach (var pair in Sets) if (pair.Value == set) return pair.Key;
            { if (RuleLog.IsOn()) ModLog.Verbose($"FeatureSet.FeatureOf: returns null (line 150)"); return null; }
        }

        // The Enable switch of each feature, read at call time.
        static bool IsOn(string featureId)
        {
            if (RuleLog.Due("FeatureSet.IsOn", 2d)) ModLog.Every("FeatureSet.IsOn", 2d, $"FeatureSet.IsOn(featureId={RuleLog.Shown(featureId)})");
            switch (featureId)
            {
                case "guano": { var returnedAt159 = ModSettings.FeatureOn(ModSettings.EnableGuano); if (RuleLog.Due("FeatureSet.IsOn:137", 2d)) ModLog.Every("FeatureSet.IsOn:159", 2d, $"FeatureSet.IsOn: returns {returnedAt159} (line 159)"); return returnedAt159; }
                case "water": { var returnedAt160 = ModSettings.FeatureOn(ModSettings.EnableWater); if (RuleLog.Due("FeatureSet.IsOn:138", 2d)) ModLog.Every("FeatureSet.IsOn:160", 2d, $"FeatureSet.IsOn: returns {returnedAt160} (line 160)"); return returnedAt160; }
                case "saltpeter": { var returnedAt161 = ModSettings.FeatureOn(ModSettings.EnableSaltpeter); if (RuleLog.Due("FeatureSet.IsOn:139", 2d)) ModLog.Every("FeatureSet.IsOn:161", 2d, $"FeatureSet.IsOn: returns {returnedAt161} (line 161)"); return returnedAt161; }
                case "sulfur": { var returnedAt162 = ModSettings.FeatureOn(ModSettings.EnableSulfur); if (RuleLog.Due("FeatureSet.IsOn:140", 2d)) ModLog.Every("FeatureSet.IsOn:162", 2d, $"FeatureSet.IsOn: returns {returnedAt162} (line 162)"); return returnedAt162; }
                case "gunpowder": { var returnedAt163 = ModSettings.FeatureOn(ModSettings.EnableGunpowder); if (RuleLog.Due("FeatureSet.IsOn:141", 2d)) ModLog.Every("FeatureSet.IsOn:163", 2d, $"FeatureSet.IsOn: returns {returnedAt163} (line 163)"); return returnedAt163; }
                case "grenades": { var returnedAt164 = ModSettings.FeatureOn(ModSettings.EnableGrenades); if (RuleLog.Due("FeatureSet.IsOn:142", 2d)) ModLog.Every("FeatureSet.IsOn:164", 2d, $"FeatureSet.IsOn: returns {returnedAt164} (line 164)"); return returnedAt164; }
                case "cannon": { var returnedAt165 = ModSettings.FeatureOn(ModSettings.EnableCannon); if (RuleLog.Due("FeatureSet.IsOn:143", 2d)) ModLog.Every("FeatureSet.IsOn:165", 2d, $"FeatureSet.IsOn: returns {returnedAt165} (line 165)"); return returnedAt165; }
                case "musket": { var returnedAt166 = ModSettings.FeatureOn(ModSettings.EnableMusket); if (RuleLog.Due("FeatureSet.IsOn:144", 2d)) ModLog.Every("FeatureSet.IsOn:166", 2d, $"FeatureSet.IsOn: returns {returnedAt166} (line 166)"); return returnedAt166; }
                case "arrows": { var returnedAt167 = ModSettings.FeatureOn(ModSettings.EnableExplosiveArrows); if (RuleLog.Due("FeatureSet.IsOn:145", 2d)) ModLog.Every("FeatureSet.IsOn:167", 2d, $"FeatureSet.IsOn: returns {returnedAt167} (line 167)"); return returnedAt167; }
                case "dynamite": { var returnedAt168 = ModSettings.FeatureOn(ModSettings.EnableDynamite); if (RuleLog.Due("FeatureSet.IsOn:146", 2d)) ModLog.Every("FeatureSet.IsOn:168", 2d, $"FeatureSet.IsOn: returns {returnedAt168} (line 168)"); return returnedAt168; }
                case "mobilecannon": { var returnedAt169 = ModSettings.FeatureOn(ModSettings.EnableCannon); if (RuleLog.Due("FeatureSet.IsOn:147", 2d)) ModLog.Every("FeatureSet.IsOn:169", 2d, $"FeatureSet.IsOn: returns {returnedAt169} (line 169)"); return returnedAt169; }   // one switch for both cannon types
                default: { if (RuleLog.Due("FeatureSet.IsOn:148", 2d)) ModLog.Every("FeatureSet.IsOn:170", 2d, $"FeatureSet.IsOn: returns false (line 170)"); return false; }
            }
        }

        static void CreateItem(ItemSpec s, FeatureSet set)
        {
            GameObject prefab = KyPrefabs.Clone(s.Id, s.CloneOf);
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null) { ModLog.Warn("ItemRegistry: item " + s.Id + " not made - could not clone " + s.CloneOf + " (prefab " + (prefab != null) + ")"); return; }
            KyPrefabs.Add(prefab);          // findable by name for the set-up below (an ingredient, a projectile) before KyItems puts it into the game

            var shared = drop.m_itemData.m_shared;
            shared.m_name = s.Name;
            shared.m_description = s.Description;
            shared.m_weight = s.Weight;
            drop.m_itemData.m_dropPrefab = prefab;
            shared.m_maxStackSize = s.Stack;
            BundleVisuals.Apply(s.Id, prefab);
            Sprite icon = BundleVisuals.Icon(s.Id);
            if (icon != null) shared.m_icons = new[] { icon };
            if (s.HasRecipe)
            {
                set.RecipeIds.Add(s.Id);
                if (s.RequiredGlobalKey != null) Keyed[s.Id] = s.RequiredGlobalKey;
            }
            if (s.Id == GunPowderedPrefabs.SpikedGrenade) GrenadeSetup.Configure(prefab);
            if (s.Id == GunPowderedPrefabs.CannonBall || s.Id == GunPowderedPrefabs.FlametalCannonBall) CannonSetup.ConfigureBall(prefab);
            if (s.Id == GunPowderedPrefabs.Musket) MusketSetup.Configure(prefab);
            if (s.Id == GunPowderedPrefabs.Dynamite) GunPowdered.Features.Dynamite.Infrastructure.DynamiteSetup.Configure(prefab);
            if (s.Id == GunPowderedPrefabs.DeepNorthPack) GunPowdered.Features.Dynamite.Infrastructure.DynamiteSetup.ConfigurePack(prefab);
            if (s.Id == GunPowderedPrefabs.ExplosiveArrow) GunPowdered.Features.ExplosiveArrows.Infrastructure.ExplosiveArrowSetup.Configure(prefab);
            ItemGroundModel.AddIfBoth(prefab);           // T-017 / T-019: an item with a hand copy is drawn once on the ground
            if (!KyItemsContributions.Contribute(s, prefab, true)) { ModLog.Warn("ItemRegistry: KY-Items refused item " + s.Id); return; }
            ModLog.Verbose("ItemRegistry: item " + s.Id + " cloned from " + s.CloneOf + ", stack " + s.Stack + ", recipe " + s.HasRecipe + " at station level " + s.StationLevel);
        }

        static void CreatePiece(ItemSpec s, FeatureSet set)
        {
            GameObject prefab = KyPrefabs.Clone(s.Id, s.CloneOf);
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece == null) { ModLog.Warn("ItemRegistry: piece " + s.Id + " not made - could not clone " + s.CloneOf + " (prefab " + (prefab != null) + ")"); return; }
            KyPrefabs.Add(prefab);
            piece.m_name = s.Name;
            piece.m_description = s.Description;
            piece.m_enabled = true;

            var station = prefab.GetComponent<CraftingStation>();
            if (station != null) station.m_name = s.Name;          // the station's own name must differ from the workbench or recipes mix up
            AddBehaviour(s, prefab);
            BundleVisuals.Apply(s.Id, prefab);
            if (s.Id == GunPowderedPrefabs.CannonStock) CannonSetup.ConfigureStock(prefab);     // needs the bundle look in place
            if (s.Id == GunPowderedPrefabs.MobileCannon) GunPowdered.Features.MobileCannon.Infrastructure.MobileCannonSetup.Configure(prefab);   // cart clone, D-104
            // D-140: stations and extensions glue side by side (snap points at ground level) and never overlap; needs the bundle look in place
            if (s.Id == GunPowderedPrefabs.LeachingVat || s.Id == GunPowderedPrefabs.PowderMill || StationOf(s.Id) != null) StationSnapPoints.Configure(prefab);
            Sprite icon = BundleVisuals.Icon(s.Id);
            if (icon != null) piece.m_icon = icon;
            // KydlCore puts every piece into the Hammer's KY-Items build category (the user's own category) and fills station and requirements
            if (!KyItemsContributions.Contribute(s, prefab, true)) { ModLog.Warn("ItemRegistry: KY-Items refused piece " + s.Id); return; }
            set.Pieces.Add(piece);
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
                if (e.Id == id) { if (RuleLog.Due("ItemRegistry.StationOf:vat", 2d)) ModLog.Every("ItemRegistry.StationOf:vat", 2d, $"ItemRegistry.StationOf({id}): returns the Leaching Station"); return GunPowderedPrefabs.LeachingVat; }
            }
            foreach (ItemSpec e in ItemCatalog.MillExtensions)
            {
                if (e.Id == id) { if (RuleLog.Due("ItemRegistry.StationOf:mill", 2d)) ModLog.Every("ItemRegistry.StationOf:mill", 2d, $"ItemRegistry.StationOf({id}): returns the Powder Mill"); return GunPowderedPrefabs.PowderMill; }
            }
            if (RuleLog.Due("ItemRegistry.StationOf:none", 2d)) ModLog.Every("ItemRegistry.StationOf:none", 2d, $"ItemRegistry.StationOf({id}): returns null");
            return null;
        }

        // Deposits are Pickables: the vanilla Pickable respawns by itself, so the 2-day refill (D-154) is its respawn time.
        static void CreateDeposit(ItemSpec s)
        {
            GameObject prefab = KyPrefabs.Clone(s.Id, s.CloneOf);
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
                GameObject drop = KyPrefabs.Get(yields);
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
            KyPrefabs.Add(prefab);
            KyItemsContributions.Contribute(s, prefab, false);          // listed in KY-Items (Materials); the prefab itself goes into the network scene through KyPrefabs
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
    }
}
