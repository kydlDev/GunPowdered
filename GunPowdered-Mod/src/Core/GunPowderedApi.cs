using System;
using System.Collections.Generic;
using System.Linq;
using KydlCore.Core;
using GunPowdered.Features.Gunpowder.Domain;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Features.Guano.Domain;
using GunPowdered.Features.Saltpeter.Domain;
using GunPowdered.Features.Water.Domain;
using GunPowdered.Core;

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
            public const string FlametalCannonBall = Core.GunPowderedPrefabs.FlametalCannonBall;
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
            public const string LeachBarrel = Core.GunPowderedPrefabs.LeachBarrel;
            public const string SettlingTank = Core.GunPowderedPrefabs.SettlingTank;
            public const string BlackMetalBoiler = Core.GunPowderedPrefabs.BlackMetalBoiler;
            public const string DvergrDripPipes = Core.GunPowderedPrefabs.DvergrDripPipes;
            public const string FlametalHeater = Core.GunPowderedPrefabs.FlametalHeater;
            public const string MillGrindstone = Core.GunPowderedPrefabs.MillGrindstone;
            public const string MillSieve = Core.GunPowderedPrefabs.MillSieve;
            public const string MillOven = Core.GunPowderedPrefabs.MillOven;
        }

        /// <summary>Every prefab name the mod adds.</summary>
        public static IReadOnlyList<string> AllPrefabs
        {
            get { return Core.ItemCatalog.All.Select(i => i.Id).ToList(); }
        }

        /// <summary>The name the player sees for a prefab of this mod, or null when the prefab is not one of ours.</summary>
        public static string DisplayName(string prefab)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.DisplayName(prefab={RuleLog.Shown(prefab)})");
            var spec = Core.ItemCatalog.All.FirstOrDefault(i => i.Id == prefab);
            { var returnedAt64 = spec == null ? null : spec.Name; if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.DisplayName: returns {RuleLog.Shown(returnedAt64)} (line 64)"); return returnedAt64; }
        }

        /// <summary>Is this prefab one of the three gunpowder ingredients made by this mod or its vanilla partners (Saltpeter, Charcoal, Sulfur)?</summary>
        public static bool IsGunpowderIngredient(string prefab)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.IsGunpowderIngredient(prefab={RuleLog.Shown(prefab)})");
            { var returnedAt71 = prefab == Prefabs.Saltpeter || prefab == Core.ItemCatalog.Charcoal || prefab == Core.ItemCatalog.Sulfur; if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.IsGunpowderIngredient: returns {RuleLog.Shown(returnedAt71)} (line 71)"); return returnedAt71; }
        }

        /// <summary>Gunpowder made from the given ingredients at the default recipe (3 Saltpeter + 1 Charcoal + 1 Sulfur gives 5).</summary>
        public static int GunpowderFrom(int saltpeter, int charcoal, int sulfur)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.GunpowderFrom(saltpeter={RuleLog.Shown(saltpeter)}, charcoal={RuleLog.Shown(charcoal)}, sulfur={RuleLog.Shown(sulfur)})");
            int batches = GunpowderRules.Batches(saltpeter, charcoal, sulfur, GunpowderRules.DefaultSaltpeter, GunpowderRules.DefaultCharcoal, GunpowderRules.DefaultSulfur);
            { var returnedAt79 = GunpowderRules.Output(batches, GunpowderRules.DefaultYield); if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.GunpowderFrom: returns {RuleLog.Shown(returnedAt79)} (line 79)"); return returnedAt79; }
        }

        /// <summary>Saltpeter a Leaching Vat makes from the given guano and water at the default recipe (10 + 10 gives 5 per batch).</summary>
        public static int SaltpeterFrom(int guano, int water)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.SaltpeterFrom(guano={RuleLog.Shown(guano)}, water={RuleLog.Shown(water)})");
            int batches = LeachingVatRules.Batches(guano, water, LeachingVatRules.DefaultGuanoPerBatch, LeachingVatRules.DefaultWaterPerBatch);
            { var returnedAt87 = LeachingVatRules.Saltpeter(batches, LeachingVatRules.DefaultSaltpeterPerBatch); if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.SaltpeterFrom: returns {RuleLog.Shown(returnedAt87)} (line 87)"); return returnedAt87; }
        }

        /// <summary>Water a Rain Barrel holds after the given real-life seconds, starting at <paramref name="current"/> (default capacity and rate).</summary>
        public static float RainBarrelWater(float current, float seconds, bool raining, bool covered)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.RainBarrelWater(current={RuleLog.Shown(current)}, seconds={RuleLog.Shown(seconds)}, raining={RuleLog.Shown(raining)}, covered={RuleLog.Shown(covered)})");
            var r = RainBarrelRules.Fill(current, RainBarrelRules.DefaultCapacity, RainBarrelRules.DefaultRatePerMinute, seconds, raining, covered);
            { var returnedAt95 = r.Status == ResponseStatus.Success ? r.Value : current; if (RuleLog.IsOn()) RuleLog.Verbose($"Prefabs.RainBarrelWater: returns {RuleLog.Shown(returnedAt95)} (line 95)"); return returnedAt95; }
        }

        /// <summary>
        /// The Iron Musket (D-095, D-097): its magazine and its stats. The members here need no running game; reading or setting the load of a musket item
        /// and the shot event are in the game-side part of this class.
        /// </summary>
        public static partial class Musket
        {
            /// <summary>The most cartridges a musket holds; a reload never goes above it.</summary>
            public const int Capacity = MusketRules.Capacity;

            /// <summary>Pierce damage of one shot with the Iron Cartridge (before the target's resistances): the musket's part plus the cartridge's (D-145).</summary>
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

            /// <summary>The prefab name of the ammunition the musket loads (shown to players as "Iron Cartridge").</summary>
            public const string Ammunition = Core.GunPowderedPrefabs.PaperCartridge;

            /// <summary>How many cartridges a reload moves in from <paramref name="carried"/>, starting at <paramref name="loaded"/> (never above <see cref="Capacity"/>).</summary>
            public static int CartridgesToLoad(int loaded, int carried) => MusketRules.CartridgesToLoad(loaded, carried);
        }

        /// <summary>Rolls how many guano deposits a cave of this kind gets (0 to 3) from a source of numbers in [0, 1).</summary>
        public static int GuanoDeposits(CaveKind kind, Func<double> next01)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"Musket.GuanoDeposits(kind={RuleLog.Shown(kind)}, next01={RuleLog.Shown(next01)})");
            { var returnedAt136 = GuanoDepositRules.DepositCount(kind, next01); if (RuleLog.IsOn()) RuleLog.Verbose($"Musket.GuanoDeposits: returns {RuleLog.Shown(returnedAt136)} (line 136)"); return returnedAt136; }
        }
    }
}
