using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using GunPowdered.Features.Grenades.Domain;
using GunPowdered.Features.Guano.Domain;
using GunPowdered.Features.Sulfur.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmarks of the world-facing rules: the cave guano plan (once per cave entry, the heaviest pure code), the sulfur drops, the
/// grenade aim state machine (per frame while aiming). Comparison axis: builds; numbers in ..\_quality\benchmarks.md. The random source is a seeded
/// System.Random per case so the plan is the same every iteration.
[MemoryDiagnoser]
public class WorldRulesBenchmarks
{
    private Random _random = null!;
    private double[] _heights = null!;

    [GlobalSetup]
    public void Setup()
    {
        _random = new Random(1234);
        _heights = new double[200];
        var r = new Random(99);
        for (int i = 0; i < _heights.Length; i++) _heights[i] = (i % 4) * 3.0 + r.NextDouble();
    }

    private double Next() => _random.NextDouble();

    [Benchmark] public int[] GuanoPlan() => GuanoDepositRules.Plan(CaveKind.MountainCave, Next);
    [Benchmark] public int DepositCount() => GuanoDepositRules.DepositCount(CaveKind.SwampBatArea, Next);
    [Benchmark] public int MaxDeposits() => GuanoDepositRules.MaxDeposits(CaveKind.TrollCaveOrBurialChamber);
    [Benchmark] public int[] FloorOrder() => GuanoSpreadRules.FloorOrder(_heights, Next);
    [Benchmark] public List<List<int>> Floors() => GuanoSpreadRules.Floors(_heights);
    [Benchmark] public bool ShouldPlace() => CaveEntryRules.ShouldPlace(true, CaveKind.MountainCave, false);
    [Benchmark] public int SulfurAmount() => SulfurRules.Amount(SulfurSource.Blob, Next);
    [Benchmark] public double SulfurChance() => SulfurRules.KillChance(SulfurSource.Blob, 0.5, 0.3);
    [Benchmark] public string Aim() => GrenadeAimRules.Next(AimPhase.Aiming, true, false).ToString();
}