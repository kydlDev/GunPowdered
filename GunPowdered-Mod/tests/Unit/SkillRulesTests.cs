using GunPowdered.Features.Skilling.Domain;
using NUnit.Framework;

namespace GunPowdered.UnitTests
{
    /// <summary>D-157 (user 2026-11-23): the skill values at level 0, 50 and 100 are the user's numbers, on a straight line, clamped outside 0 .. 100.</summary>
    [TestFixture]
    public class SkillRulesTests
    {
        const float Eps = 1e-4f;

        [TestCase(0f, 1.8f)]
        [TestCase(50f, 1.5f)]
        [TestCase(100f, 1.2f)]
        [TestCase(-10f, 1.8f)]
        [TestCase(150f, 1.2f)]
        public void MusketReload_1p8At0_1p2At100(float level, float seconds)
        {
            Assert.That(SkillRules.MusketReloadSeconds(level), Is.EqualTo(seconds).Within(Eps));
        }

        [TestCase(0f, 5f)]
        [TestCase(50f, 4f)]
        [TestCase(100f, 3f)]
        public void DynamiteFuse_OneSecondLongerAt0_OneSecondShorterAt100(float level, float seconds)
        {
            Assert.That(SkillRules.DynamiteFuseSeconds(level), Is.EqualTo(seconds).Within(Eps));
        }

        [TestCase(0f, 3f)]
        [TestCase(50f, 2f)]
        [TestCase(100f, 1f)]
        public void GrenadeFuseAfterLanding_3At0_1At100(float level, float seconds)
        {
            Assert.That(SkillRules.GrenadeFuseSeconds(level), Is.EqualTo(seconds).Within(Eps));
        }

        [TestCase(0f, 4f)]
        [TestCase(100f, 2.5f)]
        [TestCase(50f, 3.25f)]
        public void CannonLoad_4To2p5_FromTheConfiguredTime(float level, float seconds)
        {
            Assert.That(SkillRules.CannonLoadSeconds(4f, level), Is.EqualTo(seconds).Within(Eps));
        }

        [TestCase(0f, 1f)]
        [TestCase(50f, 1.25f)]
        [TestCase(100f, 1.5f)]
        public void CannonBlunt_x1To_x1p5(float level, float factor)
        {
            Assert.That(SkillRules.CannonBluntFactor(level), Is.EqualTo(factor).Within(Eps));
        }

        [Test]
        public void Share_NaN_IsZero()
        {
            Assert.That(SkillRules.Share(float.NaN), Is.EqualTo(0f));
        }
    }
}
