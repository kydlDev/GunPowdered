using System;
using GunPowdered.Core;
using GunPowdered.Features.Gunpowder.Domain;
using GunPowdered.Features.Saltpeter.Domain;
using GunPowdered.Features.Sulfur.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Leaching Vat, Powder Mill, sulfur amounts and the dice helper: the user's numbers (D-014) and every edge.
    public class ProcessingRulesTests
    {
        [TestCase(10, 10, 1)]
        [TestCase(25, 40, 2)]
        [TestCase(40, 15, 1)]
        [TestCase(9, 100, 0)]
        [TestCase(100, 9, 0)]
        [TestCase(0, 0, 0)]
        public void Vat_Batches_AreLimitedByTheScarcerInput(int guano, int water, int expected)
        {
            Assert.That(LeachingVatRules.Batches(guano, water, 10, 10), Is.EqualTo(expected));
        }

        [TestCase(-1, 10, 10, 10)]
        [TestCase(10, -1, 10, 10)]
        [TestCase(10, 10, 0, 10)]
        [TestCase(10, 10, 10, 0)]
        public void Vat_Batches_BadInputMakesNothing(int guano, int water, int guanoPer, int waterPer)
        {
            Assert.That(LeachingVatRules.Batches(guano, water, guanoPer, waterPer), Is.EqualTo(0));
        }

        [TestCase(1, 5, 5)]
        [TestCase(3, 5, 15)]
        [TestCase(0, 5, 0)]
        [TestCase(2, 0, 0)]
        [TestCase(-1, 5, 0)]
        public void Vat_Saltpeter_IsBatchesTimesYield(int batches, int perBatch, int expected)
        {
            Assert.That(LeachingVatRules.Saltpeter(batches, perBatch), Is.EqualTo(expected));
        }

        [TestCase(true, 4, 4, true)]
        [TestCase(true, 9, 4, true)]
        [TestCase(true, 3, 4, false)]
        [TestCase(false, 4, 4, false)]
        [TestCase(true, 4, 0, false)]
        [TestCase(true, 4, -1, false)]
        public void Station_CanStart_NeedsALoadedBarrelAndEnoughWood(bool loaded, int fuel, int need, bool expected)
        {
            Assert.That(LeachingVatRules.CanStart(loaded, fuel, need), Is.EqualTo(expected));
        }

        [TestCase(0d, 100d, false)]
        [TestCase(100d, 101d, true)]
        [TestCase(100d, 103d, true)]
        [TestCase(100d, 103.5d, false)]
        [TestCase(100d, 50d, false)]
        public void Mill_IsTurning_OnlyShortlyAfterTheLastUse(double lastUse, double now, bool expected)
        {
            Assert.That(GunPowdered.Features.Gunpowder.Domain.GunpowderRules.IsTurning(lastUse, now, 3d), Is.EqualTo(expected));
        }

        [Test]
        public void Vat_OneGameDay_IsDoneAfterTheDayLength()
        {
            const double Day = LeachingVatRules.DefaultDayLengthSeconds;
            Assert.That(LeachingVatRules.ReadyAt(1000d, Day, 1d), Is.EqualTo(2800d));
            Assert.That(LeachingVatRules.IsReady(1000d, 2799d, Day, 1d), Is.False);
            Assert.That(LeachingVatRules.IsReady(1000d, 2800d, Day, 1d), Is.True);
            Assert.That(LeachingVatRules.SecondsLeft(1000d, 1800d, Day, 1d), Is.EqualTo(1000d));
            Assert.That(LeachingVatRules.SecondsLeft(1000d, 5000d, Day, 1d), Is.EqualTo(0d));
        }

        [Test]
        public void Vat_ClockThatWentBackwards_IsNotReady()
        {
            Assert.That(LeachingVatRules.IsReady(5000d, 100d, 1800d, 1d), Is.False);
            Assert.That(LeachingVatRules.SecondsLeft(5000d, 100d, 1800d, 1d), Is.EqualTo(6700d));
        }

        [TestCase(3, 1, 1, 1)]
        [TestCase(6, 2, 2, 2)]
        [TestCase(30, 1, 10, 1)]
        [TestCase(30, 10, 1, 1)]
        [TestCase(2, 5, 5, 0)]
        public void Mill_Batches_AreLimitedByTheScarcestIngredient(int saltpeter, int charcoal, int sulfur, int expected)
        {
            Assert.That(GunpowderRules.Batches(saltpeter, charcoal, sulfur, 3, 1, 1), Is.EqualTo(expected));
        }

        [TestCase(-1, 1, 1, 3, 1, 1)]
        [TestCase(3, -1, 1, 3, 1, 1)]
        [TestCase(3, 1, -1, 3, 1, 1)]
        [TestCase(3, 1, 1, 0, 1, 1)]
        [TestCase(3, 1, 1, 3, 0, 1)]
        [TestCase(3, 1, 1, 3, 1, 0)]
        public void Mill_Batches_BadInputMakesNothing(int s, int c, int su, int sPer, int cPer, int suPer)
        {
            Assert.That(GunpowderRules.Batches(s, c, su, sPer, cPer, suPer), Is.EqualTo(0));
        }

        [TestCase(1, 5, 5)]
        [TestCase(4, 5, 20)]
        [TestCase(0, 5, 0)]
        [TestCase(2, 0, 0)]
        public void Mill_Output_IsBatchesTimesYield(int batches, int yield, int expected)
        {
            Assert.That(GunpowderRules.Output(batches, yield), Is.EqualTo(expected));
        }

        [TestCase(SulfurSource.SulfurousMud, 0.0, 1)]
        [TestCase(SulfurSource.SulfurousMud, 0.5, 2)]
        [TestCase(SulfurSource.SulfurousMud, 0.99, 3)]
        [TestCase(SulfurSource.Blob, 0.0, 1)]
        [TestCase(SulfurSource.Blob, 0.99, 2)]
        [TestCase(SulfurSource.Oozer, 0.0, 1)]
        [TestCase(SulfurSource.Oozer, 0.99, 2)]
        public void Sulfur_Amount_FollowsTheSourcesRange(SulfurSource source, double draw, int expected)
        {
            Assert.That(SulfurRules.Amount(source, () => draw), Is.EqualTo(expected));
        }

        [TestCase(0.0, 1)]
        [TestCase(0.499, 1)]
        [TestCase(0.5, 0)]
        [TestCase(1.0, 0)]
        public void Roll_Chance_HitsBelowTheProbability(double draw, int hits)
        {
            Assert.That(Roll.Chance(0.5d, draw) ? 1 : 0, Is.EqualTo(hits));
        }

        [TestCase(-5.0, 3)]
        [TestCase(0.0, 3)]
        [TestCase(0.999, 5)]
        [TestCase(1.0, 5)]
        [TestCase(7.0, 5)]
        public void Roll_IntInclusive_EdgeDrawsStayInsideTheRange(double draw, int expected)
        {
            Assert.That(Roll.IntInclusive(3, 5, draw), Is.EqualTo(expected));
        }

        [Test]
        public void Roll_IntInclusive_EmptyOrReversedRangeGivesMin()
        {
            Assert.That(Roll.IntInclusive(4, 4, 0.7), Is.EqualTo(4));
            Assert.That(Roll.IntInclusive(4, 2, 0.7), Is.EqualTo(4));
        }

        [Test]
        public void Roll_IntInclusive_SpreadsEvenly()
        {
            var random = new Random(99);
            var seen = new int[4];
            for (int i = 0; i < 40000; i++) seen[Roll.IntInclusive(1, 3, random.NextDouble())]++;
            Assert.That(seen[0], Is.EqualTo(0));
            for (int v = 1; v <= 3; v++) Assert.That(seen[v], Is.EqualTo(40000 / 3).Within(600));
        }
    }
}
