using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GunPowdered.Tests.Integration;

/// <summary>
/// Every item spec in ItemCatalog is registered by a feature (ItemRegistry.Register(..., ItemCatalog.X)). Origin (Claude-Agent-10, 2026-11-20): the
/// Deep North Dynamite Pack was in the catalog, unit-tested and bundled, but no feature registered it, so it did not exist in the game - a green unit
/// run said nothing (lab a10pack1: "could not give GP_DeepNorthPack", "no recipe").
/// </summary>
[TestFixture]
public class ItemRegistrationTests
{
    static readonly Regex SpecField = new Regex(@"public\s+static\s+readonly\s+ItemSpec\s+(\w+)\s*=");
    static readonly Regex Registered = new Regex(@"ItemRegistry\.Register\(([^;]*)\);");

    [Test]
    public void EveryCatalogItem_IsRegisteredByAFeature()
    {
        string catalog = File.ReadAllText(Path.Combine(TestPaths.Src, "Core", "ItemCatalog.cs"));
        var specs = SpecField.Matches(catalog).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.That(specs, Is.Not.Empty, "no ItemSpec fields found in ItemCatalog.cs");

        string registrations = string.Join("\n", Directory.GetFiles(Path.Combine(TestPaths.Src, "Features"), "*.cs", SearchOption.AllDirectories)
            .SelectMany(f => Registered.Matches(File.ReadAllText(f)).Cast<Match>().Select(m => m.Groups[1].Value)));
        var missing = specs.Where(s => !Regex.IsMatch(registrations, @"\bItemCatalog\." + s + @"\b")).ToList();
        Assert.That(missing, Is.Empty, "ItemCatalog items no feature registers (they will not exist in the game)");
    }
}
