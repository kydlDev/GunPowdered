using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Skilling.Domain
{
    /// <summary>
    /// D-157 (user 2026-11-23): the three GunPowdered skills - Explosives, Muskets, Cannons. Every value runs in a straight line from skill level 0 to level 100
    /// (spec _agent/specs/D-157-skills-and-dynamite.md). Pure, so the unit tests check it.
    /// </summary>
    internal static class SkillRules
    {
        /// <summary>Muskets: seconds per cartridge at level 0 and 100 (user: "1.8s load time be at skill lvl 0, and at lvl 100 have the 1.2s duration").</summary>
        internal const float MusketReloadAtZero = 1.8f, MusketReloadAtHundred = 1.2f;

        /// <summary>Explosives: the dynamite fuse at level 0 and 100 (user: now 4 s; "add 1 second ... at lvl 0", "1 second faster ... at level 100").</summary>
        internal const float DynamiteFuseAtZero = 5f, DynamiteFuseAtHundred = 3f;

        /// <summary>Explosives: the Spiked Grenade's fuse after it lands (user 2026-11-23: "Add a fuse after landing"; 3 s at level 0, 1 s at level 100).</summary>
        internal const float GrenadeFuseAtZero = 3f, GrenadeFuseAtHundred = 1f;

        /// <summary>Cannons: the share of the configured load time (4 s) and of the shot's blunt damage at level 0 and 100 (user: "Load 4->2.5 s, blunt x1.0->x1.5").</summary>
        internal const float CannonLoadShareAtZero = 1f, CannonLoadShareAtHundred = 2.5f / 4f, CannonBluntAtZero = 1f, CannonBluntAtHundred = 1.5f;

        /// <summary>Skill gain: a lit explosive, a cannon shot, a loaded cartridge (the game's RaiseSkill amounts).</summary>
        internal const float GainLight = 1f, GainCannonShot = 1f, GainCartridge = 0.2f;

        /// <summary>A skill level as 0 .. 1 (levels outside 0 .. 100 are clamped).</summary>
        internal static float Share(float level)
        {
            float s = float.IsNaN(level) ? 0f : Math.Max(0f, Math.Min(1f, level / 100f));
            if (RuleLog.Due("SkillRules.Share", 2d)) RuleLog.Every("SkillRules.Share", 2d, $"SkillRules.Share(level={level}): returns {s}");
            return s;
        }

        /// <summary>The value at this level on the straight line from <paramref name="atZero"/> (level 0) to <paramref name="atHundred"/> (level 100).</summary>
        internal static float Lerp(float atZero, float atHundred, float level)
        {
            float v = atZero + (atHundred - atZero) * Share(level);
            if (RuleLog.Due("SkillRules.Lerp", 2d)) RuleLog.Every("SkillRules.Lerp", 2d, $"SkillRules.Lerp(atZero={atZero}, atHundred={atHundred}, level={level}): returns {v}");
            return v;
        }

        /// <summary>Muskets: seconds per cartridge for a player of this level.</summary>
        internal static float MusketReloadSeconds(float level) => Lerp(MusketReloadAtZero, MusketReloadAtHundred, level);

        /// <summary>Explosives: the dynamite fuse (stick and Deep North Dynamite Pack) lit by a player of this level.</summary>
        internal static float DynamiteFuseSeconds(float level) => Lerp(DynamiteFuseAtZero, DynamiteFuseAtHundred, level);

        /// <summary>Explosives: how long a landed Spiked Grenade thrown by a player of this level burns before it bursts.</summary>
        internal static float GrenadeFuseSeconds(float level) => Lerp(GrenadeFuseAtZero, GrenadeFuseAtHundred, level);

        /// <summary>Cannons: the load time for a player of this level, from the configured load time (the level-0 value).</summary>
        internal static float CannonLoadSeconds(float configured, float level) => configured * Lerp(CannonLoadShareAtZero, CannonLoadShareAtHundred, level);

        /// <summary>Cannons: the factor on a shot's blunt damage for the player who lit the fuse.</summary>
        internal static float CannonBluntFactor(float level) => Lerp(CannonBluntAtZero, CannonBluntAtHundred, level);
    }
}
