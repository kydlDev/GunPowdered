// kydl:read-only-observer - Humanoid here is only the player who uses the station; no creature is moved, commanded or spawned
using System;
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Integrations.Valheim
{
    /// <summary>
    /// An automatic batch station in the world (Leaching Station D-129 / D-132, Powder Mill D-137); the rules are BatchStationRules, the station's own
    /// names, keys and numbers come from <see cref="Config"/>. The station holds three inputs up to the full batch of its step (the DIFFERENT extensions
    /// near it, BatchStationExtension). Use (E) takes the whole output pile and puts in half a full batch from the inventory, or what the player has, at
    /// any time (also during a run); holding the station's "held input" (the Leaching Station's Wood) and using the station puts in that input only.
    /// Whenever the station is idle and holds at least one base batch it starts by itself and processes the whole base batches inside, up to the step's
    /// batch; the rest waits. The output of a finished run piles up and the next run starts at once while a base batch is inside.
    /// Like the game's Smelter (D-035) every item is one request named by its prefab; the owner counts it only when the station takes that item and has room.
    /// Server-authoritative (D-005): only the owner of the network object changes the state, which lives in the ZDO (keys from the config), so a run also
    /// finishes while nobody is near (world clock: sleeping counts) and survives save / load. A run saved without the minutes key takes the step-0 minutes;
    /// any state other than idle / running (the Leaching Station's old "done" state, before D-132) moves its output onto the pile.
    /// </summary>
    internal abstract class BatchStation : MonoBehaviour, Hoverable, Interactable
    {
        internal const int StateIdle = 0, StateRunning = 1;
        const string UseKey = "[<color=yellow><b>$KEY_Use</b></color>] ";
        const float TickSeconds = 2f;
        ZNetView _nview;
        static readonly List<BatchStation> Live = new List<BatchStation>();
        Bounds _footprint;
        bool _measured;

        /// <summary>The station's prefab name (the key its extensions carry).</summary>
        internal string StationKey { get { return Config.StationKey; } }

        // a build ghost is created (and enabled) before the game marks it as the ghost: only a piece with a valid network view is a real, placed one
        bool Placed { get { return _nview != null && _nview.IsValid(); } }

        /// <summary>The station's names, keys, inputs and numbers.</summary>
        protected abstract BatchStationConfig Config { get; }

        /// <summary>Shows the station's state on its model (fire, liquid, turning parts...).</summary>
        protected abstract void ShowState(bool running, BatchAmounts held, int ready);

        /// <summary>Called once the network view is valid: find the model parts.</summary>
        protected virtual void FindParts() { }

        void Awake()
        {
            BatchStationConfig c = Config;
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.Awake({c.Label})");
            _nview = GetComponent<ZNetView>();
            if (_nview == null || !_nview.IsValid()) { if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.Awake({c.Label}): no valid network view - returns"); return; }
            _nview.Register<string>(c.RpcAdd, OnAdd);
            _nview.Register(c.RpcCollect, OnCollect);
            _nview.Register<int>(c.RpcCollected, OnCollected);
            FindParts();
            InvokeRepeating(nameof(Tick), UnityEngine.Random.Range(0.5f, TickSeconds), TickSeconds);
        }

        void OnEnable()
        {
            if (!Live.Contains(this)) Live.Add(this);
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.OnEnable({Config.Label}): {Live.Count} live station(s)");
        }

        void OnDisable()
        {
            Live.Remove(this);
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.OnDisable({Config.Label}): {Live.Count} live station(s)");
        }

        /// <summary>True when a station with this prefab name within <paramref name="range"/> m of the point is running (D-138: the extension ovens' fire).</summary>
        internal static bool AnyRunningNear(string stationKey, Vector3 point, float range)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                BatchStation s = Live[i];
                if (s == null) { Live.RemoveAt(i); continue; }
                Vector3 d = s.transform.position - point;
                if (s.Config.StationKey == stationKey && s.Running && StationPlacementRules.InRange(d.x, d.y, d.z, range))
                {
                    if (RuleLog.IsOn()) ModLog.Every("BatchStation.AnyRunningNear:yes:" + stationKey, 2d, $"BatchStation.AnyRunningNear({stationKey}, {point}): returns true");
                    return true;
                }
            }
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.AnyRunningNear:no:" + stationKey, 2d, $"BatchStation.AnyRunningNear({stationKey}, {point}): returns false of {Live.Count} live station(s)");
            return false;
        }

        /// <summary>The placed batch station a point stands on (StationPlacementRules.IsOnStation, D-140), or null. Expected outcome: null beside a station, on the ground or high above it.</summary>
        internal static BatchStation StandsOn(Vector3 point)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                BatchStation s = Live[i];
                if (s == null) { Live.RemoveAt(i); continue; }
                if (!s.Placed) continue;
                if (!s._measured) { s._footprint = StationSnapPoints.Footprint(s.gameObject); s._measured = true; }
                Vector3 local = s.transform.InverseTransformPoint(point);
                Bounds f = s._footprint;
                if (StationPlacementRules.IsOnStation(local.x, local.y, local.z, f.min.x, f.max.x, f.min.z, f.max.z, f.max.y))
                {
                    if (RuleLog.IsOn()) ModLog.Every("BatchStation.StandsOn:yes", 2d, $"BatchStation.StandsOn({point}): returns {s.StationKey}");
                    return s;
                }
            }
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.StandsOn:no", 2d, $"BatchStation.StandsOn({point}): returns null");
            return null;
        }

        /// <summary>A child of the model by name, searched at every depth; null when the model has no such part.</summary>
        protected GameObject Part(string name)
        {
            Transform t = FindDeep(transform, name);
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.Part({Config.Label}, {name}): returns {(t != null ? "found" : "null")}");
            return t != null ? t.gameObject : null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform c in root)
            {
                if (c.name == name) { if (RuleLog.IsOn()) ModLog.Every("BatchStation.FindDeep:hit", 2d, $"BatchStation.FindDeep: returns {name}"); return c; }
                Transform f = FindDeep(c, name);
                if (f != null) { if (RuleLog.IsOn()) ModLog.Every("BatchStation.FindDeep:child", 2d, $"BatchStation.FindDeep: returns {name} (deeper)"); return f; }
            }
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.FindDeep:none", 2d, $"BatchStation.FindDeep: returns null for {name}");
            return null;
        }

        /// <summary>Shows a model part or hides it (no change when it already is).</summary>
        protected static void SetActive(GameObject go, bool on)
        {
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.SetActive", 2d, $"BatchStation.SetActive(go={RuleLog.Shown(go)}, on={on})");
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        // ---------- what the station holds and can do now ----------

        /// <summary>Is a run going right now (read on every client from the ZDO).</summary>
        protected bool Running { get { return _nview != null && _nview.IsValid() && State == StateRunning; } }

        int State { get { return _nview.GetZDO().GetInt(Config.KeyState, StateIdle); } }

        /// <summary>The output piled up, waiting for a player.</summary>
        int Ready { get { return _nview.GetZDO().GetInt(Config.KeyReady, 0); } }

        BatchAmounts Held
        {
            get { ZDO z = _nview.GetZDO(); BatchStationConfig c = Config; return new BatchAmounts(z.GetInt(c.KeyFirst, 0), z.GetInt(c.KeySecond, 0), z.GetInt(c.KeyThird, 0)); }
        }

        /// <summary>The upgrade step from this station's extension pieces near it.</summary>
        int Step
        {
            get { return BatchStationRules.Step(Config.Spec, BatchStationExtension.KindsNear(Config.StationKey, transform.position)); }
        }

        // ---------- owner: start, finish ----------

        void Tick()
        {
            BatchStationConfig c = Config;
            try
            {
                if (!_nview.IsValid() || ZNet.instance == null) { if (RuleLog.IsOn()) ModLog.Every("BatchStation.Tick:invalid", 2d, $"BatchStation.Tick({c.Label}): no network view or no ZNet - returns"); return; }
                if (_nview.IsOwner() && c.FeatureOn()) Advance(_nview.GetZDO(), ZNet.instance.GetTimeSeconds());
                ShowState(State == StateRunning, Held, Ready);
                ModLog.Every(c.Label + "-" + gameObject.GetInstanceID(), 30d, $"{c.Label} state {State}, step {Step}, holds {BatchStationRules.Text(Held)} of {BatchStationRules.Text(BatchStationRules.FullBatch(c.Spec, c.Unit(), Step))}, pile {Ready} {c.OutputName}");
            }
            catch (Exception e)
            {
                ModLog.Warn($"BatchStation.Tick({c.Label}): {e.GetType().FullName}: {e.Message} - the station skips this tick");
            }
        }

        // owner: a run whose time is up puts its output on the pile; then an idle station with at least one base batch inside starts the next run
        void Advance(ZDO zdo, double now)
        {
            BatchStationConfig c = Config;
            int state = zdo.GetInt(c.KeyState, StateIdle);
            string why = "base batch inside";
            if (state != StateIdle && state != StateRunning)
            {
                Finish(zdo, "a station saved in an old 'done' state " + state);
                state = StateIdle;
            }
            else if (state == StateRunning)
            {
                double minutes = zdo.GetFloat(c.KeyMinutes, (float)c.MinutesAt(0));
                if (!BatchStationRules.IsDone(zdo.GetLong(c.KeyStart, 0L) / 1000d, now, minutes)) { if (RuleLog.IsOn()) ModLog.Every("BatchStation.Advance:running:" + c.Label, 2d, $"BatchStation.Advance({c.Label}): still running - returns"); return; }
                Finish(zdo, minutes + " min up");
                state = StateIdle;
                why = "next run";
            }
            int step = Step;
            if (!BatchStationRules.StartsNow(c.Spec, state == StateIdle, Held, c.Unit(), step))
            {
                if (RuleLog.IsOn()) ModLog.Every("BatchStation.Advance:idle:" + c.Label, 2d, $"BatchStation.Advance({c.Label}): idle, {BatchStationRules.Missing(c.Spec, Held, c.Unit())} - returns");
                return;
            }
            Begin(zdo, now, step, why);
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.Advance:started:" + c.Label, 2d, $"BatchStation.Advance({c.Label}): started ({why}) - returns");
        }

        // owner: the run's output goes onto the pile, the station is idle again (it may start the next run in the same tick)
        void Finish(ZDO zdo, string why)
        {
            BatchStationConfig c = Config;
            int run = zdo.GetInt(c.KeyOut, 0);
            int pile = BatchStationRules.Pile(zdo.GetInt(c.KeyReady, 0), run);
            zdo.Set(c.KeyReady, pile);
            zdo.Set(c.KeyOut, 0);
            zdo.Set(c.KeyUnits, 0);
            zdo.Set(c.KeyStart, 0L);
            zdo.Set(c.KeyState, StateIdle);
            ModLog.State(c.Label, "running", "idle", $"{why}: {run} {c.OutputName} onto the pile, pile now {pile}, left inside {BatchStationRules.Text(Held)}");
        }

        // owner: one run of as many whole base batches as the station holds (up to the step's batch); the rest stays in
        void Begin(ZDO zdo, double now, int step, string why)
        {
            BatchStationConfig c = Config;
            BatchAmounts held = Held, unit = c.Unit();
            int units = BatchStationRules.Units(c.Spec, held, unit, step);
            if (units < 1) { ModLog.Decision(c.Label + " start", "not started", BatchStationRules.Missing(c.Spec, held, unit) + ", holds " + BatchStationRules.Text(held)); return; }
            BatchAmounts used = BatchStationRules.Used(unit, units);
            double minutes = c.MinutesAt(step);
            zdo.Set(c.KeyFirst, held.First - used.First);
            zdo.Set(c.KeySecond, held.Second - used.Second);
            zdo.Set(c.KeyThird, held.Third - used.Third);
            zdo.Set(c.KeyUnits, units);
            zdo.Set(c.KeyMinutes, (float)minutes);
            zdo.Set(c.KeyStart, (long)(now * 1000d));
            zdo.Set(c.KeyOut, BatchStationRules.Output(units, c.OutputPerUnit()));
            zdo.Set(c.KeyState, StateRunning);
            ModLog.State(c.Label, "idle", "running", $"{why}: step {step}, {units} base batch(es) = {BatchStationRules.Text(used)} -> {zdo.GetInt(c.KeyOut, 0)} {c.OutputName} in {minutes} min, left inside {BatchStationRules.Text(Held)}");
        }

        // ---------- hover ----------

        public string GetHoverName() { return Config.HoverName; }

        public float GetHoverOffset() { return 0f; }

        public string GetHoverText()
        {
            BatchStationConfig c = Config;
            if (!_nview.IsValid()) { if (RuleLog.IsOn()) ModLog.Every("BatchStation.GetHoverText:invalid", 2d, $"BatchStation.GetHoverText({c.Label}): no network view - returns the name"); return GetHoverName(); }
            int step = Step, ready = Ready;
            BatchAmounts held = Held, unit = c.Unit(), full = BatchStationRules.FullBatch(c.Spec, unit, step);
            string run;
            if (State == StateRunning)
            {
                ZDO z = _nview.GetZDO();
                double left = BatchStationRules.SecondsLeft(z.GetLong(c.KeyStart, 0L) / 1000d, ZNet.instance.GetTimeSeconds(), z.GetFloat(c.KeyMinutes, (float)c.MinutesAt(0)));
                run = $"{c.RunVerb} {z.GetInt(c.KeyOut, 0)} {c.OutputName} ({Mathf.CeilToInt((float)(left / 60d))} min left)";
            }
            else
            {
                string missing = BatchStationRules.Missing(c.Spec, held, unit);
                run = missing.Length > 0 ? "Cannot start yet: " + missing : "Starting";
            }
            string text = GetHoverName() + "\n" + UpgradeLine(step) + "\n" + run + "\nHolds " + Holds(held, full) + "\n" + UseKey + "Fill";
            if (ready > 0) text += "\n" + UseKey + "Collect " + ready + " " + c.OutputName;
            text = Localization.instance.Localize(text);
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.GetHoverText:" + c.Label, 2d, $"BatchStation.GetHoverText({c.Label}): returns {RuleLog.Shown(text)}");
            return text;
        }

        string UpgradeLine(int step)
        {
            BatchStationConfig c = Config;
            BatchAmounts full = BatchStationRules.FullBatch(c.Spec, c.Unit(), step);
            string line = $"Upgrades {step}/{c.Spec.MaxStep}: up to {full.First} {c.Spec.FirstName} + {full.Second} {c.Spec.SecondName} + {full.Third} {c.Spec.ThirdName}"
                + $" -> {BatchStationRules.Output(BatchStationRules.UnitsPerBatch(c.Spec, step), c.OutputPerUnit())} {c.OutputName}, {c.MinutesAt(step)} min";
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.UpgradeLine:" + c.Label, 2d, $"BatchStation.UpgradeLine: returns {line}");
            return line;
        }

        string Holds(BatchAmounts held, BatchAmounts full)
        {
            BatchStationSpec s = Config.Spec;
            string text = $"{s.FirstName} {held.First}/{full.First}, {s.SecondName} {held.Second}/{full.Second}, {s.ThirdName} {held.Third}/{full.Third}";
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.Holds:" + Config.Label, 2d, $"BatchStation.Holds: returns {text}");
            return text;
        }

        // ---------- use ----------

        // E takes the whole pile and puts in half a batch (up to what the station holds), at any time; nothing to take and nothing to add says why
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.Interact", 2d, $"BatchStation.Interact({Config.Label}, user={RuleLog.Shown(user)}, hold={hold}, alt={alt})");
            if (hold || user == null || !_nview.IsValid()) { if (RuleLog.IsOn()) ModLog.Every("BatchStation.Interact:no", 2d, "BatchStation.Interact: held, no user or no network view - returns false"); return false; }
            int collected = Collect(user);
            int sent = Fill(user, -1);
            if (collected == 0 && sent == 0) TellWhyNothing(user);
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.Interact:end", 2d, $"BatchStation.Interact({Config.Label}): collected {collected}, put in {sent} - returns {collected + sent >= 0}");
            return collected + sent >= 0;
        }

        // nothing collected and nothing put in: say why (what is missing for a base batch, the run is going, or the next run starts)
        void TellWhyNothing(Humanoid user)
        {
            BatchStationConfig c = Config;
            BatchAmounts held = Held;
            string missing = BatchStationRules.Missing(c.Spec, held, c.Unit());
            string text;
            if (missing.Length > 0) text = missing;
            else if (State == StateRunning) text = c.RunningMessage;
            else text = c.HoverName + " starts";
            user.Message(MessageHud.MessageType.Center, text);
            ModLog.Decision(c.Label + " use", "nothing to take or add", $"told '{text}', state {State}, holds {BatchStationRules.Text(held)}");
        }

        // holding the station's held input (the Leaching Station's Wood) and using the station puts in that input only
        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            BatchStationConfig c = Config;
            int slot = c.HeldInputSharedName == null || item == null ? -1 : Array.IndexOf(c.InputSharedNames, item.m_shared.m_name);
            if (user == null || item == null || slot < 0 || item.m_shared.m_name != c.HeldInputSharedName || !_nview.IsValid())
            {
                if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.UseItem({c.Label}): not the held input or no station - returns false");
                return false;
            }
            int sent = Fill(user, slot);
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.UseItem({c.Label}): {sent} of input {slot} put in - returns {sent >= 0}");
            return sent >= 0;
        }

        // client: half a full batch per use, or what the player has; only one input when onlySlot is 0..2; returns how many items went in
        int Fill(Humanoid user, int onlySlot)
        {
            BatchStationConfig c = Config;
            int step = Step;
            Inventory inv = user.GetInventory();
            BatchAmounts unit = c.Unit(), held = Held;
            var player = new BatchAmounts(
                onlySlot < 0 || onlySlot == 0 ? inv.CountItems(c.InputSharedNames[0]) : 0,
                onlySlot < 0 || onlySlot == 1 ? inv.CountItems(c.InputSharedNames[1]) : 0,
                onlySlot < 0 || onlySlot == 2 ? inv.CountItems(c.InputSharedNames[2]) : 0);
            BatchAmounts take = BatchStationRules.Take(held, BatchStationRules.FullBatch(c.Spec, unit, step), BatchStationRules.FillPerUse(c.Spec, unit, step), player);
            int first = SendItems(user, 0, take.First);
            int second = SendItems(user, 1, take.Second);
            int third = SendItems(user, 2, take.Third);
            int sent = first + second + third;
            ModLog.Decision(c.Label + " fill", $"{first} {c.Spec.FirstName} + {second} {c.Spec.SecondName} + {third} {c.Spec.ThirdName} sent", $"step {step}, station held {BatchStationRules.Text(held)}, player had {BatchStationRules.Text(player)}");
            if (sent > 0) user.Message(MessageHud.MessageType.Center, $"Added {first} {c.Spec.FirstName}, {second} {c.Spec.SecondName} and {third} {c.Spec.ThirdName}");
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.Fill({c.Label}): returns {sent}");
            return sent;
        }

        // up to `wanted` items of one input: each one is removed from the inventory and announced to the owner by its prefab name
        int SendItems(Humanoid user, int slot, int wanted)
        {
            BatchStationConfig c = Config;
            string sharedName = c.InputSharedNames[slot], prefab = c.InputPrefabs[slot];
            Inventory inv = user.GetInventory();
            int before = inv.CountItems(sharedName);
            int give = Mathf.Min(Mathf.Max(wanted, 0), before);
            int sent = 0;
            for (int i = 0; i < give; i++)
            {
                // the item itself is removed (like the game's Smelter does for ore): removing by name left every item in the inventory in the test
                // profile (run core4, another mod may patch that overload), and an item is only announced when it really left the inventory
                ItemDrop.ItemData stack = FirstStack(inv, sharedName);
                int stackBefore = stack != null ? stack.m_stack : -1;
                bool removed = stack != null && inv.RemoveItem(stack, 1);
                if (i == 0 || !removed)
                    ModLog.Verbose($"BatchStation.SendItems({c.Label}): {prefab} #{i} stack {stackBefore} -> {(stack != null ? stack.m_stack : -1)}, removed {removed}, inventory count now {inv.CountItems(sharedName)}, user is the local player {user == Player.m_localPlayer}");
                if (!removed) break;
                _nview.InvokeRPC(c.RpcAdd, prefab);
                sent++;
            }
            int after = inv.CountItems(sharedName);
            ModLog.Check(c.Label + ": " + prefab + " left in the inventory of " + user.GetHoverName(), after, before - sent, 0f, "items");
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.SendItems({c.Label}, {prefab}, wanted {wanted}): returns {sent}");
            return sent;
        }

        static ItemDrop.ItemData FirstStack(Inventory inv, string sharedName)
        {
            foreach (ItemDrop.ItemData item in inv.GetAllItems())
            {
                if (item.m_shared.m_name == sharedName && item.m_stack > 0) { if (RuleLog.IsOn()) ModLog.Every("BatchStation.FirstStack", 2d, $"BatchStation.FirstStack({sharedName}): returns a stack of {item.m_stack}"); return item; }
            }
            if (RuleLog.IsOn()) ModLog.Every("BatchStation.FirstStack:none", 2d, $"BatchStation.FirstStack({sharedName}): returns null");
            return null;
        }

        // client: ask the owner for the whole pile; returns how much was asked for (0 = nothing waiting or no room)
        int Collect(Humanoid user)
        {
            BatchStationConfig c = Config;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(c.OutputPrefab) : null;
            int amount = Ready;
            if (prefab == null || amount < 1) { if (RuleLog.IsOn()) ModLog.Every("BatchStation.Collect:none", 2d, $"BatchStation.Collect({c.Label}): nothing waiting (pile {amount}) - returns 0"); return 0; }
            if (!user.GetInventory().CanAddItem(prefab, amount)) { user.Message(MessageHud.MessageType.Center, "Your inventory is full"); ModLog.Decision(c.Label + " collect", "not asked", $"inventory has no room for {amount} {c.OutputName}"); return 0; }
            _nview.InvokeRPC(c.RpcCollect);
            ModLog.Decision(c.Label + " collect", "asked the owner", $"{amount} {c.OutputName} waiting");
            return amount;
        }

        // owner: one item arrived; it counts only if it is one of the station's inputs and that slot has room up to the full batch (else refused and logged)
        void OnAdd(long sender, string prefab)
        {
            BatchStationConfig c = Config;
            if (!_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.OnAdd({c.Label}): not the owner - returns"); return; }
            var zdo = _nview.GetZDO();
            int step = Step;
            BatchAmounts have = Held, full = BatchStationRules.FullBatch(c.Spec, c.Unit(), step);
            int slot = BatchStationRules.SlotOf(prefab, c.InputPrefabs);
            bool took = slot >= 0 && BatchStationRules.HasRoom(have, full, slot);
            if (took)
            {
                BatchAmounts after = BatchStationRules.AddOne(have, slot);
                zdo.Set(c.KeyFirst, after.First);
                zdo.Set(c.KeySecond, after.Second);
                zdo.Set(c.KeyThird, after.Third);
            }
            ModLog.Decision(c.Label + " item", took ? "took " + prefab : "refused", $"item {prefab} (slot {slot}) from {sender}, state {State}, step {step}, now {BatchStationRules.Text(Held)} of {BatchStationRules.Text(full)}");
        }

        // owner: hand the whole pile over (collect always takes all); a running run and the inputs inside are not touched
        void OnCollect(long sender)
        {
            BatchStationConfig c = Config;
            if (!_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.OnCollect({c.Label}): not the owner - returns"); return; }
            var zdo = _nview.GetZDO();
            int amount = zdo.GetInt(c.KeyReady, 0);
            if (amount < 1) { ModLog.Decision(c.Label + " collect", "nothing sent", "the pile is empty, asked by " + sender); return; }
            zdo.Set(c.KeyReady, 0);
            ModLog.Decision(c.Label + " collect", "sent " + amount + " " + c.OutputName, $"to {sender}, state {State}, holds {BatchStationRules.Text(Held)}");
            _nview.InvokeRPC(sender, c.RpcCollected, amount);
        }

        // the collecting player's client: add the output to the inventory
        void OnCollected(long sender, int amount)
        {
            BatchStationConfig c = Config;
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(c.OutputPrefab) : null;
            if (p == null || prefab == null || amount <= 0) { if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.OnCollected({c.Label}, {amount}): no player, prefab or amount - returns"); return; }
            p.GetInventory().AddItem(prefab, amount);
            p.Message(MessageHud.MessageType.TopLeft, "Collected " + amount + " " + c.OutputName);
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStation.OnCollected({c.Label}): {amount} {c.OutputName} added");
        }
    }
}
