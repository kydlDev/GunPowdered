using System;

namespace GunPowdered.Features.Cannon.Domain
{
    /// <summary>
    /// What the cannon ball hit: something it can pass (a tree or a creature), something solid that takes damage (a building, a rock) or bare
    /// ground (terrain: nothing to damage).
    /// </summary>
    internal enum ImpactKind
    {
        Passable,
        Solid,
        Ground
    }

    /// <summary>What the ball does at an impact (D-053, D-054, D-055 addendum).</summary>
    internal enum ImpactOutcome
    {
        /// The first tree or creature: damage, then fly on with less momentum and land by the landing rule.
        PassThrough,
        /// A second tree or creature, or a building / rock: damage, then the ball stops there.
        StopWithDamage,
        /// Bare ground: the flight ends there.
        Stop
    }

    /// <summary>The facts that decide whether the ball hits a creature (D-055 addendum 2): its relation to the shooter and both PvP switches.</summary>
    internal readonly struct HitCheck
    {
        internal HitCheck(bool enemyOfShooter, bool shooterPvp, bool targetIsPlayer, bool targetPvp)
        {
            EnemyOfShooter = enemyOfShooter;
            ShooterPvp = shooterPvp;
            TargetIsPlayer = targetIsPlayer;
            TargetPvp = targetPvp;
        }

        internal bool EnemyOfShooter { get; }
        internal bool ShooterPvp { get; }
        internal bool TargetIsPlayer { get; }
        internal bool TargetPvp { get; }

        public override string ToString() => $"enemy {EnemyOfShooter}, shooter PvP {ShooterPvp}, player {TargetIsPlayer}, target PvP {TargetPvp}";
    }

    /// <summary>
    /// The rules of the mounted Hand Cannon's shot (D-051 .. D-054), pure. The ball leaves at a fixed speed: the speed that carries it the cannon's
    /// range on flat ground at the stock's own tilt (D-052, 9.46 degrees, "a slight arch"). It passes ONE tree or creature, loses momentum there
    /// (speed and damage times the pass factor, D-053 addendum: "maybe 50% less") and then lands half way between that impact and the spot it
    /// was aimed at (D-053 addendum 2); a second tree or creature stops it (D-054). Terrain, rock and buildings always end the flight; rocks and
    /// buildings take its damage first (D-055 addendum). Players and tamed animals follow the game's friendly-fire rule (HitsCreature).
    /// </summary>
    internal static class CannonRules
    {
        /// <summary>
        /// The launch speed (m/s) that carries a ball range metres on flat ground at pitchDegrees above the horizon under gravity (m/s per
        /// second): range = v^2 sin(2 pitch) / g. Expected outcome: the speed; an ArgumentOutOfRangeException for a range or gravity not above 0
        /// or a pitch outside 0..90 degrees (exclusive).
        /// </summary>
        internal static float SpeedForRange(float range, float gravity, float pitchDegrees)
        {
            if (range <= 0f) throw new ArgumentOutOfRangeException(nameof(range), range, "the range must be above 0");
            if (gravity <= 0f) throw new ArgumentOutOfRangeException(nameof(gravity), gravity, "the gravity must be above 0");
            if (pitchDegrees <= 0f || pitchDegrees >= 90f)
                throw new ArgumentOutOfRangeException(nameof(pitchDegrees), pitchDegrees, "the pitch must be between 0 and 90 degrees");
            double sin2 = Math.Sin(2d * pitchDegrees * Math.PI / 180d);
            return (float)Math.Sqrt(range * gravity / sin2);
        }

        /// <summary>
        /// Where a ball lands after passing a tree or creature (D-053 addendum 2): half way between the impact and the aimed spot, h + (T - h) / 2,
        /// both as flat distances from the cannon. Expected outcome: that distance; the impact distance itself when the impact is at or past the
        /// aimed spot (it cannot land behind where it already is); negative inputs count as 0.
        /// </summary>
        internal static float PassThroughLanding(float target, float hit)
        {
            float t = Math.Max(0f, target), h = Math.Max(0f, hit);
            return h >= t ? h : h + ((t - h) / 2f);
        }

        /// <summary>
        /// The outcome of an impact given how many trees / creatures the ball has already passed. Expected outcome: Stop for bare ground;
        /// StopWithDamage for a building or rock (D-055 addendum: they take the ball's damage and end the flight); PassThrough for the first
        /// passable; StopWithDamage for every later one.
        /// </summary>
        internal static ImpactOutcome OnImpact(ImpactKind kind, int passedSoFar)
        {
            if (kind == ImpactKind.Ground) return ImpactOutcome.Stop;
            if (kind == ImpactKind.Solid) return ImpactOutcome.StopWithDamage;
            return passedSoFar <= 0 ? ImpactOutcome.PassThrough : ImpactOutcome.StopWithDamage;
        }

        /// <summary>
        /// Whether the ball hits a creature, by the game's own rule for a player's projectile (Projectile.OnHit and Character.RPC_Damage, 1.0.21;
        /// D-055 addendum 2): an enemy of the shooter always; a friend (another player, a tamed animal) only when the shooter has PvP on, and a
        /// player only when that player's PvP is on too. Expected outcome: true when the ball must hit it, false when it flies through.
        /// </summary>
        internal static bool HitsCreature(HitCheck c)
        {
            bool friendHit = c.ShooterPvp && (!c.TargetIsPlayer || c.TargetPvp);
            return c.EnemyOfShooter || friendHit;
        }

        /// <summary>
        /// The share of the full speed and damage left after passing passed trees or creatures: factor ^ passed, the factor clamped into 0..1.
        /// Expected outcome: 1 before any pass, factor after one.
        /// </summary>
        internal static float MomentumAfter(int passed, float factor)
        {
            float f = factor < 0f ? 0f : (factor > 1f ? 1f : factor);
            return passed <= 0 ? 1f : (float)Math.Pow(f, passed);
        }

        /// <summary>The flat distance to aim at: the wanted one clamped into min..max. Expected outcome: a distance inside the range (max wins when min is above max).</summary>
        internal static float ClampAim(float wanted, float min, float max)
        {
            if (wanted > max) return max;
            return wanted < min ? Math.Min(min, max) : wanted;
        }
    }
}
