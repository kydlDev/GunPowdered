using System;
using System.Collections.Generic;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The grenade flight (D-047): the same steps as the game's Projectile, a landing on the first hit, about 15 m on flat ground at the best angle.
    public class ThrowArcRulesTests
    {
        const float Speed = 12f, Gravity = 10f, Dt = ThrowArcRules.DefaultStep;

        static float? Floor(ArcVector from, ArcVector to)
        {
            if (to.Y > 0f) return null;
            return from.Y <= 0f ? 0f : from.Y / (from.Y - to.Y);
        }

        static ArcVector Aim(float degreesUp) =>
            new ArcVector((float)Math.Cos(degreesUp * Math.PI / 180d), (float)Math.Sin(degreesUp * Math.PI / 180d), 0f);

        [Test]
        public void Step_WithoutDrag_IsTheGamesEulerStep()
        {
            var pos = new ArcVector(0f, 2f, 0f);
            var vel = new ArcVector(10f, 5f, 0f);
            ThrowArcRules.Step(ref pos, ref vel, 10f, 0f, 0.1f);
            Assert.That(vel.X, Is.EqualTo(10f).Within(1e-5));
            Assert.That(vel.Y, Is.EqualTo(4f).Within(1e-5));
            Assert.That(pos.X, Is.EqualTo(1f).Within(1e-5));
            Assert.That(pos.Y, Is.EqualTo(2.4f).Within(1e-5));
        }

        [Test]
        public void Step_WithDrag_SlowsTheFlightBySpeedSquared()
        {
            var pos = new ArcVector(0f, 0f, 0f);
            var vel = new ArcVector(10f, 0f, 0f);
            ThrowArcRules.Step(ref pos, ref vel, 0f, 0.01f, 0.1f);
            // 10 - 10^2 x 0.01 x 0.1 = 9.9
            Assert.That(vel.X, Is.EqualTo(9.9f).Within(1e-4));
            Assert.That(pos.X, Is.EqualTo(0.99f).Within(1e-4));
        }

        [Test]
        public void Throw_AtTheBestAngle_LandsAbout15mAway()
        {
            var points = new List<ArcVector>();
            var start = new ArcVector(0f, 1.5f, 0f);
            ArcResult r = ThrowArcRules.Simulate(start, ThrowArcRules.LaunchVelocity(Aim(45f), Speed), Gravity, 0f, Dt, 6f, Floor, points);
            Assert.That(r.Landed, Is.True);
            Assert.That(start.FlatDistanceTo(r.Landing), Is.InRange(14.5f, 16.5f));
            Assert.That(r.Landing.Y, Is.EqualTo(0f).Within(1e-4));
        }

        [TestCase(10f)]
        [TestCase(30f)]
        [TestCase(60f)]
        public void Throw_AtAnyOtherAngle_LandsCloserThanAtTheBest(float degrees)
        {
            var points = new List<ArcVector>();
            var start = new ArcVector(0f, 1.5f, 0f);
            float best = start.FlatDistanceTo(ThrowArcRules.Simulate(start, ThrowArcRules.LaunchVelocity(Aim(42f), Speed), Gravity, 0f, Dt, 6f, Floor, points).Landing);
            float other = start.FlatDistanceTo(ThrowArcRules.Simulate(start, ThrowArcRules.LaunchVelocity(Aim(degrees), Speed), Gravity, 0f, Dt, 6f, Floor, points).Landing);
            Assert.That(other, Is.LessThan(best));
        }

        [Test]
        public void Throw_LandsOnTheAnalyticRange_WithinAStepOfFlight()
        {
            var points = new List<ArcVector>();
            var start = new ArcVector(0f, 1.5f, 0f);
            ArcResult r = ThrowArcRules.Simulate(start, ThrowArcRules.LaunchVelocity(Aim(30f), Speed), Gravity, 0f, Dt, 6f, Floor, points);
            double vx = Speed * Math.Cos(Math.PI / 6d), vy = Speed * Math.Sin(Math.PI / 6d);
            double t = (vy + Math.Sqrt((vy * vy) + (2d * Gravity * 1.5d))) / Gravity;
            Assert.That(r.Landing.X, Is.EqualTo(vx * t).Within(vx * Dt * 2d));
            Assert.That(r.Seconds, Is.EqualTo(t).Within(Dt * 2d));
        }

        [Test]
        public void Simulate_RecordsEveryStep_AndEndsAtTheLanding()
        {
            var points = new List<ArcVector> { new ArcVector(9f, 9f, 9f) };
            ArcResult r = ThrowArcRules.Simulate(new ArcVector(0f, 1f, 0f), new ArcVector(5f, 0f, 0f), Gravity, 0f, Dt, 6f, Floor, points);
            Assert.That(points[0].X, Is.EqualTo(0f));
            Assert.That(points.Count, Is.EqualTo(r.Steps + 1));
            Assert.That(points[points.Count - 1].DistanceTo(r.Landing), Is.LessThan(1e-5f));
        }

        [Test]
        public void Simulate_WithNothingToHit_EndsInTheAirAfterTheTimeLimit()
        {
            var points = new List<ArcVector>();
            ArcResult r = ThrowArcRules.Simulate(new ArcVector(0f, 1f, 0f), new ArcVector(5f, 0f, 0f), Gravity, 0f, 0.1f, 1f, (a, b) => null, points);
            Assert.That(r.Landed, Is.False);
            Assert.That(r.Steps, Is.EqualTo(10));
            Assert.That(r.Seconds, Is.EqualTo(1f).Within(1e-5));
            Assert.That(points.Count, Is.EqualTo(11));
        }

        [TestCase(-0.5f, 0f)]
        [TestCase(0.25f, 0.25f)]
        [TestCase(2f, 1f)]
        public void Simulate_PlacesTheLandingAlongTheHitSegment(float fraction, float expectedAlong)
        {
            var points = new List<ArcVector>();
            ArcResult r = ThrowArcRules.Simulate(new ArcVector(0f, 0f, 0f), new ArcVector(10f, 0f, 0f), 0f, 0f, 0.1f, 1f, (a, b) => fraction, points);
            Assert.That(r.Landed, Is.True);
            Assert.That(r.Landing.X, Is.EqualTo(expectedAlong).Within(1e-5));
            Assert.That(r.Seconds, Is.EqualTo(expectedAlong * 0.1f).Within(1e-5));
            Assert.That(r.Steps, Is.EqualTo(1));
        }

        [Test]
        public void Simulate_ASecondStepHit_CountsTheFirstStepsTime()
        {
            var points = new List<ArcVector>();
            int calls = 0;
            ArcResult r = ThrowArcRules.Simulate(new ArcVector(0f, 0f, 0f), new ArcVector(10f, 0f, 0f), 0f, 0f, 0.1f, 1f, (a, b) => ++calls == 2 ? 0.5f : (float?)null, points);
            Assert.That(r.Seconds, Is.EqualTo(0.15f).Within(1e-5));
            Assert.That(r.Landing.X, Is.EqualTo(1.5f).Within(1e-5));
        }

        [TestCase(0f)]
        [TestCase(-0.02f)]
        public void Simulate_AStepOfZeroOrLess_Throws(float dt)
        {
            Assert.That(() => ThrowArcRules.Simulate(new ArcVector(0f, 0f, 0f), new ArcVector(1f, 0f, 0f), 10f, 0f, dt, 1f, Floor, new List<ArcVector>()),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void LaunchVelocity_HasTheThrowSpeed_InTheAimDirection()
        {
            ArcVector v = ThrowArcRules.LaunchVelocity(new ArcVector(0f, 3f, 4f), 10f);
            Assert.That(v.Length, Is.EqualTo(10f).Within(1e-4));
            Assert.That(v.Y, Is.EqualTo(6f).Within(1e-4));
            Assert.That(v.Z, Is.EqualTo(8f).Within(1e-4));
        }

        [Test]
        public void LaunchVelocity_WithoutADirection_IsZero()
        {
            Assert.That(ThrowArcRules.LaunchVelocity(new ArcVector(0f, 0f, 0f), 10f).Length, Is.Zero);
        }

        [TestCase(3.9f, true)]
        [TestCase(4f, true)]
        [TestCase(4.1f, false)]
        public void InBlast_WithinTheRadius(float distance, bool inside)
        {
            Assert.That(ThrowArcRules.InBlast(new ArcVector(0f, 0f, 0f), new ArcVector(0f, 0f, distance), 4f), Is.EqualTo(inside));
        }

        [Test]
        public void ArcVector_Distances_AndText()
        {
            var a = new ArcVector(1f, 2f, 3f);
            var b = new ArcVector(4f, 6f, 3f);
            Assert.That(a.DistanceTo(b), Is.EqualTo(5f).Within(1e-5));
            Assert.That(a.FlatDistanceTo(b), Is.EqualTo(3f).Within(1e-5));
            Assert.That((b - a).ToString(), Is.EqualTo("(3.00, 4.00, 0.00)"));
        }
    }
}
