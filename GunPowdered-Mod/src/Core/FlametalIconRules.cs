namespace GunPowdered.Core
{
    /// <summary>
    /// D-191 (user 2026-11-24 17:49: the re-textured items still showed their old pictures in KY-Items, the inventory and the build menu): when the
    /// inventory / build menu icons of the prefabs that FlametalLook re-textured are rendered again from their new look, and how close the icon camera
    /// stands. Rendering at load gave all-white icons in DrainTarPit (textures and light not ready), so the icons are drawn once, a few seconds after the
    /// player is in a world. Pure, so the unit tests check it.
    /// </summary>
    internal static class FlametalIconRules
    {
        /// <summary>Seconds the player must be in the world before the icons are rendered.</summary>
        internal const double DelaySeconds = 3d;

        /// <summary>How much of its square (longer side, 0..1) every icon should fill (user 18:22: "a bit bigger, proportional to the window, not to each other").</summary>
        internal const double TargetFill = 0.9d;

        /// <summary>A first render that fills at least this much (longer side) is kept as it is.</summary>
        internal const double GoodFill = 0.85d;

        /// <summary>The closest the camera may come (fraction of Jotunn's own framing distance).</summary>
        internal const double MinDistance = 0.4d;

        /// <summary>Whether to render now: a player is in the world for at least <see cref="DelaySeconds"/>, the icons were not drawn yet and there is something to draw.</summary>
        internal static bool Due(bool inWorld, double secondsInWorld, bool done, int pending)
        {
            bool due = inWorld && !done && pending > 0 && secondsInWorld >= DelaySeconds;
            if (RuleLog.Due("FlametalIconRules.Due", 2d)) RuleLog.Every("FlametalIconRules.Due", 2d, $"FlametalIconRules.Due(inWorld={inWorld}, secondsInWorld={secondsInWorld:0.0}, done={done}, pending={pending}): returns {due} (delay {DelaySeconds} s)");
            return due;
        }

        /// <summary>
        /// The camera distance for a second render, from how much of the square the first render (at distance 1) filled on its longer side: 1 (keep the
        /// first) when it fills at least <see cref="GoodFill"/> or nothing was measured; otherwise fill / <see cref="TargetFill"/> (the model's size on the
        /// icon grows as the camera comes closer), never below <see cref="MinDistance"/>. gpicons2: the ball filled 0.53, the pieces 0.92-0.93.
        /// </summary>
        internal static double Distance(double longSideFill)
        {
            double d = longSideFill <= 0d || longSideFill >= GoodFill ? 1d : System.Math.Max(MinDistance, longSideFill / TargetFill);
            if (RuleLog.Due("FlametalIconRules.Distance", 2d)) RuleLog.Every("FlametalIconRules.Distance", 2d, $"FlametalIconRules.Distance(longSideFill={longSideFill:0.00}): returns {d:0.00} (target {TargetFill}, good from {GoodFill})");
            return d;
        }
    }
}
