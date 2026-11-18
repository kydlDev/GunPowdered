using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Mouse movement while the left button is held moves the landing spot (D-050): up / down = farther / nearer, right / left = swing right / left.
    public class AimSpotTests
    {
        // 0.5 m and 1 degree per unit of mouse movement, 2-15 m, +-60 degrees
        static readonly AimTuning Tuning = new AimTuning(0.5f, 1f, 2f, 15f, 60f);

        [Test]
        public void Start_KeepsTheDistanceTheArcShowed_AndNoSwing()
        {
            AimSpot s = AimSpot.Start(9f, Tuning);
            Assert.That(s.Distance, Is.EqualTo(9f));
            Assert.That(s.SideDegrees, Is.EqualTo(0f));
        }

        [TestCase(0.5f, 2f)]
        [TestCase(40f, 15f)]
        public void Start_KeepsTheDistanceInsideTheLimits(float arcDistance, float expected)
        {
            Assert.That(AimSpot.Start(arcDistance, Tuning).Distance, Is.EqualTo(expected));
        }

        [Test]
        public void MouseUp_MovesTheSpotFarther_ByTheSensitivity()
        {
            AimSpot s = new AimSpot(8f, 0f).Move(0f, 4f, Tuning);
            Assert.That(s.Distance, Is.EqualTo(10f).Within(1e-5));
            Assert.That(s.SideDegrees, Is.EqualTo(0f));
        }

        [Test]
        public void MouseDown_MovesTheSpotNearer()
        {
            Assert.That(new AimSpot(8f, 0f).Move(0f, -4f, Tuning).Distance, Is.EqualTo(6f).Within(1e-5));
        }

        [Test]
        public void MouseRight_SwingsTheSpotRight_MouseLeftSwingsLeft()
        {
            Assert.That(new AimSpot(8f, 0f).Move(10f, 0f, Tuning).SideDegrees, Is.EqualTo(10f).Within(1e-5));
            Assert.That(new AimSpot(8f, 0f).Move(-10f, 0f, Tuning).SideDegrees, Is.EqualTo(-10f).Within(1e-5));
        }

        [Test]
        public void BothAxes_MoveAtOnce_AndAddUpOverSeveralMoves()
        {
            AimSpot s = new AimSpot(8f, 0f).Move(3f, 2f, Tuning).Move(-1f, 2f, Tuning);
            Assert.That(s.Distance, Is.EqualTo(10f).Within(1e-5));
            Assert.That(s.SideDegrees, Is.EqualTo(2f).Within(1e-5));
        }

        [Test]
        public void Distance_NeverLeavesTheLimits()
        {
            Assert.That(new AimSpot(14f, 0f).Move(0f, 100f, Tuning).Distance, Is.EqualTo(15f));
            Assert.That(new AimSpot(3f, 0f).Move(0f, -100f, Tuning).Distance, Is.EqualTo(2f));
        }

        [Test]
        public void Swing_NeverLeavesTheLimits()
        {
            Assert.That(new AimSpot(8f, 55f).Move(100f, 0f, Tuning).SideDegrees, Is.EqualTo(60f));
            Assert.That(new AimSpot(8f, -55f).Move(-100f, 0f, Tuning).SideDegrees, Is.EqualTo(-60f));
        }

        [Test]
        public void NoMouseMovement_ChangesNothing()
        {
            AimSpot s = new AimSpot(8f, 12f).Move(0f, 0f, Tuning);
            Assert.That(s.Distance, Is.EqualTo(8f));
            Assert.That(s.SideDegrees, Is.EqualTo(12f));
        }

        [Test]
        public void Sensitivity_ScalesTheMovement()
        {
            var fine = new AimTuning(0.1f, 0.2f, 2f, 15f, 60f);
            AimSpot s = new AimSpot(8f, 0f).Move(10f, 10f, fine);
            Assert.That(s.Distance, Is.EqualTo(9f).Within(1e-5));
            Assert.That(s.SideDegrees, Is.EqualTo(2f).Within(1e-5));
        }

        [Test]
        public void ToString_ShowsMetresAndDegrees()
        {
            Assert.That(new AimSpot(8.25f, -3f).ToString(), Is.EqualTo("8.3 m, -3.0 deg").Or.EqualTo("8.2 m, -3.0 deg"));
        }
    }
}
