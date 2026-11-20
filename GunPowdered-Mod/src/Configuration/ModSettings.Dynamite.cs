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
        public static readonly Tunable<float> DynamiteDamage = new Tunable<float>("Dynamite", "DynamiteDamage", 100f, 0f, 2000f,
            "Damage of the blast to buildings, rocks, trees and ore deposits, each of blunt, chop and pickaxe (0-2000); creatures take 20 % of it.");
        public static readonly Tunable<int> DynamiteToolTier = new Tunable<int>("Dynamite", "DynamiteToolTier", 2, 0, 6,
            "The toughest material the blast breaks, as the game's tool tier (0-6; 2 = what an iron pickaxe breaks).");
        public static readonly Tunable<float> DynamiteCraterRadius = new Tunable<float>("Dynamite", "DynamiteCraterRadius", 1.25f, 0.25f, 5f,
            "Radius of the round hole in the ground in m (0.25-5; 1.25 = about 2.5 m wide).");
        public static readonly Tunable<float> DynamiteCraterDepth = new Tunable<float>("Dynamite", "DynamiteCraterDepth", 1.5f, 0.1f, 5f,
            "Depth of the hole in m (0.1-5).");
        public static readonly Tunable<float> DynamiteCreatureFire = new Tunable<float>("Dynamite", "DynamiteCreatureFire", 5f, 0f, 100f,
            "Fire damage per second of the burning the blast sets on creatures, for 5 s, on top of their 20 % share (0-100, D-107).");

        // D-108: the Deep North Dynamite Pack (user 2026-11-20)
        public static readonly Tunable<float> PackRadius = new Tunable<float>("Dynamite", "PackRadius", 20f, 1f, 40f,
            "Radius of the Deep North Dynamite Pack's blast in m (1-40): everything in it is hit.");
        public static readonly Tunable<float> PackPickaxe = new Tunable<float>("Dynamite", "PackPickaxe", 500f, 0f, 5000f,
            "Pickaxe damage of the pack to rock, ore and walls (0-5000).");
        public static readonly Tunable<float> PackDamage = new Tunable<float>("Dynamite", "PackDamage", 400f, 0f, 5000f,
            "Blunt and chop damage of the pack to buildings and trees (0-5000); creatures take 20 % of it.");
        public static readonly Tunable<int> PackToolTier = new Tunable<int>("Dynamite", "PackToolTier", 6, 0, 10,
            "The toughest material the pack breaks, as the game's tool tier (0-10; 6 = the Deep North's gammel trolls).");
        public static readonly Tunable<float> PackCreatureFire = new Tunable<float>("Dynamite", "PackCreatureFire", 30f, 0f, 200f,
            "Fire damage per second of the burning the pack sets on creatures, for 5 s, on top of their 20 % share (0-200).");
        public static readonly Tunable<float> PackCraterRadius = new Tunable<float>("Dynamite", "PackCraterRadius", 5f, 0.25f, 10f,
            "Radius of the pack's hole in the ground in m (0.25-10).");
        public static readonly Tunable<float> PackCraterDepth = new Tunable<float>("Dynamite", "PackCraterDepth", 2f, 0.1f, 6f,
            "Depth of the pack's hole in m (0.1-6).");
    }
}
