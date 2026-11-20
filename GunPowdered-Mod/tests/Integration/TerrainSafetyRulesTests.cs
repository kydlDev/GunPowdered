using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GunPowdered.Tests.Integration;

/// <summary>
/// The lab's KydlFlat arena is itself a terrain modification (the template's baked levelling). Test hooks that cleared terrain modifications without
/// knowing what was there before dropped the arena ground, and the player fell through it in every later test - again and again while this mod was tested
/// (lab a8dyn1, a11full1 mud-draugr-lake; user 2026-11-20: "make an integration test that checks for this"). These guards make that impossible to merge:
/// (1) only TerrainSnapshot touches a terrain piece's modification data, and it never restores without a mark;
/// (2) every lab test that digs marks the ground before, resets it after and checks that it is back;
/// (3) no test resets terrain it did not mark.
/// </summary>
[TestFixture]
public class TerrainSafetyRulesTests
{
    /// The TerrainComp fields that hold the stored ground changes (height, level, smooth, paint): writing them by hand is what removed the arena ground.
    static readonly string[] TerrainFields = { "m_modifiedHeight", "m_levelDelta", "m_smoothDelta", "m_modifiedPaint", "m_paintMask" };

    /// Hooks that change the ground: a lit charge digs a crater, a spawned location levels and digs its own ground (SwampRuin1: a 20 m hole).
    static readonly string[] DigHooks = { "GunPowdered.dynamiteLight", "GunPowdered.spawnLocationAt" };
    static readonly string[] MarkHooks = { "GunPowdered.terrainMark", "GunPowdered.dynamiteGroundMark" };
    static readonly string[] ResetHooks = { "GunPowdered.terrainReset", "GunPowdered.dynamiteTerrainReset" };
    static readonly string[] BackHooks = { "GunPowdered.terrainBack", "GunPowdered.dynamiteGroundBack" };

    const string SnapshotFile = "TerrainSnapshot.cs";

    static IEnumerable<string> SourceFiles() => Directory.GetFiles(TestPaths.Src, "*.cs", SearchOption.AllDirectories);

    static IEnumerable<string> SuiteDirs()
    {
        string suites = Path.GetDirectoryName(TestPaths.SuiteDir);
        return Directory.GetDirectories(suites).Where(d => !Path.GetFileName(d).StartsWith("_"));
    }

    static IEnumerable<string> TestFiles(string suiteDir) =>
        Directory.GetFiles(suiteDir, "*.json").Where(f => Path.GetFileName(f) != "suite.json");

    [Test]
    public void OnlyTerrainSnapshot_WritesTheStoredGroundChanges()
    {
        var offenders = new List<string>();
        foreach (string file in SourceFiles())
        {
            if (Path.GetFileName(file) == SnapshotFile) continue;
            string code = File.ReadAllText(file);
            foreach (string field in TerrainFields)
                if (code.Contains("\"" + field + "\"")) offenders.Add(Path.GetFileName(file) + " uses " + field);
            if (Regex.IsMatch(code, @"typeof\(TerrainComp\)\.GetMethod\(\s*""Save""")) offenders.Add(Path.GetFileName(file) + " calls TerrainComp.Save");
        }
        Assert.That(offenders, Is.Empty,
            "only Testing/TestHooks/TerrainSnapshot.cs may change stored terrain data (Mark before, Restore after): a hand-made clear removes the arena's own levelling and the player falls through the ground");
    }

    [Test]
    public void TerrainSnapshot_NeverRestoresWithoutAMark()
    {
        string file = SourceFiles().Single(f => Path.GetFileName(f) == SnapshotFile);
        string code = File.ReadAllText(file);
        int restore = code.IndexOf("internal static string Restore(", System.StringComparison.Ordinal);
        Assert.That(restore, Is.GreaterThan(0), "TerrainSnapshot.Restore exists");
        string body = code.Substring(restore);
        int guard = body.IndexOf("if (Saved.Count == 0)", System.StringComparison.Ordinal);
        int firstClear = body.IndexOf("Array.Clear", System.StringComparison.Ordinal);
        Assert.That(guard, Is.GreaterThan(0).And.LessThan(firstClear),
            "Restore returns before touching anything when nothing was marked (a blind clear is what removed the arena ground)");
    }

    [Test]
    public void EveryTestThatDigs_MarksResetsAndChecksTheGround()
    {
        var problems = new List<string>();
        foreach (string suite in SuiteDirs())
        {
            foreach (string file in TestFiles(suite))
            {
                string text = Expanded(suite, File.ReadAllText(file));
                int dig = First(text, DigHooks);
                if (dig < 0) continue;
                string name = Path.GetFileName(suite) + "/" + Path.GetFileNameWithoutExtension(file);
                int mark = First(text, MarkHooks), reset = Last(text, ResetHooks), back = Last(text, BackHooks);
                if (mark < 0 || mark > dig) problems.Add(name + ": no ground mark before it digs (" + string.Join(" / ", MarkHooks) + ")");
                if (reset < 0 || reset < dig) problems.Add(name + ": no terrain reset after it digs (" + string.Join(" / ", ResetHooks) + ")");
                if (back < 0 || back < reset) problems.Add(name + ": no check that the ground is back after the reset (" + string.Join(" / ", BackHooks) + ")");
            }
        }
        Assert.That(problems, Is.Empty, "a test that digs must leave the arena ground as it found it, and prove it");
    }

    [Test]
    public void NoTest_ResetsTerrainItDidNotMark()
    {
        var problems = new List<string>();
        foreach (string suite in SuiteDirs())
        {
            foreach (string file in TestFiles(suite))
            {
                string text = Expanded(suite, File.ReadAllText(file));
                int reset = First(text, ResetHooks);
                if (reset < 0) continue;
                int mark = First(text, MarkHooks);
                if (mark < 0 || mark > reset) problems.Add(Path.GetFileName(suite) + "/" + Path.GetFileNameWithoutExtension(file));
            }
        }
        Assert.That(problems, Is.Empty, "these tests reset terrain without marking it first");
    }

    // The test text with every suite function it calls by name put in at the call, so a dig or reset inside a function counts at its place.
    static string Expanded(string suiteDir, string text)
    {
        string functions = Path.Combine(suiteDir, "_functions");
        if (!Directory.Exists(functions)) return text;
        foreach (string fn in Directory.GetFiles(functions, "*.json"))
        {
            string call = "\"do\": \"" + Path.GetFileNameWithoutExtension(fn) + "\"";
            if (text.Contains(call)) text = text.Replace(call, call + " " + File.ReadAllText(fn));
        }
        return text;
    }

    static int First(string text, IEnumerable<string> names)
    {
        var at = names.Select(n => text.IndexOf("\"" + n + "\"", System.StringComparison.Ordinal)).Where(i => i >= 0).ToList();
        return at.Count == 0 ? -1 : at.Min();
    }

    static int Last(string text, IEnumerable<string> names)
    {
        var at = names.Select(n => text.LastIndexOf("\"" + n + "\"", System.StringComparison.Ordinal)).Where(i => i >= 0).ToList();
        return at.Count == 0 ? -1 : at.Max();
    }
}
