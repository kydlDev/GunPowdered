using GunPowdered.Core;
using NUnit.Framework;

namespace GunPowdered.UnitTests
{
    public class BurnRulesTests
    {
        [TestCase(5f, 25f)]
        [TestCase(10f, 50f)]
        [TestCase(0f, 0f)]
        [TestCase(-3f, 0f)]
        public void Total_IsPerSecondTimesTheFiveSecondBurn(float perSecond, float expected)
        {
            Assert.That(BurnRules.Total(perSecond), Is.EqualTo(expected));
        }

        [Test]
        public void Seconds_MatchesTheGamesBurning()
        {
            Assert.That(BurnRules.Seconds, Is.EqualTo(5f));
        }
    }
}
