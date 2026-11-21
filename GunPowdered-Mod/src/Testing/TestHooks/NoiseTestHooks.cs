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
            if (best == null) { ModLog.Verbose("NoiseTestHooks.Creature: no " + prefab + " - returns why"); return "no " + prefab; }
            Character t = Target(best);
            string s = $"alerted {best.IsAlerted()}, target {(t != null ? t.m_name : "none")}, last known {Vector3.Distance(LastKnown(best), _noiseAt):0.0} m from the blast, now {Vector3.Distance(best.transform.position, _noiseAt):0.0} m from the blast";
            ModLog.Info("NoiseTestHooks.Creature: " + prefab + " returns " + s);
            return s;
        }
    }
}
