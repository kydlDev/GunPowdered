using GunPowdered.Features.Sulfur.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Blob and Oozer kills: 10 % each chance to drop sulfur (D-016).
    public class SulfurDropTests
    {
        [TestCase(0.0, true)]
        [TestCase(0.0999, true)]
        [TestCase(0.1, false)]
        [TestCase(0.5, false)]
        [TestCase(0.9999, false)]
        public void KillDrops_IsATenPercentChance(double roll, bool expected)
        {
            Assert.That(SulfurRules.KillDrops(roll), Is.EqualTo(expected));
        }
    }
}
