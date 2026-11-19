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
    }
}
