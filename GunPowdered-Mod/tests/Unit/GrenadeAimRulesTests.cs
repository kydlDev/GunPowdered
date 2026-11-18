using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Tests.Unit
{
    /// The Spiked Grenade controls (D-050): hold the left button to aim, let go to throw, the right button while aiming cancels, the right button
    /// with the grenade only in hand is the normal block.
    public class GrenadeAimRulesTests
    {
        [Test]
        public void Idle_NoButton_LeavesTheControlsAlone()
        {
            AimStep s = GrenadeAimRules.Next(AimPhase.Idle, false, false);
            Assert.That(s.Next, Is.EqualTo(AimPhase.Idle));
            Assert.That(s.Intercept, Is.False);
            Assert.That(s.Happened, Is.EqualTo(AimEvent.None));
        }

        [Test]
        public void Idle_RightButtonOnly_IsTheNormalBlock()
        {
            AimStep s = GrenadeAimRules.Next(AimPhase.Idle, false, true);
            Assert.That(s.Intercept, Is.False, "the block must reach the game untouched");
            Assert.That(s.SuppressBlock, Is.False);
            Assert.That(s.Next, Is.EqualTo(AimPhase.Idle));
        }

        [Test]
        public void Idle_LeftButtonDown_StartsAimingWithoutThrowing()
        {
            AimStep s = GrenadeAimRules.Next(AimPhase.Idle, true, false);
            Assert.That(s.Next, Is.EqualTo(AimPhase.Aiming));
            Assert.That(s.Happened, Is.EqualTo(AimEvent.Started));
            Assert.That(s.Intercept, Is.True);
            Assert.That(s.Attack, Is.False, "no throw while the button is down");
            Assert.That(s.SuppressBlock, Is.True);
        }

        [Test]
        public void Aiming_ButtonStillDown_KeepsAimingAndNeverThrows()
        {
            AimStep s = GrenadeAimRules.Next(AimPhase.Aiming, true, false);
            Assert.That(s.Next, Is.EqualTo(AimPhase.Aiming));
            Assert.That(s.Happened, Is.EqualTo(AimEvent.None));
            Assert.That(s.Attack, Is.False);
            Assert.That(s.Intercept, Is.True);
            Assert.That(s.SuppressBlock, Is.True);
        }

        [Test]
        public void Aiming_ButtonLetGo_ThrowsOnce()
        {
            AimStep s = GrenadeAimRules.Next(AimPhase.Aiming, false, false);
            Assert.That(s.Next, Is.EqualTo(AimPhase.Idle));
            Assert.That(s.Happened, Is.EqualTo(AimEvent.Thrown));
            Assert.That(s.Attack, Is.True);
            Assert.That(s.Intercept, Is.True);
            Assert.That(s.SuppressBlock, Is.True, "a block on the throw step must not interrupt the throw");
        }

        [Test]
        public void Aiming_RightButtonPressed_CancelsWithoutThrowing()
        {
            AimStep s = GrenadeAimRules.Next(AimPhase.Aiming, true, true);
            Assert.That(s.Next, Is.EqualTo(AimPhase.Cancelled));
            Assert.That(s.Happened, Is.EqualTo(AimEvent.Cancelled));
            Assert.That(s.Attack, Is.False);
            Assert.That(s.SuppressBlock, Is.True, "the cancel click is not also a block");
        }

        [Test]
        public void Aiming_RightButtonAndLeftLetGoInTheSameStep_CancelWins()
        {
            AimStep s = GrenadeAimRules.Next(AimPhase.Aiming, false, true);
            Assert.That(s.Next, Is.EqualTo(AimPhase.Cancelled));
            Assert.That(s.Attack, Is.False);
            Assert.That(s.Happened, Is.EqualTo(AimEvent.Cancelled));
        }

        [Test]
        public void Cancelled_LeftButtonStillDown_SwallowsTheClickButLetsABlockThrough()
        {
            AimStep s = GrenadeAimRules.Next(AimPhase.Cancelled, true, false);
            Assert.That(s.Next, Is.EqualTo(AimPhase.Cancelled));
            Assert.That(s.Intercept, Is.True);
            Assert.That(s.Attack, Is.False, "no throw after a cancel");
            Assert.That(s.SuppressBlock, Is.False);
        }

        [Test]
        public void Cancelled_LeftButtonLetGo_ReturnsToIdleWithoutThrowing()
        {
            AimStep s = GrenadeAimRules.Next(AimPhase.Cancelled, false, false);
            Assert.That(s.Next, Is.EqualTo(AimPhase.Idle));
            Assert.That(s.Intercept, Is.False);
            Assert.That(s.Attack, Is.False);
            Assert.That(s.Happened, Is.EqualTo(AimEvent.None));
        }

        [Test]
        public void HoldAndRelease_GivesExactlyOneThrow()
        {
            AimPhase phase = AimPhase.Idle;
            int throws = 0, attacks = 0, started = 0;
            bool[] held = { false, true, true, true, true, false, false, false };
            foreach (bool h in held)
            {
                AimStep s = GrenadeAimRules.Next(phase, h, false);
                phase = s.Next;
                if (s.Happened == AimEvent.Thrown) throws++;
                if (s.Happened == AimEvent.Started) started++;
                if (s.Intercept && s.Attack) attacks++;
            }
            Assert.That(started, Is.EqualTo(1));
            Assert.That(throws, Is.EqualTo(1));
            Assert.That(attacks, Is.EqualTo(1));
            Assert.That(phase, Is.EqualTo(AimPhase.Idle));
        }

        [Test]
        public void CancelThenLetGo_ThrowsNothing_AndTheNextPressAimsAgain()
        {
            AimPhase phase = AimPhase.Idle;
            bool anyAttack = false;
            // press, hold, right click, still held, let go
            var steps = new[] { (true, false), (true, false), (true, true), (true, false), (false, false) };
            foreach ((bool held, bool block) in steps)
            {
                AimStep s = GrenadeAimRules.Next(phase, held, block);
                phase = s.Next;
                anyAttack |= s.Intercept && s.Attack;
            }
            Assert.That(anyAttack, Is.False, "a cancelled aim never throws");
            Assert.That(phase, Is.EqualTo(AimPhase.Idle));
            Assert.That(GrenadeAimRules.Next(phase, true, false).Happened, Is.EqualTo(AimEvent.Started));
        }
    }
}
