using System.Globalization;
using GunPowdered.Core;

namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>
    /// Where the player wants the grenade to land, relative to the player and the way the (frozen) camera faces (D-050): a distance on the ground
    /// and a sideways swing in degrees (positive = right). The mouse moves it while the left button is held; the camera does not turn.
    /// </summary>
    internal readonly struct AimSpot
    {
        internal AimSpot(float distance, float sideDegrees)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"AimSpot.AimSpot(distance={RuleLog.Shown(distance)}, sideDegrees={RuleLog.Shown(sideDegrees)})");
            Distance = distance;
            SideDegrees = sideDegrees;
        }

        internal float Distance { get; }
        internal float SideDegrees { get; }

        /// <summary>The spot where the arc already points when aiming starts, kept inside the limits (D-050: nothing jumps when the button goes down).</summary>
        internal static AimSpot Start(float distance, AimTuning tuning) => new AimSpot(Clamp(distance, tuning.MinDistance, tuning.MaxDistance), 0f);

        /// <summary>
        /// Moves the spot by one mouse movement: up/down = farther/nearer, right/left = swing right/left. Expected outcome: the new spot, never outside
        /// MinDistance..MaxDistance or +-MaxSideDegrees.
        /// </summary>
        internal AimSpot Move(float mouseRight, float mouseUp, AimTuning tuning) => new AimSpot(
            Clamp(Distance + (mouseUp * tuning.MetersPerUnit), tuning.MinDistance, tuning.MaxDistance),
            Clamp(SideDegrees + (mouseRight * tuning.DegreesPerUnit), -tuning.MaxSideDegrees, tuning.MaxSideDegrees));

        static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:0.0} m, {1:0.0} deg", Distance, SideDegrees);
    }
}
