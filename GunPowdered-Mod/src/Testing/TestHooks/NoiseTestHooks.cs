// kydl:read-only-observer - test-only: reads what the gunpowder noise did to creatures (D-149); GunNoise does the alerting
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Integrations.Valheim;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>D-149 lab hooks: the last gunpowder noise and how a creature reacted to it.</summary>
    internal static class NoiseTestHooks
    {
        static readonly AccessTools.FieldRef<MonsterAI, Character> Target = AccessTools.FieldRefAccess<MonsterAI, Character>("m_targetCreature");
        static readonly AccessTools.FieldRef<MonsterAI, Vector3> LastKnown = AccessTools.FieldRefAccess<MonsterAI, Vector3>("m_lastKnownTargetPos");
        static Vector3 _noiseAt;

        /// <summary>"NOISE, alerted N" - the last gunpowder noise (GunNoise.LastNoise) and how many creatures it alerted.</summary>
        [TestHook("GunPowdered.noiseLast")]
        static string Last()
        {
            ModLog.Verbose("NoiseTestHooks.Last()");
            string s = (GunNoise.LastNoise.Length > 0 ? GunNoise.LastNoise : "no noise yet") + ", alerted " + GunNoise.LastAlerted;
            ModLog.Info("NoiseTestHooks.Last: returns " + s);
            return s;
        }

        /// <summary>Remembers where the last dynamite blast was, so creatures' distances can be measured from it. Returns "noise at ...".</summary>
        [TestHook("GunPowdered.noiseMarkBlast")]
        static string MarkBlast()
        {
            ModLog.Verbose("NoiseTestHooks.MarkBlast()");
            _noiseAt = GunPowdered.Features.Dynamite.Infrastructure.DynamiteCharge.LastBlast;
            _startDistance.Clear();
            string s = "noise at " + _noiseAt.ToString("0.0");
            ModLog.Verbose("NoiseTestHooks.MarkBlast: returns " + s);
            return s;
        }

        /// <summary>
        /// The nearest living <paramref name="prefab"/> to the local player: "alerted A, target T, last known K m from the blast, now D m from the blast".
        /// </summary>
        [TestHook("GunPowdered.noiseCreature")]
        static string Creature(string prefab)
        {
            ModLog.Verbose("NoiseTestHooks.Creature(prefab=" + prefab + ")");
            MonsterAI best = Nearest(prefab);
            if (best == null) { ModLog.Verbose("NoiseTestHooks.Creature: no " + prefab + " - returns why"); return "no " + prefab; }
            string s = Describe(best);
            ModLog.Info("NoiseTestHooks.Creature: " + prefab + " returns " + s);
            return s;
        }

        /// <summary>
        /// T-030 (lab a17ab6: a `wait until` with `check: hook` + `regex` ended at once - the regex is not applied there, lessons LESSONS_LEARNED 22): the hook
        /// computes the verdict so the suite compares a plain text with `expect`. Expected outcome: "within" when the nearest <paramref name="prefab"/> is at most
        /// <paramref name="metres"/> from the marked blast, otherwise "not within: " + what noiseCreature reports.
        /// </summary>
        [TestHook("GunPowdered.noiseCreatureWithin")]
        static string Within(string prefab, double metres)
        {
            ModLog.Verbose("NoiseTestHooks.Within(prefab=" + prefab + ", metres=" + metres + ")");
            MonsterAI best = Nearest(prefab);
            if (best == null) { ModLog.Verbose("NoiseTestHooks.Within: no " + prefab + " - returns why"); return "not within: no " + prefab; }
            if (Vector3.Distance(best.transform.position, _noiseAt) <= metres) { ModLog.Verbose("NoiseTestHooks.Within: returns within"); return "within"; }
            string s = "not within: " + Describe(best);
            if (ModLog.IsDue("NoiseTestHooks.Within", 1.0)) ModLog.Verbose("NoiseTestHooks.Within: returns " + s);
            return s;
        }

        static readonly System.Collections.Generic.Dictionary<string, float> _startDistance = new System.Collections.Generic.Dictionary<string, float>();

        /// <summary>
        /// a18g16-1 (full run): the Brute walked towards the blast (43.7 -> 43.2 m in 0.3 s) but started further out than in earlier runs, so a fixed
        /// "within 33 m in 12 s" failed. What the test proves is that it HEADS for the blast: the first call after noiseMarkBlast remembers how far the nearest
        /// <paramref name="prefab"/> is from the blast; later calls return "closer" once it is at least <paramref name="metres"/> nearer, otherwise
        /// "not closer: start S m, now N m" + what noiseCreature reports.
        /// </summary>
        [TestHook("GunPowdered.noiseCreatureCloser")]
        static string Closer(string prefab, double metres)
        {
            ModLog.Verbose("NoiseTestHooks.Closer(prefab=" + prefab + ", metres=" + metres + ")");
            MonsterAI best = Nearest(prefab);
            if (best == null) { ModLog.Verbose("NoiseTestHooks.Closer: no " + prefab + " - returns why"); return "not closer: no " + prefab; }
            float now = Vector3.Distance(best.transform.position, _noiseAt);
            if (!_startDistance.TryGetValue(prefab, out float start)) { start = now; _startDistance[prefab] = now; ModLog.Info("NoiseTestHooks.Closer: " + prefab + " starts " + now.ToString("0.0") + " m from the blast"); }
            if (start - now >= metres) { ModLog.Info("NoiseTestHooks.Closer: " + prefab + " " + start.ToString("0.0") + " -> " + now.ToString("0.0") + " m - returns closer"); return "closer"; }
            string s = "not closer: start " + start.ToString("0.0") + " m, now " + now.ToString("0.0") + " m; " + Describe(best);
            if (ModLog.IsDue("NoiseTestHooks.Closer", 1.0)) ModLog.Verbose("NoiseTestHooks.Closer: returns " + s);
            return s;
        }

        static string Describe(MonsterAI m)
        {
            Character t = Target(m);
            return $"alerted {m.IsAlerted()}, target {(t != null ? t.m_name : "none")}, last known {Vector3.Distance(LastKnown(m), _noiseAt):0.0} m from the blast, now {Vector3.Distance(m.transform.position, _noiseAt):0.0} m from the blast";
        }

        /// <summary>The nearest living <paramref name="prefab"/> to the local player, or null. Reads only.</summary>
        static MonsterAI Nearest(string prefab)
        {
            Player p = Player.m_localPlayer;
            MonsterAI best = null;
            float bestDist = float.MaxValue;
            foreach (BaseAI ai in BaseAI.BaseAIInstances)
            {
                var m = ai as MonsterAI;
                Character c = m != null ? m.GetComponent<Character>() : null;
                if (c == null || c.IsDead() || Utils.GetPrefabName(c.gameObject) != prefab || p == null) continue;
                float d = Vector3.Distance(c.transform.position, p.transform.position);
                if (d < bestDist) { best = m; bestDist = d; }
            }
            return best;
        }
    }
}
