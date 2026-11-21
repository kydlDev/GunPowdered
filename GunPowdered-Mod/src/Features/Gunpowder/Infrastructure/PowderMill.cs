using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Gunpowder.Domain;
using GunPowdered.Integrations.Valheim;

namespace GunPowdered.Features.Gunpowder.Infrastructure
{
    /// <summary>
    /// The automatic Powder Mill (D-137, user 2026-11-20): an automatic batch station like the Leaching Station (BatchStation). Inputs Saltpeter /
    /// Charcoal / Sulfur, base batch 3 / 1 / 1 -> 5 Gunpowder (the Mill* Tunables), up to 3 extension steps (MillUpgradeRules: Obsidian Grindstone,
    /// Dvergr Powder Sieve, Flametal Drying Oven), 10 minutes minus 2.5 per step. It is no crafting station any more (no Craft menu). Its state lives in
    /// the ZDO ("gp_mill_*"); the sweep arm and stones turn while it grinds (PowderMillSpin reads <see cref="IsGrinding"/>).
    /// </summary>
    internal sealed class PowderMill : BatchStation
    {
        static BatchStationConfig _settings;

        protected override BatchStationConfig Config { get { return _settings ?? (_settings = Build()); } }

        /// <summary>Is the mill grinding now (every client reads it from the ZDO).</summary>
        internal bool IsGrinding { get { return Running; } }

        // the vanilla Charcoal / Sulfur items carry their own shared names ($item_...): read them from the game once, the prefab name when the game has none
        static BatchStationConfig Build()
        {
            var config = new BatchStationConfig
            {
                Label = "powder mill",
                HoverName = ItemCatalog.PowderMill.Name,
                StationKey = GunPowderedPrefabs.PowderMill,
                Spec = MillUpgradeRules.Spec,
                RunVerb = "Grinding",
                RunningMessage = "The gunpowder is being ground",
                KeyState = "gp_mill_state", KeyFirst = "gp_mill_saltpeter", KeySecond = "gp_mill_charcoal", KeyThird = "gp_mill_sulfur",
                KeyStart = "gp_mill_start", KeyMinutes = "gp_mill_minutes", KeyUnits = "gp_mill_units", KeyOut = "gp_mill_out", KeyReady = "gp_mill_ready",
                RpcAdd = "GP_MillAdd", RpcCollect = "GP_MillCollect", RpcCollected = "GP_MillCollected",
                InputPrefabs = new[] { GunPowderedPrefabs.Saltpeter, ItemCatalog.Charcoal, ItemCatalog.Sulfur },
                InputSharedNames = new[] { ItemCatalog.Saltpeter.Name, SharedName(ItemCatalog.Charcoal), SharedName(ItemCatalog.Sulfur) },
                HeldInputSharedName = null,
                OutputPrefab = GunPowderedPrefabs.Gunpowder,
                OutputName = ItemCatalog.Gunpowder.Name,
                Unit = () => new BatchAmounts(ModSettings.MillSaltpeter.Value, ModSettings.MillCharcoal.Value, ModSettings.MillSulfur.Value),
                OutputPerUnit = () => ModSettings.MillYield.Value,
                MinutesAt = step => MillUpgradeRules.Minutes(step, ModSettings.MillBaseMinutes.Value, ModSettings.MillMinutesPerStep.Value),
                FeatureOn = () => ModSettings.FeatureOn(ModSettings.EnableGunpowder)
            };
            ModLog.Verbose($"PowderMill.Build: inputs {string.Join(", ", config.InputSharedNames)} from {string.Join(", ", config.InputPrefabs)}");
            return config;
        }

        static string SharedName(string prefab)
        {
            GameObject go = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            ItemDrop drop = go != null ? go.GetComponent<ItemDrop>() : null;
            string name = drop != null ? drop.m_itemData.m_shared.m_name : prefab;
            ModLog.Check("powder mill: shared name of " + prefab + " found in ObjectDB", drop != null ? 1 : 0, 1, 0f, "items");
            return name;
        }

        // the arm turns by itself (PowderMillSpin); nothing else of the model changes with the state
        protected override void ShowState(bool running, BatchAmounts held, int ready)
        {
            if (RuleLog.Due("PowderMill.ShowState", 2d)) ModLog.Every("PowderMill.ShowState", 2d, $"PowderMill.ShowState(running={running}, held={BatchStationRules.Text(held)}, ready={ready})");
        }
    }
}
