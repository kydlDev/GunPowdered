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
    internal abstract partial class BatchStation : MonoBehaviour, Hoverable, Interactable
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
                if (s == null) { Live.RemoveAt(i); { if (RuleLog.Due("BatchStation.AnyRunningNear:78", 2d)) ModLog.Every("BatchStation.AnyRunningNear:78", 2d, $"BatchStation.AnyRunningNear: skips to the next item (continue) because s == null (line 78)"); continue; } }
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
                if (s == null) { Live.RemoveAt(i); { if (RuleLog.Due("BatchStation.StandsOn:96", 2d)) ModLog.Every("BatchStation.StandsOn:96", 2d, $"BatchStation.StandsOn: skips to the next item (continue) because s == null (line 96)"); continue; } }
                if (!s.Placed) { if (RuleLog.Due("BatchStation.StandsOn:97", 2d)) ModLog.Every("BatchStation.StandsOn:97", 2d, $"BatchStation.StandsOn: skips to the next item (continue) because !s.Placed (line 97)"); continue; }
                if (!s._measured) { s._footprint = StationSnapPoints.Footprint(s.gameObject); s._measured = true; }
                Vector3 local = s.transform.InverseTransformPoint(point);
                Bounds f = s._footprint;
                if (StationPlacementRules.IsOnStation(local.x, local.y, local.z, f.min.x, f.max.x, f.min.z, f.max.z, f.max.y))
                {
                    if (RuleLog.Due("BatchStation.StandsOn:yes", 2d)) ModLog.Every("BatchStation.StandsOn:yes", 2d, $"BatchStation.StandsOn({point}): returns {s.StationKey}");
                    return s;
                }
            }
            if (RuleLog.Due("BatchStation.StandsOn:no", 2d)) ModLog.Every("BatchStation.StandsOn:no", 2d, $"BatchStation.StandsOn({point}): returns null");
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
                if (c.name == name) { if (RuleLog.Due("BatchStation.FindDeep:hit", 2d)) ModLog.Every("BatchStation.FindDeep:hit", 2d, $"BatchStation.FindDeep: returns {name}"); return c; }
                Transform f = FindDeep(c, name);
                if (f != null) { if (RuleLog.Due("BatchStation.FindDeep:child", 2d)) ModLog.Every("BatchStation.FindDeep:child", 2d, $"BatchStation.FindDeep: returns {name} (deeper)"); return f; }
            }
            if (RuleLog.Due("BatchStation.FindDeep:none", 2d)) ModLog.Every("BatchStation.FindDeep:none", 2d, $"BatchStation.FindDeep: returns null for {name}");
            return null;
        }

        /// <summary>Shows a model part or hides it (no change when it already is).</summary>
        protected static void SetActive(GameObject go, bool on)
        {
            if (RuleLog.Due("BatchStation.SetActive", 2d)) ModLog.Every("BatchStation.SetActive", 2d, $"BatchStation.SetActive(go={RuleLog.Shown(go)}, on={on})");
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
                if (!_nview.IsValid() || ZNet.instance == null) { if (RuleLog.Due("BatchStation.Tick:invalid", 2d)) ModLog.Every("BatchStation.Tick:invalid", 2d, $"BatchStation.Tick({c.Label}): no network view or no ZNet - returns"); return; }
                if (_nview.IsOwner() && c.FeatureOn()) Advance(_nview.GetZDO(), ZNet.instance.GetTimeSeconds());
                ShowState(State == StateRunning, Held, Ready);
                ModLog.Every(c.Label + "-" + gameObject.GetInstanceID(), 30d, $"{c.Label} state {State}, step {Step}, holds {BatchStationRules.Text(Held)} of {BatchStationRules.Text(BatchStationRules.FullBatch(c.Spec, c.Unit(), Step))}, pile {Ready} {c.OutputName}");
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn($"BatchStation.Tick({c.Label}): {e.GetType().FullName}: {e.Message} - the station skips this tick" + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn($"BatchStation.Tick({c.Label}): {e.GetType().FullName}: {e.Message} - the station skips this tick" + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn($"BatchStation.Tick({c.Label}): {e.GetType().FullName}: {e.Message} - the station skips this tick" + "\n" + e.StackTrace);
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

    }
}
