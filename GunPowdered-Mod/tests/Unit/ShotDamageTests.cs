using GunPowdered.Features.Cannon.Domain;

namespace GunPowdered.Tests.Unit
{
    /// D-145: a shot's damage = the weapon's part + the ammunition's part (G14: every field and the tool tier rule pinned).
    public class ShotDamageTests
    {
        static readonly ShotDamage Weapon = new ShotDamage(10f, 20f, 30f, 4f, 2);
        static readonly ShotDamage Ammo = new ShotDamage(1f, 2f, 3f, 0.5f, 5);

        [Test]
        public void Constructor_KeepsEveryPart()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Weapon.Pierce, Is.EqualTo(10f));
                Assert.That(Weapon.Blunt, Is.EqualTo(20f));
                Assert.That(Weapon.Pickaxe, Is.EqualTo(30f));
                Assert.That(Weapon.FirePerSecond, Is.EqualTo(4f));
                Assert.That(Weapon.ToolTier, Is.EqualTo(2));
            });
        }

        [Test]
        public void Sum_AddsEveryDamageAndTakesTheHigherToolTier()
        {
            ShotDamage s = ShotDamage.Sum(Weapon, Ammo);
            Assert.Multiple(() =>
            {
                Assert.That(s.Pierce, Is.EqualTo(11f));
                Assert.That(s.Blunt, Is.EqualTo(22f));
                Assert.That(s.Pickaxe, Is.EqualTo(33f));
                Assert.That(s.FirePerSecond, Is.EqualTo(4.5f));
                Assert.That(s.ToolTier, Is.EqualTo(5), "the ammo's tier is higher");
            });
        }

        [TestCase(2, 5, 5)]
        [TestCase(5, 2, 5)]
        [TestCase(0, 3, 3)]
        [TestCase(3, 0, 3)]
        [TestCase(0, 0, 0)]
        public void Sum_ToolTier_IsTheHigherOfTheTwo_ANoTierPartDoesNotLowerIt(int weaponTier, int ammoTier, int expected)
        {
            ShotDamage s = ShotDamage.Sum(new ShotDamage(0f, 0f, 0f, 0f, weaponTier), new ShotDamage(0f, 0f, 0f, 0f, ammoTier));
            Assert.That(s.ToolTier, Is.EqualTo(expected));
        }

        [Test]
        public void Times_ScalesEveryDamageButNotTheToolTier()
        {
            ShotDamage t = Weapon.Times(0.5f);
            Assert.Multiple(() =>
            {
                Assert.That(t.Pierce, Is.EqualTo(5f));
                Assert.That(t.Blunt, Is.EqualTo(10f));
                Assert.That(t.Pickaxe, Is.EqualTo(15f));
                Assert.That(t.FirePerSecond, Is.EqualTo(2f));
                Assert.That(t.ToolTier, Is.EqualTo(2));
            });
        }

        [Test]
        public void ToString_NamesEveryPart()
        {
            Assert.That(new ShotDamage(1f, 2f, 3f, 4f, 5).ToString(), Is.EqualTo("pierce 1, blunt 2, pickaxe 3, fire 4/s, tool tier 5"));
        }
    }
}
