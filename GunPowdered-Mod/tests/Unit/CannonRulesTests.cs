using System;
using System.Collections.Generic;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The Hand Cannon's shot rules (D-052 .. D-054): launch speed, the landing rule after a pass-through, one pass only, momentum loss.
    public class CannonRulesTests
    {
        const float Range = 30f, Gravity = 10f, Pitch = 9.46f, Step = 0.02f;

        static float? Floor(ArcVector from, ArcVector to)
        {
            if (to.Y > 0f) return null;
            return from.Y <= 0f ? 0f : from.Y / (from.Y - to.Y);
        }

        [Test]
        public void SpeedForRange_CarriesTheBallTheRange_AtTheStocksTilt()
        {
            float v = CannonRules.SpeedForRange(Range, Gravity, Pitch);
            double rad = Pitch * Math.PI / 180d;
            var velocity = new ArcVector((float)(v * Math.Cos(rad)), (float)(v * Math.Sin(rad)), 0f);
            ArcResult r = ThrowArcRules.Simulate(new ArcVector(0f, 0f, 0f), velocity, Gravity, 0f, Step, 6f, Floor, new List<ArcVector>());
            Assert.That(r.Landed, Is.True);
            Assert.That(r.Landing.X, Is.EqualTo(Range).Within(0.7f), "lands about 30 m away on flat ground (the stepped flight is a hair short)");
            Assert.That(v, Is.EqualTo(30.4f).Within(0.2f));
        }

        [TestCase(0f, 10f, 9f)]
        [TestCase(30f, 0f, 9f)]
        [TestCase(30f, 10f, 0f)]
        [TestCase(30f, 10f, 90f)]
        public void SpeedForRange_RefusesImpossibleInputs(float range, float gravity, float pitch)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CannonRules.SpeedForRange(range, gravity, pitch));
        }

        [Test]
        public void TheSpeedReachesEveryAimedSpotUpToTheRange_ByTheLowArc()
        {
            float v = CannonRules.SpeedForRange(Range, Gravity, Pitch);
            foreach (float d in new[] { 3f, 10f, 20f, 30f })
            {
                LaunchSolution s = LaunchSolver.Solve(new ArcVector(0f, 0.6f, 0f), new ArcVector(d, 0f, 0f), v, Gravity, Step);
                Assert.That(s.Reachable, Is.True, d + " m");
                Assert.That(s.ElevationDegrees, Is.LessThan(Pitch + 0.5f), d + " m: never steeper than the stock's own tilt");
            }
        }

        // the user's own examples (D-053 addendum 2)
        [TestCase(30f, 20f, 25f)]
        [TestCase(30f, 10f, 20f)]
        [TestCase(30f, 30f, 30f)]
        [TestCase(20f, 25f, 25f)]
        [TestCase(30f, -5f, 15f)]
        public void PassThroughLanding_IsHalfWayBetweenImpactAndTarget(float target, float hit, float expected)
        {
            Assert.That(CannonRules.PassThroughLanding(target, hit), Is.EqualTo(expected).Within(0.0001f));
        }

        // the enums are internal: the cases name them by text ("Ground" / "Solid" / "Passable", "Stop" / "PassThrough" / "StopWithDamage")
        // D-055 addendum: buildings and rocks (Solid) take the ball's damage and still end the flight; bare terrain (Ground) only ends it
        [TestCase("Ground", 0, "Stop")]
        [TestCase("Ground", 1, "Stop")]
        [TestCase("Solid", 0, "StopWithDamage")]
        [TestCase("Solid", 1, "StopWithDamage")]
        [TestCase("Passable", 0, "PassThrough")]
        [TestCase("Passable", 1, "StopWithDamage")]
        [TestCase("Passable", 2, "StopWithDamage")]
        [TestCase("Passable", -1, "PassThrough")]
        public void OnImpact_PassesOneTreeOrCreature_TheSecondStopsTheBall(string kind, int passed, string expected)
        {
            var k = (ImpactKind)Enum.Parse(typeof(ImpactKind), kind);
            Assert.That(CannonRules.OnImpact(k, passed).ToString(), Is.EqualTo(expected));
        }

        // D-055 addendum 2 = the game's own rule for a player's projectile (Projectile.OnHit + Character.RPC_Damage, 1.0.21): an enemy of the
        // shooter is always hit; a friend (a player, a tamed animal) only when the shooter has PvP on, and a player only when their PvP is on too
        [TestCase(true, false, false, false, true)]
        [TestCase(true, true, false, false, true)]
        [TestCase(false, false, false, false, false)]
        [TestCase(false, true, false, false, true)]
        [TestCase(false, false, true, true, false)]
        [TestCase(false, true, true, false, false)]
        [TestCase(false, true, true, true, true)]
        public void HitsCreature_FollowsTheGamesFriendlyFireRule(bool enemy, bool shooterPvp, bool targetIsPlayer, bool targetPvp, bool expected)
        {
            Assert.That(CannonRules.HitsCreature(new HitCheck(enemy, shooterPvp, targetIsPlayer, targetPvp)), Is.EqualTo(expected));
        }

        [TestCase(0, 0.5f, 1f)]
        [TestCase(1, 0.5f, 0.5f)]
        [TestCase(2, 0.5f, 0.25f)]
        [TestCase(1, 1.5f, 1f)]
        [TestCase(1, -1f, 0f)]
        [TestCase(-3, 0.5f, 1f)]
        public void MomentumAfter_IsTheFactorPerPass(int passed, float factor, float expected)
        {
            Assert.That(CannonRules.MomentumAfter(passed, factor), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(40f, 3f, 30f, 30f)]
        [TestCase(1f, 3f, 30f, 3f)]
        [TestCase(12f, 3f, 30f, 12f)]
        [TestCase(1f, 40f, 30f, 30f)]
        public void ClampAim_KeepsTheSpotInsideTheRange(float wanted, float min, float max, float expected)
        {
            Assert.That(CannonRules.ClampAim(wanted, min, max), Is.EqualTo(expected));
        }
    }
}
