using GunPowdered.Features.Saltpeter.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The Leaching Station takes one item per request and checks it like the game's Smelter does (D-035).
    public class VatFeedRulesTests
    {
        static readonly LeachingVatRules.VatAmounts Need = new LeachingVatRules.VatAmounts(10, 10, 4);

        [TestCase("GP_BatGuano", 0, 0, 0, "Guano")]
        [TestCase("GP_BatGuano", 9, 0, 0, "Guano")]
        [TestCase("GP_BatGuano", 10, 0, 0, "Refused")]
        [TestCase("GP_Water", 0, 9, 0, "Water")]
        [TestCase("GP_Water", 0, 10, 0, "Refused")]
        [TestCase("Wood", 0, 0, 3, "Wood")]
        [TestCase("Wood", 0, 0, 4, "Refused")]
        [TestCase("Stone", 0, 0, 0, "Refused")]
        [TestCase("", 0, 0, 0, "Refused")]
        [TestCase(null, 0, 0, 0, "Refused")]
        public void Accept_OneItemIntoItsSlotWhileThereIsRoom(string prefab, int guano, int water, int fuel, string expected)
        {
            Assert.That(LeachingVatRules.Accept(prefab, true, new LeachingVatRules.VatAmounts(guano, water, fuel), Need).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void Accept_NothingWhileTheStationIsBusy()
        {
            Assert.That(LeachingVatRules.Accept("GP_BatGuano", false, new LeachingVatRules.VatAmounts(0, 0, 0), Need), Is.EqualTo(LeachingVatRules.Feed.Refused));
        }

        [TestCase(10, 10, true)]
        [TestCase(9, 10, false)]
        [TestCase(10, 9, false)]
        [TestCase(12, 15, true)]
        public void BarrelFull_NeedsAWholeBatchOfBoth(int guano, int water, bool expected)
        {
            Assert.That(LeachingVatRules.BarrelFull(guano, water, 10, 10), Is.EqualTo(expected));
        }

        [Test]
        public void BarrelFull_BadSettingNeverFull()
        {
            Assert.That(LeachingVatRules.BarrelFull(10, 10, 0, 10), Is.False);
        }
    }
}
