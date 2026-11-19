using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Moving the Mobile Cannon by hand (D-105): capped speed, stamina while moving, letting go at no stamina.
    public class MobileCannonPullRulesTests
    {
        [TestCase(7f, 3.5f, 3.5f)]
        [TestCase(2f, 3.5f, 2f)]
        [TestCase(-1f, 3.5f, 0f)]
        [TestCase(5f, -2f, 0f)]
        public void CapSpeed_NeverAboveTheMax(float speed, float max, float expected)
        {
            Assert.That(MobileCannonPullRules.CapSpeed(speed, max), Is.EqualTo(expected).Within(1e-5f));
        }

        [TestCase(3f, 0.5f, 6f, 3f)]
        [TestCase(0.2f, 0.5f, 6f, 0f)]
        [TestCase(3f, 0f, 6f, 0f)]
        [TestCase(3f, 0.5f, 0f, 0f)]
        public void StaminaCost_OnlyWhileMoving(float speed, float dt, float perSecond, float expected)
        {
            Assert.That(MobileCannonPullRules.StaminaCost(speed, dt, perSecond), Is.EqualTo(expected).Within(1e-5f));
        }

        [TestCase(0f, true)]
        [TestCase(0.005f, true)]
        [TestCase(5f, false)]
        public void LetsGo_AtNoStamina(float stamina, bool expected)
        {
            Assert.That(MobileCannonPullRules.LetsGo(stamina), Is.EqualTo(expected));
        }
    }
}
