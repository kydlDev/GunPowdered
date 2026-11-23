// kydl:read-only-observer - alerts creatures through the game's own AI state (target, last known position, Alert); it does not move them itself
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Integrations.Valheim
{
    /// <summary>
    /// D-149: the noise of gunpowder. Every hostile creature within the radius (NoiseRules.Reacts) is alerted, gets the player who made the noise as its
    /// target and the noise's spot as the place it last knew the player at - the game's MonsterAI then goes there (running, as it is alerted) and searches
    /// (as when it loses sight of a target), and attacks the player when it hears or sees them. Some creatures (the Greydwarf Brute) circle their target
    /// now and then on the way. (In DrakesTestLab with [TestControls] CreaturesHostile off, the lab blocks every alert: the noise tests switch it on.) Only creatures whose AI runs on this client are changed (the AI owner); the
    /// player in debug fly mode is not heard by the game, so creatures still go to the spot but do not find them.
    /// </summary>
    internal static class GunNoise
    {
        static readonly AccessTools.FieldRef<MonsterAI, Character> Target = AccessTools.FieldRefAccess<MonsterAI, Character>("m_targetCreature");
        static readonly AccessTools.FieldRef<MonsterAI, Vector3> LastKnown = AccessTools.FieldRefAccess<MonsterAI, Vector3>("m_lastKnownTargetPos");
        static readonly AccessTools.FieldRef<MonsterAI, bool> BeenAtLast = AccessTools.FieldRefAccess<MonsterAI, bool>("m_beenAtLastPos");

        /// <summary>How many creatures the last noise alerted, and what it was (test hooks).</summary>
        internal static int LastAlerted { get; private set; }
        internal static string LastNoise { get; private set; } = "";

        /// <summary>Alerts the creatures around <paramref name="at"/>. Expected outcome: how many were alerted.</summary>
        internal static int Alert(Vector3 at, float radius, Character culprit, string what)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"GunNoise.Alert(at={at:0.0}, radius={radius:0}, culprit={RuleLog.Shown(culprit)}, what={what})");
            int alerted = 0;
            foreach (BaseAI ai in BaseAI.BaseAIInstances)
            {
                var monster = ai as MonsterAI;
                Character c = ai != null ? ai.GetComponent<Character>() : null;
                ZNetView view = ai != null ? ai.GetComponent<ZNetView>() : null;
                if (monster == null || c == null || view == null || !view.IsValid() || !view.IsOwner()) continue;
                bool hostile = culprit != null ? BaseAI.IsEnemy(c, culprit) : c.m_faction != Character.Faction.Players;
                if (!NoiseRules.Reacts(Vector3.Distance(at, c.transform.position), radius, hostile, c.IsTamed(), c.IsDead(), Target(monster) != null)) continue;
                if (culprit != null) Target(monster) = culprit;
                LastKnown(monster) = at;
                BeenAtLast(monster) = false;
                monster.Alert();
                alerted++;
                if (RuleLog.IsOn() && !monster.IsAlerted() && ModLog.IsDue("GunNoise.AlertPatches", 600.0)) ModLog.Verbose("GunNoise: Alert() left it unalerted - Harmony patches on BaseAI.SetAlerted: " + PatchOwners(typeof(BaseAI), "SetAlerted") + "; on BaseAI.Alert: " + PatchOwners(typeof(BaseAI), "Alert"));
                if (RuleLog.IsOn()) ModLog.Verbose($"GunNoise: alerted {Utils.GetPrefabName(c.gameObject)} {Vector3.Distance(at, c.transform.position):0.0} m from it - alerted now {monster.IsAlerted()} (can be alerted {monster.m_canBeAlerted}), hunts players {monster.HuntPlayer()}");
            }
            LastAlerted = alerted;
            LastNoise = what + " at " + at.ToString("0.0") + ", radius " + radius.ToString("0") + " m";
            ModLog.Info("GunNoise: " + LastNoise + " - " + alerted + " creature(s) alerted" + (culprit != null ? ", hunting " + culprit.m_name : ""));
            return alerted;
        }

        // Diagnostics (user 2026-11-23: the Brute never stayed alerted): which mods patch a method. Expected outcome: "owner (prefix|postfix), ..." or "none".
        static string PatchOwners(System.Type type, string method)
        {
            System.Reflection.MethodBase m = AccessTools.Method(type, method);
            Patches info = m != null ? Harmony.GetPatchInfo(m) : null;
            if (info == null) return "none";
            var parts = new System.Collections.Generic.List<string>();
            foreach (Patch p in info.Prefixes) parts.Add(p.owner + " prefix " + p.PatchMethod.DeclaringType?.FullName);
            foreach (Patch p in info.Postfixes) parts.Add(p.owner + " postfix " + p.PatchMethod.DeclaringType?.FullName);
            foreach (Patch p in info.Transpilers) parts.Add(p.owner + " transpiler " + p.PatchMethod.DeclaringType?.FullName);
            return parts.Count == 0 ? "none" : string.Join(", ", parts.ToArray());
        }
    }
}
