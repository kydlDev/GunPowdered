using KydlCore.Common;

namespace GunPowdered.Configuration
{
    /// <summary>
    /// The balance numbers the user approved (D-014, D-016, D-019) as dev-only Tunables: not in the player's config file, KydlTools (F7) changes them in game.
    /// Recipe costs and stack sizes are read when the items are registered, so a change of those needs a restart; the rest applies at once.
    /// </summary>
    internal static partial class ModSettings
    {
        public static readonly Tunable<int> WaterPerBucket = new Tunable<int>("Water", "WaterPerBucket", 10, 1, 50,
            "Water units that one filled bucket puts into the inventory (1-50). Assumption of Claude, not yet confirmed by the user.");
        public static readonly Tunable<float> BucketFillSeconds = new Tunable<float>("Water", "BucketFillSeconds", 1.5f, 0.2f, 10f,
            "Seconds the bucket stays full before its water lands in the inventory (0.2-10).");
        public static readonly Tunable<float> RainBarrelCapacity = new Tunable<float>("Water", "RainBarrelCapacity", 200f, 10f, 1000f,
            "Water a Rain Barrel holds (10-1000), D-010.");
        public static readonly Tunable<float> RainBarrelRatePerMinute = new Tunable<float>("Water", "RainBarrelRatePerMinute", 5f, 0.5f, 100f,
            "Water a Rain Barrel collects per real-life minute of rain (0.5-100), D-012.");

        public static readonly Tunable<int> VatGuano = new Tunable<int>("Saltpeter", "VatGuano", 10, 1, 50, "Bat Guano per Leaching Vat batch (1-50), D-014.");
        public static readonly Tunable<int> VatWater = new Tunable<int>("Saltpeter", "VatWater", 10, 1, 50, "Water per Leaching Vat batch (1-50), D-014.");
        public static readonly Tunable<int> VatSaltpeter = new Tunable<int>("Saltpeter", "VatSaltpeter", 5, 1, 50, "Saltpeter one batch gives (1-50), D-014.");
        public static readonly Tunable<int> VatFuel = new Tunable<int>("Saltpeter", "VatFuel", 4, 1, 50, "Wood the Leaching Station hearth burns per batch (1-50), D-024.");
        public static readonly Tunable<float> VatBaseMinutes = new Tunable<float>("Saltpeter", "VatBaseMinutes", 30f, 1f, 120f, "Real minutes of one Leaching Station run without extensions (1-120; 30 = one in-game day), D-129.");
        public static readonly Tunable<float> VatMinutesPerStep = new Tunable<float>("Saltpeter", "VatMinutesPerStep", 5f, 0f, 30f, "Minutes each extension step takes off a run (0-30; 5 makes the last step 5 min), D-129.");

        public static readonly Tunable<int> MillSaltpeter = new Tunable<int>("Gunpowder", "MillSaltpeter", 3, 1, 50, "Saltpeter per Powder Mill craft (1-50), D-014. Read at start.");
        public static readonly Tunable<int> MillCharcoal = new Tunable<int>("Gunpowder", "MillCharcoal", 1, 1, 50, "Charcoal per Powder Mill craft (1-50), D-014. Read at start.");
        public static readonly Tunable<int> MillSulfur = new Tunable<int>("Gunpowder", "MillSulfur", 1, 1, 50, "Sulfur per Powder Mill craft (1-50), D-014. Read at start.");
        public static readonly Tunable<int> MillYield = new Tunable<int>("Gunpowder", "MillYield", 5, 1, 50, "Gunpowder one craft gives (1-50), D-014. Read at start.");

        public static readonly Tunable<float> DepositRefillDays = new Tunable<float>("Deposits", "DepositRefillDays", 3f, 0.1f, 30f,
            "In-game days until an emptied Bat Guano or Sulfurous Mud deposit is full again (0.1-30), D-019.");
        public static readonly Tunable<int> MudPerGeyser = new Tunable<int>("Sulfur", "MudPerGeyser", 2, 0, 6,
            "Sulfurous Mud deposits placed around each swamp geyser (FireHole) when it is created (0-6). Assumption of Claude, not yet confirmed by the user.");
        public static readonly Tunable<int> MudPerDraugrPile = new Tunable<int>("Sulfur", "MudPerDraugrPile", 1, 0, 4,
            "Sulfurous Mud deposits placed in the water next to each Draugr pile outside sunken crypts (0-4). Assumption of Claude, not yet confirmed by the user.");
        public static readonly Tunable<float> SwampGuanoPerZone = new Tunable<float>("Guano", "SwampGuanoPerZone", 0.05f, 0f, 2f,
            "Bat Guano deposits per world zone inside a Bat Swamp area (0-2, a fraction is a chance). Approximation of the user's per-area chance (D-016), to be tuned in game.");

        public static readonly Tunable<float> KillSulfurChance = new Tunable<float>("Sulfur", "KillSulfurChance", 0.1f, 0f, 1f,
            "Chance that a killed Blob or Oozer drops sulfur (0-1), D-016.");
    }
}
