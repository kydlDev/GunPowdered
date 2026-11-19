using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The Mobile Cannon's elevation crank (D-104): notches, limits, crank and screw follow the pitch.
    public class MobileCannonAimRulesTests
    {
        [TestCase(0f, 1, 1f, -5f, 25f, 1f)]
        [TestCase(0f, -1, 1f, -5f, 25f, -1f)]
        [TestCase(24.5f, 3, 1f, -5f, 25f, 25f)]
        [TestCase(-4f, -5, 1f, -5f, 25f, -5f)]
        [TestCase(0f, 2, -1f, -5f, 25f, 2f)]
        [TestCase(0f, 1, 1f, 25f, -5f, 1f)]
        public void Turn_StepsInsideTheLimits(float pitch, int notches, float step, float min, float max, float expected)
        {
            Assert.That(MobileCannonAimRules.Turn(pitch, notches, step, min, max), Is.EqualTo(expected).Within(1e-5f));
        }

        [TestCase(0.1f, 1)]
        [TestCase(-0.1f, -1)]
        [TestCase(0f, 0)]
        public void Notches_OnePerSign(float scroll, int expected)
        {
            Assert.That(MobileCannonAimRules.Notches(scroll), Is.EqualTo(expected));
        }

        [Test]
        public void CrankAndScrew_FollowThePitch()
        {
            Assert.That(MobileCannonAimRules.CrankAngle(2f), Is.EqualTo(60f).Within(1e-4f));
            Assert.That(MobileCannonAimRules.ScrewDrop(0f), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(MobileCannonAimRules.ScrewDrop(30f), Is.EqualTo(0.215f).Within(1e-4f));
            Assert.That(MobileCannonAimRules.ScrewDrop(-10f), Is.LessThan(0f));
        }
    }
}
