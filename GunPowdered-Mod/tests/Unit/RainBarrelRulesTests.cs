using KydlCore.Core;
using GunPowdered.Features.Water.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Rain Barrel: fills only in rain and uncovered, 5 per real minute, capped at 200 (D-010, D-012); whole units come out.
    public class RainBarrelRulesTests
    {
        const float Cap = RainBarrelRules.DefaultCapacity, Rate = RainBarrelRules.DefaultRatePerMinute;

        [Test]
        public void Fill_InRain_GrowsByTheRatePerRealMinute()
        {
            var r = RainBarrelRules.Fill(0f, Cap, Rate, 60f, true, false);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(r.Value, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(r.Reason, Is.EqualTo(RainBarrelRules.Filled));
        }

        [Test]
        public void Fill_HalfAMinute_GrowsByHalfTheRate()
        {
            Assert.That(RainBarrelRules.Fill(10f, Cap, Rate, 30f, true, false).Value, Is.EqualTo(12.5f).Within(0.0001f));
        }

        [Test]
        public void Fill_NeverPassesTheCapacity()
        {
            var r = RainBarrelRules.Fill(198f, Cap, Rate, 600f, true, false);
            Assert.That(r.Value, Is.EqualTo(Cap));
            Assert.That(r.Reason, Is.EqualTo(RainBarrelRules.Filled));
        }

        [Test]
        public void Fill_NotRaining_KeepsTheContent()
        {
            var r = RainBarrelRules.Fill(40f, Cap, Rate, 600f, false, false);
            Assert.That(r.Value, Is.EqualTo(40f));
            Assert.That(r.Reason, Is.EqualTo(RainBarrelRules.NotRaining));
        }

        [Test]
        public void Fill_UnderARoof_KeepsTheContentEvenInRain()
        {
            var r = RainBarrelRules.Fill(40f, Cap, Rate, 600f, true, true);
            Assert.That(r.Value, Is.EqualTo(40f));
            Assert.That(r.Reason, Is.EqualTo(RainBarrelRules.Covered));
        }

        [Test]
        public void Fill_FullBarrel_StaysFullWhateverTheWeather()
        {
            Assert.That(RainBarrelRules.Fill(Cap, Cap, Rate, 60f, true, false).Reason, Is.EqualTo(RainBarrelRules.Full));
            Assert.That(RainBarrelRules.Fill(Cap, Cap, Rate, 60f, false, true).Value, Is.EqualTo(Cap));
        }

        [Test]
        public void Fill_ContentAboveALoweredCapacity_IsCutToTheCapacity()
        {
            var r = RainBarrelRules.Fill(150f, 100f, Rate, 60f, false, false);
            Assert.That(r.Value, Is.EqualTo(100f));
            Assert.That(r.Reason, Is.EqualTo(RainBarrelRules.Full));
        }

        [TestCase(float.NaN, Cap, Rate, 60f)]
        [TestCase(-1f, Cap, Rate, 60f)]
        [TestCase(0f, float.PositiveInfinity, Rate, 60f)]
        [TestCase(0f, Cap, -5f, 60f)]
        [TestCase(0f, Cap, Rate, -1f)]
        [TestCase(0f, Cap, Rate, float.NaN)]
        public void Fill_RejectsBadInput(float current, float capacity, float rate, float seconds)
        {
            var r = RainBarrelRules.Fill(current, capacity, rate, seconds, true, false);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Is.EqualTo(RainBarrelRules.BadInput));
        }

        [TestCase(37.9f, 10, 10)]
        [TestCase(37.9f, 50, 37)]
        [TestCase(1f, 5, 1)]
        public void Take_GivesAtMostTheWholeUnitsInside(float current, int requested, int expected)
        {
            var r = RainBarrelRules.Take(current, requested);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Success));
            Assert.That(r.Value, Is.EqualTo(expected));
        }

        [TestCase(0f, 5)]
        [TestCase(0.9f, 5)]
        public void Take_FromAnEmptyBarrel_IsRejected(float current, int requested)
        {
            var r = RainBarrelRules.Take(current, requested);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Is.EqualTo(RainBarrelRules.Empty));
        }

        [TestCase(10f, 0)]
        [TestCase(10f, -3)]
        [TestCase(float.NaN, 5)]
        [TestCase(-1f, 5)]
        public void Take_RejectsBadInput(float current, int requested)
        {
            var r = RainBarrelRules.Take(current, requested);
            Assert.That(r.Status, Is.EqualTo(ResponseStatus.Rejected));
            Assert.That(r.Reason, Is.EqualTo(RainBarrelRules.BadInput));
        }
    }
}
