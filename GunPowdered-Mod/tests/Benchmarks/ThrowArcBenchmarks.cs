using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmark (PROJECT_RULES 30) of the grenade arc (D-047): ThrowArcView simulates the whole flight EVERY FRAME while a grenade is in
/// hand. Comparison axis: builds. The floor test stands in for the game's raycast (measured in game, not here); the cases are a short throw at the
/// feet, the best-angle throw (~15 m) and a throw that never lands within the fuse (the longest loop).
[MemoryDiagnoser]
public class ThrowArcBenchmarks
{
    private readonly List<ArcVector> _points = new List<ArcVector>(512);
    private ArcVector _start, _velocity;
    private float _gravity, _step, _maxSeconds;
    private System.Func<ArcVector, ArcVector, float?> _hit;

    /// Pitch of the throw in degrees (+ up); 90 = straight up, which only lands after the fuse with no floor in reach.
    [Params(-60f, 42f, 90f)]
    public float Pitch { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        double rad = Pitch * System.Math.PI / 180d;
        _start = new ArcVector(0f, 1.5f, 0f);
        _velocity = ThrowArcRules.LaunchVelocity(new ArcVector((float)System.Math.Cos(rad), (float)System.Math.Sin(rad), 0f), 12f);
        _gravity = 10f;
        _step = ThrowArcRules.DefaultStep;
        _maxSeconds = 8f;
        // straight up never reaches the floor before the fuse ends: a floor 100 m down
        float floor = Pitch >= 89f ? -1000f : 0f;
        _hit = (from, to) => to.Y > floor ? (float?)null : (from.Y - floor) / (from.Y - to.Y);
    }

    [Benchmark]
    public int Simulate() => ThrowArcRules.Simulate(_start, _velocity, _gravity, 0f, _step, _maxSeconds, _hit, _points).Steps;
}
