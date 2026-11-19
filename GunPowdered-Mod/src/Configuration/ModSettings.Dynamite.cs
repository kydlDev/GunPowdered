using KydlCore.Common;

namespace GunPowdered.Configuration
{
    /// <summary>
    /// The Dynamite numbers (D-098 proposal approved in D-100) as dev-only Tunables (KydlTools F7), put on the blast and the crater at start
    /// (DynamiteSetup.Apply). Creatures take DynamiteRules.CreatureShare (20 %) of the building damage.
    /// </summary>
    internal static partial class ModSettings
    {
        public static readonly Tunable<float> DynamiteRadius = new Tunable<float>("Dynamite", "DynamiteRadius", 4f, 1f, 12f,
            "Radius of the blast in m (1-12).");
        public static readonly Tunable<float> DynamiteDamage = new Tunable<float>("Dynamite", "DynamiteDamage", 150f, 0f, 2000f,
            "Damage of the blast to buildings, rocks, trees and ore deposits, each of blunt, chop and pickaxe (0-2000); creatures take 20 % of it.");
        public static readonly Tunable<int> DynamiteToolTier = new Tunable<int>("Dynamite", "DynamiteToolTier", 2, 0, 6,
            "The toughest material the blast breaks, as the game's tool tier (0-6; 2 = what an iron pickaxe breaks).");
        public static readonly Tunable<float> DynamiteCraterRadius = new Tunable<float>("Dynamite", "DynamiteCraterRadius", 1.25f, 0.25f, 5f,
            "Radius of the round hole in the ground in m (0.25-5; 1.25 = about 2.5 m wide).");
        public static readonly Tunable<float> DynamiteCraterDepth = new Tunable<float>("Dynamite", "DynamiteCraterDepth", 1.5f, 0.1f, 5f,
            "Depth of the hole in m (0.1-5).");
    }
}
