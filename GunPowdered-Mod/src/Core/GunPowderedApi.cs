using System;
using System.Collections.Generic;
using System.Linq;
using KydlCore.Core;
using GunPowdered.Features.Gunpowder.Domain;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Features.Guano.Domain;
using GunPowdered.Features.Saltpeter.Domain;
using GunPowdered.Features.Water.Domain;

namespace GunPowdered
{
    /// <summary>
    /// The public API of GunPowdered (D-006), for other mods. Reference GunPowdered.dll and call these static members; nothing here needs the game
    /// to be running, so a mod can use it from its own Awake. Prefab names never change after the first release (worlds store them).
    /// The weapons part that touches game objects (a musket's load, the shot event) is in src/Integrations/Api/GunPowderedApi.Weapons.cs (D-097).
    /// </summary>
    public static partial class GunPowderedApi
    {
        /// <summary>Bumped when a member is removed or changes meaning; adding members does not change it.</summary>
        public const int ApiVersion = 1;

        /// <summary>Prefab names of everything the mod adds (Bat Guano, Saltpeter, Gunpowder, Water, Bucket, ...).</summary>
        public static class Prefabs
        {
            public const string BatGuano = Core.GunPowderedPrefabs.BatGuano;
            public const string Saltpeter = Core.GunPowderedPrefabs.Saltpeter;
            public const string Gunpowder = Core.GunPowderedPrefabs.Gunpowder;
            public const string Water = Core.GunPowderedPrefabs.Water;
            public const string Bucket = Core.GunPowderedPrefabs.Bucket;
            public const string BucketFull = Core.GunPowderedPrefabs.BucketFull;
            public const string LeachingVat = Core.GunPowderedPrefabs.LeachingVat;
            public const string PowderMill = Core.GunPowderedPrefabs.PowderMill;
            public const string RainBarrel = Core.GunPowderedPrefabs.RainBarrel;
            public const string GuanoDeposit = Core.GunPowderedPrefabs.GuanoDeposit;
            public const string SulfurousMud = Core.GunPowderedPrefabs.SulfurousMud;
            public const string SpikedGrenade = Core.GunPowderedPrefabs.SpikedGrenade;
            public const string CannonBall = Core.GunPowderedPrefabs.CannonBall;
            public const string Cannon = Core.GunPowderedPrefabs.Cannon;
            public const string CannonStock = Core.GunPowderedPrefabs.CannonStock;
            public const string Musket = Core.GunPowderedPrefabs.Musket;
            public const string ExplosiveArrow = Core.GunPowderedPrefabs.ExplosiveArrow;
            public const string Dynamite = Core.GunPowderedPrefabs.Dynamite;
            public const string MobileCannon = Core.GunPowderedPrefabs.MobileCannon;
            public const string AsksvinHarness = Core.GunPowderedPrefabs.AsksvinHarness;
            public const string Paper = Core.GunPowderedPrefabs.Paper;
            public const string IronBullet = Core.GunPowderedPrefabs.IronBullet;
            public const string PaperCartridge = Core.GunPowderedPrefabs.PaperCartridge;
            public const string DeepNorthPack = Core.GunPowderedPrefabs.DeepNorthPack;
        }

        /// <summary>Every prefab name the mod adds.</summary>
        public static IReadOnlyList<string> AllPrefabs
        {
            get { return Core.ItemCatalog.All.Select(i => i.Id).ToList(); }
        }

        /// <summary>The name the player sees for a prefab of this mod, or null when the prefab is not one of ours.</summary>
        public static string DisplayName(string prefab)
        {
            var spec = Core.ItemCatalog.All.FirstOrDefault(i => i.Id == prefab);
            return spec == null ? null : spec.Name;
        }

        /// <summary>Is this prefab one of the three gunpowder ingredients made by this mod or its vanilla partners (Saltpeter, Charcoal, Sulfur)?</summary>
        public static bool IsGunpowderIngredient(string prefab)
        {
            return prefab == Prefabs.Saltpeter || prefab == Core.ItemCatalog.Charcoal || prefab == Core.ItemCatalog.Sulfur;
        }

        /// <summary>Gunpowder made from the given ingredients at the default recipe (3 Saltpeter + 1 Charcoal + 1 Sulfur gives 5).</summary>
        public static int GunpowderFrom(int saltpeter, int charcoal, int sulfur)
        {
            int batches = GunpowderRules.Batches(saltpeter, charcoal, sulfur, GunpowderRules.DefaultSaltpeter, GunpowderRules.DefaultCharcoal, GunpowderRules.DefaultSulfur);
            return GunpowderRules.Output(batches, GunpowderRules.DefaultYield);
        }

        /// <summary>Saltpeter a Leaching Vat makes from the given guano and water at the default recipe (10 + 10 gives 5 per batch).</summary>
        public static int SaltpeterFrom(int guano, int water)
        {
            int batches = LeachingVatRules.Batches(guano, water, LeachingVatRules.DefaultGuanoPerBatch, LeachingVatRules.DefaultWaterPerBatch);
            return LeachingVatRules.Saltpeter(batches, LeachingVatRules.DefaultSaltpeterPerBatch);
        }

        /// <summary>Water a Rain Barrel holds after the given real-life seconds, starting at <paramref name="current"/> (default capacity and rate).</summary>
        public static float RainBarrelWater(float current, float seconds, bool raining, bool covered)
        {
            var r = RainBarrelRules.Fill(current, RainBarrelRules.DefaultCapacity, RainBarrelRules.DefaultRatePerMinute, seconds, raining, covered);
            return r.Status == ResponseStatus.Success ? r.Value : current;
        }

        /// <summary>
        /// The Iron Musket (D-095, D-097): its magazine and its stats. The members here need no running game; reading or setting the load of a musket item
        /// and the shot event are in the game-side part of this class.
        /// </summary>
        public static partial class Musket
        {
            /// <summary>The most cartridges a musket holds; a reload never goes above it.</summary>
            public const int Capacity = MusketRules.Capacity;

            /// <summary>Pierce damage of one bullet (before the target's resistances).</summary>
            public const float BulletPierce = MusketRules.BulletPierce;

            /// <summary>The bullet's speed out of the muzzle (m/s).</summary>
            public const float BulletSpeed = MusketRules.BulletSpeed;

            /// <summary>The largest random turn of a shot away from the aim, each way (degrees).</summary>
            public const float BulletSpreadDegrees = MusketRules.BulletSpreadDegrees;

            /// <summary>How long a bullet flies before it is gone (s).</summary>
            public const float BulletSeconds = MusketRules.BulletSeconds;

            /// <summary>The shortest time between two shots (s).</summary>
            public const float ShotIntervalSeconds = MusketRules.ShotIntervalSeconds;

            /// <summary>One cartridge goes in per this many seconds of reloading.</summary>
            public const float ReloadSecondsPerCartridge = MusketRules.ReloadSecondsPerCartridge;

            /// <summary>The prefab name of the ammunition the musket loads (shown to players as "Cartridge").</summary>
            public const string Ammunition = Core.GunPowderedPrefabs.PaperCartridge;

            /// <summary>How many cartridges a reload moves in from <paramref name="carried"/>, starting at <paramref name="loaded"/> (never above <see cref="Capacity"/>).</summary>
            public static int CartridgesToLoad(int loaded, int carried) => MusketRules.CartridgesToLoad(loaded, carried);
        }

        /// <summary>Rolls how many guano deposits a cave of this kind gets (0 to 3) from a source of numbers in [0, 1).</summary>
        public static int GuanoDeposits(CaveKind kind, Func<double> next01)
        {
            return GuanoDepositRules.DepositCount(kind, next01);
        }
    }
}
