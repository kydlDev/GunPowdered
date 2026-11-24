using GunPowdered.Features.Dynamite.Domain;
using GunPowdered.Features.MobileCannon.Domain;
using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Tests.Unit
{
    /// D-182 (user play test 2026-11-24): blast holes from the ground, rock pieces past the game's buffer, the cannon's handle, the musket's auto reload and facing.
    public class PlayTestFixRulesTests
    {
        [TestCase(69.4f, 66.4f, 5f, 66.4f, TestName = "CraterTop: a pack on a 3 m rock digs from the ground (shot-004)")]
        [TestCase(10.2f, 10f, 3f, 10f, TestName = "CraterTop: dynamite on the ground digs from the ground")]
        [TestCase(14f, 10f, 3f, float.NaN, TestName = "CraterTop: ground 4 m below a 3 m stick is out of reach")]
        [TestCase(11.5f, 10f, 1.5f, 10f, TestName = "CraterTop: ground exactly at the grenade's depth is reached")]
        [TestCase(9f, 10f, 3f, 9f, TestName = "CraterTop: a charge below the sampled ground digs from itself")]
        [TestCase(10f, float.NaN, 3f, float.NaN, TestName = "CraterTop: no ground found, no hole")]
        public void CraterTop(float chargeY, float groundY, float depth, float expected)
        {
            float top = CraterRules.Top(chargeY, groundY, depth);
            if (float.IsNaN(expected)) Assert.That(float.IsNaN(top), Is.True, "no hole");
            else Assert.That(top, Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void CraterDepths_AreTheUsersNumbers()
        {
            Assert.That(CraterRules.StickDepth, Is.EqualTo(3f));
            Assert.That(CraterRules.PackDepth, Is.EqualTo(5f));
            Assert.That(CraterRules.GrenadeDepth, Is.EqualTo(1.5f));
        }

        [TestCase(127, false)]
        [TestCase(128, true)]
        [TestCase(600, true)]
        [TestCase(0, false)]
        public void BlastOnRock_TakesOverOnlyWhenTheGameBufferIsFull(int found, bool modSearches)
        {
            Assert.That(BlastRockRules.GameMissesPieces(found), Is.EqualTo(modSearches));
        }

        [TestCase(0.3f, true)]
        [TestCase(1.2f, true)]
        [TestCase(1.21f, false)]
        [TestCase(2.5f, false, TestName = "OnHandle: the muzzle end of the cart loads instead of grabbing (shot-007)")]
        [TestCase(float.NaN, false)]
        public void OnHandle(float metres, bool grabs)
        {
            Assert.That(CartUseRules.OnHandle(metres), Is.EqualTo(grabs));
        }

        [TestCase(0.1f, true, true, TestName = "SwallowsPunch: the press that set the last stick down is no punch")]
        [TestCase(0.59f, true, true)]
        [TestCase(0.6f, true, false, TestName = "SwallowsPunch: a later press punches as usual")]
        [TestCase(0.1f, false, false, TestName = "SwallowsPunch: a weapon in hand attacks as usual")]
        public void SwallowsPunch(float seconds, bool empty, bool swallowed)
        {
            Assert.That(DynamiteRules.SwallowsPunch(seconds, empty), Is.EqualTo(swallowed));
        }

        [TestCase(0, 5, true)]
        [TestCase(0, 1, true)]
        [TestCase(0, 0, false, TestName = "AutoReloads: no cartridges, no reload and no message")]
        [TestCase(1, 5, false, TestName = "AutoReloads: one still loaded, no reload")]
        public void AutoReloads(int left, int carried, bool reloads)
        {
            Assert.That(MusketRules.AutoReloads(left, carried), Is.EqualTo(reloads));
        }

        [Test]
        public void AutoReload_WaitsForTheFirePose()
        {
            Assert.That(MusketRules.AutoReloadDelay, Is.GreaterThan(MusketPoseRules.FireSeconds));
        }

        [TestCase(true, true, true, TestName = "FacesAim: musket in both hands faces the camera (shot-002/003)")]
        [TestCase(true, false, false, TestName = "FacesAim: lowered at the side turns with the walk")]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public void FacesAim(bool inHand, bool raised, bool faces)
        {
            Assert.That(MusketPoseRules.FacesAim(inHand, raised), Is.EqualTo(faces));
        }
    }
}
