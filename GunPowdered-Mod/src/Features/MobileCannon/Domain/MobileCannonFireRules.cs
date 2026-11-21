using System;
using GunPowdered.Core;

namespace GunPowdered.Features.MobileCannon.Domain
{
    /// <summary>
    /// Loading and firing the Mobile Cannon (D-045: one shot loaded at a time, kept over relogs; D-104: a torch on the touch hole lights a short fuse).
    /// Pure, so the unit tests check it: each check answers null when allowed, else the reason shown to the player; the range follows the elevation.
    /// </summary>
    internal static class MobileCannonFireRules
    {
        internal const string AlreadyLoaded = "The Mobile Cannon is already loaded", NotLoaded = "Load the Mobile Cannon first (gunpowder and a Flametal Cannon Ball at the muzzle)",
            NeedBall = "You need a Flametal Cannon Ball or a better one", NeedTorch = "Hold a torch to light the fuse", FuseBurning = "The fuse is already burning";

        /// <summary>Loading needs an empty cannon, <paramref name="powderNeed"/> gunpowder and one Cannon Ball.</summary>
        internal static string WhyNotLoad(bool loaded, int powderHave, int powderNeed, int ballsHave)
        {
            if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.WhyNotLoad", 2d, $"MobileCannonFireRules.WhyNotLoad(loaded={RuleLog.Shown(loaded)}, powderHave={RuleLog.Shown(powderHave)}, powderNeed={RuleLog.Shown(powderNeed)}, ballsHave={RuleLog.Shown(ballsHave)})");
            if (loaded) { var returnedAt19 = AlreadyLoaded; if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.WhyNotLoad:19", 2d, $"MobileCannonFireRules.WhyNotLoad: returns {RuleLog.Shown(returnedAt19)} (line 19)"); return returnedAt19; }
            if (powderHave < powderNeed) { var returnedAt20 = "You need " + powderNeed + " Gunpowder"; if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.WhyNotLoad:20", 2d, $"MobileCannonFireRules.WhyNotLoad: returns {RuleLog.Shown(returnedAt20)} (line 20)"); return returnedAt20; }
            { var returnedAt21 = ballsHave >= 1 ? null : NeedBall; if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.WhyNotLoad:21", 2d, $"MobileCannonFireRules.WhyNotLoad: returns {RuleLog.Shown(returnedAt21)} (line 21)"); return returnedAt21; }
        }

        /// <summary>Lighting needs a loaded cannon, no fuse burning yet and a torch in hand.</summary>
        internal static string WhyNotLight(bool loaded, bool lit, bool torchInHand)
        {
            if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.WhyNotLight", 2d, $"MobileCannonFireRules.WhyNotLight(loaded={RuleLog.Shown(loaded)}, lit={RuleLog.Shown(lit)}, torchInHand={RuleLog.Shown(torchInHand)})");
            if (!loaded) { var returnedAt28 = NotLoaded; if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.WhyNotLight:28", 2d, $"MobileCannonFireRules.WhyNotLight: returns {RuleLog.Shown(returnedAt28)} (line 28)"); return returnedAt28; }
            if (lit) { var returnedAt29 = FuseBurning; if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.WhyNotLight:29", 2d, $"MobileCannonFireRules.WhyNotLight: returns {RuleLog.Shown(returnedAt29)} (line 29)"); return returnedAt29; }
            { var returnedAt30 = torchInHand ? null : NeedTorch; if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.WhyNotLight:30", 2d, $"MobileCannonFireRules.WhyNotLight: returns {RuleLog.Shown(returnedAt30)} (line 30)"); return returnedAt30; }
        }

        /// <summary>The flat distance a ball flies on level ground: speed^2 x sin(2 x pitch) / gravity (m); 0 for a level or lower barrel or no gravity.</summary>
        internal static float Range(float speed, float pitchDegrees, float gravity)
        {
            if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.Range", 2d, $"MobileCannonFireRules.Range(speed={RuleLog.Shown(speed)}, pitchDegrees={RuleLog.Shown(pitchDegrees)}, gravity={RuleLog.Shown(gravity)})");
            if (gravity <= 0f || speed <= 0f) { if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.Range:37", 2d, "MobileCannonFireRules.Range: returns 0f (line 37)"); return 0f; }
            double r = speed * speed * Math.Sin(2.0 * pitchDegrees * Math.PI / 180.0) / gravity;
            { var returnedAt39 = r > 0 ? (float)r : 0f; if (RuleLog.IsOn()) RuleLog.Every("MobileCannonFireRules.Range:39", 2d, $"MobileCannonFireRules.Range: returns {RuleLog.Shown(returnedAt39)} (line 39)"); return returnedAt39; }
        }
    }
}
