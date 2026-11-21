// kydl:read-only-observer - test-only: measures frame times (D-147 stutter); it drives no creature
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;

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

    /// <summary>Test-only: records every frame's unscaled time while enabled.</summary>
    internal sealed class FrameStatsProbe : MonoBehaviour
    {
        readonly List<float> _ms = new List<float>(8192);

        internal void Clear() { _ms.Clear(); ModLog.Verbose("FrameStatsProbe.Clear: cleared"); }

        void Update()
        {
            _ms.Add(Time.unscaledDeltaTime * 1000f);
            ModLog.Every("frame-stats-probe", 10d, "FrameStatsProbe: " + _ms.Count + " frames recorded");
        }

        internal string Report()
        {
            if (_ms.Count == 0) { ModLog.Verbose("FrameStatsProbe.Report: no frames - returns"); return "frames 0"; }
            var sorted = new List<float>(_ms);
            sorted.Sort();
            float sum = 0f;
            int over50 = 0, over100 = 0;
            foreach (float m in _ms) { sum += m; if (m >= 50f) over50++; if (m >= 100f) over100++; }
            string s = $"frames {_ms.Count}, average {sum / _ms.Count:0.0} ms, median {sorted[sorted.Count / 2]:0.0} ms, worst {sorted[sorted.Count - 1]:0} ms, over 50 ms {over50}, over 100 ms {over100}";
            ModLog.Verbose("FrameStatsProbe.Report: returns " + s);
            return s;
        }
    }
}
