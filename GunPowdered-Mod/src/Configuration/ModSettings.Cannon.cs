using KydlCore.Common;

namespace GunPowdered.Configuration
{
    /// <summary>
    /// The mounted Hand Cannon numbers the user approved (D-051 .. D-054) as dev-only Tunables (KydlTools F7). They are read when they are used
    /// (loading, aiming, the shot), so a change shows in game without a restart.
    /// </summary>
    internal static partial class ModSettings
    {
        public static readonly Tunable<float> CannonRange = new Tunable<float>("Cannon", "CannonRange", 30f, 5f, 80f,
            "Farthest aimed spot in metres (5-80). The ball's speed is the one that carries it this far on flat ground at the stock's tilt (D-052).");
        public static readonly Tunable<float> CannonMinRange = new Tunable<float>("Cannon", "CannonMinRange", 3f, 1f, 20f,
            "Nearest aimed spot in metres (1-20).");
        public static readonly Tunable<float> CannonPitch = new Tunable<float>("Cannon", "CannonPitch", 9.46f, 2f, 45f,
            "The stock's own tilt in degrees (2-45): the rear trestle is 4 cm lower than the front one (D-052); with the range it sets the ball speed.");
        public static readonly Tunable<float> CannonGravity = new Tunable<float>("Cannon", "CannonGravity", 10f, 1f, 30f,
            "Gravity of the ball in flight, m/s per second (1-30).");
        public static readonly Tunable<float> CannonPierce = new Tunable<float>("Cannon", "CannonPierce", 30f, 0f, 1000f,
            "Pierce damage of the ball (0-1000, D-107).");
        public static readonly Tunable<float> CannonBlunt = new Tunable<float>("Cannon", "CannonBlunt", 70f, 0f, 1000f,
            "Blunt damage of the ball (0-1000, D-107).");
        public static readonly Tunable<float> CannonFire = new Tunable<float>("Cannon", "CannonFire", 10f, 0f, 100f,
            "Fire damage per second of the burning the ball sets, for 5 s (0-100, D-107).");
        public static readonly Tunable<float> CannonToolTier = new Tunable<float>("Cannon", "CannonToolTier", 2f, 0f, 10f,
            "Tool tier of the ball against trees (0-10); 2 = bronze, so the swamp's ancient trees can be broken too.");
        public static readonly Tunable<float> CannonPassFactor = new Tunable<float>("Cannon", "CannonPassFactor", 0.5f, 0f, 1f,
            "Share of speed and damage the ball keeps after passing a tree or creature (0-1, D-053 addendum: 'maybe 50% less').");
        public static readonly Tunable<float> CannonRecoil = new Tunable<float>("Cannon", "CannonRecoil", 4f, 0f, 20f,
            "Push back on the player who fires (0-20, the game's recoil pushback).");
        public static readonly Tunable<float> CannonLoadSeconds = new Tunable<float>("Cannon", "CannonLoadSeconds", 4f, 0.5f, 20f,
            "Seconds the loading at the muzzle takes (0.5-20, D-053).");
        public static readonly Tunable<float> CannonFuseSeconds = new Tunable<float>("Cannon", "CannonFuseSeconds", 0.4f, 0.1f, 5f,
            "Seconds from lighting the fuse to the shot (0.1-5, D-052 addendum).");
        public static readonly Tunable<float> CannonPowderPerShot = new Tunable<float>("Cannon", "CannonPowderPerShot", 1f, 1f, 20f,
            "Gunpowder used by one load (1-20, whole numbers).");
        public static readonly Tunable<float> CannonPreviewMeters = new Tunable<float>("Cannon", "CannonPreviewMeters", 4f, 0f, 30f,
            "Length of the short aiming line from the muzzle in metres (0-30; 0 = no line).");
        public static readonly Tunable<float> CannonMaxFlightSeconds = new Tunable<float>("Cannon", "CannonMaxFlightSeconds", 8f, 1f, 30f,
            "A ball still in the air after this many seconds drops where it is (1-30).");
    }
}
