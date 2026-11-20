using System;
using GunPowdered.Core;

namespace GunPowdered.Integrations.Valheim
{
    /// <summary>
    /// Everything that differs between two automatic batch stations (BatchStation): names, the ZDO keys and RPC names (kept per station, so saved
    /// stations and the TestLab tests keep working), the three inputs and the output, and the live numbers (read from the Tunables each time).
    /// </summary>
    internal sealed class BatchStationConfig
    {
        /// <summary>Lower-case name for log lines ("leaching station").</summary>
        internal string Label { get; set; }
        /// <summary>The name the player sees ("Leaching Station").</summary>
        internal string HoverName { get; set; }
        /// <summary>The station's prefab name: its extensions carry it (BatchStationExtension.Station).</summary>
        internal string StationKey { get; set; }
        internal BatchStationSpec Spec { get; set; }
        /// <summary>"Leaching" / "Grinding": the hover text while a run goes.</summary>
        internal string RunVerb { get; set; }
        /// <summary>The message when nothing was taken or added while a run goes ("The saltpeter is forming").</summary>
        internal string RunningMessage { get; set; }

        internal string KeyState { get; set; }
        internal string KeyFirst { get; set; }
        internal string KeySecond { get; set; }
        internal string KeyThird { get; set; }
        internal string KeyStart { get; set; }
        internal string KeyMinutes { get; set; }
        internal string KeyUnits { get; set; }
        internal string KeyOut { get; set; }
        internal string KeyReady { get; set; }

        internal string RpcAdd { get; set; }
        internal string RpcCollect { get; set; }
        internal string RpcCollected { get; set; }

        /// <summary>The inputs in rule order: the item's shared name ($item_...) and its prefab.</summary>
        internal string[] InputSharedNames { get; set; }
        internal string[] InputPrefabs { get; set; }
        /// <summary>The input that a player can also put in alone by holding it and using the station (the Leaching Station's Wood); null = none.</summary>
        internal string HeldInputSharedName { get; set; }

        internal string OutputPrefab { get; set; }
        internal string OutputName { get; set; }

        internal Func<BatchAmounts> Unit { get; set; }
        internal Func<int> OutputPerUnit { get; set; }
        /// <summary>Minutes of a run at a step (from the Tunables).</summary>
        internal Func<int, double> MinutesAt { get; set; }
        /// <summary>Is the station's feature switched on (Enable Mod + its own Enable).</summary>
        internal Func<bool> FeatureOn { get; set; }
    }
}
