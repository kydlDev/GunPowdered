using GunPowdered.Core;
namespace GunPowdered.Features.MobileCannon.Domain
{
    /// <summary>
    /// Moving the Mobile Cannon by hand (D-105, user 2026-11-19): slow and tiring, so a harnessed Asksvin is the way to move it far. Pure, so the unit tests
    /// check it: the player's speed is capped while hitched, moving costs stamina per second, and at no stamina the player lets go.
    /// </summary>
    internal static class MobileCannonPullRules
    {
        /// <summary>Below this speed (m/s) the player counts as standing: no stamina is used.</summary>
        internal const float StandingSpeed = 0.3f;

        /// <summary>The walking speed while hitched: the game's speed, but never above <paramref name="maxSpeed"/> (and never below 0).</summary>
        internal static float CapSpeed(float speed, float maxSpeed)
        {
            if (RuleLog.Due("MobileCannonPullRules.CapSpeed", 2d)) RuleLog.Every("MobileCannonPullRules.CapSpeed", 2d, $"MobileCannonPullRules.CapSpeed(speed={RuleLog.Shown(speed)}, maxSpeed={RuleLog.Shown(maxSpeed)})");
            // speed clamped into 0 .. max(maxSpeed, 0): the Min / Max form has no ties where a mutant gives the same number (a11gate Stryker: 3 such survivors)
            float max = System.Math.Max(maxSpeed, 0f);
            { var returnedAt19 = System.Math.Min(System.Math.Max(speed, 0f), max); if (RuleLog.Due("MobileCannonPullRules.CapSpeed:19", 2d)) RuleLog.Every("MobileCannonPullRules.CapSpeed:19", 2d, $"MobileCannonPullRules.CapSpeed: returns {returnedAt19} (line 19)"); return returnedAt19; }
        }

        /// <summary>Stamina used in one frame: <paramref name="perSecond"/> x the frame time while moving faster than <see cref="StandingSpeed"/>, else 0.</summary>
        internal static float StaminaCost(float speed, float deltaSeconds, float perSecond)
        {
            if (RuleLog.Due("MobileCannonPullRules.StaminaCost", 2d)) RuleLog.Every("MobileCannonPullRules.StaminaCost", 2d, $"MobileCannonPullRules.StaminaCost(speed={RuleLog.Shown(speed)}, deltaSeconds={RuleLog.Shown(deltaSeconds)}, perSecond={RuleLog.Shown(perSecond)})");
            if (speed <= StandingSpeed || deltaSeconds <= 0f || perSecond <= 0f) return 0f;
            { var returnedAt27 = perSecond * deltaSeconds; if (RuleLog.Due("MobileCannonPullRules.StaminaCost:27", 2d)) RuleLog.Every("MobileCannonPullRules.StaminaCost:27", 2d, $"MobileCannonPullRules.StaminaCost: returns {returnedAt27} (line 27)"); return returnedAt27; }
        }

        /// <summary>True when the player has no stamina left and lets go of the cannon.</summary>
        internal static bool LetsGo(float stamina) => stamina <= 0.01f;
    }
}
