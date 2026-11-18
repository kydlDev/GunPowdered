namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>The launch that sends a grenade onto a chosen spot: a unit direction and the angle above the horizon. Reachable is false when the spot is farther than the throw can go.</summary>
    internal readonly struct LaunchSolution
    {
        internal LaunchSolution(ArcVector direction, float elevationDegrees, bool reachable)
        {
            Direction = direction;
            ElevationDegrees = elevationDegrees;
            Reachable = reachable;
        }

        internal ArcVector Direction { get; }
        internal float ElevationDegrees { get; }
        internal bool Reachable { get; }
    }
}
