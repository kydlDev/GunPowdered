using GunPowdered.Core;

namespace GunPowdered.Features.Dynamite.Domain
{
    /// <summary>
    /// D-182 (user 2026-11-24): where the hole of a blast starts. The game lowers the ground only below the height the terrain operation stands at
    /// (TerrainComp.RaiseTerrain: target = op height + delta, skipped when that is above the ground), so a charge lying on a rock dug nothing: the pack at
    /// y 69.4 on a rock, 2 m down, never reached the ground. The hole is now measured from the GROUND under the charge, as long as that ground is within the
    /// blast's depth below the charge (dynamite 3 m, pack 5 m, grenade 1.5 m). Pure, so the unit tests check it.
    /// </summary>
    internal static class CraterRules
    {
        /// <summary>Default hole depths (m) the user set: dynamite 3, pack 5, grenade 1.5.</summary>
        internal const float StickDepth = 3f, PackDepth = 5f, GrenadeDepth = 1.5f;

        /// <summary>
        /// The height the hole starts at: the ground under the charge, or the charge's own height when it lies below the sampled ground (in a hole,
        /// under an overhang). Returns NaN when the ground is more than <paramref name="depth"/> m below the charge: the blast does not reach it.
        /// </summary>
        internal static float Top(float chargeY, float groundY, float depth)
        {
            float top;
            if (float.IsNaN(groundY) || chargeY - groundY > depth) top = float.NaN;
            else top = groundY < chargeY ? groundY : chargeY;
            if (RuleLog.Due("CraterRules.Top", 2d)) RuleLog.Every("CraterRules.Top", 2d, $"CraterRules.Top(chargeY={RuleLog.Shown(chargeY)}, groundY={RuleLog.Shown(groundY)}, depth={RuleLog.Shown(depth)}): returns {RuleLog.Shown(top)} ({(float.IsNaN(top) ? "ground out of reach" : "hole from there")})");
            return top;
        }
    }
}
