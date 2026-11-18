using GunPowdered.Features.Cannon.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Which action a Cannon Stock allows (D-052 addendum, D-053): mount, load at the muzzle, aim, light the fuse with a torch, take the cannon off.
    public class CannonStateRulesTests
    {
        static readonly CannonStatus Empty = new CannonStatus(false, false, false), Mounted = new CannonStatus(true, false, false),
            Loaded = new CannonStatus(true, true, false), Lit = new CannonStatus(true, true, true);

        [Test]
        public void Mount_NeedsAnEmptyStockAndACannon()
        {
            Assert.That(CannonStateRules.WhyNotMount(Empty, true), Is.Null);
            Assert.That(CannonStateRules.WhyNotMount(Empty, false), Is.EqualTo(CannonStateRules.NeedCannon));
            Assert.That(CannonStateRules.WhyNotMount(Mounted, true), Is.EqualTo(CannonStateRules.AlreadyMounted));
        }

        [Test]
        public void Unmount_NeedsAMountedCannon_AndNoBurningFuse()
        {
            Assert.That(CannonStateRules.WhyNotUnmount(Empty), Is.EqualTo(CannonStateRules.NotMounted));
            Assert.That(CannonStateRules.WhyNotUnmount(Mounted), Is.Null);
            Assert.That(CannonStateRules.WhyNotUnmount(Loaded), Is.Null, "a loaded cannon can be taken off; its load comes back");
            Assert.That(CannonStateRules.WhyNotUnmount(Lit), Is.EqualTo(CannonStateRules.FuseBurning));
        }

        [TestCase(1, 1, 1, null)]
        [TestCase(3, 1, 2, null)]
        [TestCase(0, 1, 1, "You need 1 Gunpowder")]
        [TestCase(1, 2, 1, "You need 2 Gunpowder")]
        [TestCase(5, 1, 0, CannonStateRules.NeedBall)]
        public void Load_NeedsGunpowderAndOneBall(int powder, int need, int balls, string expected)
        {
            Assert.That(CannonStateRules.WhyNotLoad(Mounted, powder, need, balls), Is.EqualTo(expected));
        }

        [Test]
        public void Load_NeedsAMountedEmptyCannon()
        {
            Assert.That(CannonStateRules.WhyNotLoad(Empty, 9, 1, 9), Is.EqualTo(CannonStateRules.NotMounted));
            Assert.That(CannonStateRules.WhyNotLoad(Loaded, 9, 1, 9), Is.EqualTo(CannonStateRules.AlreadyLoaded));
        }

        [Test]
        public void Aim_NeedsALoadedCannon()
        {
            Assert.That(CannonStateRules.WhyNotAim(Empty), Is.EqualTo(CannonStateRules.NotMounted));
            Assert.That(CannonStateRules.WhyNotAim(Mounted), Is.EqualTo(CannonStateRules.NotLoaded));
            Assert.That(CannonStateRules.WhyNotAim(Loaded), Is.Null);
        }

        [Test]
        public void Light_NeedsALoadedCannon_ATorchInHand_AndNoBurningFuse()
        {
            Assert.That(CannonStateRules.WhyNotLight(Loaded, true), Is.Null);
            Assert.That(CannonStateRules.WhyNotLight(Loaded, false), Is.EqualTo(CannonStateRules.NeedTorch));
            Assert.That(CannonStateRules.WhyNotLight(Lit, true), Is.EqualTo(CannonStateRules.FuseBurning));
            Assert.That(CannonStateRules.WhyNotLight(Mounted, true), Is.EqualTo(CannonStateRules.NotLoaded));
            Assert.That(CannonStateRules.WhyNotLight(Empty, true), Is.EqualTo(CannonStateRules.NotMounted));
        }
    }
}
