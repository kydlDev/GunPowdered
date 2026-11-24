using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using GunPowdered.Core;
using GunPowdered.Features.Gunpowder.Domain;
using GunPowdered.Features.Saltpeter.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmarks of the shared batch-station rules (Leaching Station and Powder Mill: extension step, batch size, fill, take, start,
/// pile, timing), the Gunpowder batch maths and the placement / noise / burn helpers the stations and charges use. Comparison axis: builds; numbers in
/// ..\_quality\benchmarks.md.
[MemoryDiagnoser]
public class StationRulesBenchmarks
{
    private BatchStationSpec _spec = null!;
    private BatchAmounts _unit, _have, _full, _perUse, _player;
    private string[] _inRange = null!, _inputs = null!;

    [GlobalSetup]
    public void Setup()
    {
        _spec = new BatchStationSpec(3, "Saltpeter", "Charcoal", "Sulfur");
        _unit = new BatchAmounts(3, 1, 1); _have = new BatchAmounts(6, 2, 2); _full = new BatchAmounts(9, 3, 3);
        _perUse = new BatchAmounts(3, 1, 1); _player = new BatchAmounts(20, 20, 20);
        _inRange = new[] { "Saltpeter", "Charcoal", "Saltpeter", "Sulfur" };
        _inputs = new[] { "Saltpeter", "Charcoal", "Sulfur" };
    }

    [Benchmark] public int Step() => BatchStationRules.Step(_spec, _inRange);
    [Benchmark] public int UnitsPerBatch() => BatchStationRules.UnitsPerBatch(_spec, 2);
    [Benchmark] public int FullBatch() => BatchStationRules.FullBatch(_spec, _unit, 2).First;
    [Benchmark] public int FillPerUse() => BatchStationRules.FillPerUse(_spec, _unit, 2).First;
    [Benchmark] public int Take() => BatchStationRules.Take(_have, _full, _perUse, _player).First;
    [Benchmark] public int Units() => BatchStationRules.Units(_spec, _have, _unit, 2);
    [Benchmark] public bool StartsNow() => BatchStationRules.StartsNow(_spec, true, _have, _unit, 2);
    [Benchmark] public int Pile() => BatchStationRules.Pile(4, 5);
    [Benchmark] public int SlotOf() => BatchStationRules.SlotOf("Sulfur", _inputs);
    [Benchmark] public bool HasRoom() => BatchStationRules.HasRoom(_have, _full, 1);
    [Benchmark] public int AddOne() => BatchStationRules.AddOne(_have, 1).First;
    [Benchmark] public string Missing() => BatchStationRules.Missing(_spec, new BatchAmounts(1, 0, 0), _unit);
    [Benchmark] public double Minutes() => BatchStationRules.Minutes(_spec, 2, 10d, 2.5d);
    [Benchmark] public bool IsDone() => BatchStationRules.IsDone(100d, 700d, 10d);
    [Benchmark] public int LeachingStep() => LeachingUpgradeRules.Step(_inRange);
    [Benchmark] public int MillStep() => MillUpgradeRules.Step(_inRange);
    [Benchmark] public int GunpowderBatches() => GunpowderRules.Batches(20, 20, 20, 3, 1, 1);
    [Benchmark] public bool InRange() => StationPlacementRules.InRange(1f, 0.5f, 2f, 4f);
    [Benchmark] public bool IsOnStation() => StationPlacementRules.IsOnStation(1f, 2f, 1f, -2f, 2f, -2f, 2f, 3f);
    [Benchmark] public bool NoiseReacts() => NoiseRules.Reacts(12f, 30f, true, false, false, false);
    [Benchmark] public float BurnTotal() => BurnRules.Total(7f);
}