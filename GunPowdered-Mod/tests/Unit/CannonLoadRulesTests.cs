using GunPowdered.Features.Cannon.Domain;

namespace GunPowdered.Tests.Unit
{
    /// Loading the Hand Cannon and the Mobile Cannon (user 2026-11-26: no fill animation once the cannon is filled, no refill prompt until it is empty).
    public class CannonLoadRulesTests
    {
        const float Tail = 0.9f;

        [Test]
        public void Step_StopsAtOnce_WhenTheCannonIsAlreadyFilled()
        {
            // the regression: someone else filled the cannon mid-load - the loop kept animating until its own timer ended
            Assert.That(CannonLoadRules.Step(true, 1f, 0f, 4f, Tail), Is.EqualTo(CannonLoadStep.StopFilled));
            Assert.That(CannonLoadRules.Step(true, 3.95f, 5f, 4f, Tail), Is.EqualTo(CannonLoadStep.StopFilled));
        }

        [Test]
        public void Step_Animates_WhenDueAndEnoughTimeIsLeft()
        {
            Assert.That(CannonLoadRules.Step(false, 1.2f, 1.2f, 4f, Tail), Is.EqualTo(CannonLoadStep.Animate));
            Assert.That(CannonLoadRules.Step(false, 1.0f, 1.2f, 4f, Tail), Is.EqualTo(CannonLoadStep.Wait));
        }

        [Test]
        public void Step_StartsNoAnimation_InTheLastTailSeconds()
        {
            // an animation started 0.4 s before the end would still be playing after "loaded"
            Assert.That(CannonLoadRules.Step(false, 3.6f, 3.6f, 4f, Tail), Is.EqualTo(CannonLoadStep.Wait));
            Assert.That(CannonLoadRules.Step(false, 3.0f, 3.0f, 4f, Tail), Is.EqualTo(CannonLoadStep.Animate));
        }

        [Test]
        public void OffersLoad_OnlyForAnEmptyIdleCannon()
        {
            Assert.That(CannonLoadRules.OffersLoad(false, false, false), Is.True);
            Assert.That(CannonLoadRules.OffersLoad(true, false, false), Is.False, "filled: no refill until it is empty");
            Assert.That(CannonLoadRules.OffersLoad(false, false, true), Is.False, "being loaded");
            Assert.That(CannonLoadRules.OffersLoad(true, true, false), Is.False, "fuse burning");
        }
    }
}
