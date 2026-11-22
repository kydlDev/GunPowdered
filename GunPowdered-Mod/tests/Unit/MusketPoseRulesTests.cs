using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The musket arm poses (D-092): which authored pose each action and moment gives.
    public class MusketPoseRulesTests
    {
        static float[] Run(MusketAction action, float seconds, float carry)
        {
            var into = new float[MusketPoses.Count];
            Assert.That(MusketPoseRules.Evaluate(action, seconds, carry, into), Is.True);
            return into;
        }

        // ---- D-098 action counter: which counter changes start an action on another player's rig

        [Test]
        public void IsNewAction_FirstLookWithoutHistory_RemembersZeroSoTheFirstShotPlays()
        {
            bool start = MusketPoseRules.IsNewAction(MusketPoseRules.NoActionYet, -1, out int seen);
            bool firstShot = MusketPoseRules.IsNewAction(1, seen, out int after);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(start, Is.False);
                Assert.That(seen, Is.Zero);
                Assert.That(firstShot, Is.True, "the shooter's very first shot must show (a17bjornita3: it was swallowed)");
                Assert.That(after, Is.EqualTo(1));
            }
        }

        [Test]
        public void IsNewAction_FirstLookAtAShooterWithHistory_DoesNotReplayTheOldAction()
        {
            bool start = MusketPoseRules.IsNewAction(5, -1, out int seen);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(start, Is.False);
                Assert.That(seen, Is.EqualTo(5));
            }
        }

        [Test]
        public void IsNewAction_SameCounter_DoesNotRestart()
        {
            Assert.That(MusketPoseRules.IsNewAction(3, 3, out int seen), Is.False);
            Assert.That(seen, Is.EqualTo(3));
        }

        [Test]
        public void IsNewAction_NextCounter_Starts()
        {
            Assert.That(MusketPoseRules.IsNewAction(4, 3, out int seen), Is.True);
            Assert.That(seen, Is.EqualTo(4));
        }

        [Test]
        public void IsNewAction_CounterGoneAfterAHistory_KeepsWhatWasSeen()
        {
            Assert.That(MusketPoseRules.IsNewAction(MusketPoseRules.NoActionYet, 7, out int seen), Is.False);
            Assert.That(seen, Is.EqualTo(7));
        }

        [Test]
        public void Poses_HaveOneValuePerMuscle()
        {
            foreach (float[] p in new[] { MusketPoses.Hold, MusketPoses.Recoil, MusketPoses.Carry, MusketPoses.ReloadLow, MusketPoses.ReloadHigh })
                Assert.That(p.Length, Is.EqualTo(MusketPoses.Count));
        }

        [Test]
        public void Evaluate_TooShortArray_IsRefused()
        {
            Assert.That(MusketPoseRules.Evaluate(MusketAction.Hold, 0f, 0f, new float[3]), Is.False);
        }

        [Test]
        public void Hold_Standing_IsTheHoldPose()
        {
            Assert.That(Run(MusketAction.Hold, 5f, 0f), Is.EqualTo(MusketPoses.Hold).Within(1e-5f));
        }

        [Test]
        public void Hold_Running_IsTheCarryPose()
        {
            Assert.That(Run(MusketAction.Hold, 0f, 1f), Is.EqualTo(MusketPoses.Carry).Within(1e-5f));
        }

        [Test]
        public void Fire_StartsAtTheRecoil_AndEndsBackInTheHold()
        {
            Assert.That(Run(MusketAction.Fire, 0f, 0f), Is.EqualTo(MusketPoses.Recoil).Within(1e-5f));
            Assert.That(Run(MusketAction.Fire, MusketPoseRules.FireSeconds, 0f), Is.EqualTo(MusketPoses.Hold).Within(1e-5f));
        }

        [Test]
        public void Reload_StartsInTheHold_ThenStrokesBetweenLowAndHigh()
        {
            Assert.That(Run(MusketAction.Reload, 0f, 0f), Is.EqualTo(MusketPoses.Hold).Within(1e-5f));
            Assert.That(Run(MusketAction.Reload, MusketPoseRules.ReloadSecondsPerCartridge, 0f), Is.EqualTo(MusketPoses.ReloadLow).Within(1e-4f));
            Assert.That(Run(MusketAction.Reload, MusketPoseRules.ReloadSecondsPerCartridge * 1.5f, 0f), Is.EqualTo(MusketPoses.ReloadHigh).Within(1e-4f));
        }

        [TestCase(0f, 0f)]
        [TestCase(0.6f, 1f)]
        [TestCase(1.2f, 0f)]
        [TestCase(-1f, 0f)]
        public void Stroke_OnePerCartridge(float seconds, float expected)
        {
            Assert.That(MusketPoseRules.Stroke(seconds), Is.EqualTo(expected).Within(1e-4f));
        }

        [TestCase(0f, 6f, 0.1f, 0.4f)]
        [TestCase(1f, 2f, 0.1f, 0.6f)]
        [TestCase(0.9f, 6f, 1f, 1f)]
        [TestCase(0.2f, 0f, 1f, 0f)]
        [TestCase(0.5f, 6f, 0f, 0.5f)]
        public void NextCarry_FollowsTheRunningSpeed(float carry, float speed, float delta, float expected)
        {
            Assert.That(MusketPoseRules.NextCarry(carry, speed, delta), Is.EqualTo(expected).Within(1e-4f));
        }
        [Test]
        public void ReloadWeight_GoesOverInTheBlendTime_OnlyWhileReloading()
        {
            Assert.That(MusketPoseRules.ReloadWeight(MusketAction.Reload, 0f), Is.EqualTo(0f));
            Assert.That(MusketPoseRules.ReloadWeight(MusketAction.Reload, MusketPoseRules.BlendSeconds), Is.EqualTo(1f));
            Assert.That(MusketPoseRules.ReloadWeight(MusketAction.Hold, 5f), Is.EqualTo(0f));
            Assert.That(MusketPoseRules.ReloadWeight(MusketAction.Fire, 5f), Is.EqualTo(0f));
        }

        [Test]
        public void RodAboveMuzzle_StrokesBetweenLowAndHigh()
        {
            Assert.That(MusketPoseRules.RodAboveMuzzle(0f), Is.EqualTo(MusketPoses.RodLow).Within(1e-5f));
            Assert.That(MusketPoseRules.RodAboveMuzzle(MusketPoseRules.ReloadSecondsPerCartridge / 2f), Is.EqualTo(MusketPoses.RodHigh).Within(1e-4f));
        }

        [Test]
        public void Grips_HavePositionAndRotation()
        {
            Assert.That(MusketPoses.GripHold.Length, Is.EqualTo(6));
            Assert.That(MusketPoses.GripReload.Length, Is.EqualTo(6));
        }

        // D-099: the steadied spine while aiming
        [TestCase(0, 0f, 1f)]
        [TestCase(1, 0f, 1f)]
        [TestCase(0, 1f, 0f)]
        [TestCase(0, 0.25f, 0.75f)]
        [TestCase(2, 0f, 0f)]
        public void SpineSteadiness_FullWhileAiming_NoneWhileRunningOrReloading(int action, float carry, float expected)
        {
            Assert.That(MusketPoseRules.SpineSteadiness((MusketAction)action, carry), Is.EqualTo(expected).Within(1e-5f));
        }

        [TestCase(0f, 1.5f, 0f)]
        [TestCase(-1f, 1.5f, 0f)]
        [TestCase(0.1f, 0f, 1f)]
        [TestCase(1.5f, 1.5f, 0.6321f)]
        [TestCase(100f, 1.5f, 1f)]
        public void FollowFactor_IsTheExponentialShareOfTheWay(float dt, float seconds, float expected)
        {
            Assert.That(MusketPoseRules.FollowFactor(dt, seconds), Is.EqualTo(expected).Within(1e-3f));
        }

        [Test]
        public void FollowFactor_ManySmallStepsEqualOneBigStep()
        {
            float left = 1f;
            for (int i = 0; i < 100; i++) left *= 1f - MusketPoseRules.FollowFactor(0.015f, 1.5f);
            Assert.That(1f - left, Is.EqualTo(MusketPoseRules.FollowFactor(1.5f, 1.5f)).Within(1e-3f));
        }    }
}
