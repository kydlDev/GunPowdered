using System.IO;
using System.Linq;

namespace GunPowdered.Tests.Integration;

/// <summary>
/// Every inventory icon has a transparent background (user 2026-11-19: "the musket icon does not have transparent background ... ensure that all icons do").
/// Eight icons from GunPowderedArtBuilder were saved as RGB (no alpha channel at all) because PreviewRenderUtility.EndStaticPreview returns an RGB
/// texture; the builder now reads back an ARGB target. A PNG without an alpha channel (colour type 2 = RGB, 0 = grey) cannot be transparent, so this
/// guards it from the file header.
/// </summary>
[TestFixture]
public class IconRulesTests
{
    static string IconDir => Path.Combine(TestPaths.ProjectRoot, "GunPowdered-Unity", "Assets", "GunPowdered_Art", "Icons");

    [Test] public void EveryIcon_HasAnAlphaChannel()
    {
        Assert.That(Directory.Exists(IconDir), Is.True, "icon folder " + IconDir);
        var files = Directory.GetFiles(IconDir, "*.png");
        Assert.That(files, Is.Not.Empty);
        // PNG: 8-byte signature, then the IHDR chunk (length 4, type 4, width 4, height 4, bit depth 1, colour type 1) - colour type at byte 25
        var noAlpha = files.Where(f => { var b = File.ReadAllBytes(f); return b.Length < 26 || (b[25] != 6 && b[25] != 4); }).Select(Path.GetFileName).ToList();
        Assert.That(noAlpha, Is.Empty, "icons without an alpha channel (they show a solid background in the inventory)");
    }
}
