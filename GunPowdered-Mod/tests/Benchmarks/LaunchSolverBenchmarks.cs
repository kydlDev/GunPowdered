using BenchmarkDotNet.Attributes;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmark (PROJECT_RULES 30) of the launch solver (D-050): while a grenade is aimed, GrenadeAim solves the launch for the chosen spot
/// every frame (the arc preview) and again for every GetAimDir call of the real throw. Comparison axis: the exact parabola (Exact, baseline) against the
/// step-aware solution the game needs (Stepped = the exact one plus 3 refinements). The distances are a close spot, a far one inside the throw's
/// reach (~14 m at 12 m/s) and one beyond it (the not-reachable branch).
[MemoryDiagnoser]
public class LaunchSolverBenchmarks
{
    private ArcVector _spawn, _target;
    private float _speed, _gravity, _step;

    /// Flat distance of the spot in metres; the spot is 1 m below the spawn.
    [Params(3f, 12f, 25f)]
    public float Distance { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _spawn = new ArcVector(0f, 1.5f, 0f);
        _target = new ArcVector(Distance, 0.5f, 0f);
        _speed = 12f;
        _gravity = 10f;
        _step = ThrowArcRules.DefaultStep;
    }

    [Benchmark(Baseline = true)]
    public float Exact() => LaunchSolver.Solve(_spawn, _target, _speed, _gravity).ElevationDegrees;

    [Benchmark]
    public float Stepped() => LaunchSolver.Solve(_spawn, _target, _speed, _gravity, _step).ElevationDegrees;
}
