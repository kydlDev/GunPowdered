using GunPowdered.Features.Dynamite.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Dynamite (D-100): placed, lit with a Torch, blows after 4 s; creatures take 20 % of the building damage.
    public class DynamiteRulesTests
    {
        [TestCase(150f, 30f)]
        [TestCase(100f, 20f)]
        [TestCase(0f, 0f)]
        [TestCase(-5f, 0f)]
        public void CreatureDamage_IsAFifthOfTheBuildingDamage(float building, float expected)
        {
            Assert.That(DynamiteRules.CreatureDamage(building), Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void CreatureShare_IsTwentyPercent()
        {
            Assert.That(DynamiteRules.CreatureShare, Is.EqualTo(0.2f));
        }

        [TestCase(-1d, 100d, 4f)]
        [TestCase(100d, 100d, 4f)]
        [TestCase(100d, 101.5d, 2.5f)]
        [TestCase(100d, 104d, 0f)]
        [TestCase(100d, 130d, 0f)]
        public void FuseLeft_CountsDownFromFourSeconds(double litAt, double now, float expected)
        {
            Assert.That(DynamiteRules.FuseLeft(litAt, now), Is.EqualTo(expected).Within(1e-4f));
        }

        [TestCase(-1d, 1000d, false)]
        [TestCase(100d, 103.99d, false)]
        [TestCase(100d, 104d, true)]
        [TestCase(100d, 200d, true)]
        public void Blows_OnlyALitStickAfterItsFuse(double litAt, double now, bool expected)
        {
            Assert.That(DynamiteRules.Blows(litAt, now), Is.EqualTo(expected));
        }

        [Test]
        public void WhyNotLight_NeedsATorchAndAnUnlitStick()
        {
            Assert.That(DynamiteRules.WhyNotLight(false, true), Is.Null);
            Assert.That(DynamiteRules.WhyNotLight(false, false), Is.EqualTo(DynamiteRules.NeedTorch));
            Assert.That(DynamiteRules.WhyNotLight(true, true), Is.EqualTo(DynamiteRules.Burning));
        }
    }
}
