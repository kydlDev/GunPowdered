using BenchmarkDotNet.Attributes;
using GunPowdered.Features.Gunpowder.Domain;
using GunPowdered.Features.Saltpeter.Domain;
using GunPowdered.Features.Water.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmarks (skill microbenchmarking, use case 1; PROJECT_RULES 30) of the rules the game asks on every tick of every placed
/// station: the Rain Barrel fill (RainBarrel.Tick), the Leaching Station ready check (LeachingVat.Tick) and the Powder Mill turning check. Comparison
/// axis: builds (before / after a change of these rules), numbers kept in ..\_quality\benchmarks.md. Inputs live in fields set from [Params], so the
/// JIT cannot fold them; the cases cover the branches a tick really takes (raining and open, covered, full barrel).
[MemoryDiagnoser]
public class TickRulesBenchmarks
{
    private float _current, _capacity, _rate, _elapsed;
    private bool _raining, _covered;

    /// Which branch of the Rain Barrel fill a tick takes.
    public enum BarrelCase { RainingOpen, Covered, Full }

    [Params(BarrelCase.RainingOpen, BarrelCase.Covered, BarrelCase.Full)]
    public BarrelCase Barrel { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _capacity = RainBarrelRules.DefaultCapacity;
        _rate = RainBarrelRules.DefaultRatePerMinute;
        _elapsed = 2f;   // the barrel's tick interval in seconds
        _current = Barrel == BarrelCase.Full ? _capacity : 50f;
        _raining = true;
        _covered = Barrel == BarrelCase.Covered;
    }

    [Benchmark]
    public float RainBarrelFill() => RainBarrelRules.Fill(_current, _capacity, _rate, _elapsed, _raining, _covered).Value;

}

/// The two station checks of the same ticks that take no barrel case (split off so the [Params] of the barrel do not repeat them).
[MemoryDiagnoser]
public class StationTickBenchmarks
{
    private double _start, _now, _dayLength, _days, _lastUse, _grace;

    [GlobalSetup]
    public void Setup()
    {
        _start = 1000d;
        _now = 1500d;
        _dayLength = 1800d;
        _days = 1d;
        _lastUse = 1490d;
        _grace = 3d;
    }

    [Benchmark]
    public bool LeachingStationReady() => LeachingVatRules.IsReady(_start, _now, _dayLength, _days);

    [Benchmark]
    public bool PowderMillTurning() => GunpowderRules.IsTurning(_lastUse, _now, _grace);
}
