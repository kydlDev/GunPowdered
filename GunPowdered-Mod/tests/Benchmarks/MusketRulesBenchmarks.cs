using BenchmarkDotNet.Attributes;
using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmarks of the musket rules: the magazine / reload / fire decisions and the per-frame pose maths (carry blend, reload stroke,
/// the muscle evaluation of the 58 arm and finger muscles that runs every frame while a musket is in hand). Comparison axis: builds; numbers in
/// ..\_quality\benchmarks.md.
[MemoryDiagnoser]
public class MusketRulesBenchmarks
{
    private float[] _into = null!;
    private float _seconds, _carry, _dt;

    [GlobalSetup]
    public void Setup()
    {
        _into = new float[MusketPoses.Count];
        _seconds = 0.7f; _carry = 0.5f; _dt = 0.016f;
    }

    [Benchmark] public int CartridgesToLoad() => MusketRules.CartridgesToLoad(2, 9);
    [Benchmark] public bool AutoReloads() => MusketRules.AutoReloads(0, 5);
    [Benchmark] public string WhyNotFire() => MusketRules.WhyNotFire(0);
    [Benchmark] public int AfterReload() => MusketRules.AfterReload(2, 9);
    [Benchmark] public float Recoil() => MusketRules.RecoilPushForce(2f, 70f, 0.9f);
    [Benchmark] public int CartridgesDue() => MusketRules.CartridgesDue(1f, 3.4f);
    [Benchmark] public int ParseLoaded() => MusketRules.ParseLoaded("3");
    [Benchmark] public string LoadText() => MusketRules.LoadText(3);
    [Benchmark] public bool EvaluateReload() => MusketPoseRules.Evaluate(MusketAction.Reload, _seconds, _carry, _into);
    [Benchmark] public bool EvaluateHold() => MusketPoseRules.Evaluate(MusketAction.Hold, _seconds, _carry, _into);
    [Benchmark] public float Stroke() => MusketPoseRules.Stroke(_seconds);
    [Benchmark] public float NextCarry() => MusketPoseRules.NextCarry(_carry, true, _dt);
    [Benchmark] public bool NextRaised() => MusketPoseRules.NextRaised(true, false, false);
    [Benchmark] public bool FacesAim() => MusketPoseRules.FacesAim(true, true);
    [Benchmark] public float FollowFactor() => MusketPoseRules.FollowFactor(_dt, 0.15f);
}