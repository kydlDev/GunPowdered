using BenchmarkDotNet.Attributes;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmark (PROJECT_RULES 30) of the Hand Cannon's per-frame aim (D-052 .. D-054): while the cannon is aimed, every frame computes
/// the ball speed (CannonRules.SpeedForRange), clamps the spot (ClampAim) and solves the launch twice (the muzzle moves with the pitch). Comparison
/// axis: the aim frame (baseline) against the rules the flight calls once per impact (OnImpact, MomentumAfter, PassThroughLanding). The spots are
/// a near one, the middle and the full range.
[MemoryDiagnoser]
public class CannonAimBenchmarks
{
    private ArcVector _muzzle, _target;
    private float _wanted;

    /// Flat distance of the aimed spot in metres.
    [Params(3f, 15f, 30f)]
    public float Distance { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _muzzle = new ArcVector(0f, 0.6f, 0f);
        _target = new ArcVector(Distance, 0f, 0f);
        _wanted = Distance;
    }

    [Benchmark(Baseline = true)]
    public float AimFrame()
    {
        float speed = CannonRules.SpeedForRange(30f, 10f, 9.46f);
        float d = CannonRules.ClampAim(_wanted, 3f, 30f);
        float e1 = LaunchSolver.Solve(_muzzle, _target, speed, 10f, ThrowArcRules.DefaultStep).ElevationDegrees;
        float e2 = LaunchSolver.Solve(_muzzle, _target, speed, 10f, ThrowArcRules.DefaultStep).ElevationDegrees;
        return d + e1 + e2;
    }

    [Benchmark]
    public float ImpactRules()
    {
        int passed = CannonRules.OnImpact(ImpactKind.Passable, 0) == ImpactOutcome.PassThrough ? 1 : 0;
        return CannonRules.MomentumAfter(passed, 0.5f) + CannonRules.PassThroughLanding(30f, _wanted * 0.5f);
    }

    /// D-055 addendum: the friendly-fire rule runs once per creature the flight meets (and OnImpact for a building: Solid).
    [Benchmark]
    public int FriendlyFireRule()
    {
        int hits = CannonRules.HitsCreature(new HitCheck(false, true, false, false)) ? 1 : 0;
        hits += CannonRules.HitsCreature(new HitCheck(true, false, false, false)) ? 1 : 0;
        return hits + (int)CannonRules.OnImpact(ImpactKind.Solid, 1);
    }
}
