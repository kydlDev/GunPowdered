using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Tests.Unit
{
    /// D-144 (T-014): backing the Asksvin up, ridden (S) and led by the chest rope.
    public class AsksvinReverseRulesTests
    {
        [TestCase(-1f, false, true, TestName = "WantsBack: S held")]
        [TestCase(-0.5f, false, false, TestName = "WantsBack: the dead zone edge is not back")]
        [TestCase(-0.51f, false, true, TestName = "WantsBack: just past the dead zone")]
        [TestCase(-1f, true, false, TestName = "WantsBack: running wins")]
        [TestCase(0f, false, false, TestName = "WantsBack: no key")]
        [TestCase(1f, false, false, TestName = "WantsBack: W held")]
        public void WantsBack(float moveZ, bool run, bool expected)
        {
            Assert.That(AsksvinReverseRules.WantsBack(moveZ, run), Is.EqualTo(expected));
        }

        [TestCase(1f, 1)]
        [TestCase(0.51f, 1)]
        [TestCase(0.5f, 0)]
        [TestCase(0f, 0)]
        [TestCase(-0.5f, 0)]
        [TestCase(-0.51f, -1)]
        [TestCase(-1f, -1)]
        public void TurnInput_HasADeadZone(float moveX, int expected)
        {
            Assert.That(AsksvinReverseRules.TurnInput(moveX), Is.EqualTo(expected));
        }

        [TestCase(1f, 0.5f, 15f)]
        [TestCase(-1f, 0.5f, -15f)]
        [TestCase(3f, 1f, 30f)]
        [TestCase(-3f, 1f, -30f)]
        [TestCase(0.5f, 1f, 15f)]
        [TestCase(0f, 1f, 0f)]
        public void TurnStep_IsTheClampedInputTimesTheRate(float turn, float dt, float expected)
        {
            Assert.That(AsksvinReverseRules.TurnStep(turn, dt), Is.EqualTo(expected).Within(1e-4f));
        }

        [TestCase(1.4f, 1.4f, 1.4f, "stay", TestName = "Lead: where the rope was taken")]
        [TestCase(1.1f, 1.4f, 1.1f, "stay", TestName = "Lead: on the back edge stays")]
        [TestCase(1.09f, 1.4f, 1.1f, "back", TestName = "Lead: the player moved towards its back - it backs up")]
        [TestCase(-0.5f, 1.4f, 1f, "back", TestName = "Lead: the player well behind the grab point - back")]
        [TestCase(1.69f, 1.4f, 1.7f, "stay", TestName = "Lead: just inside the forward edge stays")]
        [TestCase(1.71f, 1.4f, 1.8f, "forward", TestName = "Lead: the player moved forward - it walks forward")]
        [TestCase(5f, 0f, 6f, "forward", TestName = "Lead: at the let-go distance still led")]
        [TestCase(5f, 0f, 6.01f, "let go", TestName = "Lead: too far lets go")]
        [TestCase(-1f, -1f, 1f, "stay", TestName = "Lead: taken from beside or behind the rope works the same")]
        public void Lead(float ahead, float grabAhead, float distance, string expected)
        {
            Assert.That(AsksvinReverseRules.Lead(ahead, grabAhead, distance), Is.EqualTo(expected));
        }

        [TestCase(0f, 2f, 0f)]
        [TestCase(10f, 2f, 10f * 0.0174533f * 3f)]
        [TestCase(-10f, 2f, -10f * 0.0174533f * 3f)]
        [TestCase(90f, 2f, 2f)]
        [TestCase(-90f, 2f, -2f)]
        [TestCase(90f, -2f, 2f)]
        public void StraightSpin_TurnsTowardsStraightUpToTheLimit(float angle, float max, float expected)
        {
            Assert.That(AsksvinReverseRules.StraightSpin(angle, max), Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void Constants()
        {
            Assert.Multiple(() =>
            {
                Assert.That(AsksvinReverseRules.BackSpeedFactor, Is.EqualTo(0.5f), "half the walk speed (user, D-144)");
                Assert.That(AsksvinReverseRules.BackTurnRate, Is.EqualTo(30f));
                Assert.That(AsksvinReverseRules.LeadBand, Is.EqualTo(0.3f));
                Assert.That(AsksvinReverseRules.LeadLetGo, Is.EqualTo(6f));
            });
        }
    }
}
