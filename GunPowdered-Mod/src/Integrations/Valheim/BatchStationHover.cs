using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Integrations.Valheim
{
    /// <summary>
    /// The lines of a batch station's hover text (Leaching Station D-132, Powder Mill D-137): what the upgrades give, what runs or is missing, what it
    /// holds. Split from <see cref="BatchStation"/> (class size, PROJECT_RULES 7); the station decides when to show them.
    /// </summary>
    internal static class BatchStationHover
    {
        /// "Upgrades N/M: up to a X + b Y + c Z -> n Output, m min" for the station's step.
        internal static string UpgradeLine(BatchStationConfig c, int step)
        {
            BatchAmounts full = BatchStationRules.FullBatch(c.Spec, c.Unit(), step);
            string line = $"Upgrades {step}/{c.Spec.MaxStep}: up to {full.First} {c.Spec.FirstName} + {full.Second} {c.Spec.SecondName} + {full.Third} {c.Spec.ThirdName}"
                + $" -> {BatchStationRules.Output(BatchStationRules.UnitsPerBatch(c.Spec, step), c.OutputPerUnit())} {c.OutputName}, {c.MinutesAt(step)} min";
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.UpgradeLine:" + c.Label, 2d, $"BatchStationHover.UpgradeLine: returns {line}");
            return line;
        }

        /// "X a/A, Y b/B, Z c/C": what the station holds against its step's full batch.
        internal static string Holds(BatchStationConfig c, BatchAmounts held, BatchAmounts full)
        {
            BatchStationSpec s = c.Spec;
            string text = $"{s.FirstName} {held.First}/{full.First}, {s.SecondName} {held.Second}/{full.Second}, {s.ThirdName} {held.Third}/{full.Third}";
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.Holds:" + c.Label, 2d, $"BatchStationHover.Holds: returns {text}");
            return text;
        }

        /// While running: "&lt;verb&gt; N &lt;output&gt; (M min left)" from the run's start and minutes in the ZDO; idle: "Cannot start yet: ..." or "Starting".
        internal static string RunLine(BatchStationConfig c, ZDO zdo, bool running, BatchAmounts held, BatchAmounts unit)
        {
            string line;
            if (running)
            {
                double left = BatchStationRules.SecondsLeft(zdo.GetLong(c.KeyStart, 0L) / 1000d, ZNet.instance.GetTimeSeconds(), zdo.GetFloat(c.KeyMinutes, (float)c.MinutesAt(0)));
                line = $"{c.RunVerb} {zdo.GetInt(c.KeyOut, 0)} {c.OutputName} ({Mathf.CeilToInt((float)(left / 60d))} min left)";
            }
            else
            {
                string missing = BatchStationRules.Missing(c.Spec, held, unit);
                line = missing.Length > 0 ? "Cannot start yet: " + missing : "Starting";
            }
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.RunLine:" + c.Label, 2d, $"BatchStationHover.RunLine(running {running}): returns {line}");
            return line;
        }
    }
}
