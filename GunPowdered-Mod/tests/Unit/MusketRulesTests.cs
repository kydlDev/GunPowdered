using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The Iron Musket's magazine (D-080 / D-081): up to 5 paper cartridges, reload below 5 and never above 5, one cartridge per shot.
    public class MusketRulesTests
    {
        [Test]
        public void Capacity_IsFive()
        {
            Assert.That(MusketRules.Capacity, Is.EqualTo(5));
        }

        [TestCase(0, 10, 5)]
        [TestCase(0, 3, 3)]
        [TestCase(2, 10, 3)]
        [TestCase(2, 1, 1)]
        [TestCase(4, 9, 1)]
        [TestCase(5, 9, 0)]
        [TestCase(0, 0, 0)]
        [TestCase(3, -2, 0)]
        public void CartridgesToLoad_FillsUpToFive_FromWhatIsCarried(int loaded, int carried, int expected)
        {
            Assert.That(MusketRules.CartridgesToLoad(loaded, carried), Is.EqualTo(expected));
        }

        [TestCase(0, 99)]
        [TestCase(1, 99)]
        [TestCase(2, 4)]
        [TestCase(4, 1)]
        [TestCase(4, 50)]
        public void AfterReload_NeverGoesAboveFive(int loaded, int carried)
        {
            Assert.That(MusketRules.AfterReload(loaded, carried), Is.LessThanOrEqualTo(5));
            Assert.That(MusketRules.AfterReload(loaded, carried), Is.GreaterThanOrEqualTo(loaded));
        }

        [Test]
        public void AfterReload_FromTwoWithTenCarried_IsFive()
        {
            Assert.That(MusketRules.AfterReload(2, 10), Is.EqualTo(5));
            Assert.That(MusketRules.AfterReload(0, 3), Is.EqualTo(3));
        }

        [Test]
        public void Reload_IsRefusedWhenFull_OrWithoutCartridges()
        {
            Assert.That(MusketRules.WhyNotReload(5, 10), Is.EqualTo(MusketRules.Full));
            Assert.That(MusketRules.WhyNotReload(2, 0), Is.EqualTo(MusketRules.NoCartridges));
            Assert.That(MusketRules.WhyNotReload(4, 1), Is.Null, "one short of full is still allowed");
            Assert.That(MusketRules.WhyNotReload(0, 1), Is.Null);
            Assert.That(MusketRules.WhyNotReload(5, 0), Is.EqualTo(MusketRules.Full), "full wins over missing cartridges");
        }

        [Test]
        public void Fire_NeedsALoadedCartridge()
        {
            Assert.That(MusketRules.WhyNotFire(0), Is.EqualTo(MusketRules.Empty));
            Assert.That(MusketRules.WhyNotFire(1), Is.Null);
            Assert.That(MusketRules.WhyNotFire(5), Is.Null);
        }

        [Test]
        public void AfterShot_UsesOneCartridge_NeverGoesBelowZero()
        {
            Assert.That(MusketRules.AfterShot(5), Is.EqualTo(4));
            Assert.That(MusketRules.AfterShot(1), Is.EqualTo(0));
            Assert.That(MusketRules.AfterShot(0), Is.EqualTo(0));
        }

        [Test]
        public void FiveShotsEmptyAFullMusket_AndASixthIsRefused()
        {
            int loaded = MusketRules.Capacity;
            for (int i = 0; i < 5; i++)
            {
                Assert.That(MusketRules.WhyNotFire(loaded), Is.Null, "shot " + (i + 1));
                loaded = MusketRules.AfterShot(loaded);
            }
            Assert.That(loaded, Is.EqualTo(0));
            Assert.That(MusketRules.WhyNotFire(loaded), Is.EqualTo(MusketRules.Empty));
        }

        [TestCase(-3, 0)]
        [TestCase(0, 0)]
        [TestCase(3, 3)]
        [TestCase(5, 5)]
        [TestCase(9, 5)]
        public void ClampLoaded_KeepsAStoredLoadInsideTheMagazine(int stored, int expected)
        {
            Assert.That(MusketRules.ClampLoaded(stored), Is.EqualTo(expected));
        }

        [Test]
        public void ADamagedStoredLoad_CannotBeReloadedAboveFive()
        {
            Assert.That(MusketRules.CartridgesToLoad(9, 10), Is.EqualTo(0));
            Assert.That(MusketRules.WhyNotReload(9, 10), Is.EqualTo(MusketRules.Full));
            Assert.That(MusketRules.AfterReload(-2, 10), Is.EqualTo(5));
        }
    }
}
