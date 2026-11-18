using GunPowdered.Features.Guano.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Which locations get guano on first entry, and when an entry places (D-033).
    public class CaveEntryRulesTests
    {
        [TestCase("TrollCave02", "", CaveKind.TrollCaveOrBurialChamber)]
        [TestCase("Crypt2", "DG_ForestCrypt", CaveKind.TrollCaveOrBurialChamber)]
        [TestCase("Crypt4", "", CaveKind.TrollCaveOrBurialChamber)]
        [TestCase("MountainCave02", "DG_Cave", CaveKind.MountainCave)]
        [TestCase("SomeRuin", "DG_HalfBurried_ForestCrypt", CaveKind.TrollCaveOrBurialChamber)]
        [TestCase("SomeCave", "DG_Cave(Clone)", CaveKind.MountainCave)]
        public void KindOf_KnownCaves(string location, string generator, CaveKind expected)
        {
            Assert.That(CaveEntryRules.KindOf(location, generator), Is.EqualTo(expected));
        }

        [TestCase("SunkenCrypt4", "DG_SunkenCrypt")]
        [TestCase("Hildir_crypt", "DG_Hildir_ForestCrypt")]
        [TestCase("FireHole", "")]
        [TestCase(null, null)]
        public void KindOf_NoGuano(string location, string generator)
        {
            Assert.That(CaveEntryRules.KindOf(location, generator), Is.Null);
        }

        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        [TestCase(false, false, false)]
        public void ShouldPlace_OnlyFirstWayIn(bool goingIn, bool alreadyPlaced, bool expected)
        {
            Assert.That(CaveEntryRules.ShouldPlace(goingIn, CaveKind.MountainCave, alreadyPlaced), Is.EqualTo(expected));
        }

        [Test]
        public void ShouldPlace_NotACave_Never()
        {
            Assert.That(CaveEntryRules.ShouldPlace(true, null, false), Is.False);
        }
    }
}
