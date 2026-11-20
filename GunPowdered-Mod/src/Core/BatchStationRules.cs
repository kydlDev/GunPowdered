using System;
using System.Collections.Generic;

namespace GunPowdered.Core
{
    /// <summary>
    /// The rules every automatic batch station of the mod shares (Leaching Station D-129 / D-132, Powder Mill D-137). Each DIFFERENT extension piece near
    /// the station is one step (0 .. spec.MaxStep). A step makes the full batch one base batch bigger and the run shorter by a fixed number of minutes. The
    /// station holds at most its full batch; one use fills half of it, or what the player has, at any time. Whenever the station is idle and holds at least
    /// one whole base batch it starts by itself and processes the whole base batches inside (up to the step's full batch); the rest waits. The output of a
    /// finished run piles up until a player takes it all. Pure: the game adapter passes in the amounts and the extension kinds it found.
    /// </summary>
    internal static class BatchStationRules
    {
        /// <summary>A run never gets shorter than this, whatever the settings say.</summary>
        internal const double MinMinutes = 1d;

        /// <summary>The station's step: the number of DIFFERENT extension kinds in range, at most spec.MaxStep. Expected outcome: 0 for none, null or empty kinds; duplicates count once.</summary>
        internal static int Step(BatchStationSpec spec, IEnumerable<string> kindsInRange)
        {
            if (kindsInRange == null) { if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.Step:null", 2d, "BatchStationRules.Step: returns 0 (no kinds)"); return 0; }
            var distinct = new HashSet<string>(StringComparer.Ordinal);
            foreach (string kind in kindsInRange)
            {
                if (!string.IsNullOrEmpty(kind)) distinct.Add(kind);
            }
            int step = Math.Min(distinct.Count, spec.MaxStep);
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.Step", 2d, $"BatchStationRules.Step: {distinct.Count} kind(s) in range, max {spec.MaxStep}, returns {step}");
            return step;
        }

        /// <summary>How many base batches a full batch holds at this step. Expected outcome: step + 1, the step clamped to 0 .. spec.MaxStep.</summary>
        internal static int UnitsPerBatch(BatchStationSpec spec, int step)
        {
            int units = Clamp(spec, step) + 1;
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.UnitsPerBatch", 2d, $"BatchStationRules.UnitsPerBatch(step={step}): returns {units}");
            return units;
        }

        /// <summary>The full batch at this step: the base batch times <see cref="UnitsPerBatch"/>. Expected outcome: never negative.</summary>
        internal static BatchAmounts FullBatch(BatchStationSpec spec, BatchAmounts unit, int step)
        {
            int n = UnitsPerBatch(spec, step);
            var full = new BatchAmounts(Math.Max(0, unit.First) * n, Math.Max(0, unit.Second) * n, Math.Max(0, unit.Third) * n);
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.FullBatch", 2d, $"BatchStationRules.FullBatch(step={step}): returns {Text(full)}");
            return full;
        }

        /// <summary>What one use puts in at most: half of the full batch, rounded up. Expected outcome: (full + 1) / 2 for each input.</summary>
        internal static BatchAmounts FillPerUse(BatchStationSpec spec, BatchAmounts unit, int step)
        {
            BatchAmounts full = FullBatch(spec, unit, step);
            var half = new BatchAmounts((full.First + 1) / 2, (full.Second + 1) / 2, (full.Third + 1) / 2);
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.FillPerUse", 2d, $"BatchStationRules.FillPerUse(step={step}): returns {Text(half)}");
            return half;
        }

        /// <summary>
        /// What one use takes from the player: per input the smallest of the fill per use, the room left in the station and what the player has.
        /// Expected outcome: never negative, never more than any of the three.
        /// </summary>
        internal static BatchAmounts Take(BatchAmounts have, BatchAmounts full, BatchAmounts perUse, BatchAmounts player)
        {
            var take = new BatchAmounts(
                One(have.First, full.First, perUse.First, player.First),
                One(have.Second, full.Second, perUse.Second, player.Second),
                One(have.Third, full.Third, perUse.Third, player.Third));
            if (RuleLog.IsOn()) RuleLog.Verbose($"BatchStationRules.Take(have={Text(have)}, full={Text(full)}, perUse={Text(perUse)}, player={Text(player)}): returns {Text(take)}");
            return take;
        }

        static int One(int have, int full, int perUse, int player)
        {
            int room = Math.Max(0, full - Math.Max(0, have));
            int one = Math.Max(0, Math.Min(Math.Min(room, perUse), player));
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.One", 2d, $"BatchStationRules.One(have={have}, full={full}, perUse={perUse}, player={player}): returns {one}");
            return one;
        }

        /// <summary>
        /// How many whole base batches a run processes: the fewest of the three inputs allow, at most <see cref="UnitsPerBatch"/> of the step.
        /// Expected outcome: 0 below one base batch or with a bad base batch (any part below 1).
        /// </summary>
        internal static int Units(BatchStationSpec spec, BatchAmounts have, BatchAmounts unit, int step)
        {
            if (unit.First < 1 || unit.Second < 1 || unit.Third < 1) { if (RuleLog.IsOn()) RuleLog.Verbose($"BatchStationRules.Units: returns 0 (bad base batch {Text(unit)})"); return 0; }
            int units = Math.Min(Math.Min(have.First / unit.First, have.Second / unit.Second), have.Third / unit.Third);
            units = Math.Max(0, Math.Min(units, UnitsPerBatch(spec, step)));
            if (RuleLog.IsOn()) RuleLog.Verbose($"BatchStationRules.Units(have={Text(have)}, unit={Text(unit)}, step={step}): returns {units}");
            return units;
        }

        /// <summary>What a run of this many base batches uses up. Expected outcome: the base batch times the count (0 for none).</summary>
        internal static BatchAmounts Used(BatchAmounts unit, int units)
        {
            int n = Math.Max(0, units);
            var used = new BatchAmounts(unit.First * n, unit.Second * n, unit.Third * n);
            if (RuleLog.IsOn()) RuleLog.Verbose($"BatchStationRules.Used(units={units}): returns {Text(used)}");
            return used;
        }

        /// <summary>Does an idle station start a run now: at least one whole base batch inside. A running station never starts a second one. Expected outcome: idle and Units >= 1.</summary>
        internal static bool StartsNow(BatchStationSpec spec, bool idle, BatchAmounts have, BatchAmounts unit, int step)
        {
            bool starts = idle && Units(spec, have, unit, step) >= 1;
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.StartsNow", 2d, $"BatchStationRules.StartsNow(idle={idle}, have={Text(have)}, step={step}): returns {starts}");
            return starts;
        }

        /// <summary>The output waiting after a run of <paramref name="runOutput"/> ended. Expected outcome: the pile plus the run, never less than the pile.</summary>
        internal static int Pile(int ready, int runOutput)
        {
            int pile = Math.Max(0, ready) + Math.Max(0, runOutput);
            if (RuleLog.IsOn()) RuleLog.Verbose($"BatchStationRules.Pile(ready={ready}, runOutput={runOutput}): returns {pile}");
            return pile;
        }

        /// <summary>Output of a run of this many base batches. Expected outcome: units x outputPerUnit, 0 for no units or a bad setting.</summary>
        internal static int Output(int units, int outputPerUnit)
        {
            int output = units < 1 || outputPerUnit < 1 ? 0 : units * outputPerUnit;
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.Output", 2d, $"BatchStationRules.Output(units={units}, outputPerUnit={outputPerUnit}): returns {output}");
            return output;
        }

        /// <summary>Which input a sent item is: its index in the station's input prefabs (0..2), or -1 for an item the station does not take. Expected outcome: -1 for null / empty.</summary>
        internal static int SlotOf(string prefab, string[] inputPrefabs)
        {
            int slot = string.IsNullOrEmpty(prefab) || inputPrefabs == null ? -1 : Array.IndexOf(inputPrefabs, prefab);
            if (RuleLog.IsOn()) RuleLog.Verbose($"BatchStationRules.SlotOf(prefab={prefab}): returns {slot}");
            return slot;
        }

        /// <summary>Does the input slot still have room below the full batch. Expected outcome: false for a slot outside 0..2.</summary>
        internal static bool HasRoom(BatchAmounts have, BatchAmounts full, int slot)
        {
            bool room = slot == 0 ? have.First < full.First : slot == 1 ? have.Second < full.Second : slot == 2 && have.Third < full.Third;
            if (RuleLog.IsOn()) RuleLog.Verbose($"BatchStationRules.HasRoom(have={Text(have)}, full={Text(full)}, slot={slot}): returns {room}");
            return room;
        }

        /// <summary>The amounts with one more item in the slot. Expected outcome: unchanged for a slot outside 0..2.</summary>
        internal static BatchAmounts AddOne(BatchAmounts have, int slot)
        {
            var after = new BatchAmounts(have.First + (slot == 0 ? 1 : 0), have.Second + (slot == 1 ? 1 : 0), have.Third + (slot == 2 ? 1 : 0));
            if (RuleLog.IsOn()) RuleLog.Verbose($"BatchStationRules.AddOne(have={Text(have)}, slot={slot}): returns {Text(after)}");
            return after;
        }
        /// <summary>What is missing for one base batch, e.g. "Needs 3 more Water and 2 more Wood". Expected outcome: empty when the station holds at least one base batch.</summary>
        internal static string Missing(BatchStationSpec spec, BatchAmounts have, BatchAmounts unit)
        {
            var parts = new List<string>(3);
            if (have.First < unit.First) parts.Add($"{unit.First - Math.Max(0, have.First)} more {spec.FirstName}");
            if (have.Second < unit.Second) parts.Add($"{unit.Second - Math.Max(0, have.Second)} more {spec.SecondName}");
            if (have.Third < unit.Third) parts.Add($"{unit.Third - Math.Max(0, have.Third)} more {spec.ThirdName}");
            string text = parts.Count == 0 ? string.Empty : "Needs " + Join(parts);
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.Missing", 2d, $"BatchStationRules.Missing(have={Text(have)}, unit={Text(unit)}): returns '{text}'");
            return text;
        }

        static string Join(List<string> parts)
        {
            if (parts.Count == 1) { if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.Join:1", 2d, "BatchStationRules.Join: returns one part"); return parts[0]; }
            string text = string.Join(", ", parts.GetRange(0, parts.Count - 1)) + " and " + parts[parts.Count - 1];
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.Join", 2d, $"BatchStationRules.Join: returns '{text}'");
            return text;
        }

        /// <summary>Real minutes of one run at this step: base minus minutes per step times the step, never below <see cref="MinMinutes"/>.</summary>
        internal static double Minutes(BatchStationSpec spec, int step, double baseMinutes, double minutesPerStep)
        {
            double minutes = Math.Max(MinMinutes, baseMinutes - Math.Max(0d, minutesPerStep) * Clamp(spec, step));
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.Minutes", 2d, $"BatchStationRules.Minutes(step={step}, base={baseMinutes}, perStep={minutesPerStep}): returns {minutes}");
            return minutes;
        }

        /// <summary>Is a run started at <paramref name="startSeconds"/> (world clock) and lasting <paramref name="minutes"/> done at <paramref name="nowSeconds"/>. A clock that went back (an older save) is not done.</summary>
        internal static bool IsDone(double startSeconds, double nowSeconds, double minutes)
        {
            bool done = nowSeconds >= startSeconds + minutes * 60d;
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.IsDone", 2d, $"BatchStationRules.IsDone(start={startSeconds}, now={nowSeconds}, minutes={minutes}): returns {done}");
            return done;
        }

        /// <summary>Seconds left of a run, never below 0. Expected outcome: 0 exactly when <see cref="IsDone"/>.</summary>
        internal static double SecondsLeft(double startSeconds, double nowSeconds, double minutes)
        {
            double left = Math.Max(0d, startSeconds + minutes * 60d - nowSeconds);
            if (RuleLog.IsOn()) RuleLog.Every("BatchStationRules.SecondsLeft", 2d, $"BatchStationRules.SecondsLeft(start={startSeconds}, now={nowSeconds}, minutes={minutes}): returns {left}");
            return left;
        }
        static int Clamp(BatchStationSpec spec, int step) => Math.Max(0, Math.Min(step, spec.MaxStep));

        /// <summary>The amounts as "first/second/third" for log lines.</summary>
        internal static string Text(BatchAmounts a) => $"{a.First}/{a.Second}/{a.Third}";
    }
}
