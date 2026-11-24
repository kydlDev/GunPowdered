using BenchmarkDotNet.Attributes;
using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmarks of the Mobile Cannon and Asksvin tow rules that run per frame or per interaction (pull speed and stamina, aim turn,
/// crank angle, tow slip / upright / push, hitch checks, handle reach, reverse lead). Comparison axis: builds; numbers in ..\_quality\benchmarks.md.
[MemoryDiagnoser]
public class MobileCannonRulesBenchmarks
{
    private float _speed, _dt, _pitch, _scroll, _angle, _side, _ahead;

    [GlobalSetup]
    public void Setup()
    {
        _speed = 4.2f; _dt = 0.016f; _pitch = 12f; _scroll = 0.5f; _angle = 7f; _side = 0.4f; _ahead = 2.5f;
    }

    [Benchmark] public float PullCap() => MobileCannonPullRules.CapSpeed(_speed, 5f);
    [Benchmark] public float PullStamina() => MobileCannonPullRules.StaminaCost(_speed, _dt, 8f);
    [Benchmark] public float AimTurn() => MobileCannonAimRules.Turn(_pitch, MobileCannonAimRules.Notches(_scroll), 2f, -5f, 45f);
    [Benchmark] public float Crank() => MobileCannonAimRules.CrankAngle(_pitch);
    [Benchmark] public float Screw() => MobileCannonAimRules.ScrewDrop(_pitch);
    [Benchmark] public float Range() => MobileCannonFireRules.Range(40f, _pitch, 9.81f);
    [Benchmark] public string WhyNotLoad() => MobileCannonFireRules.WhyNotLoad(false, 3, 5, 1);
    [Benchmark] public bool OnHandle() => CartUseRules.OnHandle(0.8f);
    [Benchmark] public float Upright() => AsksvinTowRules.UprightAccel(_angle, 0.1f);
    [Benchmark] public float Slip() => AsksvinTowRules.SlipScale(6f, 10f);
    [Benchmark] public float SidePush() => AsksvinTowRules.SidePush(_side, _ahead);
    [Benchmark] public float Excess() => AsksvinTowRules.Excess(_angle, 5f);
    [Benchmark] public float HookDrop() => AsksvinTowRules.HookDrop(10f, 10.4f, 10.2f, 0.1f);
    [Benchmark] public string WhyNotHitch() => AsksvinTowRules.WhyNotHitch(false, false, true, 3f);
    [Benchmark] public string Lead() => AsksvinReverseRules.Lead(_ahead, 1f, 2f);
    [Benchmark] public float TurnStep() => AsksvinReverseRules.TurnStep(1f, _dt);
}