using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Saltpeter.Domain;
using GunPowdered.Integrations.Valheim;

namespace GunPowdered.Features.Saltpeter.Infrastructure
{
    /// <summary>
    /// The Leaching Station piece (D-004, D-024, D-129, D-132): a stone-filled barrel that drips into a cauldron over a hearth. It is an automatic batch
    /// station (BatchStation, shared with the Powder Mill since D-137): inputs Bat Guano / Water / Wood (base batch 10 / 10 / 4 -> 5 Saltpeter), up to
    /// 5 extension steps (LeachingUpgradeRules), 30 minutes minus 5 per step. Holding Wood and using the station puts in Wood only. The ZDO keys
    /// ("gp_vat_*") and RPC names ("GP_Vat*") stay as before, so saved stations and the TestLab tests keep working. The model's state parts
    /// (cauldron_fire, cauldron_liquid, leach_flow, cauldron_salt) show the run, the liquid inside and the Saltpeter pile on every client.
    /// </summary>
    internal sealed class LeachingVat : BatchStation
    {
        const string WoodSharedName = "$item_wood";

        static readonly BatchStationConfig Settings = new BatchStationConfig
        {
            Label = "leaching station",
            HoverName = ItemCatalog.LeachingVat.Name,
            StationKey = GunPowderedPrefabs.LeachingVat,
            Spec = LeachingUpgradeRules.Spec,
            RunVerb = "Leaching",
            RunningMessage = "The saltpeter is forming",
            KeyState = "gp_vat_state", KeyFirst = "gp_vat_guano", KeySecond = "gp_vat_water", KeyThird = "gp_vat_fuel",
            KeyStart = "gp_vat_start", KeyMinutes = "gp_vat_minutes", KeyUnits = "gp_vat_units", KeyOut = "gp_vat_out", KeyReady = "gp_vat_ready",
            RpcAdd = "GP_VatAdd", RpcCollect = "GP_VatCollect", RpcCollected = "GP_VatCollected",
            InputSharedNames = new[] { ItemCatalog.BatGuano.Name, ItemCatalog.Water.Name, WoodSharedName },
            InputPrefabs = new[] { GunPowderedPrefabs.BatGuano, GunPowderedPrefabs.Water, "Wood" },
            HeldInputSharedName = WoodSharedName,
            OutputPrefab = GunPowderedPrefabs.Saltpeter,
            OutputName = ItemCatalog.Saltpeter.Name,
            Unit = () => new BatchAmounts(ModSettings.VatGuano.Value, ModSettings.VatWater.Value, ModSettings.VatFuel.Value),
            OutputPerUnit = () => ModSettings.VatSaltpeter.Value,
            MinutesAt = step => LeachingUpgradeRules.Minutes(step, ModSettings.VatBaseMinutes.Value, ModSettings.VatMinutesPerStep.Value),
            FeatureOn = () => ModSettings.FeatureOn(ModSettings.EnableSaltpeter)
        };

        GameObject _fire, _liquid, _flow, _salt;

        protected override BatchStationConfig Config { get { return Settings; } }

        protected override void FindParts()
        {
            _fire = Part("cauldron_fire"); _liquid = Part("cauldron_liquid"); _flow = Part("leach_flow"); _salt = Part("cauldron_salt");
            if (RuleLog.IsOn()) ModLog.Verbose($"LeachingVat.FindParts: fire {_fire != null}, liquid {_liquid != null}, flow {_flow != null}, salt {_salt != null}");
        }

        // the fire burns during a run; liquid and drip while a run goes or guano / water wait inside; the salt shows while Saltpeter is piled up
        protected override void ShowState(bool running, BatchAmounts held, int ready)
        {
            bool wet = running || held.First > 0 || held.Second > 0;
            SetActive(_fire, running);
            SetActive(_liquid, wet);
            SetActive(_flow, wet);
            SetActive(_salt, ready > 0);
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.ShowState", 2d, $"LeachingVat.ShowState(running={running}, held={BatchStationRules.Text(held)}, ready={ready}): wet {wet}");
        }
    }
}
