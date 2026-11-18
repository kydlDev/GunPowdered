namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>
    /// The Spiked Grenade's throw controls (D-050): hold the left button to aim, let go to throw, the right button while aiming cancels, the right
    /// button with the grenade only in hand stays the normal block. Pure: one call per controls update, the game adapter applies the answer.
    /// </summary>
    internal static class GrenadeAimRules
    {
        /// <summary>
        /// One controls step. attackHeld = the left button is down; blockPressed = the right button went down this step.
        /// Expected outcome: Idle + button down starts aiming (no throw); Aiming + button up throws once; Aiming + right press cancels (the left button
        /// is then ignored until it is let go); everything else leaves the controls alone.
        /// </summary>
        internal static AimStep Next(AimPhase phase, bool attackHeld, bool blockPressed)
        {
            switch (phase)
            {
                case AimPhase.Aiming:
                    if (blockPressed) return new AimStep(AimPhase.Cancelled, true, false, true, AimEvent.Cancelled);
                    if (attackHeld) return new AimStep(AimPhase.Aiming, true, false, true, AimEvent.None);
                    return new AimStep(AimPhase.Idle, true, true, true, AimEvent.Thrown);
                case AimPhase.Cancelled:
                    return attackHeld ? new AimStep(AimPhase.Cancelled, true, false, false, AimEvent.None) : new AimStep(AimPhase.Idle, false, false, false, AimEvent.None);
                default:
                    return attackHeld ? new AimStep(AimPhase.Aiming, true, false, true, AimEvent.Started) : new AimStep(AimPhase.Idle, false, false, false, AimEvent.None);
            }
        }
    }
}
