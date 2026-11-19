using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The Asksvin harness and towing the Mobile Cannon (D-116).
    public class AsksvinTowRulesTests
    {
        [Test]
        public void Harness_NeedsTameSaddledAndNoHarnessYet()
        {
            Assert.That(AsksvinTowRules.WhyNotHarness(true, true, false), Is.Null);
            Assert.That(AsksvinTowRules.WhyNotHarness(false, true, false), Is.EqualTo(AsksvinTowRules.NotTamed));
            Assert.That(AsksvinTowRules.WhyNotHarness(true, false, false), Is.EqualTo(AsksvinTowRules.NoSaddle));
            Assert.That(AsksvinTowRules.WhyNotHarness(true, true, true), Is.EqualTo(AsksvinTowRules.HasHarness));
        }

        [Test]
        public void Hitch_NeedsFreeCannonAndAnAnimalNear()
        {
            Assert.That(AsksvinTowRules.WhyNotHitch(false, false, true, 6f), Is.Null);
            Assert.That(AsksvinTowRules.WhyNotHitch(true, false, true, 6f), Is.EqualTo(AsksvinTowRules.AlreadyTowed));
            Assert.That(AsksvinTowRules.WhyNotHitch(false, true, true, 6f), Is.EqualTo(AsksvinTowRules.HeldByPlayer));
            Assert.That(AsksvinTowRules.WhyNotHitch(false, false, false, 6f), Is.EqualTo("No harnessed Asksvin within 6 m of the tow ring"));
        }

        [TestCase(true, false, true, true)]
        [TestCase(false, false, true, false)]
        [TestCase(true, true, true, false)]
        [TestCase(true, false, false, false)]
        public void Towing_StopsWhenTheAnimalIsGoneDeadOrUnharnessed(bool exists, bool dead, bool harnessed, bool expected)
        {
            Assert.That(AsksvinTowRules.KeepsTowing(exists, dead, harnessed), Is.EqualTo(expected));
        }

        [TestCase(3f, -1f, 6f, true)]
        [TestCase(7f, -1f, 6f, false)]
        [TestCase(2f, 3f, 6f, true)]
        [TestCase(4f, 3f, 6f, false)]
        public void Nearest_TakesTheClosestWithinRange(float candidate, float best, float range, bool expected)
        {
            Assert.That(AsksvinTowRules.Nearer(candidate, best, range), Is.EqualTo(expected));
        }
    }
}
