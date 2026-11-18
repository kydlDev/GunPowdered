namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>Where the grenade throw is (D-050): nothing pressed, left button held and aiming, or cancelled with the right button until the left one is let go.</summary>
    internal enum AimPhase
    {
        Idle,
        Aiming,
        Cancelled
    }
}
