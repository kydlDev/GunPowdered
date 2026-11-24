// Builds textures, materials, prefabs and icons for the weapon / vehicle models (D-075).
// Menu: GunPowdered/Build weapon art. Safe to run again: it overwrites its own output only.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GunPowderedArtBuilder
{
    private const string Art = "Assets/GunPowdered_Art";
    private const string Bundle = "gunpowdered";

    private enum Kind { Wood, Ash, Iron, Metal, Leather, Horn, Powder, Wax, Feather, Twine, Paper, Dark, Glow, Flametal, Grausten, Marble, Glass }

    private sealed class MatDef
    {
        public string Name; public Kind Kind; public Color Color; public float Smooth; public float Metal;
        public MatDef(string n, Kind k, float r, float g, float b, float smooth, float metal = 0f) { Name = n; Kind = k; Color = new Color(r, g, b, 1f); Smooth = smooth; Metal = metal; }
    }

    // Base colours are the Blender colours already converted to sRGB (what the FBX import shows); smoothness = 1 - roughness.
    private static readonly MatDef[] Mats =
    {
        new MatDef("musket_wood", Kind.Wood, 0.48f, 0.37f, 0.26f, 0.20f),
        new MatDef("musket_iron", Kind.Iron, 0.27f, 0.27f, 0.28f, 0.45f),
        new MatDef("musket_brass", Kind.Metal, 0.77f, 0.65f, 0.38f, 0.60f),
        new MatDef("musket_steel", Kind.Metal, 0.58f, 0.58f, 0.60f, 0.60f),
        new MatDef("flametal", Kind.Flametal, 0.24f, 0.28f, 0.32f, 0.40f, 0.6f),         // D-192: our own look like the game's FlametalNew (slate blue-grey, speckled)
        new MatDef("flametal_wheel", Kind.Flametal, 0.56f, 0.64f, 0.72f, 0.82f, 0.85f),  // D-192: the Mobile Cannon's wheel metal, shinier and lighter than the ash wood
        new MatDef("ashwood", Kind.Wood, 0.40f, 0.36f, 0.33f, 0.15f),
        new MatDef("carriage_blackiron", Kind.Iron, 0.25f, 0.25f, 0.26f, 0.40f),
        new MatDef("flametal_glow", Kind.Glow, 0.35f, 0.62f, 1.00f, 0.50f),             // D-192: blue glow like the game's new flametal (was orange)
        new MatDef("arrow_ash", Kind.Ash, 0.77f, 0.68f, 0.55f, 0.15f),
        new MatDef("arrow_feather", Kind.Feather, 0.70f, 0.29f, 0.25f, 0.10f),
        new MatDef("arrow_horn", Kind.Horn, 0.93f, 0.91f, 0.84f, 0.50f),
        new MatDef("arrow_blackmetal", Kind.Iron, 0.19f, 0.19f, 0.21f, 0.50f),
        new MatDef("arrow_leather", Kind.Leather, 0.27f, 0.25f, 0.23f, 0.20f),
        new MatDef("arrow_twine", Kind.Twine, 0.80f, 0.74f, 0.60f, 0.05f),
        new MatDef("arrow_wax", Kind.Wax, 0.77f, 0.25f, 0.22f, 0.60f),
        new MatDef("arrow_powder", Kind.Powder, 0.10f, 0.10f, 0.10f, 0.00f),
        new MatDef("dyn_iron", Kind.Iron, 0.33f, 0.33f, 0.35f, 0.45f),
        new MatDef("dyn_band", Kind.Metal, 0.51f, 0.45f, 0.35f, 0.50f),
        new MatDef("dyn_red", Kind.Paper, 0.77f, 0.27f, 0.22f, 0.30f),
        new MatDef("dyn_cap", Kind.Iron, 0.46f, 0.40f, 0.31f, 0.40f),
        new MatDef("dyn_fuse", Kind.Twine, 0.81f, 0.75f, 0.62f, 0.05f),
        new MatDef("dyn_tip", Kind.Dark, 0.25f, 0.19f, 0.15f, 0.10f),
        new MatDef("harness_leather", Kind.Leather, 0.58f, 0.40f, 0.26f, 0.35f),
        new MatDef("harness_brass", Kind.Metal, 0.90f, 0.77f, 0.48f, 0.65f),
        new MatDef("harness_iron", Kind.Iron, 0.25f, 0.25f, 0.26f, 0.40f),
        new MatDef("ammo_paper", Kind.Paper, 0.93f, 0.91f, 0.82f, 0.05f),
        new MatDef("ammo_iron", Kind.Iron, 0.33f, 0.33f, 0.35f, 0.45f),
        new MatDef("ammo_cartridge", Kind.Paper, 0.91f, 0.85f, 0.73f, 0.05f),
        new MatDef("ammo_thread", Kind.Twine, 0.57f, 0.48f, 0.38f, 0.05f),
        // D-129 Leaching Station extensions (make_leaching_extensions.py)
        new MatDef("ext_wood", Kind.Wood, 0.48f, 0.37f, 0.26f, 0.15f),
        new MatDef("ext_iron", Kind.Iron, 0.22f, 0.21f, 0.20f, 0.55f, 0.7f),
        new MatDef("ext_stone", Kind.Dark, 0.58f, 0.58f, 0.57f, 0.05f),
        new MatDef("ext_silver", Kind.Metal, 0.80f, 0.82f, 0.83f, 0.70f, 0.8f),
        new MatDef("ext_obsidian", Kind.Dark, 0.17f, 0.15f, 0.20f, 0.85f),
        new MatDef("ext_brine", Kind.Wax, 0.67f, 0.66f, 0.53f, 0.90f),
        new MatDef("ext_finewood", Kind.Wood, 0.67f, 0.53f, 0.38f, 0.30f),
        new MatDef("ext_blackmetal", Kind.Iron, 0.20f, 0.22f, 0.20f, 0.60f, 0.9f),
        new MatDef("ext_yggwood", Kind.Wood, 0.62f, 0.66f, 0.74f, 0.35f),          // D-138: pale blue-grey like the game's Yggdrasil wood
        new MatDef("ext_copper", Kind.Metal, 0.79f, 0.56f, 0.38f, 0.65f, 0.8f),
        new MatDef("ext_marble", Kind.Marble, 0.24f, 0.24f, 0.26f, 0.70f),
        new MatDef("ext_ashwood", Kind.Wood, 0.40f, 0.37f, 0.35f, 0.15f),
        new MatDef("ext_flametal", Kind.Flametal, 0.24f, 0.28f, 0.32f, 0.40f, 0.6f),     // D-192: like the game's FlametalNew (was D-138's dark metal with orange veins)
        new MatDef("ext_glow", Kind.Glow, 0.98f, 0.58f, 0.25f, 0.50f),
        new MatDef("ext_grausten", Kind.Grausten, 0.46f, 0.48f, 0.52f, 0.10f),   // D-138: the Flametal Drying Oven's table, like Grausten
        new MatDef("ext_glass", Kind.Glass, 1.00f, 0.55f, 0.20f, 0.90f),        // D-138: the see-through orange panes of the fire boxes
        new MatDef("ext_flame", Kind.Glow, 1.00f, 0.72f, 0.28f, 0.50f),         // D-138: the flames inside (shown only while the station runs)
    };

    private sealed class ItemDef
    {
        public string Model, Prefab, Icon; public Vector3 IconDir; public bool Rigid; public float Mass;
        public ItemDef(string m, string p, string i, Vector3 dir, bool rigid, float mass) { Model = m; Prefab = p; Icon = i; IconDir = dir; Rigid = rigid; Mass = mass; }
    }

    // Unity axes: the weapons point along +Z. IconDir = direction from the object to the camera.
    private static readonly ItemDef[] Items =
    {
        new ItemDef("Musket", "GP_Musket", "Musket", new Vector3(1f, 0.30f, -0.45f), true, 4f),
        new ItemDef("MobileCannon", "GP_MobileCannon", "MobileCannon", new Vector3(1f, 0.45f, -0.75f), true, 400f),
        new ItemDef("ExplosiveArrow", "GP_ExplosiveArrow", "ExplosiveArrow", new Vector3(1f, 0.35f, -0.40f), true, 0.3f),
        new ItemDef("Dynamite", "GP_Dynamite", "Dynamite", new Vector3(1f, 0.55f, -0.65f), true, 0.5f),
        new ItemDef("Harness", "GP_AsksvinHarness", "AsksvinHarness", new Vector3(1f, 0.55f, -0.80f), false, 0f),
        new ItemDef("Paper", "GP_Paper", "Paper", new Vector3(0.4f, 1.0f, -0.9f), true, 0.1f),
        new ItemDef("IronBullet", "GP_IronBullet", "IronBullet", new Vector3(1f, 0.55f, -0.65f), true, 0.05f),
        new ItemDef("Cartridge", "GP_PaperCartridge", "PaperCartridge", new Vector3(1f, 0.55f, -0.65f), true, 0.05f),
        new ItemDef("LeachBarrel", "GP_LeachBarrel", "LeachBarrel", new Vector3(1f, 0.55f, -0.80f), false, 0f),
        new ItemDef("SettlingTank", "GP_SettlingTank", "SettlingTank", new Vector3(1f, 0.55f, -0.80f), false, 0f),
        new ItemDef("BlackMetalBoiler", "GP_BlackMetalBoiler", "BlackMetalBoiler", new Vector3(1f, 0.55f, -0.80f), false, 0f),
        new ItemDef("DvergrDripPipes", "GP_DvergrDripPipes", "DvergrDripPipes", new Vector3(1f, 0.55f, -0.80f), false, 0f),
        new ItemDef("FlametalHeater", "GP_FlametalHeater", "FlametalHeater", new Vector3(-0.6f, 0.5f, 1f), false, 0f),
        new ItemDef("MillGrindstone", "GP_MillGrindstone", "MillGrindstone", new Vector3(1f, 0.55f, -0.80f), false, 0f),
        new ItemDef("MillSieve", "GP_MillSieve", "MillSieve", new Vector3(1f, 0.55f, -0.80f), false, 0f),
        new ItemDef("MillOven", "GP_MillOven", "MillOven", new Vector3(-0.6f, 0.5f, 1f), false, 0f),
    };

    [MenuItem("GunPowdered/Build weapon art")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Art + "/Textures");
        Directory.CreateDirectory(Art + "/Materials");
        Directory.CreateDirectory(Art + "/Prefabs");
        Directory.CreateDirectory(Art + "/Icons");
        var made = new Dictionary<string, Material>();
        foreach (var d in Mats) WriteTexture(d);
        AssetDatabase.Refresh();
        foreach (var d in Mats) ConfigureTexture(d);
        foreach (var d in Mats) made[d.Name] = MakeMaterial(d);
        foreach (var it in Items) RemapModel(it.Model, made);
        AssetDatabase.Refresh();
        foreach (var it in Items) MakePrefab(it);
        AssetDatabase.SaveAssets();
        foreach (var it in Items) MakeIcon(it);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GunPowdered] weapon art built: " + Mats.Length + " materials, " + Items.Length + " prefabs, " + Items.Length + " icons");
    }

    // ---------- textures: 64x64 point-filtered procedural, base colour baked in (like D-029) ----------
    private static float Hash(int x, int y, int seed)
    {
        unchecked { int h = x * 374761393 + y * 668265263 + seed * 1274126177; h = (h ^ (h >> 13)) * 1103515245; return ((h ^ (h >> 16)) & 0xffff) / 65535f; }
    }

    private static void WriteTexture(MatDef d)
    {
        if (d.Kind == Kind.Glow || d.Kind == Kind.Glass) return;
        const int N = 64;
        int seed = d.Name.GetHashCode() & 0xfff;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float v = Shade(d.Kind, x, y, N, seed);                        // 1 = base colour
                var c = new Color(Mathf.Clamp01(d.Color.r * v), Mathf.Clamp01(d.Color.g * v), Mathf.Clamp01(d.Color.b * v), 1f);
                if (d.Kind == Kind.Flametal) c = FlametalSpeck(c, x, y, seed);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        File.WriteAllBytes(Art + "/Textures/" + d.Name + "_tex.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void ConfigureTexture(MatDef d)
    {
        if (d.Kind == Kind.Glow || d.Kind == Kind.Glass) return;
        string p = Art + "/Textures/" + d.Name + "_tex.png";
        var imp = (TextureImporter)AssetImporter.GetAtPath(p);
        imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false; imp.wrapMode = TextureWrapMode.Repeat;
        imp.textureCompression = TextureImporterCompression.Uncompressed; imp.sRGBTexture = true;
        imp.SaveAndReimport();
    }

    // D-192: the game's FlametalNew ingot is slate blue-grey with darker blotches and a few lighter teal-grey flecks (measured from the game's
    // texture as a reference only: base ~(57,67,77), dark ~(41,51,62), light ~(105,122,118)); our own pattern, nothing of the game copied
    private static Color FlametalSpeck(Color c, int x, int y, int seed)
    {
        float blot = Hash(x / 4, y / 4, seed + 3);
        if (blot > 0.82f) return new Color(c.r * 0.72f, c.g * 0.74f, c.b * 0.78f, 1f);                              // darker patches
        float fleck = Hash(x / 2, y / 2, seed + 11);
        if (fleck > 0.93f) return Color.Lerp(c, new Color(c.r * 1.65f, c.g * 1.75f, c.b * 1.5f, 1f), 0.8f);          // lighter teal-grey flecks
        return c;
    }

    private static float Shade(Kind k, int x, int y, int n, int seed)
    {
        float r = Hash(x, y, seed);
        switch (k)
        {
            case Kind.Wood:   // grain runs along v: columns differ, with soft rings and a few pores
            { float col = Hash(x / 2, 0, seed); float ring = 0.5f + 0.5f * Mathf.Sin(x * 0.55f + Hash(x / 4, y / 16, seed) * 3f); return 0.80f + 0.14f * col + 0.12f * ring + (r > 0.96f ? -0.12f : 0f); }
            case Kind.Ash:
            { float col = Hash(x / 3, 0, seed); return 0.90f + 0.10f * col + 0.05f * (r - 0.5f); }
            case Kind.Iron:   // dark speckled iron
                return 0.90f + 0.14f * (r - 0.5f) + (Hash(x / 3, y / 3, seed) > 0.9f ? -0.10f : 0f);
            case Kind.Metal:  // brushed: streaks across the part
            { float row = Hash(0, y, seed); return 0.88f + 0.16f * row + 0.04f * (r - 0.5f); }
            case Kind.Leather:
            { float blot = Hash(x / 5, y / 5, seed); return 0.86f + 0.20f * blot + 0.06f * (r - 0.5f) + ((x % 16 == 0 || y % 16 == 0) ? -0.04f : 0f); }
            case Kind.Horn:
            { float st = Hash(0, y / 2, seed); return 0.92f + 0.08f * st - 0.03f * r; }
            case Kind.Powder:
                return 0.55f + 0.9f * (r > 0.88f ? 1f : r * 0.4f);
            case Kind.Wax:
                return 0.97f + 0.05f * (r - 0.5f);
            case Kind.Feather: // barbs: thin diagonal stripes
            { float s = ((x + y) % 6 < 2) ? -0.10f : 0.05f; return 0.96f + s + 0.04f * (r - 0.5f); }
            case Kind.Twine:  // twist: diagonal bands
            { float s = ((x * 2 + y) % 8 < 4) ? 0.10f : -0.12f; return 0.96f + s + 0.04f * (r - 0.5f); }
            case Kind.Paper:
                return 0.95f + 0.08f * (Hash(x / 2, y, seed) - 0.5f) + 0.03f * (r - 0.5f);
            case Kind.Grausten:   // grey stone with darker and lighter speckles and faint blocks
            { float blot = Hash(x / 6, y / 6, seed); return 0.86f + 0.16f * blot + 0.10f * (r - 0.5f) + ((y % 21 == 0) ? -0.10f : 0f); }
            case Kind.Marble:     // dark stone with thin light veins
            { float vein = Mathf.Abs(Mathf.Sin((x + 1.7f * y + 9f * Hash(x / 8, y / 8, seed)) * 0.35f)); return 0.85f + 0.08f * (r - 0.5f) + (vein < 0.06f ? 1.6f : 0f); }
            default:
                return 0.94f + 0.12f * (r - 0.5f);
        }
    }

    // ---------- materials ----------
    private static Material MakeMaterial(MatDef d)
    {
        string path = Art + "/Materials/" + d.Name + "_mat.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
        m.name = d.Name + "_mat";
        m.SetFloat("_Metallic", d.Metal);
        m.SetFloat("_Glossiness", d.Smooth);
        if (d.Kind == Kind.Glass)
        {
            m.SetFloat("_Mode", 2f);                                           // Fade: see-through (D-138)
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            m.color = new Color(d.Color.r, d.Color.g, d.Color.b, 0.28f);
            m.mainTexture = null;
        }
        else if (d.Kind == Kind.Glow)
        {
            m.color = d.Color;
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor("_EmissionColor", new Color(d.Color.r, d.Color.g, d.Color.b) * 1.6f);
        }
        else
        {
            m.color = Color.white;
            m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/" + d.Name + "_tex.png");
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void RemapModel(string model, Dictionary<string, Material> mats)
    {
        string p = Art + "/Models/" + model + "/" + model + ".fbx";
        var imp = (ModelImporter)AssetImporter.GetAtPath(p);
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.isReadable = true;                                                 // the game's navmesh builder needs readable meshes on pieces (like the older models)
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
        var seen = new HashSet<string>();
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || !seen.Add(m.name)) continue;
                if (m.name.EndsWith("_mat")) continue;                 // already remapped by an earlier run
                Material target;
                if (mats.TryGetValue(m.name, out target)) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), target);
                else Debug.LogWarning("[GunPowdered] no material definition for " + m.name + " in " + model);
            }
        imp.SaveAndReimport();
    }

    // ---------- prefabs: root + child "model" + one box collider (+ Rigidbody for items), like D-027 ----------
    private static Bounds Measure(GameObject root)
    {
        var rs = root.GetComponentsInChildren<MeshRenderer>(true);
        var b = new Bounds(root.transform.position, Vector3.zero); bool first = true;
        foreach (var r in rs) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        return b;
    }

    private static void MakePrefab(ItemDef it)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Models/" + it.Model + "/" + it.Model + ".fbx");
        var root = new GameObject(it.Prefab);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        model.name = "model";
        model.transform.SetParent(root.transform, false);
        var b = Measure(root);
        // every prefab gets the box: the mod copies it onto the item, and an item without one falls through the ground when dropped
        var box = root.AddComponent<BoxCollider>();
        box.center = root.transform.InverseTransformPoint(b.center);
        box.size = b.size;
        if (it.Rigid) { var rb = root.AddComponent<Rigidbody>(); rb.mass = it.Mass; }
        string path = Art + "/Prefabs/" + it.Prefab + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        var imp = AssetImporter.GetAtPath(path);
        imp.assetBundleName = Bundle;
        imp.SaveAndReimport();
        if (it.Prefab == "GP_MobileCannon") SetWheelMetal(path, AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/flametal_wheel_mat.mat"));   // D-192
    }

    // ---------- icons: 256 px, transparent background, rendered from the prefab ----------
    // D-192 (user 2026-11-24 18:22 "a bit bigger, proportional to the window, not to each other"): these icons are fitted to their own square - the
    // camera frames the bounding box's sphere, which leaves a round model (the cannon ball) at about half the square; after a first render the visible
    // pixels are measured and, when the longer side fills less than GoodFill, the camera comes closer so it fills TargetFill.
    private static readonly HashSet<string> FitIcons = new HashSet<string> { "MobileCannon", "FlametalHeater", "MillOven", "FlametalCannonBall" };
    private const float TargetFill = 0.9f;
    private const float GoodFill = 0.85f;

    private static void MakeIcon(ItemDef it)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Prefabs/" + it.Prefab + ".prefab");
        var pru = new PreviewRenderUtility();
        try
        {
            var inst = (GameObject)Object.Instantiate(prefab);
            pru.AddSingleGO(inst);
            var b = Measure(inst);
            var tex = RenderIcon(pru, b, it.IconDir, 1f);
            if (FitIcons.Contains(it.Icon))
            {
                float fill = LongSideFill(tex);
                if (fill > 0f && fill < GoodFill)
                {
                    Object.DestroyImmediate(tex);
                    tex = RenderIcon(pru, b, it.IconDir, fill / TargetFill);
                    Debug.Log("[GunPowdered] icon " + it.Icon + ": first render filled " + fill.ToString("0.00") + ", drawn again closer, now " + LongSideFill(tex).ToString("0.00"));
                }
                else Debug.Log("[GunPowdered] icon " + it.Icon + " fills " + fill.ToString("0.00") + " of its square");
            }
            string path = Art + "/Icons/" + it.Icon + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(inst);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.assetBundleName = Bundle;
            imp.SaveAndReimport();
        }
        finally { pru.Cleanup(); }
    }

    // distance 1 = the bounding box's sphere just fits the view (with a 12 % margin); smaller = closer
    private static Texture2D RenderIcon(PreviewRenderUtility pru, Bounds b, Vector3 iconDir, float distance)
    {
        var cam = pru.camera;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.fieldOfView = 25f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 100f;
        float radius = b.extents.magnitude;
        Vector3 dir = iconDir.normalized;
        cam.transform.position = b.center + dir * (radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * 1.12f * distance;
        cam.transform.LookAt(b.center);
        pru.lights[0].intensity = 1.3f; pru.lights[0].transform.rotation = Quaternion.Euler(40f, -35f, 0f);
        pru.lights[1].intensity = 0.7f; pru.lights[1].transform.rotation = Quaternion.Euler(340f, 150f, 0f);
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
        // EndStaticPreview returns an RGB texture (no alpha: the icons had a black background, user 2026-11-19), so the preview renders into our
        // own ARGB target and is read back as RGBA
        pru.BeginStaticPreview(new Rect(0, 0, 256, 256));
        var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;
        pru.Render(true);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        pru.EndStaticPreview();
        rt.Release();
        Object.DestroyImmediate(rt);
        return tex;
    }

    // how much of the square (0..1) the visible pixels (alpha above 0.1) cover on their longer side; 0 when nothing is visible
    private static float LongSideFill(Texture2D t)
    {
        var px = t.GetPixels32();
        int w = t.width, h = t.height, minX = w, minY = h, maxX = -1, maxY = -1;
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a <= 25) continue;
            int x = i % w, y = i / w;
            if (x < minX) minX = x; if (x > maxX) maxX = x;
            if (y < minY) minY = y; if (y > maxY) maxY = y;
        }
        if (maxX < 0) return 0f;
        return Mathf.Max((maxX - minX + 1) / (float)w, (maxY - minY + 1) / (float)h);
    }

    // ---------- D-192: our own Flametal look ----------
    private static readonly string[] FlametalMats = { "flametal", "flametal_wheel", "flametal_glow", "ext_flametal" };
    private static readonly string[] WheelTyres = { "model/wheel_left/wheel_left_tyre", "model/wheel_right/wheel_right_tyre" };

    // Rebuilds only the flametal textures and materials, gives the Mobile Cannon's tyres the shiny wheel metal and redraws the icons of the four
    // flametal prefabs. Leaves every other prefab and icon alone (BuildAll rebuilds all prefabs). Then: GunPowdered/Build AssetBundles.
    [MenuItem("GunPowdered/Build Flametal look (D-192)")]
    public static void BuildFlametalLook()
    {
        var made = new Dictionary<string, Material>();
        foreach (var d in Mats) if (System.Array.IndexOf(FlametalMats, d.Name) >= 0) WriteTexture(d);
        AssetDatabase.Refresh();
        foreach (var d in Mats) if (System.Array.IndexOf(FlametalMats, d.Name) >= 0) { ConfigureTexture(d); made[d.Name] = MakeMaterial(d); }
        AssetDatabase.SaveAssets();
        SetWheelMetal(Art + "/Prefabs/GP_MobileCannon.prefab", made["flametal_wheel"]);
        foreach (var it in Items) if (FitIcons.Contains(it.Icon)) MakeIcon(it);
        MakeIcon(new ItemDef("FlametalCannonBall", "GP_FlametalCannonBall", "FlametalCannonBall", new Vector3(1f, 0.55f, -0.65f), true, 0f));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GunPowdered] D-192 flametal look built: " + made.Count + " materials, tyres on flametal_wheel_mat, " + FitIcons.Count + " icons");
    }

    private static void SetWheelMetal(string prefabPath, Material wheel)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            int set = 0;
            foreach (var p in WheelTyres)
            {
                var t = root.transform.Find(p);
                var r = t != null ? t.GetComponent<Renderer>() : null;
                if (r == null) { Debug.LogWarning("[GunPowdered] no tyre renderer at " + p + " in " + prefabPath); continue; }
                r.sharedMaterial = wheel; set++;
            }
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[GunPowdered] " + prefabPath + ": " + set + " tyre(s) use " + wheel.name);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
