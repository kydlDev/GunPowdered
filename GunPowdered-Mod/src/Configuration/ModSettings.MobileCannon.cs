using KydlCore.Common;

namespace GunPowdered.Configuration
{
    /// <summary>The Mobile Cannon numbers (D-105: moving it by hand is slow and tiring) as dev-only Tunables (KydlTools F7).</summary>
    internal static partial class ModSettings
    {
        public static readonly Tunable<float> MobileCannonPullSpeed = new Tunable<float>("MobileCannon", "MobileCannonPullSpeed", 3.5f, 1f, 10f,
            "Fastest a player walks while moving the Mobile Cannon by hand, in m/s (1-10; user: about 3-4).");
        public static readonly Tunable<float> MobileCannonPullStamina = new Tunable<float>("MobileCannon", "MobileCannonPullStamina", 6f, 0f, 50f,
            "Stamina per second a player uses while moving the Mobile Cannon by hand (0-50); at no stamina the player lets go.");
        public static readonly Tunable<float> MobileCannonMinPitch = new Tunable<float>("MobileCannon", "MobileCannonMinPitch", -5f, -20f, 0f,
            "Lowest barrel elevation in degrees (-20-0).");
        public static readonly Tunable<float> MobileCannonMaxPitch = new Tunable<float>("MobileCannon", "MobileCannonMaxPitch", 25f, 5f, 45f,
            "Highest barrel elevation in degrees (5-45).");
        public static readonly Tunable<float> MobileCannonPitchStep = new Tunable<float>("MobileCannon", "MobileCannonPitchStep", 1f, 0.25f, 5f,
            "Degrees of elevation per mouse-wheel notch on the crank (0.25-5).");
        public static readonly Tunable<float> MobileCannonMuzzleSpeed = new Tunable<float>("MobileCannon", "MobileCannonMuzzleSpeed", 30f, 10f, 80f,
            "Speed of the Mobile Cannon's ball at the muzzle in m/s (10-80; 30 = about 45 m at 15 degrees, 69 m at 25).");
        public static readonly Tunable<int> MobileCannonPowderPerShot = new Tunable<int>("MobileCannon", "MobileCannonPowderPerShot", 3, 1, 20,
            "Gunpowder one Mobile Cannon shot uses (1-20); plus one Cannon Ball.");
        public static readonly Tunable<float> MobileCannonLoadSeconds = new Tunable<float>("MobileCannon", "MobileCannonLoadSeconds", 4f, 0.5f, 20f,
            "Seconds the player loads at the muzzle (0.5-20).");
        public static readonly Tunable<float> MobileCannonFuseSeconds = new Tunable<float>("MobileCannon", "MobileCannonFuseSeconds", 1.5f, 0.2f, 6f,
            "Seconds from the torch on the touch hole to the shot (0.2-6).");
        public static readonly Tunable<float> MobileCannonRecoil = new Tunable<float>("MobileCannon", "MobileCannonRecoil", 1.5f, 0f, 6f,
            "Speed in m/s the cannon rolls back when it fires (0-6).");
        public static readonly Tunable<float> MobileCannonHitchRange = new Tunable<float>("MobileCannon", "MobileCannonHitchRange", 6f, 1f, 20f,
            "Farthest a harnessed Asksvin's tow hook may be from the cannon's tow ring to hitch it, in m (1-20; D-116).");
        public static readonly Tunable<float> MobileCannonTowExtraMass = new Tunable<float>("MobileCannon", "MobileCannonTowExtraMass", 200f, 0f, 1000f,
            "Mass added to a towing Asksvin while the cannon is hitched (0-1000; the game does the same for a player pulling a cart). Without it the cannon holds the Asksvin back to about 5 of its 9 m/s (lab a8tow5); D-105 wants its full speed.");
    }
}
