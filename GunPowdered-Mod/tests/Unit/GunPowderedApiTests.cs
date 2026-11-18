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
            Assert.That(GunPowderedApi.AllPrefabs.Count, Is.EqualTo(15), "12 before the hand cannon + Cannon Ball, Cannon, Cannon Stock");
            Assert.That(GunPowderedApi.AllPrefabs, Does.Contain(GunPowderedApi.Prefabs.Gunpowder));
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
    }
}
