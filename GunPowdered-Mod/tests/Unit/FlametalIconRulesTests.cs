using GunPowdered.Core;
using NUnit.Framework;

namespace GunPowdered.Tests.Unit
{
    /// D-191 (user 2026-11-24): the icons of the re-textured flametal prefabs are rendered once, a few seconds after the player is in a world.
    public class FlametalIconRulesTests
    {
        [TestCase(true, 3.0, false, 4, true, TestName = "Due: in the world for the delay, not drawn yet, four to draw")]
        [TestCase(true, 2.9, false, 4, false, TestName = "Due: not before the delay (white icons while textures load)")]
        [TestCase(false, 10.0, false, 4, false, TestName = "Due: never in the main menu")]
        [TestCase(true, 10.0, true, 4, false, TestName = "Due: only once per game start")]
        [TestCase(true, 10.0, false, 0, false, TestName = "Due: nothing re-textured, nothing to draw")]
        [TestCase(true, 60.0, false, 1, true, TestName = "Due: one prefab is enough")]
        public void Due(bool inWorld, double seconds, bool done, int pending, bool expected)
        {
            Assert.That(FlametalIconRules.Due(inWorld, seconds, done, pending), Is.EqualTo(expected));
        }

        [Test]
        public void Delay_IsThreeSeconds()
        {
            Assert.That(FlametalIconRules.DelaySeconds, Is.EqualTo(3d));
        }

        [TestCase(0.53, 0.53 / 0.9, TestName = "Distance: the ball (gpicons2, 53 %) comes closer so it fills 90 %")]
        [TestCase(0.93, 1.0, TestName = "Distance: a piece that already fills 93 % keeps its first render")]
        [TestCase(0.85, 1.0, TestName = "Distance: exactly the good fill is kept")]
        [TestCase(0.84, 0.84 / 0.9, TestName = "Distance: just below the good fill is redrawn")]
        [TestCase(0.1, 0.4, TestName = "Distance: never closer than 0.4 (a tiny model is not blown up past its edges)")]
        [TestCase(0.0, 1.0, TestName = "Distance: nothing visible measured, keep the first render")]
        [TestCase(-1.0, 1.0, TestName = "Distance: no measurement, keep the first render")]
        public void Distance(double fill, double expected)
        {
            Assert.That(FlametalIconRules.Distance(fill), Is.EqualTo(expected).Within(1e-9));
        }
    }
}
