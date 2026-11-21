using KydlCore.Common;

namespace GunPowdered.Configuration
{
    /// <summary>
    /// The cannon balls' part of a shot (D-145, user 2026-11-21) as dev-only Tunables (KydlTools F7): a shot deals the cannon's part (ModSettings.Cannon /
    /// ModSettings.MobileCannon) plus the ball's part. Read at every hit, so a change shows in game without a restart.
    /// </summary>
    internal static partial class ModSettings
    {
        public static readonly Tunable<float> IronBallPierce = new Tunable<float>("CannonBalls", "IronBallPierce", 30f, 0f, 1000f,
            "Pierce damage of the Iron Cannon Ball (0-1000, D-145).");
        public static readonly Tunable<float> IronBallToolTier = new Tunable<float>("CannonBalls", "IronBallToolTier", 2f, 0f, 10f,
            "Tool tier of the Iron Cannon Ball (0-10); 2 = bronze, so the swamp's ancient trees can be broken too (D-145).");
        public static readonly Tunable<float> FlametalBallPierce = new Tunable<float>("CannonBalls", "FlametalBallPierce", 50f, 0f, 2000f,
            "Pierce damage of the Flametal Cannon Ball (0-2000, D-145).");
        public static readonly Tunable<float> FlametalBallToolTier = new Tunable<float>("CannonBalls", "FlametalBallToolTier", 5f, 0f, 10f,
            "Tool tier of the Flametal Cannon Ball (0-10); 5 = the Ashlands fortresses (D-107, D-145).");
    }
}
