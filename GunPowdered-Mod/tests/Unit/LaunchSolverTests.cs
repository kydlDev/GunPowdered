using System;
using System.Collections.Generic;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The launch that sends the grenade onto the chosen landing spot (D-050), checked against the same flight steps the game and the drawn arc use.
    public class LaunchSolverTests
    {
        const float Speed = 12f, Gravity = 10f;
        // the solver knows the game steps the flight in whole physics steps, so the flight lands on the spot (a hair of tolerance for the step grid)
        const float StepTolerance = 0.15f;

        static float? Floor(ArcVector from, ArcVector to)
        {
            if (to.Y > 0f) return null;
            return from.Y <= 0f ? 0f : from.Y / (from.Y - to.Y);
        }

        // ground at height h that only catches a descending flight (a hill top / a pit floor the grenade comes down onto)
        static Func<ArcVector, ArcVector, float?> Plane(float h) =>
            (from, to) => to.Y < from.Y && to.Y <= h ? (from.Y <= h ? 0f : (from.Y - h) / (from.Y - to.Y)) : (float?)null;

        static ArcVector Fly(ArcVector spawn, LaunchSolution s)
        {
            ArcVector v = ThrowArcRules.LaunchVelocity(s.Direction, Speed);
            ArcResult r = ThrowArcRules.Simulate(spawn, v, Gravity, 0f, ThrowArcRules.DefaultStep, 6f, Floor, new List<ArcVector>());
            Assert.That(r.Landed, Is.True);
            return r.Landing;
        }

        [TestCase(5f)]
        [TestCase(10f)]
        [TestCase(14f)]
        public void FlatGround_TheFlightLandsOnTheSpot(float distance)
        {
            var spawn = new ArcVector(0f, 1.5f, 0f);
            var target = new ArcVector(distance, 0f, 0f);
            LaunchSolution s = LaunchSolver.Solve(spawn, target, Speed, Gravity, ThrowArcRules.DefaultStep);
            Assert.That(s.Reachable, Is.True);
            Assert.That(Fly(spawn, s).FlatDistanceTo(target), Is.LessThan(StepTolerance));
        }

        [Test]
        public void SpotToTheSide_TheFlightLandsOnIt()
        {
            var spawn = new ArcVector(2f, 1.5f, -3f);
            var target = new ArcVector(-4f, 0f, 6f);
            LaunchSolution s = LaunchSolver.Solve(spawn, target, Speed, Gravity, ThrowArcRules.DefaultStep);
            Assert.That(Fly(spawn, s).FlatDistanceTo(target), Is.LessThan(StepTolerance));
        }

        [Test]
        public void SpotOnAHill_AboveTheThrower_StillHit()
        {
            var spawn = new ArcVector(0f, 1.5f, 0f);
            var target = new ArcVector(8f, 4f, 0f);   // a 2.5 m rise
            LaunchSolution s = LaunchSolver.Solve(spawn, target, Speed, Gravity, ThrowArcRules.DefaultStep);
            Assert.That(s.Reachable, Is.True);
            ArcResult r = ThrowArcRules.Simulate(spawn, ThrowArcRules.LaunchVelocity(s.Direction, Speed), Gravity, 0f, ThrowArcRules.DefaultStep, 6f, Plane(4f), new List<ArcVector>());
            Assert.That(r.Landed, Is.True);
            Assert.That(r.Landing.FlatDistanceTo(target), Is.LessThan(StepTolerance));
        }

        [Test]
        public void SpotInAPit_BelowTheThrower_StillHit()
        {
            var spawn = new ArcVector(0f, 1.5f, 0f);
            var target = new ArcVector(10f, -3f, 0f);
            LaunchSolution s = LaunchSolver.Solve(spawn, target, Speed, Gravity, ThrowArcRules.DefaultStep);
            Assert.That(s.Reachable, Is.True);
            ArcResult r = ThrowArcRules.Simulate(spawn, ThrowArcRules.LaunchVelocity(s.Direction, Speed), Gravity, 0f, ThrowArcRules.DefaultStep, 6f, Plane(-3f), new List<ArcVector>());
            Assert.That(r.Landing.FlatDistanceTo(target), Is.LessThan(StepTolerance));
        }

        [Test]
        public void TheLowArcIsUsed_NotTheLobbed_Arc()
        {
            LaunchSolution s = LaunchSolver.Solve(new ArcVector(0f, 1.5f, 0f), new ArcVector(10f, 0f, 0f), Speed, Gravity);
            Assert.That(s.ElevationDegrees, Is.LessThan(45f));
            Assert.That(s.ElevationDegrees, Is.GreaterThan(0f));
        }

        [Test]
        public void FartherSpots_NeedAHigherAngle()
        {
            var spawn = new ArcVector(0f, 1.5f, 0f);
            float near = LaunchSolver.Solve(spawn, new ArcVector(5f, 0f, 0f), Speed, Gravity).ElevationDegrees;
            float far = LaunchSolver.Solve(spawn, new ArcVector(13f, 0f, 0f), Speed, Gravity).ElevationDegrees;
            Assert.That(far, Is.GreaterThan(near));
        }

        [Test]
        public void SpotBeyondTheReach_IsNotReachable_AndGoesAsFarAsItCan()
        {
            var spawn = new ArcVector(0f, 0f, 0f);
            LaunchSolution s = LaunchSolver.Solve(spawn, new ArcVector(30f, 0f, 0f), Speed, Gravity);
            Assert.That(s.Reachable, Is.False);
            // v^2 / (g d) = 144 / 300 -> about 25.6 degrees
            Assert.That(s.ElevationDegrees, Is.EqualTo(Math.Atan(144d / 300d) * 180d / Math.PI).Within(1e-3));
        }

        [Test]
        public void SpotJustInsideTheReach_IsReachable_JustOutsideIsNot()
        {
            var spawn = new ArcVector(0f, 0f, 0f);
            float maxRange = Speed * Speed / Gravity;   // 14.4 m on level ground
            Assert.That(LaunchSolver.Solve(spawn, new ArcVector(maxRange - 0.05f, 0f, 0f), Speed, Gravity).Reachable, Is.True);
            Assert.That(LaunchSolver.Solve(spawn, new ArcVector(maxRange + 0.05f, 0f, 0f), Speed, Gravity).Reachable, Is.False);
        }

        [Test]
        public void TheDirectionIsAUnitVectorTowardsTheSpot()
        {
            LaunchSolution s = LaunchSolver.Solve(new ArcVector(1f, 1.5f, 1f), new ArcVector(-5f, 0f, 7f), Speed, Gravity);
            Assert.That(s.Direction.Length, Is.EqualTo(1f).Within(1e-4));
            Assert.That(s.Direction.X, Is.LessThan(0f));
            Assert.That(s.Direction.Z, Is.GreaterThan(0f));
            Assert.That(s.Direction.Y, Is.GreaterThan(0f));
        }

        [Test]
        public void SpotRightUnderTheThrower_ThrowsStraightUp()
        {
            LaunchSolution s = LaunchSolver.Solve(new ArcVector(0f, 1.5f, 0f), new ArcVector(0f, 0f, 0.001f), Speed, Gravity);
            Assert.That(s.Direction.Y, Is.EqualTo(1f));
            Assert.That(s.ElevationDegrees, Is.EqualTo(90f));
            Assert.That(s.Reachable, Is.True);
        }

        [Test]
        public void SpotJustAboveTheMinimumFlatDistance_IsSolvedNormally()
        {
            LaunchSolution s = LaunchSolver.Solve(new ArcVector(0f, 0f, 0f), new ArcVector(LaunchSolver.MinFlatDistance * 1.5f, 0f, 0f), Speed, Gravity);
            Assert.That(s.ElevationDegrees, Is.LessThan(90f));
        }

        [Test]
        public void WithoutTheStep_AShallowLandingIsMissed_WhichIsWhyTheStepIsPartOfTheSolution()
        {
            var spawn = new ArcVector(0f, 1.5f, 0f);
            var target = new ArcVector(8f, 4f, 0f);
            LaunchSolution exact = LaunchSolver.Solve(spawn, target, Speed, Gravity);
            ArcResult r = ThrowArcRules.Simulate(spawn, ThrowArcRules.LaunchVelocity(exact.Direction, Speed), Gravity, 0f, ThrowArcRules.DefaultStep, 6f, Plane(4f), new List<ArcVector>());
            Assert.That(r.Landing.FlatDistanceTo(target), Is.GreaterThan(0.5f));
        }

        [Test]
        public void StepZero_IsTheExactParabola_LikeTheShortOverload()
        {
            var spawn = new ArcVector(0f, 1.5f, 0f);
            var target = new ArcVector(9f, 0f, 3f);
            LaunchSolution a = LaunchSolver.Solve(spawn, target, Speed, Gravity);
            LaunchSolution b = LaunchSolver.Solve(spawn, target, Speed, Gravity, 0f);
            Assert.That(b.ElevationDegrees, Is.EqualTo(a.ElevationDegrees));
            Assert.That(b.Direction.Z, Is.EqualTo(a.Direction.Z));
        }

        [Test]
        public void ABiggerStep_NeedsAHigherAngle_ForTheSameSpot()
        {
            var spawn = new ArcVector(0f, 1.5f, 0f);
            var target = new ArcVector(8f, 4f, 0f);
            float exact = LaunchSolver.Solve(spawn, target, Speed, Gravity, 0f).ElevationDegrees;
            float stepped = LaunchSolver.Solve(spawn, target, Speed, Gravity, ThrowArcRules.DefaultStep).ElevationDegrees;
            Assert.That(stepped, Is.GreaterThan(exact));
        }

        [Test]
        public void ANegativeStep_IsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchSolver.Solve(new ArcVector(0f, 0f, 0f), new ArcVector(5f, 0f, 0f), Speed, Gravity, -0.01f));
        }

        [TestCase(0f, 10f)]
        [TestCase(-1f, 10f)]
        [TestCase(12f, 0f)]
        [TestCase(12f, -2f)]
        public void NoSpeedOrNoGravity_IsRefused(float speed, float gravity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LaunchSolver.Solve(new ArcVector(0f, 0f, 0f), new ArcVector(5f, 0f, 0f), speed, gravity));
        }
    }
}
