using System;
using GunPowdered.Core;
using GunPowdered.Features.Example.Domain;
using GunPowdered.Features.Guano.Domain;
using GunPowdered.Features.Gunpowder.Domain;
using GunPowdered.Features.Saltpeter.Domain;
using GunPowdered.Features.Water.Domain;
using KydlCore.Core;

namespace GunPowdered.Tests.Unit
{
    /// Edges the mutation run found untested (T-008, Stryker 2026-11-18): each case pins one boundary or one input of a rule, so a changed
    /// comparison or a dropped condition fails a test.
    public class RuleBoundaryTests
    {
        // ---- Powder Mill ------------------------------------------------------------------------------------------------------------
        [Test]
        public void Gunpowder_RecipeAmountsOfOne_UseEveryIngredient()
        {
            Assert.That(GunpowderRules.Batches(5, 6, 7, 1, 1, 1), Is.EqualTo(5));
        }

        [Test]
        public void Gunpowder_CharcoalIsTheScarcest_LimitsTheBatches()
        {
            Assert.That(GunpowderRules.Batches(30, 4, 30, 3, 2, 1), Is.EqualTo(2));
        }

        [Test]
        public void Gunpowder_SulfurIsTheScarcest_LimitsTheBatches()
        {
            Assert.That(GunpowderRules.Batches(30, 30, 4, 3, 1, 2), Is.EqualTo(2));
        }

        [TestCase(-3, 5, 5)]
        [TestCase(9, -1, 5)]
        [TestCase(9, 5, -2)]
        public void Gunpowder_ANegativeAmount_MakesNoBatch(int saltpeter, int charcoal, int sulfur)
        {
            Assert.That(GunpowderRules.Batches(saltpeter, charcoal, sulfur, 3, 1, 1), Is.Zero);
        }

        [TestCase(2, 1, 2)]
        [TestCase(-2, 5, 0)]
        [TestCase(2, -1, 0)]
        public void Gunpowder_Output(int batches, int yieldPerBatch, int expected)
        {
            Assert.That(GunpowderRules.Output(batches, yieldPerBatch), Is.EqualTo(expected));
        }

        // ---- Leaching Station -------------------------------------------------------------------------------------------------------
        [Test]
        public void Vat_AFuelNeedOfOne_StartsWithOneLog()
        {
            Assert.That(LeachingVatRules.CanStart(true, 1, 1), Is.True);
        }

        [Test]
        public void Vat_ANeedOfOne_IsFullWithOneEach()
        {
            Assert.That(LeachingVatRules.BarrelFull(1, 1, 1, 1), Is.True);
        }

        [Test]
        public void Vat_BatchSizesOfOne_UseEveryUnit()
        {
            Assert.That(LeachingVatRules.Batches(4, 6, 1, 1), Is.EqualTo(4));
        }

        [TestCase(-2, 5)]
        [TestCase(5, -2)]
        public void Vat_ANegativeAmount_MakesNoBatch(int guano, int water)
        {
            Assert.That(LeachingVatRules.Batches(guano, water, 1, 1), Is.Zero);
        }

        [Test]
        public void Vat_SaltpeterPerBatchOfOne_GivesOnePerBatch()
        {
            Assert.That(LeachingVatRules.Saltpeter(2, 1), Is.EqualTo(2));
        }

        [Test]
        public void Vat_SecondsLeft_IsTheRestOfTheDay_ThenZero()
        {
            Assert.That(LeachingVatRules.SecondsLeft(100d, 400d, 1800d, 1d), Is.EqualTo(1500d));
            Assert.That(LeachingVatRules.SecondsLeft(100d, 5000d, 1800d, 1d), Is.Zero);
        }

        // ---- Rain Barrel ------------------------------------------------------------------------------------------------------------
        [Test]
        public void Barrel_GrowingPastTheCapacity_StopsAtIt()
        {
            var r = RainBarrelRules.Fill(199f, 200f, 60f, 60f, true, false);
            Assert.That(r.Value, Is.EqualTo(200f));
            Assert.That(r.Reason, Is.EqualTo(RainBarrelRules.Filled));
        }

        [Test]
        public void Barrel_TakingOne_GivesOne()
        {
            var r = RainBarrelRules.Take(5f, 1);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(r.Value, Is.EqualTo(1));
        }

        [TestCase(5f, 3, 3)]
        [TestCase(5.9f, 9, 5)]
        public void Barrel_Take_GivesTheSmallerOfRequestAndContent(float current, int requested, int expected)
        {
            Assert.That(RainBarrelRules.Take(current, requested).Value, Is.EqualTo(expected));
        }

        // ---- Deposit refill ---------------------------------------------------------------------------------------------------------
        [Test]
        public void Refill_ZeroDays_IsAtOnce()
        {
            Assert.That(DepositRefill.RefillAt(100d, 0d, 1800d), Is.EqualTo(100d));
        }

        [Test]
        public void Refill_AnInfiniteEmptyTime_IsBadInput()
        {
            Assert.That(DepositRefill.RefillAt(double.PositiveInfinity, 3d, 1800d), Is.NaN);
        }

        [Test]
        public void Refill_AnUnknownNow_IsNeverFull()
        {
            Assert.That(DepositRefill.IsFull(0d, double.NaN, 3d, 1800d), Is.False);
        }

        // ---- Example rule (template slice) ------------------------------------------------------------------------------------------
        [Test]
        public void Example_TheLowestValue_IsInRange()
        {
            var r = ExampleRules.Decide(ExampleRules.Min);
            Assert.That(r.Value, Is.EqualTo(ExampleRules.Min));
            Assert.That(r.Reason, Is.EqualTo(ExampleRules.InRange));
        }

        // ---- Cave kinds -------------------------------------------------------------------------------------------------------------
        [TestCase("SunkenCrypt4", "DG_ForestCrypt")]
        [TestCase("Crypt2", "DG_SunkenCrypt")]
        public void Cave_AnySunkenName_GetsNoGuano(string location, string generator)
        {
            Assert.That(CaveEntryRules.KindOf(location, generator), Is.Null);
        }

        // D-166 (user 2026-11-23): Hildir's crypt and cave get guano like their kind, whichever name carries the Hildir part
        [TestCase("Hildir_crypt", "DG_ForestCrypt", CaveKind.TrollCaveOrBurialChamber)]
        [TestCase("MountainCave02", "DG_Hildir_Cave", CaveKind.MountainCave)]
        [TestCase("Hildir_cave", "", CaveKind.MountainCave)]
        public void Cave_HildirCryptOrCave_GetsGuanoLikeItsKind(string location, string generator, CaveKind expected)
        {
            Assert.That(CaveEntryRules.KindOf(location, generator), Is.EqualTo(expected));
        }

        [TestCase("Hildir_camp", "")]
        [TestCase("Hildir_plainsfortress", "")]
        public void Cave_HildirNonCave_GetsNoGuano(string location, string generator)
        {
            Assert.That(CaveEntryRules.KindOf(location, generator), Is.Null);
        }

        [Test]
        public void Cave_AMountainCaveLocationWithASuffix_IsAMountainCave()
        {
            Assert.That(CaveEntryRules.KindOf("MountainCave02", ""), Is.EqualTo(CaveKind.MountainCave));
        }

        [TestCase("DG_ForestCrypt(Clone)")]
        [TestCase("DG_HalfBurried_ForestCrypt(Clone)")]
        public void Cave_ACryptGeneratorWithASuffix_IsABurialChamber(string generator)
        {
            Assert.That(CaveEntryRules.KindOf("", generator), Is.EqualTo(CaveKind.TrollCaveOrBurialChamber));
        }

        // ---- Item catalog and API ---------------------------------------------------------------------------------------------------
        [Test]
        public void Catalog_EveryEntry_HasADescription()
        {
            foreach (ItemSpec spec in ItemCatalog.All)
                Assert.That(spec.Description, Has.Length.GreaterThan(10), spec.Id + " has no real description");
        }

        [Test]
        public void Api_RainBarrelWater_KeepsTheContentOnBadInput()
        {
            Assert.That(GunPowderedApi.RainBarrelWater(5f, -1f, true, false), Is.EqualTo(5f));
        }

        // ---- Seeded generator -------------------------------------------------------------------------------------------------------
        [Test]
        public void Seeded_ANegativeMax_SaysWhatIsWrong()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new SeededRandom(1).Next(-5));
            Assert.That(ex.ParamName, Is.EqualTo("maxExclusive"));
            Assert.That(ex.Message, Does.Contain("must not be negative"));
        }
    }
}
