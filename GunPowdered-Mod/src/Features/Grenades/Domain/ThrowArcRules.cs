using System;
using System.Collections.Generic;
using GunPowdered.Core;

namespace GunPowdered.Features.Grenades.Domain
{
    /// <summary>What a simulated throw found: where it lands, after how long, and whether the thrower stands inside the blast.</summary>
    internal readonly struct ArcResult
    {
        internal ArcResult(bool landed, ArcVector landing, float seconds, int steps)
        {
            if (RuleLog.On) RuleLog.Verbose($"ArcResult.ArcResult(landed={RuleLog.Shown(landed)}, landing={RuleLog.Shown(landing)}, seconds={RuleLog.Shown(seconds)}, steps={RuleLog.Shown(steps)})");
            Landed = landed;
            Landing = landing;
            Seconds = seconds;
            Steps = steps;
        }

        /// <summary>False when nothing was hit within the time limit (the flight line ends in the air).</summary>
        internal bool Landed { get; }
        internal ArcVector Landing { get; }
        internal float Seconds { get; }
        internal int Steps { get; }
    }

    /// <summary>
    /// The flight of a thrown grenade (D-047), stepped exactly like the game's Projectile.FixedUpdate (1.0.21): velocity += down x gravity x dt,
    /// then the quadratic drag, then position += velocity x dt, once per physics step. The drawn arc uses these same steps, so the real throw follows
    /// it. Pure: the ground / object test is a function the game adapter passes in (a raycast), tests pass a flat floor.
    /// </summary>
    internal static class ThrowArcRules
    {
        /// The game's physics step (Time.fixedDeltaTime 0.02 s) and how long a flight is followed at most.
        internal const float DefaultStep = 0.02f, DefaultMaxSeconds = 6f;

        /// <summary>One physics step of a projectile. Expected outcome: the new velocity and position, as Projectile.FixedUpdate computes them.</summary>
        internal static void Step(ref ArcVector position, ref ArcVector velocity, float gravity, float drag, float dt)
        {
            if (RuleLog.On) RuleLog.Every("ThrowArcRules.Step", 2d, $"ThrowArcRules.Step(position={RuleLog.Shown(position)}, velocity={RuleLog.Shown(velocity)}, gravity={RuleLog.Shown(gravity)}, drag={RuleLog.Shown(drag)}, dt={RuleLog.Shown(dt)})");
            velocity += new ArcVector(0f, -gravity * dt, 0f);
            float speed = velocity.Length;
            if (speed > 0f && drag > 0f)
            {
                float slow = speed * speed * drag * dt;
                velocity += velocity * (-slow / speed);
            }
            position += velocity * dt;
        }

        /// <summary>
        /// Follows the flight from start with the given velocity until hit(from, to) reports a hit on that segment (a fraction 0..1 along it) or the
        /// time runs out. Every step's position goes into points (cleared first) for the drawn line; the last point is the landing point.
        /// Expected outcome: Landed with the hit point, or not Landed after maxSeconds.
        /// </summary>
        internal static ArcResult Simulate(ArcVector start, ArcVector velocity, float gravity, float drag, float dt, float maxSeconds,
            Func<ArcVector, ArcVector, float?> hit, List<ArcVector> points)
        {
            if (dt <= 0f) throw new ArgumentOutOfRangeException(nameof(dt), dt, "the step must be above 0");
            points.Clear();
            points.Add(start);
            ArcVector pos = start, vel = velocity;
            int steps = (int)Math.Ceiling(maxSeconds / dt);
            for (int i = 1; i <= steps; i++)
            {
                ArcVector from = pos;
                Step(ref pos, ref vel, gravity, drag, dt);
                float? f = hit(from, pos);
                if (f.HasValue)
                {
                    ArcVector landing = from + ((pos - from) * Clamp01(f.Value));
                    points.Add(landing);
                    return new ArcResult(true, landing, ((i - 1) * dt) + (Clamp01(f.Value) * dt), i);
                }
                points.Add(pos);
            }
            return new ArcResult(false, pos, steps * dt, steps);
        }

        /// <summary>The launch velocity of a throw: the aim direction (any length) scaled to speed. Expected outcome: a vector of length speed (zero for no direction).</summary>
        internal static ArcVector LaunchVelocity(ArcVector aimDirection, float speed)
        {
            if (RuleLog.On) RuleLog.Every("ThrowArcRules.LaunchVelocity", 2d, $"ThrowArcRules.LaunchVelocity(aimDirection={RuleLog.Shown(aimDirection)}, speed={RuleLog.Shown(speed)})");
            float len = aimDirection.Length;
            { var returnedAt84 = len > 0f ? aimDirection * (speed / len) : new ArcVector(0f, 0f, 0f); if (RuleLog.On) RuleLog.Every("ThrowArcRules.LaunchVelocity:84", 2d, $"ThrowArcRules.LaunchVelocity: returns {RuleLog.Shown(returnedAt84)} (line 84)"); return returnedAt84; }
        }

        /// <summary>Would the thrower be hit by the blast at the landing point (the red ring). Expected outcome: true when the thrower is within radius of it.</summary>
        internal static bool InBlast(ArcVector thrower, ArcVector landing, float radius) => thrower.DistanceTo(landing) <= radius;

        static float Clamp01(float f) => f < 0f ? 0f : (f > 1f ? 1f : f);
    }
}
