namespace GunPowdered.Core
{
    /// <summary>
    /// Where the extension pieces of the automatic batch stations may go (user 2026-11-20, D-140). An extension counts within a 5 m BUBBLE around its
    /// station: up, down and sideways alike (<see cref="InRange"/>). It may never stand ON a station: the build ghost turns red there
    /// (<see cref="IsOnStation"/>), so the station never carries an extension. Stations and extensions carry snap points at ground level in the middle of
    /// each side (<see cref="SideSnapPoints"/>): pieces glue side by side, never on top of each other, and an extension snaps only to its OWN base station
    /// (<see cref="SnapAllowed"/>). Pure: the game adapter passes positions in the station's own space.
    /// </summary>
    internal static class StationPlacementRules
    {
        /// <summary>A ghost this high (m) above the station's foot or more stands on the station, not next to it on the same ground.</summary>
        internal const float GroundTolerance = 0.1f;

        /// <summary>Up to this far (m) above the station's top a ghost inside its footprint still rests on it; higher up it stands on something else (a floor above).</summary>
        internal const float AboveTopTolerance = 0.5f;

        /// <summary>The footprint is shrunk by this much (m) per side, so a piece glued to a side never counts as standing inside it.</summary>
        internal const float EdgeMargin = 0.05f;

        /// <summary>True when the point (dx, dy, dz) away from the station is inside the bubble of this radius. Expected outcome: up, down and sideways count alike; the edge counts.</summary>
        internal static bool InRange(float dx, float dy, float dz, float range)
        {
            bool inside = dx * dx + dy * dy + dz * dz <= range * range;
            if (RuleLog.IsOn()) RuleLog.Every("StationPlacementRules.InRange", 2d, $"StationPlacementRules.InRange({dx:0.00}, {dy:0.00}, {dz:0.00}, {range}): returns {inside}");
            return inside;
        }

        /// <summary>
        /// True when a ghost at (x, y, z) in the station's own space (origin = the station's foot) stands on the station: inside its footprint
        /// (minX..maxX, minZ..maxZ, shrunk by <see cref="EdgeMargin"/>), raised above the station's ground and not higher than its top + <see cref="AboveTopTolerance"/>.
        /// Expected outcome: false beside the station, false inside the footprint at ground level (that is an overlap, the game's clipping test refuses it), false on a floor high above.
        /// </summary>
        internal static bool IsOnStation(float x, float y, float z, float minX, float maxX, float minZ, float maxZ, float topY)
        {
            bool inside = x > minX + EdgeMargin && x < maxX - EdgeMargin && z > minZ + EdgeMargin && z < maxZ - EdgeMargin;
            bool on = inside && y > GroundTolerance && y <= topY + AboveTopTolerance;
            if (RuleLog.IsOn()) RuleLog.Every("StationPlacementRules.IsOnStation", 2d, $"StationPlacementRules.IsOnStation(({x:0.00}, {y:0.00}, {z:0.00}) in x {minX:0.00}..{maxX:0.00}, z {minZ:0.00}..{maxZ:0.00}, top {topY:0.00}): inside {inside}, returns {on}");
            return on;
        }

        /// <summary>
        /// May a build ghost snap onto this snap point? ghostExtensionOf = the station the ghost is an extension of (null: not an extension);
        /// targetStation = the station the point belongs to (null: not a batch station); targetExtensionOf = the station of the extension the point belongs to (null: no extension).
        /// Expected outcome: an extension snaps only to its own base station; a station's points serve only its extensions; an extension's points are never a target.
        /// </summary>
        internal static bool SnapAllowed(string ghostExtensionOf, string targetStation, string targetExtensionOf)
        {
            bool allowed;
            if (targetExtensionOf != null) allowed = false;
            else if (targetStation != null) allowed = ghostExtensionOf == targetStation;
            else allowed = ghostExtensionOf == null;
            if (RuleLog.IsOn()) RuleLog.Every("StationPlacementRules.SnapAllowed", 2d, $"StationPlacementRules.SnapAllowed(ghost of {ghostExtensionOf ?? "-"}, station {targetStation ?? "-"}, extension of {targetExtensionOf ?? "-"}): returns {allowed}");
            return allowed;
        }

        /// <summary>
        /// The snap points of a piece with this footprint (its own space): the middle of each side, at ground level, as x0, z0, x1, z1, ... (4 points: -x, +x, -z, +z).
        /// Expected outcome: 8 numbers; no point on top, so pieces glue side by side only.
        /// </summary>
        internal static float[] SideSnapPoints(float minX, float maxX, float minZ, float maxZ)
        {
            float cx = (minX + maxX) / 2f, cz = (minZ + maxZ) / 2f;
            var points = new[] { minX, cz, maxX, cz, cx, minZ, cx, maxZ };
            if (RuleLog.IsOn()) RuleLog.Every("StationPlacementRules.SideSnapPoints", 2d, $"StationPlacementRules.SideSnapPoints(x {minX:0.00}..{maxX:0.00}, z {minZ:0.00}..{maxZ:0.00}): returns 4 side middles");
            return points;
        }
    }
}
