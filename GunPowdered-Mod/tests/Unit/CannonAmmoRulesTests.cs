using GunPowdered.Core;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Tests.Unit
{
    /// D-145 (user 2026-11-21): a shot deals the weapon's part plus the ammunition's part; each cannon ball has a tier and a cannon takes its lowest
    /// tier or higher (Hand Cannon: Iron or better, Mobile Cannon: Flametal or better).
    public class CannonAmmoRulesTests
    {
        static readonly string Iron = GunPowderedPrefabs.CannonBall, Flametal = GunPowderedPrefabs.FlametalCannonBall;

        [Test]
        public void Tiers_IronIsOne_FlametalIsTwo_AnythingElseIsNone()
        {
            Assert.That(CannonAmmoRules.TierOf(Iron), Is.EqualTo(CannonAmmoRules.IronTier).And.EqualTo(1));
            Assert.That(CannonAmmoRules.TierOf(Flametal), Is.EqualTo(CannonAmmoRules.FlametalTier).And.EqualTo(2));
            Assert.That(CannonAmmoRules.TierOf("Stone"), Is.EqualTo(0));
            Assert.That(CannonAmmoRules.TierOf(null), Is.EqualTo(0));
        }

        [Test]
        public void HandCannon_TakesIronOrBetter()
        {
            Assert.That(CannonAmmoRules.Takes(CannonAmmoRules.HandCannonMinTier, Iron), Is.True);
            Assert.That(CannonAmmoRules.Takes(CannonAmmoRules.HandCannonMinTier, Flametal), Is.True, "a higher tier ball fits too");
            Assert.That(CannonAmmoRules.Takes(CannonAmmoRules.HandCannonMinTier, "Stone"), Is.False);
        }

        [Test]
        public void MobileCannon_TakesFlametalOrBetter_NotIron()
        {
            Assert.That(CannonAmmoRules.Takes(CannonAmmoRules.MobileCannonMinTier, Flametal), Is.True);
            Assert.That(CannonAmmoRules.Takes(CannonAmmoRules.MobileCannonMinTier, Iron), Is.False, "the user: the Mobile Cannon takes Flametal or higher tiers");
        }

        [Test]
        public void Choose_UsesTheLowestTierThatFits()
        {
            Assert.That(CannonAmmoRules.Choose(CannonAmmoRules.HandCannonMinTier, new[] { 3, 2 }), Is.EqualTo(Iron), "both carried: the iron one is used");
            Assert.That(CannonAmmoRules.Choose(CannonAmmoRules.HandCannonMinTier, new[] { 0, 2 }), Is.EqualTo(Flametal), "only flametal carried: it is used");
            Assert.That(CannonAmmoRules.Choose(CannonAmmoRules.MobileCannonMinTier, new[] { 5, 1 }), Is.EqualTo(Flametal));
            Assert.That(CannonAmmoRules.Choose(CannonAmmoRules.MobileCannonMinTier, new[] { 5, 0 }), Is.Null, "iron only: the Mobile Cannon cannot load");
            Assert.That(CannonAmmoRules.Choose(CannonAmmoRules.HandCannonMinTier, new[] { 0, 0 }), Is.Null);
            Assert.That(CannonAmmoRules.Choose(CannonAmmoRules.HandCannonMinTier, null), Is.Null);
        }

        [Test]
        public void CountTaken_CountsOnlyTheBallsTheCannonTakes()
        {
            Assert.That(CannonAmmoRules.CountTaken(CannonAmmoRules.HandCannonMinTier, new[] { 3, 2 }), Is.EqualTo(5));
            Assert.That(CannonAmmoRules.CountTaken(CannonAmmoRules.MobileCannonMinTier, new[] { 3, 2 }), Is.EqualTo(2));
            Assert.That(CannonAmmoRules.CountTaken(CannonAmmoRules.MobileCannonMinTier, new[] { 3, 0 }), Is.EqualTo(0));
            Assert.That(CannonAmmoRules.CountTaken(CannonAmmoRules.MobileCannonMinTier, new[] { 3 }), Is.EqualTo(0), "a shorter list counts as none of the rest");
        }

        [Test]
        public void LoadedOrOld_ACannonLoadedBeforeD145_HoldsAnIronBall()
        {
            Assert.That(CannonAmmoRules.LoadedOrOld(Flametal), Is.EqualTo(Flametal));
            Assert.That(CannonAmmoRules.LoadedOrOld(""), Is.EqualTo(Iron));
            Assert.That(CannonAmmoRules.LoadedOrOld(null), Is.EqualTo(Iron));
        }

        [Test]
        public void ShotDamage_IsTheWeaponPartPlusTheAmmoPart()
        {
            // the user's numbers: Hand Cannon 70 blunt, 150 pickaxe, fire 10/s + Iron Cannon Ball 30 pierce, tool tier 2
            var hand = new ShotDamage(0f, 70f, 150f, 10f, 0);
            var iron = new ShotDamage(30f, 0f, 0f, 0f, 2);
            ShotDamage shot = ShotDamage.Sum(hand, iron);
            Assert.That(shot.Pierce, Is.EqualTo(30f));
            Assert.That(shot.Blunt, Is.EqualTo(70f));
            Assert.That(shot.Pickaxe, Is.EqualTo(150f));
            Assert.That(shot.FirePerSecond, Is.EqualTo(10f));
            Assert.That(shot.ToolTier, Is.EqualTo(2), "the ball brings the tool tier");

            // Mobile Cannon 120 blunt, 250 pickaxe, fire 10/s + Flametal Cannon Ball 50 pierce, tool tier 5
            ShotDamage mobile = ShotDamage.Sum(new ShotDamage(0f, 120f, 250f, 10f, 0), new ShotDamage(50f, 0f, 0f, 0f, 5));
            Assert.That(mobile.Pierce, Is.EqualTo(50f));
            Assert.That(mobile.Blunt, Is.EqualTo(120f));
            Assert.That(mobile.Pickaxe, Is.EqualTo(250f));
            Assert.That(mobile.ToolTier, Is.EqualTo(5));
        }

        [Test]
        public void ShotDamage_ToolTierIsTheHigherPart_AndAddsUpPierceFromBoth()
        {
            ShotDamage s = ShotDamage.Sum(new ShotDamage(5f, 1f, 2f, 3f, 4), new ShotDamage(7f, 1f, 1f, 1f, 2));
            Assert.That(s.Pierce, Is.EqualTo(12f));
            Assert.That(s.ToolTier, Is.EqualTo(4), "a lower ball tier does not lower the weapon's");
        }

        [Test]
        public void ShotDamage_Times_ScalesTheDamage_NotTheToolTier()
        {
            ShotDamage half = new ShotDamage(30f, 70f, 150f, 10f, 2).Times(0.5f);
            Assert.That(half.Pierce, Is.EqualTo(15f));
            Assert.That(half.Blunt, Is.EqualTo(35f));
            Assert.That(half.Pickaxe, Is.EqualTo(75f));
            Assert.That(half.FirePerSecond, Is.EqualTo(5f));
            Assert.That(half.ToolTier, Is.EqualTo(2));
        }

        [Test]
        public void Musket_IsTwentyPierce_PlusThirtyFromTheIronCartridge()
        {
            Assert.That(MusketRules.MusketPierce, Is.EqualTo(20f));
            Assert.That(MusketRules.IronCartridgePierce, Is.EqualTo(30f));
            Assert.That(MusketRules.BulletPierce, Is.EqualTo(50f), "the shot with the Iron Cartridge");
            Assert.That(MusketRules.BulletFirePerSecond, Is.EqualTo(5f), "the musket's own burning, whatever it loads");
        }

        [Test]
        public void Names_FollowTheMetal()
        {
            Assert.That(ItemCatalog.CannonBall.Name, Is.EqualTo("Iron Cannon Ball"));
            Assert.That(ItemCatalog.FlametalCannonBall.Name, Is.EqualTo("Flametal Cannon Ball"));
            Assert.That(ItemCatalog.PaperCartridge.Name, Is.EqualTo("Iron Cartridge"));
            Assert.That(ItemCatalog.CannonBall.Id, Is.EqualTo("GP_CannonBall"), "the prefab keeps its name: carried balls stay");
            Assert.That(ItemCatalog.PaperCartridge.Id, Is.EqualTo("GP_PaperCartridge"));
        }

        [Test]
        public void FlametalBall_Recipe_IsFourFlametal_AtTheBlackForge()
        {
            ItemSpec s = ItemCatalog.FlametalCannonBall;
            Assert.That(s.Station, Is.EqualTo("blackforge"));
            Assert.That(s.Cost.Count, Is.EqualTo(1));
            Assert.That(s.Cost[0].Prefab, Is.EqualTo("FlametalNew"), "the current game's Flametal, never the removed early-access one");
            Assert.That(s.Cost[0].Amount, Is.EqualTo(4));
            Assert.That(s.Stack, Is.EqualTo(50), "D-182: cannon balls stack to 50");
        }
    }
}
