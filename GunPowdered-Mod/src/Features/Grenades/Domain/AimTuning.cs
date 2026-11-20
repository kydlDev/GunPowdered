using GunPowdered.Core;
namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>How mouse movement moves the landing spot and how far it may go (D-050; every value is a Tunable in the game adapter).</summary>
    internal readonly struct AimTuning
    {
        internal AimTuning(float metersPerUnit, float degreesPerUnit, float minDistance, float maxDistance, float maxSideDegrees)
        {
            if (RuleLog.On) RuleLog.Verbose($"AimTuning.AimTuning(metersPerUnit={metersPerUnit}, degreesPerUnit={degreesPerUnit}, minDistance={minDistance}, maxDistance={maxDistance}, maxSideDegrees={maxSideDegrees})");
            MetersPerUnit = metersPerUnit;
            DegreesPerUnit = degreesPerUnit;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            MaxSideDegrees = maxSideDegrees;
        }

        /// <summary>Metres the spot moves farther per unit of mouse movement up (nearer for down).</summary>
        internal float MetersPerUnit { get; }

        /// <summary>Degrees the spot swings to the right per unit of mouse movement right (to the left for left).</summary>
        internal float DegreesPerUnit { get; }

        internal float MinDistance { get; }
        internal float MaxDistance { get; }

        /// <summary>The spot never swings further than this to either side of where the player faces.</summary>
        internal float MaxSideDegrees { get; }
    }
}
