namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>What a controls step did to the throw, so the game adapter can log it and act on it.</summary>
    internal enum AimEvent
    {
        None,
        Started,
        Thrown,
        Cancelled
    }
}
