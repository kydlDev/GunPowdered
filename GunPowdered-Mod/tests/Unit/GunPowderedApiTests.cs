using System.Linq;
using GunPowdered;
using GunPowdered.Features.Guano.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The public API other mods call (D-006): names, display names and the default recipes.
    public class GunPowderedApiTests
    {
        [Test]
        public void AllPrefabs_AreTheGpNames()
        {
            Assert.That(GunPowderedApi.AllPrefabs.Count, Is.EqualTo(33), "Flametal Cannon Ball (D-145) + 3 Powder Mill extensions (D-137) + 5 Leaching Station extensions (D-129) + Deep North Dynamite Pack (D-108) + 3 musket ammunition items (Paper, Iron Bullet, Paper Cartridge) + 12 before the hand cannon + Cannon Ball, Cannon, Cannon Stock + Musket, Explosive Arrow, Dynamite, Mobile Cannon, Asksvin Harness");
            Assert.That(GunPowderedApi.AllPrefabs, Does.Contain(GunPowderedApi.Prefabs.Gunpowder));
            Assert.That(GunPowderedApi.AllPrefabs, Does.Contain(GunPowderedApi.Prefabs.FlametalHeater));
            Assert.That(GunPowderedApi.AllPrefabs.All(p => p.StartsWith("GP_")), Is.True);
        }

        [Test]
        public void DisplayName_IsTheEnglishName_OrNullForForeignPrefabs()
        {
            Assert.That(GunPowderedApi.DisplayName(GunPowderedApi.Prefabs.PowderMill), Is.EqualTo("Powder Mill"));
            Assert.That(GunPowderedApi.DisplayName("Wood"), Is.Null);
        }

        [TestCase("GP_Saltpeter", true)]
        [TestCase("Coal", true)]
        [TestCase("SulfurStone", true)]
        [TestCase("Wood", false)]
        public void IsGunpowderIngredient_KnowsTheThree(string prefab, bool expected)
        {
            Assert.That(GunPowderedApi.IsGunpowderIngredient(prefab), Is.EqualTo(expected));
        }

        [TestCase(3, 1, 1, 5)]
        [TestCase(6, 1, 5, 5)]
        [TestCase(2, 9, 9, 0)]
        public void GunpowderFrom_UsesTheDefaultRecipe(int saltpeter, int charcoal, int sulfur, int expected)
        {
            Assert.That(GunPowderedApi.GunpowderFrom(saltpeter, charcoal, sulfur), Is.EqualTo(expected));
        }

        [TestCase(10, 10, 5)]
        [TestCase(20, 30, 10)]
        [TestCase(9, 50, 0)]
        public void SaltpeterFrom_UsesTheDefaultRecipe(int guano, int water, int expected)
        {
            Assert.That(GunPowderedApi.SaltpeterFrom(guano, water), Is.EqualTo(expected));
        }

        [Test]
        public void RainBarrelWater_GrowsInRain_AndStaysUnderARoof()
        {
            Assert.That(GunPowderedApi.RainBarrelWater(0f, 60f, true, false), Is.EqualTo(5f).Within(0.001f));
            Assert.That(GunPowderedApi.RainBarrelWater(10f, 600f, true, true), Is.EqualTo(10f));
            Assert.That(GunPowderedApi.RainBarrelWater(199f, 6000f, true, false), Is.EqualTo(200f));
        }

        [Test]
        public void GuanoDeposits_NeverMoreThanTheCaveMaximum()
        {
            Assert.That(GunPowderedApi.GuanoDeposits(CaveKind.TrollCaveOrBurialChamber, () => 0.99), Is.EqualTo(0));
            Assert.That(GunPowderedApi.GuanoDeposits(CaveKind.TrollCaveOrBurialChamber, () => 0.01), Is.EqualTo(1));
        }

        // D-097: the weapons part (the game-side members are checked in game by the TestLab test api-musket)
        [Test]
        public void Musket_StatsAreTheGameRules()
        {
            Assert.That(GunPowderedApi.Musket.Capacity, Is.EqualTo(5));
            Assert.That(GunPowderedApi.Musket.BulletPierce, Is.EqualTo(50f));
            Assert.That(GunPowderedApi.Musket.BulletSpeed, Is.GreaterThan(0f));
            Assert.That(GunPowderedApi.Musket.ShotIntervalSeconds, Is.EqualTo(0.8f));
            Assert.That(GunPowderedApi.Musket.ReloadSecondsPerCartridge, Is.EqualTo(1.2f));
            Assert.That(GunPowderedApi.Musket.Ammunition, Is.EqualTo(GunPowderedApi.Prefabs.PaperCartridge));
        }

        [TestCase(0, 9, 5)]
        [TestCase(3, 9, 2)]
        [TestCase(5, 9, 0)]
        [TestCase(1, 2, 2)]
        public void Musket_CartridgesToLoad_NeverAboveCapacity(int loaded, int carried, int expected)
        {
            Assert.That(GunPowderedApi.Musket.CartridgesToLoad(loaded, carried), Is.EqualTo(expected));
        }
    }
}
