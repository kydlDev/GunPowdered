using System;

namespace GunPowdered.Features.MobileCannon.Domain
{
    /// <summary>
    /// Loading and firing the Mobile Cannon (D-045: one shot loaded at a time, kept over relogs; D-104: a torch on the touch hole lights a short fuse).
    /// Pure, so the unit tests check it: each check answers null when allowed, else the reason shown to the player; the range follows the elevation.
    /// </summary>
    internal static class MobileCannonFireRules
    {
        internal const string AlreadyLoaded = "The Mobile Cannon is already loaded", NotLoaded = "Load the Mobile Cannon first (gunpowder and a Cannon Ball at the muzzle)",
            NeedBall = "You need a Cannon Ball", NeedTorch = "Hold a torch to light the fuse", FuseBurning = "The fuse is already burning";

        /// <summary>Loading needs an empty cannon, <paramref name="powderNeed"/> gunpowder and one Cannon Ball.</summary>
        internal static string WhyNotLoad(bool loaded, int powderHave, int powderNeed, int ballsHave)
        {
            if (loaded) return AlreadyLoaded;
            if (powderHave < powderNeed) return "You need " + powderNeed + " Gunpowder";
            return ballsHave >= 1 ? null : NeedBall;
        }

        /// <summary>Lighting needs a loaded cannon, no fuse burning yet and a torch in hand.</summary>
        internal static string WhyNotLight(bool loaded, bool lit, bool torchInHand)
        {
            if (!loaded) return NotLoaded;
            if (lit) return FuseBurning;
            return torchInHand ? null : NeedTorch;
        }

        /// <summary>The flat distance a ball flies on level ground: speed^2 x sin(2 x pitch) / gravity (m); 0 for a level or lower barrel or no gravity.</summary>
        internal static float Range(float speed, float pitchDegrees, float gravity)
        {
            if (gravity <= 0f || speed <= 0f) return 0f;
            double r = speed * speed * Math.Sin(2.0 * pitchDegrees * Math.PI / 180.0) / gravity;
            return r > 0 ? (float)r : 0f;
        }
    }
}
