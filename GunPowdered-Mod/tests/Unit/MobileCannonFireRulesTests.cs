using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Loading and firing the Mobile Cannon (D-045, D-104).
    public class MobileCannonFireRulesTests
    {
        [Test]
        public void Load_NeedsEmptyCannonPowderAndBall()
        {
            Assert.That(MobileCannonFireRules.WhyNotLoad(false, 3, 3, 1), Is.Null);
            Assert.That(MobileCannonFireRules.WhyNotLoad(true, 3, 3, 1), Is.EqualTo(MobileCannonFireRules.AlreadyLoaded));
            Assert.That(MobileCannonFireRules.WhyNotLoad(false, 2, 3, 1), Is.EqualTo("You need 3 Gunpowder"));
            Assert.That(MobileCannonFireRules.WhyNotLoad(false, 3, 3, 0), Is.EqualTo(MobileCannonFireRules.NeedBall));
        }

        [Test]
        public void Light_NeedsLoadNoFuseAndTorch()
        {
            Assert.That(MobileCannonFireRules.WhyNotLight(true, false, true), Is.Null);
            Assert.That(MobileCannonFireRules.WhyNotLight(false, false, true), Is.EqualTo(MobileCannonFireRules.NotLoaded));
            Assert.That(MobileCannonFireRules.WhyNotLight(true, true, true), Is.EqualTo(MobileCannonFireRules.FuseBurning));
            Assert.That(MobileCannonFireRules.WhyNotLight(true, false, false), Is.EqualTo(MobileCannonFireRules.NeedTorch));
        }

        [TestCase(30f, 15f, 10f, 45f)]
        [TestCase(30f, 45f, 10f, 90f)]
        [TestCase(30f, 0f, 10f, 0f)]
        [TestCase(30f, -5f, 10f, 0f)]
        [TestCase(30f, 15f, 0f, 0f)]
        public void Range_FollowsTheElevation(float speed, float pitch, float gravity, float expected)
        {
            Assert.That(MobileCannonFireRules.Range(speed, pitch, gravity), Is.EqualTo(expected).Within(0.01f));
        }
    }
}
