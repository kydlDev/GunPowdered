using Jotunn.Configs;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Integrations.Items;
using GunPowdered.Core;

namespace GunPowdered.Features.Skilling.Infrastructure
{
    /// <summary>
    /// D-157 (user 2026-11-23): the three GunPowdered skills in the game's Skills window - Explosives, Muskets, Cannons - registered once at start with Jotunn's
    /// SkillManager (the identifiers never change: they are what the players' saved skill levels are kept under). The values per level: SkillRules.
    /// A skill's level is known only on its player's own client (the game does not sync skills), so everything that depends on it is worked out there and
    /// the result is synced (a fuse end, a stroke length).
    /// </summary>
    internal static class GunSkills
    {
        internal const string ExplosivesId = "com.kydldev.GunPowdered.Explosives", MusketsId = "com.kydldev.GunPowdered.Muskets", CannonsId = "com.kydldev.GunPowdered.Cannons";

        internal static Skills.SkillType Explosives { get; private set; }
        internal static Skills.SkillType Muskets { get; private set; }
        internal static Skills.SkillType Cannons { get; private set; }
        static bool _registered;

        /// <summary>Registers the three skills (Plugin.Awake, after the bundle loaded: the icons are the items' icons). Expected outcome: true when registered.</summary>
        internal static bool Register()
        {
            if (_registered) { ModLog.Verbose("GunSkills.Register: already registered - returns true"); return true; }
            Explosives = Add(ExplosivesId, "Explosives", "Dynamite and grenades: a higher level burns the fuse faster (dynamite 5 s at level 0 to 3 s at level 100, a landed grenade 3 s to 1 s). Lighting or throwing one trains it.", Core.GunPowderedPrefabs.Dynamite);
            Muskets = Add(MusketsId, "Muskets", "The Iron Musket: a higher level reloads faster (1.8 s per cartridge at level 0 to 1.2 s at level 100). Each hit on a creature and each cartridge loaded trains it.", Core.GunPowderedPrefabs.Musket);
            Cannons = Add(CannonsId, "Cannons", "The Cannon and the Mobile Cannon: a higher level loads faster (4 s at level 0 to 2.5 s at level 100) and hits harder (blunt damage up to x1.5). Each shot trains it.", Core.GunPowderedPrefabs.CannonBall);
            _registered = true;
            ModLog.Info("GunSkills: Explosives " + (int)Explosives + ", Muskets " + (int)Muskets + ", Cannons " + (int)Cannons + " registered");
            return true;
        }

        static Skills.SkillType Add(string id, string name, string description, string iconItem)
        {
            Sprite icon = BundleVisuals.Icon(iconItem);
            if (icon == null) ModLog.Warn("GunSkills: no icon for the " + name + " skill (item " + iconItem + " has none in the bundle)");
            Skills.SkillType type = SkillManager.Instance.AddSkill(new SkillConfig { Identifier = id, Name = name, Description = description, Icon = icon, IncreaseStep = 1f });
            ModLog.Verbose("GunSkills.Add: " + name + " -> " + (int)type);
            return type;
        }

        /// <summary>The player's level (0 .. 100) in a GunPowdered skill; 0 without a player. Only right on the player's own client.</summary>
        internal static float Level(Character who, Skills.SkillType skill)
        {
            float level = who != null && _registered ? who.GetSkillLevel(skill) : 0f;
            if (Core.RuleLog.Due("GunSkills.Level", 2d)) ModLog.Every("GunSkills.Level", 2d, "GunSkills.Level: " + (who != null ? who.name : "nobody") + " skill " + (int)skill + " = " + level.ToString("0.0"));
            { var returnedAt51 = level; if (RuleLog.Due("GunSkills.Level:51", 2d)) ModLog.Every("GunSkills.Level:51", 2d, $"GunSkills.Level: returns {returnedAt51} (line 51)"); return returnedAt51; }
        }

        /// <summary>Raises a GunPowdered skill for a player (the game's own RaiseSkill: its gain, its level-up message). Expected outcome: true when raised.</summary>
        internal static bool Raise(Character who, Skills.SkillType skill, float amount)
        {
            if (who == null || !_registered || amount <= 0f) { ModLog.Verbose("GunSkills.Raise: nothing raised (no player, not registered or no amount) - returns false"); return false; }
            who.RaiseSkill(skill, amount);
            ModLog.Verbose("GunSkills.Raise: " + who.name + " skill " + (int)skill + " +" + amount);
            return true;
        }
    }
}
