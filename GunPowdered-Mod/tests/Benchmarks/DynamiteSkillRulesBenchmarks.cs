using BenchmarkDotNet.Attributes;
using GunPowdered.Features.Dynamite.Domain;
using GunPowdered.Features.Skilling.Domain;

namespace GunPowdered.Benchmarks;

/// Coverage-suite benchmarks of the dynamite / grenade crater and fuse rules (each blast, each placed stick's timer) and of the skill curves (read
/// whenever a musket reloads, a fuse is lit or a cannon loads). Comparison axis: builds; numbers in ..\_quality\benchmarks.md.
[MemoryDiagnoser]
public class DynamiteSkillRulesBenchmarks
{
    private double _litAt, _now;
    private float _level;

    [GlobalSetup]
    public void Setup()
    {
        _litAt = 100d; _now = 103.5d; _level = 42f;
    }

    [Benchmark] public float CraterTop() => CraterRules.Top(12f, 10.5f, 3f);
    [Benchmark] public bool GameMissesPieces() => BlastRockRules.GameMissesPieces(128);
    [Benchmark] public bool SwallowsPunch() => DynamiteRules.SwallowsPunch(0.3f, true);
    [Benchmark] public float ClearDistance() => DynamiteRules.ClearDistance(3f, 1.4f);
    [Benchmark] public bool SurfaceOk() => DynamiteRules.SurfaceOk(10f, 10.2f);
    [Benchmark] public bool ShowsTimer() => DynamiteRules.ShowsTimer(4f, 6f);
    [Benchmark] public string TimerText() => DynamiteRules.TimerText("Dynamite", 2.5f);
    [Benchmark] public float CreatureDamage() => DynamiteRules.CreatureDamage(400f);
    [Benchmark] public float FuseLeft() => DynamiteRules.FuseLeft(_litAt, _now);
    [Benchmark] public bool Blows() => DynamiteRules.Blows(_litAt, _now);
    [Benchmark] public float SkillShare() => SkillRules.Share(_level);
    [Benchmark] public float MusketReload() => SkillRules.MusketReloadSeconds(_level);
    [Benchmark] public float DynamiteFuse() => SkillRules.DynamiteFuseSeconds(_level);
    [Benchmark] public float GrenadeFuse() => SkillRules.GrenadeFuseSeconds(_level);
    [Benchmark] public float CannonLoad() => SkillRules.CannonLoadSeconds(3f, _level);
    [Benchmark] public float CannonBlunt() => SkillRules.CannonBluntFactor(_level);
}