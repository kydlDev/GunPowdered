using GunPowdered.Features.Gunpowder.Domain;

namespace GunPowdered.Tests.Unit
{
    /// <summary>
    /// The automatic Powder Mill's own numbers and names (D-137). The batch rules themselves are BatchStationRules (tested there); these pin what the mill
    /// hands them: its inputs in the order Saltpeter / Charcoal / Sulfur (the hover, the "Needs ..." message and the fill message name them), three steps,
    /// 10 minutes less 2.5 per step (a14gate Stryker: the three names were unpinned, MillUpgradeRules.cs 0 %).
    /// </summary>
    public class MillUpgradeRulesTests
    {
        [Test]
        public void Spec_NamesTheInputsInTheirOrder()
        {
            Assert.Multiple(() =>
            {
                Assert.That(MillUpgradeRules.Spec.FirstName, Is.EqualTo("Saltpeter"));
                Assert.That(MillUpgradeRules.Spec.SecondName, Is.EqualTo("Charcoal"));
                Assert.That(MillUpgradeRules.Spec.ThirdName, Is.EqualTo("Sulfur"));
                Assert.That(MillUpgradeRules.Spec.MaxStep, Is.EqualTo(3));
            });
        }

        [TestCase(0, 10d)]
        [TestCase(1, 7.5d)]
        [TestCase(2, 5d)]
        [TestCase(3, 2.5d)]
        public void Minutes_TenLessTwoAndAHalfPerStep(int step, double expected)
        {
            Assert.That(MillUpgradeRules.Minutes(step, MillUpgradeRules.DefaultBaseMinutes, MillUpgradeRules.DefaultMinutesPerStep), Is.EqualTo(expected).Within(1e-9));
        }
    }
}
