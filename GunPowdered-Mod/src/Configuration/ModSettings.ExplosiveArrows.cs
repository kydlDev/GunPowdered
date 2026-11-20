using KydlCore.Common;

namespace GunPowdered.Configuration
{
    /// <summary>
    /// The Explosive Arrow numbers the user approved (D-100) as dev-only Tunables (KydlTools F7), applied to the arrow, its flight and its burst at start
    /// (ExplosiveArrowSetup.Apply).
    /// </summary>
    internal static partial class ModSettings
    {
        public static readonly Tunable<float> ExplosiveArrowPierce = new Tunable<float>("ExplosiveArrows", "ExplosiveArrowPierce", 30f, 0f, 500f,
            "Pierce damage of a direct hit (0-500); the bow's own damage is added as with any arrow.");
        public static readonly Tunable<float> ExplosiveArrowRadius = new Tunable<float>("ExplosiveArrows", "ExplosiveArrowRadius", 3f, 0.5f, 10f,
            "Radius of the burst on impact in m (0.5-10).");
        public static readonly Tunable<float> ExplosiveArrowBlunt = new Tunable<float>("ExplosiveArrows", "ExplosiveArrowBlunt", 20f, 0f, 500f,
            "Blunt damage of the burst (0-500): creatures (with their armour), buildings, trees and rocks take it like any other damage.");
        public static readonly Tunable<float> ExplosiveArrowFire = new Tunable<float>("ExplosiveArrows", "ExplosiveArrowFire", 5f, 0f, 100f,
            "Fire damage of the burst (0-500).");
    }
}
