using KydlCore.Common;
using GunPowdered.Features.Skilling.Domain;
using GunPowdered.Features.Skilling.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>D-157 test hooks: set and read the local player's GunPowdered skill levels (Explosives, Muskets, Cannons) and the values they give.</summary>
    internal static class SkillTestHooks
    {
        static bool TryType(string name, out global::Skills.SkillType type)
        {
            type = name == "Explosives" ? GunSkills.Explosives : name == "Muskets" ? GunSkills.Muskets : name == "Cannons" ? GunSkills.Cannons : global::Skills.SkillType.None;
            bool ok = type != global::Skills.SkillType.None;
            ModLog.Verbose("SkillTestHooks.TryType(" + name + "): returns " + ok);
            return ok;
        }

        /// <summary>Sets the local player's level in a GunPowdered skill (name: Explosives, Muskets or Cannons; level 0 .. 100): "Muskets set to 100.0".</summary>
        [TestHook("GunPowdered.setSkill")]
        static string SetSkill(string name, double level)
        {
            ModLog.Verbose("SkillTestHooks.SetSkill(name=" + name + ", level=" + level + ")");
            Player p = Player.m_localPlayer;
            if (p == null || !TryType(name, out global::Skills.SkillType type)) { ModLog.Verbose("SkillTestHooks.SetSkill: no player or unknown skill - returns why"); return "no player or unknown skill " + name; }
            object skill = HarmonyLib.Traverse.Create(p.GetSkills()).Method("GetSkill", type).GetValue();
            if (skill == null) { ModLog.Verbose("SkillTestHooks.SetSkill: the game has no such skill - returns why"); return "no skill " + name + " in the game"; }
            HarmonyLib.Traverse.Create(skill).Field("m_level").SetValue((float)level);
            HarmonyLib.Traverse.Create(skill).Field("m_accumulator").SetValue(0f);
            string s = name + " set to " + p.GetSkillLevel(type).ToString("0.0");
            ModLog.Info("SkillTestHooks.SetSkill: returns " + s);
            return s;
        }

        /// <summary>The local player's level in a GunPowdered skill and what it gives: "Muskets 100.0: reload 1.20 s, fuse 3.00 s, grenade 1.00 s, cannon load x0.625, blunt x1.50".</summary>
        [TestHook("GunPowdered.skillOf")]
        static string SkillOf(string name)
        {
            ModLog.Verbose("SkillTestHooks.SkillOf(name=" + name + ")");
            Player p = Player.m_localPlayer;
            if (p == null || !TryType(name, out global::Skills.SkillType type)) { ModLog.Verbose("SkillTestHooks.SkillOf: no player or unknown skill - returns why"); return "no player or unknown skill " + name; }
            float level = GunSkills.Level(p, type);
            string s = name + " " + level.ToString("0.0") + ": reload " + SkillRules.MusketReloadSeconds(level).ToString("0.00") + " s, fuse " + SkillRules.DynamiteFuseSeconds(level).ToString("0.00")
                + " s, grenade " + SkillRules.GrenadeFuseSeconds(level).ToString("0.00") + " s, cannon load x" + SkillRules.CannonLoadSeconds(1f, level).ToString("0.000") + ", blunt x" + SkillRules.CannonBluntFactor(level).ToString("0.00");
            ModLog.Info("SkillTestHooks.SkillOf: returns " + s);
            return s;
        }
    }
}
