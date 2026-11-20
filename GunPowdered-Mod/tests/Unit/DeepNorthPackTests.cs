using System.Linq;
using GunPowdered.Core;
using GunPowdered.Features.Dynamite.Domain;

namespace GunPowdered.Tests.Unit
{
    /// D-108 (user 2026-11-20): the Deep North Dynamite Pack - its recipe, the Fader key gate and the on-screen countdown.
    public class DeepNorthPackTests
    {
        [Test]
        public void Recipe_IsFourDynamiteTwoGrenadesFourSealHide_AtTheForgeLevelTwo()
        {
            var p = ItemCatalog.DeepNorthPack;
            Assert.That(p.Station, Is.EqualTo(ItemCatalog.Forge));
            Assert.That(p.StationLevel, Is.EqualTo(2));
            Assert.That(p.Yield, Is.EqualTo(1));
            Assert.That(p.Cost.Count, Is.EqualTo(3));
            Assert.That(p.Cost.Single(c => c.Prefab == GunPowderedPrefabs.Dynamite).Amount, Is.EqualTo(4));
            Assert.That(p.Cost.Single(c => c.Prefab == GunPowderedPrefabs.SpikedGrenade).Amount, Is.EqualTo(2));
            Assert.That(p.Cost.Single(c => c.Prefab == "SealHide").Amount, Is.EqualTo(4));
            Assert.That(p.Name, Is.EqualTo("Deep North Dynamite Pack"));
        }

        [Test]
        public void Recipe_WaitsForFadersKey_AndNoOtherEntryWaits()
        {
            Assert.That(ItemCatalog.DeepNorthPack.RequiredGlobalKey, Is.EqualTo("defeated_fader"));
            Assert.That(ItemCatalog.All.Where(i => i.RequiredGlobalKey != null).Select(i => i.Id), Is.EqualTo(new[] { GunPowderedPrefabs.DeepNorthPack }));
            Assert.That(ItemCatalog.All, Does.Contain(ItemCatalog.DeepNorthPack));
        }

        [Test]
        public void Gate_IsOnOnlyWithTheFeatureOnAndTheKeySet()
        {
            Assert.That(RecipeGate.IsOn(true, "defeated_fader", k => k == "defeated_fader"), Is.True);
            Assert.That(RecipeGate.IsOn(true, "defeated_fader", k => false), Is.False);
            Assert.That(RecipeGate.IsOn(false, "defeated_fader", k => true), Is.False);
            Assert.That(RecipeGate.IsOn(true, "defeated_fader", null), Is.False);
            Assert.That(RecipeGate.IsOn(true, null, k => false), Is.True);
            Assert.That(RecipeGate.IsOn(false, null, k => true), Is.False);
        }

        [TestCase(0f, 20f, true)]
        [TestCase(20f, 20f, true)]
        [TestCase(30f, 20f, true)]
        [TestCase(30.5f, 20f, false)]
        [TestCase(-1f, 20f, false)]
        public void Timer_ShowsWithinTheBlastAndTenMetresBeyond(float distance, float radius, bool shows)
        {
            Assert.That(DynamiteRules.ShowsTimer(distance, radius), Is.EqualTo(shows));
        }

        [Test]
        public void TimerText_CountsDownWithOneDecimal_AndNeverBelowZero()
        {
            Assert.That(DynamiteRules.TimerText("Deep North Dynamite Pack", 3.24f), Is.EqualTo("Deep North Dynamite Pack: 3.2 s - get away!"));
            Assert.That(DynamiteRules.TimerText("Deep North Dynamite Pack", -1f), Is.EqualTo("Deep North Dynamite Pack: 0.0 s - get away!"));
        }

        [Test]
        public void Fuse_IsFourSeconds()
        {
            Assert.That(DynamiteRules.FuseSeconds, Is.EqualTo(4f));
            Assert.That(DynamiteRules.Blows(10d, 13.9d), Is.False);
            Assert.That(DynamiteRules.Blows(10d, 14d), Is.True);
        }
    }
}
