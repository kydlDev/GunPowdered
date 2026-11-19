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

        [TestCase(0f, 1f)]
        [TestCase(-1f, 1f)]
        [TestCase(-1.775f, 0.5f)]
        [TestCase(-2.55f, 0f)]
        [TestCase(-3.5f, 0f)]
        public void SaddleWeight_FrontOnTheSaddleRearOnTheRoot(float z, float expected)
        {
            Assert.That(AsksvinTowRules.SaddleWeight(z, -1f, -2.55f), Is.EqualTo(expected).Within(0.001f));
        }

        [TestCase(0f, 25f, 0f)]
        [TestCase(20f, 25f, 0f)]
        [TestCase(-25f, 25f, 0f)]
        [TestCase(40f, 25f, 15f)]
        [TestCase(-90f, 25f, -65f)]
        public void Excess_OnlyPastTheLimit(float angle, float limit, float expected)
        {
            Assert.That(AsksvinTowRules.Excess(angle, limit), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void UprightAccel_PullsBackAndDamps()
        {
            Assert.That(AsksvinTowRules.UprightAccel(0f, 0f), Is.EqualTo(0f));
            Assert.That(AsksvinTowRules.UprightAccel(10f, 0f), Is.LessThan(0f), "tilted right: pulled left");
            Assert.That(AsksvinTowRules.UprightAccel(-10f, 0f), Is.GreaterThan(0f), "tilted left: pulled right");
            Assert.That(AsksvinTowRules.UprightAccel(0f, 2f), Is.LessThan(0f), "rolling: damped");
        }

        [TestCase(1L, 1L, 2L, "push")]
        [TestCase(1L, 2L, 1L, "pull")]
        [TestCase(1L, 1L, 1L, null)]
        [TestCase(1L, 2L, 2L, null)]
        [TestCase(1L, 2L, 0L, null)]
        public void OwnerMove_TheAnimalsOwnerSimulatesTheCannon(long me, long cannon, long animal, string expected)
        {
            Assert.That(AsksvinTowRules.OwnerMove(me, cannon, animal), Is.EqualTo(expected));
        }
    }
}
