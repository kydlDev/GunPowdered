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

        [TestCase(0f, 0f, 0f)]
        [TestCase(5f, 0f, 5f)]
        [TestCase(5f, -9f, -9f)]
        [TestCase(-3f, 2f, -3f)]
        public void Tighter_TakesTheLargerExcess(float a, float b, float expected)
        {
            Assert.That(AsksvinTowRules.Tighter(a, b), Is.EqualTo(expected));
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

        // ---- the edges the a11gate Stryker run found unpinned (D-133) ----

        [Test]
        public void OwnerMove_NoAnimalOwnerMovesNothing_EvenWhenIOwnTheCannon()
        {
            Assert.That(AsksvinTowRules.OwnerMove(1L, 1L, 0L), Is.Null);
        }

        [Test]
        public void NoAnimal_NamesTheRangeInWholeMetres()
        {
            Assert.That(AsksvinTowRules.NoAnimal(12.4f), Is.EqualTo("No harnessed Asksvin within 12 m of the tow ring"));
        }

        [TestCase(10f, -1f, 10f, true, TestName = "Nearer: a candidate exactly at the range counts")]
        [TestCase(10.5f, -1f, 10f, false, TestName = "Nearer: a candidate past the range never wins")]
        [TestCase(1f, 0f, 10f, false, TestName = "Nearer: a best of 0 m is a real best, not 'none yet'")]
        [TestCase(5f, 5f, 10f, false, TestName = "Nearer: a tie keeps the old best")]
        [TestCase(4f, 5f, 10f, true, TestName = "Nearer: a closer candidate wins")]
        public void Nearer_Edges(float candidate, float best, float range, bool expected)
        {
            Assert.That(AsksvinTowRules.Nearer(candidate, best, range), Is.EqualTo(expected));
        }

        [TestCase(1f, 1f, 1f, 1f, TestName = "SaddleWeight: front == rear, at the point -> on the saddle")]
        [TestCase(1f, 1f, 2f, 1f, TestName = "SaddleWeight: front behind rear, exactly at the front -> on the saddle")]
        [TestCase(0.5f, 1f, 2f, 0f, TestName = "SaddleWeight: front behind rear, behind the front -> on the root")]
        [TestCase(1.5f, 1f, 2f, 1f, TestName = "SaddleWeight: front behind rear, ahead of the front -> on the saddle")]
        public void SaddleWeight_WhenFrontIsNotAheadOfRear(float z, float front, float rear, float expected)
        {
            Assert.That(AsksvinTowRules.SaddleWeight(z, front, rear), Is.EqualTo(expected).Within(1e-6f));
        }

        [Test]
        public void UprightAccel_IsTheSpringPlusTheDamper()
        {
            Assert.Multiple(() =>
            {
                Assert.That(AsksvinTowRules.UprightAccel(10f, 0f), Is.EqualTo(-10f * 0.0174533f * 60f).Within(1e-4f), "spring only");
                Assert.That(AsksvinTowRules.UprightAccel(0f, 1f), Is.EqualTo(-14f).Within(1e-4f), "damper only");
                Assert.That(AsksvinTowRules.UprightAccel(10f, 1f), Is.EqualTo(-10f * 0.0174533f * 60f - 14f).Within(1e-4f), "both");
            });
        }

        [TestCase(2f, -2f, 2f, TestName = "Tighter: a tie keeps the first")]
        [TestCase(-3f, 2f, -3f, TestName = "Tighter: the larger size wins whatever its sign")]
        [TestCase(1f, -4f, -4f, TestName = "Tighter: the second wins when larger")]
        public void Tighter_Edges(float a, float b, float expected)
        {
            Assert.That(AsksvinTowRules.Tighter(a, b), Is.EqualTo(expected));
        }
    }
}
