// kydl:read-only-observer - test-only: measures frame times (D-147 stutter); it drives no creature
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>D-147 lab hooks: frame-time statistics over a stretch of a test (frames, average, worst, how many frames took 50 ms or more).</summary>
    internal static class FrameStatsTestHooks
    {
        static FrameStatsProbe _probe;

        /// <summary>Starts collecting frame times (unscaled). Returns "collecting".</summary>
        [TestHook("GunPowdered.frameStatsStart")]
        static string Start()
        {
            ModLog.Verbose("FrameStatsTestHooks.Start()");
            if (_probe == null) _probe = Plugin.Instance.gameObject.AddComponent<FrameStatsProbe>();
            _probe.Clear();
            _probe.enabled = true;
            ModLog.Verbose("FrameStatsTestHooks.Start: returns collecting");
            return "collecting";
        }

        /// <summary>Stops and reports: "frames N, average A ms, median M ms, worst W ms, over 50 ms K, over 100 ms L, debug log D".</summary>
        [TestHook("GunPowdered.frameStatsStop")]
        static string Stop()
        {
            ModLog.Verbose("FrameStatsTestHooks.Stop()");
            if (_probe == null) { ModLog.Verbose("FrameStatsTestHooks.Stop: never started - returns why"); return "never started"; }
            _probe.enabled = false;
            string s = _probe.Report() + ", debug log " + ModLog.DebugOn;
            ModLog.Info("FrameStatsTestHooks.Stop: returns " + s);
            return s;
        }
    }

    /// <summary>
    /// Test-only: records every frame's unscaled time while enabled, plus the managed memory the whole game allocates (Unity's release-build
    /// counter "GC Allocated In Frame") and how many garbage collections ran (T-018: the user's freezes came every ~10.5 s, like a GC).
    /// </summary>
    internal sealed class FrameStatsProbe : MonoBehaviour
    {
        readonly List<float> _ms = new List<float>(8192);
        // Unity's "GC Allocated In Frame" counter reads 0 in the game's release build (lab a17gc1), so the allocation is the managed heap's growth
        // between frames (GC.GetTotalMemory; a drop means a collection ran). GC frames: the frame times of the frames in which a collection ran.
        readonly List<float> _gcMs = new List<float>(64);
        long _bytes, _lastMem, _maxMem;
        int _gcAtStart, _lastGc;
        float _startedAt;

        internal void Clear()
        {
            _ms.Clear();
            _gcMs.Clear();
            _bytes = 0; _maxMem = 0;
            _lastMem = System.GC.GetTotalMemory(false);
            _gcAtStart = _lastGc = System.GC.CollectionCount(0);
            _startedAt = Time.realtimeSinceStartup;
            ModLog.Verbose("FrameStatsProbe.Clear: cleared, heap " + (_lastMem >> 20) + " MB, GC count " + _gcAtStart);
        }

        void Update()
        {
            float frame = Time.unscaledDeltaTime * 1000f;
            _ms.Add(frame);
            long mem = System.GC.GetTotalMemory(false);
            if (mem > _lastMem) _bytes += mem - _lastMem;
            if (mem > _maxMem) _maxMem = mem;
            _lastMem = mem;
            int gc = System.GC.CollectionCount(0);
            if (gc != _lastGc) { _gcMs.Add(frame); _lastGc = gc; }
            if (RuleLog.Due("frame-stats-probe", 10d)) ModLog.Every("frame-stats-probe", 10d, "FrameStatsProbe: " + _ms.Count + " frames recorded");
        }

        internal string Report()
        {
            if (_ms.Count == 0) { ModLog.Verbose("FrameStatsProbe.Report: no frames - returns"); return "frames 0"; }
            var sorted = new List<float>(_ms);
            sorted.Sort();
            float sum = 0f;
            int over50 = 0, over100 = 0;
            foreach (float m in _ms) { sum += m; if (m >= 50f) over50++; if (m >= 100f) over100++; }
            string s = $"frames {_ms.Count}, average {sum / _ms.Count:0.0} ms, median {sorted[sorted.Count / 2]:0.0} ms, worst {sorted[sorted.Count - 1]:0} ms, over 50 ms {over50}, over 100 ms {over100}"
                + $", alloc {_bytes / 1024f / Mathf.Max(0.001f, Time.realtimeSinceStartup - _startedAt):0} KB/s ({_bytes / 1024f / _ms.Count:0.0} KB per frame), heap max {_maxMem >> 20} MB"
                + $", GC {System.GC.CollectionCount(0) - _gcAtStart} in {Time.realtimeSinceStartup - _startedAt:0} s, GC frames {GcFrames()}";
            ModLog.Verbose("FrameStatsProbe.Report: returns " + s);
            return s;
        }

        // "average A ms, worst W ms" of the frames in which a collection ran
        string GcFrames()
        {
            if (_gcMs.Count == 0) { ModLog.Verbose("FrameStatsProbe.GcFrames: no collection - returns none"); return "none"; }
            float sum = 0f, worst = 0f;
            foreach (float m in _gcMs) { sum += m; if (m > worst) worst = m; }
            string s = $"average {sum / _gcMs.Count:0} ms, worst {worst:0} ms";
            ModLog.Verbose("FrameStatsProbe.GcFrames: returns " + s);
            return s;
        }
    }
}
