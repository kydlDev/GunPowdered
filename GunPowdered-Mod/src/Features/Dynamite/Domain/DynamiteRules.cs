using GunPowdered.Core;
namespace GunPowdered.Features.Dynamite.Domain
{
    /// <summary>
    /// The Dynamite rules the user approved (D-098 proposal, D-100): a stick is placed on the ground, lit with a Torch, and blows after its fuse. The blast
    /// wrecks buildings, rocks, trees and ore deposits; creatures take only a fifth of that damage ("Creatures take 20% of building damage"). Pure, so the
    /// unit tests check it.
    /// </summary>
    internal static class DynamiteRules
    {
        /// <summary>Seconds from lighting to the blast before the Explosives skill (D-157: the fuse now runs 5 s at level 0 to 3 s at level 100, SkillRules; this is level 50).</summary>
        internal const float FuseSeconds = 4f;

        /// <summary>D-158 (user 2026-11-23: "once the character has placed the dynamite, have it get back up and not remain kneeled"): how long the kneel lasts before the character stands up again.</summary>
        internal const float KneelSeconds = 1.0f;

        /// <summary>
        /// D-183 (user 2026-11-24 08:45: "when placing the last dynamite on the ground, the character does a fist kick right after that"): the attack
        /// press that set the last stick down is still held, and with the hands now empty the game turns it into a punch. For this long after a charge was
        /// set down, an attack with nothing in the hands is swallowed.
        /// </summary>
        internal const float NoPunchSeconds = 0.6f;

        /// <summary>Whether an attack with empty hands <paramref name="secondsSincePlaced"/> s after a charge was set down is the leftover of that press.</summary>
        internal static bool SwallowsPunch(float secondsSincePlaced, bool handsEmpty)
        {
            bool swallow = handsEmpty && secondsSincePlaced >= 0f && secondsSincePlaced < NoPunchSeconds;
            if (RuleLog.Due("DynamiteRules.SwallowsPunch", 2d)) RuleLog.Every("DynamiteRules.SwallowsPunch", 2d, $"DynamiteRules.SwallowsPunch(secondsSincePlaced={secondsSincePlaced}, handsEmpty={handsEmpty}): returns {swallow}");
            return swallow;
        }

        /// <summary>The share of the building damage a creature takes (user: 20 %).</summary>
        internal const float CreatureShare = 0.2f;

        /// <summary>How far in front of the player a stick is set down (m).</summary>
        internal const float PlaceDistance = 1.2f;

        internal const string NeedTorch = "Hold a Torch to light the fuse", Burning = "The fuse is burning - get away!", NoGround = "No place for the dynamite here";

        // D-148 (user 2026-11-21 12:25): the stick goes ON the ground the player stands on, in front of whatever blocks the way, never inside a rock
        /// <summary>Height (m) above the player's feet the placing checks start from (chest height: below overhangs, above small steps).</summary>
        internal const float ChestHeight = 1.0f;

        /// <summary>How far (m) the stick stays in front of a rock / wall that blocks the way; the nearest it is ever set down in front of the player.</summary>
        internal const float BlockMargin = 0.25f, NearestPlace = 0.3f;

        /// <summary>How far (m) below / above the player's feet the surface may lie: "the ground on which the player is standing", a step at most.</summary>
        internal const float SurfaceBelow = 1.0f, SurfaceAbove = 0.8f;

        /// <summary>
        /// How far in front of the player the stick goes: the wanted distance, or less when something blocks the way at <paramref name="blockedAt"/> m (a
        /// negative value = nothing blocks). Expected outcome: never past the block minus <see cref="BlockMargin"/>, never nearer than <see cref="NearestPlace"/>.
        /// </summary>
        internal static float ClearDistance(float wanted, float blockedAt)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"DynamiteRules.ClearDistance(wanted={RuleLog.Shown(wanted)}, blockedAt={RuleLog.Shown(blockedAt)})");
            float d = blockedAt < 0f ? wanted : System.Math.Min(wanted, blockedAt - BlockMargin);
            d = System.Math.Max(NearestPlace, d);
            if (RuleLog.IsOn()) RuleLog.Verbose($"DynamiteRules.ClearDistance: returns {RuleLog.Shown(d)}");
            return d;
        }

        /// <summary>Whether a surface at <paramref name="surfaceY"/> is the ground the player stands on (feet at <paramref name="feetY"/>). Expected outcome: true within the step limits.</summary>
        internal static bool SurfaceOk(float feetY, float surfaceY)
        {
            bool ok = surfaceY >= feetY - SurfaceBelow && surfaceY <= feetY + SurfaceAbove;
            if (RuleLog.IsOn()) RuleLog.Verbose($"DynamiteRules.SurfaceOk(feetY={RuleLog.Shown(feetY)}, surfaceY={RuleLog.Shown(surfaceY)}): returns {ok}");
            return ok;
        }

        /// <summary>How far beyond its blast radius a player still sees a burning pack's countdown on screen (m).</summary>
        internal const float TimerMargin = 10f;

        /// <summary>Whether a player <paramref name="distance"/> m from a burning charge with a <paramref name="blastRadius"/> m blast sees the countdown (D-108).</summary>
        internal static bool ShowsTimer(float distance, float blastRadius)
        {
            if (RuleLog.Due("DynamiteRules.ShowsTimer", 2d)) RuleLog.Every("DynamiteRules.ShowsTimer", 2d, $"DynamiteRules.ShowsTimer(distance={RuleLog.Shown(distance)}, blastRadius={RuleLog.Shown(blastRadius)})");
            return distance >= 0f && distance <= blastRadius + TimerMargin;
        }

        /// <summary>The on-screen countdown of a burning charge, e.g. "Deep North Dynamite Pack: 3.2 s - get away!" (0.0 at the end).</summary>
        internal static string TimerText(string name, float secondsLeft)
        {
            if (RuleLog.Due("DynamiteRules.TimerText", 2d)) RuleLog.Every("DynamiteRules.TimerText", 2d, $"DynamiteRules.TimerText(name={RuleLog.Shown(name)}, secondsLeft={RuleLog.Shown(secondsLeft)})");
            float s = secondsLeft > 0f ? secondsLeft : 0f;
            { var returnedAt71 = name + ": " + s.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s - get away!"; if (RuleLog.Due("DynamiteRules.TimerText:71", 2d)) RuleLog.Every("DynamiteRules.TimerText:71", 2d, $"DynamiteRules.TimerText: returns {returnedAt71} (line 71)"); return returnedAt71; }
        }

        /// <summary>The damage a creature takes from a blast that does <paramref name="buildingDamage"/> to buildings (never below 0).</summary>
        internal static float CreatureDamage(float buildingDamage)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"DynamiteRules.CreatureDamage(buildingDamage={RuleLog.Shown(buildingDamage)})");
            return buildingDamage > 0f ? buildingDamage * CreatureShare : 0f;
        }

        /// <summary>Seconds of fuse left for a stick lit at <paramref name="litAt"/> (s) at <paramref name="now"/>; the full fuse for an unlit one (litAt below 0).</summary>
        internal static float FuseLeft(double litAt, double now, float fuseSeconds = FuseSeconds)
        {
            if (RuleLog.Due("DynamiteRules.FuseLeft", 2d)) RuleLog.Every("DynamiteRules.FuseLeft", 2d, $"DynamiteRules.FuseLeft(litAt={RuleLog.Shown(litAt)}, now={RuleLog.Shown(now)})");
            if (litAt < 0d) return fuseSeconds;
            double left = fuseSeconds - (now - litAt);
            { var returnedAt87 = left > 0d ? (float)left : 0f; if (RuleLog.Due("DynamiteRules.FuseLeft:87", 2d)) RuleLog.Every("DynamiteRules.FuseLeft:87", 2d, $"DynamiteRules.FuseLeft: returns {returnedAt87} (line 87)"); return returnedAt87; }
        }

        /// <summary>Whether a stick lit at <paramref name="litAt"/> blows at <paramref name="now"/> (never for an unlit one).</summary>
        internal static bool Blows(double litAt, double now, float fuseSeconds = FuseSeconds)
        {
            if (RuleLog.Due("DynamiteRules.Blows", 2d)) RuleLog.Every("DynamiteRules.Blows", 2d, $"DynamiteRules.Blows(litAt={RuleLog.Shown(litAt)}, now={RuleLog.Shown(now)})");
            return litAt >= 0d && now - litAt >= fuseSeconds;
        }

        /// <summary>Why a stick cannot be lit (null = it can): it burns already, or the player holds no Torch.</summary>
        internal static string WhyNotLight(bool lit, bool torchInHand)
        {
            if (RuleLog.Due("DynamiteRules.WhyNotLight", 2d)) RuleLog.Every("DynamiteRules.WhyNotLight", 2d, $"DynamiteRules.WhyNotLight(lit={RuleLog.Shown(lit)}, torchInHand={RuleLog.Shown(torchInHand)})");
            if (lit) return Burning;
            { var returnedAt102 = torchInHand ? null : NeedTorch; if (RuleLog.Due("DynamiteRules.WhyNotLight:102", 2d)) RuleLog.Every("DynamiteRules.WhyNotLight:102", 2d, $"DynamiteRules.WhyNotLight: returns {returnedAt102} (line 102)"); return returnedAt102; }
        }
    }
}
