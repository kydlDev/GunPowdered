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
        /// <summary>Seconds from lighting to the blast.</summary>
        internal const float FuseSeconds = 4f;

        /// <summary>The share of the building damage a creature takes (user: 20 %).</summary>
        internal const float CreatureShare = 0.2f;

        /// <summary>How far in front of the player a stick is set down (m).</summary>
        internal const float PlaceDistance = 1.2f;

        internal const string NeedTorch = "Hold a Torch to light the fuse", Burning = "The fuse is burning - get away!", NoGround = "No place for the dynamite here";

        /// <summary>How far beyond its blast radius a player still sees a burning pack's countdown on screen (m).</summary>
        internal const float TimerMargin = 10f;

        /// <summary>Whether a player <paramref name="distance"/> m from a burning charge with a <paramref name="blastRadius"/> m blast sees the countdown (D-108).</summary>
        internal static bool ShowsTimer(float distance, float blastRadius)
        {
            if (RuleLog.Due("DynamiteRules.ShowsTimer", 2d)) RuleLog.Every("DynamiteRules.ShowsTimer", 2d, $"DynamiteRules.ShowsTimer(distance={RuleLog.Shown(distance)}, blastRadius={RuleLog.Shown(blastRadius)})");
            { var returnedAt29 = distance >= 0f && distance <= blastRadius + TimerMargin; if (RuleLog.Due("DynamiteRules.ShowsTimer:29", 2d)) RuleLog.Every("DynamiteRules.ShowsTimer:29", 2d, $"DynamiteRules.ShowsTimer: returns {RuleLog.Shown(returnedAt29)} (line 29)"); return returnedAt29; }
        }

        /// <summary>The on-screen countdown of a burning charge, e.g. "Deep North Dynamite Pack: 3.2 s - get away!" (0.0 at the end).</summary>
        internal static string TimerText(string name, float secondsLeft)
        {
            if (RuleLog.Due("DynamiteRules.TimerText", 2d)) RuleLog.Every("DynamiteRules.TimerText", 2d, $"DynamiteRules.TimerText(name={RuleLog.Shown(name)}, secondsLeft={RuleLog.Shown(secondsLeft)})");
            float s = secondsLeft > 0f ? secondsLeft : 0f;
            { var returnedAt37 = name + ": " + s.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s - get away!"; if (RuleLog.Due("DynamiteRules.TimerText:37", 2d)) RuleLog.Every("DynamiteRules.TimerText:37", 2d, $"DynamiteRules.TimerText: returns {RuleLog.Shown(returnedAt37)} (line 37)"); return returnedAt37; }
        }

        /// <summary>The damage a creature takes from a blast that does <paramref name="buildingDamage"/> to buildings (never below 0).</summary>
        internal static float CreatureDamage(float buildingDamage)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"DynamiteRules.CreatureDamage(buildingDamage={RuleLog.Shown(buildingDamage)})");
            { var returnedAt44 = buildingDamage > 0f ? buildingDamage * CreatureShare : 0f; if (RuleLog.IsOn()) RuleLog.Verbose($"DynamiteRules.CreatureDamage: returns {RuleLog.Shown(returnedAt44)} (line 44)"); return returnedAt44; }
        }

        /// <summary>Seconds of fuse left for a stick lit at <paramref name="litAt"/> (s) at <paramref name="now"/>; the full fuse for an unlit one (litAt below 0).</summary>
        internal static float FuseLeft(double litAt, double now)
        {
            if (RuleLog.Due("DynamiteRules.FuseLeft", 2d)) RuleLog.Every("DynamiteRules.FuseLeft", 2d, $"DynamiteRules.FuseLeft(litAt={RuleLog.Shown(litAt)}, now={RuleLog.Shown(now)})");
            if (litAt < 0d) { var returnedAt51 = FuseSeconds; if (RuleLog.Due("DynamiteRules.FuseLeft:51", 2d)) RuleLog.Every("DynamiteRules.FuseLeft:51", 2d, $"DynamiteRules.FuseLeft: returns {RuleLog.Shown(returnedAt51)} (line 51)"); return returnedAt51; }
            double left = FuseSeconds - (now - litAt);
            { var returnedAt53 = left > 0d ? (float)left : 0f; if (RuleLog.Due("DynamiteRules.FuseLeft:53", 2d)) RuleLog.Every("DynamiteRules.FuseLeft:53", 2d, $"DynamiteRules.FuseLeft: returns {RuleLog.Shown(returnedAt53)} (line 53)"); return returnedAt53; }
        }

        /// <summary>Whether a stick lit at <paramref name="litAt"/> blows at <paramref name="now"/> (never for an unlit one).</summary>
        internal static bool Blows(double litAt, double now)
        {
            if (RuleLog.Due("DynamiteRules.Blows", 2d)) RuleLog.Every("DynamiteRules.Blows", 2d, $"DynamiteRules.Blows(litAt={RuleLog.Shown(litAt)}, now={RuleLog.Shown(now)})");
            { var returnedAt60 = litAt >= 0d && now - litAt >= FuseSeconds; if (RuleLog.Due("DynamiteRules.Blows:60", 2d)) RuleLog.Every("DynamiteRules.Blows:60", 2d, $"DynamiteRules.Blows: returns {RuleLog.Shown(returnedAt60)} (line 60)"); return returnedAt60; }
        }

        /// <summary>Why a stick cannot be lit (null = it can): it burns already, or the player holds no Torch.</summary>
        internal static string WhyNotLight(bool lit, bool torchInHand)
        {
            if (RuleLog.Due("DynamiteRules.WhyNotLight", 2d)) RuleLog.Every("DynamiteRules.WhyNotLight", 2d, $"DynamiteRules.WhyNotLight(lit={RuleLog.Shown(lit)}, torchInHand={RuleLog.Shown(torchInHand)})");
            if (lit) { var returnedAt67 = Burning; if (RuleLog.Due("DynamiteRules.WhyNotLight:67", 2d)) RuleLog.Every("DynamiteRules.WhyNotLight:67", 2d, $"DynamiteRules.WhyNotLight: returns {RuleLog.Shown(returnedAt67)} (line 67)"); return returnedAt67; }
            { var returnedAt68 = torchInHand ? null : NeedTorch; if (RuleLog.Due("DynamiteRules.WhyNotLight:68", 2d)) RuleLog.Every("DynamiteRules.WhyNotLight:68", 2d, $"DynamiteRules.WhyNotLight: returns {RuleLog.Shown(returnedAt68)} (line 68)"); return returnedAt68; }
        }
    }
}
