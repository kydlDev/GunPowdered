using GunPowdered.Core;
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
            if (RuleLog.Due("GrenadeAimRules.Next", 2d)) RuleLog.Every("GrenadeAimRules.Next", 2d, $"GrenadeAimRules.Next(phase={RuleLog.Shown(phase)}, attackHeld={RuleLog.Shown(attackHeld)}, blockPressed={RuleLog.Shown(blockPressed)})");
            switch (phase)
            {
                case AimPhase.Aiming:
                    if (blockPressed) { var returnedAt21 = new AimStep(AimPhase.Cancelled, true, false, true, AimEvent.Cancelled); if (RuleLog.Due("GrenadeAimRules.Next:21", 2d)) RuleLog.Every("GrenadeAimRules.Next:21", 2d, $"GrenadeAimRules.Next: returns {RuleLog.Shown(returnedAt21)} (line 21)"); return returnedAt21; }
                    if (attackHeld) { var returnedAt22 = new AimStep(AimPhase.Aiming, true, false, true, AimEvent.None); if (RuleLog.Due("GrenadeAimRules.Next:22", 2d)) RuleLog.Every("GrenadeAimRules.Next:22", 2d, $"GrenadeAimRules.Next: returns {RuleLog.Shown(returnedAt22)} (line 22)"); return returnedAt22; }
                    { var returnedAt23 = new AimStep(AimPhase.Idle, true, true, true, AimEvent.Thrown); if (RuleLog.Due("GrenadeAimRules.Next:23", 2d)) RuleLog.Every("GrenadeAimRules.Next:23", 2d, $"GrenadeAimRules.Next: returns {RuleLog.Shown(returnedAt23)} (line 23)"); return returnedAt23; }
                case AimPhase.Cancelled:
                    { var returnedAt25 = attackHeld ? new AimStep(AimPhase.Cancelled, true, false, false, AimEvent.None) : new AimStep(AimPhase.Idle, false, false, false, AimEvent.None); if (RuleLog.Due("GrenadeAimRules.Next:25", 2d)) RuleLog.Every("GrenadeAimRules.Next:25", 2d, $"GrenadeAimRules.Next: returns {RuleLog.Shown(returnedAt25)} (line 25)"); return returnedAt25; }
                default:
                    { var returnedAt27 = attackHeld ? new AimStep(AimPhase.Aiming, true, false, true, AimEvent.Started) : new AimStep(AimPhase.Idle, false, false, false, AimEvent.None); if (RuleLog.Due("GrenadeAimRules.Next:27", 2d)) RuleLog.Every("GrenadeAimRules.Next:27", 2d, $"GrenadeAimRules.Next: returns {RuleLog.Shown(returnedAt27)} (line 27)"); return returnedAt27; }
            }
        }
    }
}
