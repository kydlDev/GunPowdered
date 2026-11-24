using BenchmarkDotNet.Attributes;
using GunPowdered.Features.Cannon.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmarks (skill microbenchmarking, use case 1; PROJECT_RULES 30) of the cannon rules the game asks per shot: ball choice from the
/// carried stacks, launch speed, impact outcome, who a ball hits, the state checks behind every cannon hover text. Comparison axis: builds; numbers in
/// ..\_quality\benchmarks.md. Inputs sit in fields so the JIT cannot fold them.
[MemoryDiagnoser]
public class CannonRulesBenchmarks
{
    private int[] _carried = null!;
    private float _range, _gravity, _pitch, _target, _hit, _wanted;
    private HitCheck _check;
    private CannonStatus _status;

    [GlobalSetup]
    public void Setup()
    {
        _carried = new[] { 12, 30 };
        _range = 25f; _gravity = 9.81f; _pitch = 20f; _target = 25f; _hit = 18f; _wanted = 47f;
        _check = new HitCheck(true, false, false, false);
        _status = new CannonStatus(true, false, false);
    }

    [Benchmark] public string ChooseBall() => CannonAmmoRules.Choose(CannonAmmoRules.MobileCannonMinTier, _carried);
    [Benchmark] public int CountBalls() => CannonAmmoRules.CountTaken(CannonAmmoRules.HandCannonMinTier, _carried);
    [Benchmark] public int TierOfBall() => CannonAmmoRules.TierOf("GP_FlametalCannonBall");
    [Benchmark] public float LaunchSpeed() => CannonRules.SpeedForRange(_range, _gravity, _pitch);
    [Benchmark] public float PassThrough() => CannonRules.PassThroughLanding(_target, _hit);
    [Benchmark] public int Impact() => (int)CannonRules.OnImpact(ImpactKind.Passable, 2);
    [Benchmark] public bool HitsCreature() => CannonRules.HitsCreature(_check);
    [Benchmark] public float Momentum() => CannonRules.MomentumAfter(2, 0.7f);
    [Benchmark] public float Clamp() => CannonRules.ClampAim(_wanted, -10f, 45f);
    [Benchmark] public string WhyNotLoad() => CannonStateRules.WhyNotLoad(_status, 3, 5, 1);
    [Benchmark] public string WhyNotLight() => CannonStateRules.WhyNotLight(_status, true);
}