using GunPowdered.Core;
using GunPowdered.Features.Dynamite.Domain;

namespace GunPowdered.Tests.Unit
{
    /// D-149 (gunpowder noise) and D-148 (where a dynamite stick is set down).
    public class NoiseAndPlaceRulesTests
    {
        [TestCase(10f, 15f, true, false, false, false, true, TestName = "Reacts: hostile inside the radius")]
        [TestCase(15f, 15f, true, false, false, false, true, TestName = "Reacts: on the edge")]
        [TestCase(15.01f, 15f, true, false, false, false, false, TestName = "Reacts: just outside")]
        [TestCase(5f, 15f, false, false, false, false, false, TestName = "Reacts: not hostile")]
        [TestCase(5f, 15f, true, true, false, false, false, TestName = "Reacts: tamed")]
        [TestCase(5f, 15f, true, false, true, false, false, TestName = "Reacts: dead")]
        [TestCase(5f, 15f, true, false, false, true, false, TestName = "Reacts: already fighting keeps its target")]
        [TestCase(-1f, 15f, true, false, false, false, false, TestName = "Reacts: a negative distance is no distance")]
        public void Reacts(float distance, float radius, bool hostile, bool tamed, bool dead, bool hasTarget, bool expected)
        {
            Assert.That(NoiseRules.Reacts(distance, radius, hostile, tamed, dead, hasTarget), Is.EqualTo(expected));
        }

        [Test]
        public void NoiseRadii_TheUsersOrder()
        {
            Assert.Multiple(() =>
            {
                Assert.That(NoiseRules.FuseRadius, Is.EqualTo(15f), "a lit fuse: like a pickaxe hit");
                Assert.That(NoiseRules.StickBlastRadius, Is.EqualTo(60f));
                Assert.That(NoiseRules.PackBlastRadius, Is.EqualTo(90f));
                Assert.That(NoiseRules.MusketShotRadius, Is.EqualTo(40f));
                Assert.That(NoiseRules.CannonShotRadius, Is.EqualTo(60f));
                Assert.That(NoiseRules.MobileCannonShotRadius, Is.EqualTo(70f));
                Assert.That(NoiseRules.FuseRadius, Is.LessThan(NoiseRules.MusketShotRadius), "the fuse attracts only a little");
                Assert.That(NoiseRules.StickBlastRadius, Is.LessThan(NoiseRules.PackBlastRadius));
            });
        }

        [TestCase(1.2f, -1f, 1.2f, TestName = "ClearDistance: nothing in the way")]
        [TestCase(1.2f, 0.9f, 0.65f, TestName = "ClearDistance: a wall at 0.9 m - in front of it")]
        [TestCase(1.2f, 2f, 1.2f, TestName = "ClearDistance: a wall further than wanted changes nothing")]
        [TestCase(1.2f, 0.4f, 0.3f, TestName = "ClearDistance: a wall right in front - the nearest place")]
        [TestCase(8f, -1f, 8f, TestName = "ClearDistance: a test's long distance")]
        public void ClearDistance(float wanted, float blockedAt, float expected)
        {
            Assert.That(DynamiteRules.ClearDistance(wanted, blockedAt), Is.EqualTo(expected).Within(1e-4f));
        }

        [TestCase(10f, 10f, true)]
        [TestCase(10f, 9f, true)]
        [TestCase(10f, 8.99f, false)]
        [TestCase(10f, 10.8f, true)]
        [TestCase(10f, 10.81f, false)]
        public void SurfaceOk_IsTheGroundThePlayerStandsOn(float feet, float surface, bool expected)
        {
            Assert.That(DynamiteRules.SurfaceOk(feet, surface), Is.EqualTo(expected));
        }
    }
}
