using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>
    /// The launch direction that makes a grenade of fixed speed land on a chosen spot (D-050), for the flight ThrowArcRules steps (gravity, no drag):
    /// the low arc of the classic projectile equation. The game steps the flight in whole physics steps (velocity first, then position), which is the
    /// exact parabola with a vertical speed lowered by gravity x step / 2; the step-aware Solve folds that in so shallow landings are not missed. Pure.
    /// </summary>
    internal static class LaunchSolver
    {
        /// Closer than this (flat metres) the spot is straight above / below the thrower: throw straight up.
        internal const float MinFlatDistance = 0.01f;

        /// How often the step correction is repeated (the flight time it needs depends on the angle it corrects); 3 is converged far below a millimetre.
        const int Refinements = 3;

        /// <summary>The exact parabola (no physics step): see the overload with a step.</summary>
        internal static LaunchSolution Solve(ArcVector spawn, ArcVector target, float speed, float gravity) => Solve(spawn, target, speed, gravity, 0f);

        /// <summary>
        /// The direction from spawn that lands on target at the given speed (m/s) and gravity (m/s per second) when the flight is stepped every step
        /// seconds (0 = exact parabola). Expected outcome: the low arc when the spot is within reach; otherwise (Reachable false) the angle that goes
        /// farthest towards it.
        /// </summary>
        internal static LaunchSolution Solve(ArcVector spawn, ArcVector target, float speed, float gravity, float step)
        {
            if (RuleLog.On) RuleLog.Every("LaunchSolver.Solve", 2d, $"LaunchSolver.Solve(spawn={RuleLog.Shown(spawn)}, target={RuleLog.Shown(target)}, speed={RuleLog.Shown(speed)}, gravity={RuleLog.Shown(gravity)}, step={RuleLog.Shown(step)})");
            if (speed <= 0f) throw new ArgumentOutOfRangeException(nameof(speed), speed, "the speed must be above 0");
            if (gravity <= 0f) throw new ArgumentOutOfRangeException(nameof(gravity), gravity, "the gravity must be above 0");
            if (step < 0f) throw new ArgumentOutOfRangeException(nameof(step), step, "the step must not be negative");

            float flat = spawn.FlatDistanceTo(target);
            if (flat < MinFlatDistance) { var returnedAt35 = new LaunchSolution(new ArcVector(0f, 1f, 0f), 90f, true); if (RuleLog.On) RuleLog.Every("LaunchSolver.Solve:35", 2d, $"LaunchSolver.Solve: returns {RuleLog.Shown(returnedAt35)} (line 35)"); return returnedAt35; }

            double v = speed, g = gravity, d = flat, dy = target.Y - spawn.Y;
            bool reachable;
            double elevation = Elevation(v, g, d, dy, out reachable);
            double lowering = g * step / 2d;     // the vertical speed the step costs
            for (int i = 0; i < Refinements && step > 0f && reachable; i++)
            {
                double seconds = d / (v * Math.Cos(elevation));
                elevation = Elevation(v, g, d, dy + (lowering * seconds), out reachable);
            }

            float fx = (target.X - spawn.X) / flat, fz = (target.Z - spawn.Z) / flat;
            float cos = (float)Math.Cos(elevation);
            var direction = new ArcVector(fx * cos, (float)Math.Sin(elevation), fz * cos);
            { var returnedAt50 = new LaunchSolution(direction, (float)(elevation * 180d / Math.PI), reachable); if (RuleLog.On) RuleLog.Every("LaunchSolver.Solve:50", 2d, $"LaunchSolver.Solve: returns {RuleLog.Shown(returnedAt50)} (line 50)"); return returnedAt50; }
        }

        // the low-arc angle for a spot d metres away and dy metres higher; out of reach: the angle that goes farthest towards it
        static double Elevation(double v, double g, double d, double dy, out bool reachable)
        {
            if (RuleLog.On) RuleLog.Every("LaunchSolver.Elevation", 2d, $"LaunchSolver.Elevation(v={RuleLog.Shown(v)}, g={RuleLog.Shown(g)}, d={RuleLog.Shown(d)}, dy={RuleLog.Shown(dy)})");
            double v2 = v * v;
            double discriminant = (v2 * v2) - (g * ((g * d * d) + (2d * dy * v2)));
            reachable = discriminant >= 0d;
            double tan = reachable ? (v2 - Math.Sqrt(discriminant)) / (g * d) : v2 / (g * d);
            { var returnedAt61 = Math.Atan(tan); if (RuleLog.On) RuleLog.Every("LaunchSolver.Elevation:61", 2d, $"LaunchSolver.Elevation: returns {RuleLog.Shown(returnedAt61)} (line 61)"); return returnedAt61; }
        }
    }
}
