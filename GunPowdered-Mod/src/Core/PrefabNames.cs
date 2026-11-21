namespace GunPowdered.Core
{
    /// <summary>
    /// The prefab names of everything this mod adds (D-013). Other mods use them through the public API; the names never change after the first
    /// release because worlds and saves store them. The player sees the English names (Bat Guano, Saltpeter, ...), never these.
    /// </summary>
    public static class GunPowderedPrefabs
    {
        public const string BatGuano = "GP_BatGuano";
        public const string Saltpeter = "GP_Saltpeter";
        public const string Gunpowder = "GP_Gunpowder";
        public const string Water = "GP_Water";
        public const string Bucket = "GP_Bucket";
        public const string BucketFull = "GP_BucketFull";
        public const string LeachingVat = "GP_LeachingVat";
        public const string PowderMill = "GP_PowderMill";
        public const string RainBarrel = "GP_RainBarrel";
        public const string GuanoDeposit = "GP_GuanoDeposit";
        public const string SulfurousMud = "GP_SulfurousMud";
        public const string SpikedGrenade = "GP_SpikedGrenade";
        public const string CannonBall = "GP_CannonBall";                       // shown as "Iron Cannon Ball" (D-145)
        public const string FlametalCannonBall = "GP_FlametalCannonBall";       // D-145: the Mobile Cannon's first ball
        public const string Cannon = "GP_Cannon";
        public const string CannonStock = "GP_CannonStock";
        public const string Musket = "GP_Musket";
        public const string ExplosiveArrow = "GP_ExplosiveArrow";
        public const string Dynamite = "GP_Dynamite";
        public const string MobileCannon = "GP_MobileCannon";
        public const string AsksvinHarness = "GP_AsksvinHarness";
        public const string Paper = "GP_Paper";
        public const string IronBullet = "GP_IronBullet";
        public const string PaperCartridge = "GP_PaperCartridge";
        public const string DeepNorthPack = "GP_DeepNorthPack";
        public const string LeachBarrel = "GP_LeachBarrel";
        public const string SettlingTank = "GP_SettlingTank";
        public const string BlackMetalBoiler = "GP_BlackMetalBoiler";
        public const string DvergrDripPipes = "GP_DvergrDripPipes";
        public const string FlametalHeater = "GP_FlametalHeater";
        public const string MillGrindstone = "GP_MillGrindstone";
        public const string MillSieve = "GP_MillSieve";
        public const string MillOven = "GP_MillOven";
    }
}
