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

    private enum Kind { Wood, Ash, Iron, Metal, Leather, Horn, Powder, Wax, Feather, Twine, Paper, Dark, Glow }

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
        new MatDef("flametal", Kind.Iron, 0.33f, 0.32f, 0.33f, 0.55f),
        new MatDef("ashwood", Kind.Wood, 0.40f, 0.36f, 0.33f, 0.15f),
        new MatDef("carriage_blackiron", Kind.Iron, 0.25f, 0.25f, 0.26f, 0.40f),
        new MatDef("flametal_glow", Kind.Glow, 0.98f, 0.58f, 0.25f, 0.50f),
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
        if (d.Kind == Kind.Glow) return;
        const int N = 64;
        int seed = d.Name.GetHashCode() & 0xfff;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float v = Shade(d.Kind, x, y, N, seed);                        // 1 = base colour
                var c = new Color(Mathf.Clamp01(d.Color.r * v), Mathf.Clamp01(d.Color.g * v), Mathf.Clamp01(d.Color.b * v), 1f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        File.WriteAllBytes(Art + "/Textures/" + d.Name + "_tex.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void ConfigureTexture(MatDef d)
    {
        if (d.Kind == Kind.Glow) return;
        string p = Art + "/Textures/" + d.Name + "_tex.png";
        var imp = (TextureImporter)AssetImporter.GetAtPath(p);
        imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false; imp.wrapMode = TextureWrapMode.Repeat;
        imp.textureCompression = TextureImporterCompression.Uncompressed; imp.sRGBTexture = true;
        imp.SaveAndReimport();
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
        if (d.Kind == Kind.Glow)
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
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
        var seen = new HashSet<string>();
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || !seen.Add(m.name)) continue;
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
        if (it.Prefab != "GP_AsksvinHarness")
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = root.transform.InverseTransformPoint(b.center);
            box.size = b.size;
        }
        if (it.Rigid) { var rb = root.AddComponent<Rigidbody>(); rb.mass = it.Mass; }
        string path = Art + "/Prefabs/" + it.Prefab + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        var imp = AssetImporter.GetAtPath(path);
        imp.assetBundleName = Bundle;
        imp.SaveAndReimport();
    }

    // ---------- icons: 256 px, transparent background, rendered from the prefab ----------
    private static void MakeIcon(ItemDef it)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Prefabs/" + it.Prefab + ".prefab");
        var pru = new PreviewRenderUtility();
        try
        {
            var inst = (GameObject)Object.Instantiate(prefab);
            pru.AddSingleGO(inst);
            var b = Measure(inst);
            var cam = pru.camera;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.fieldOfView = 25f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 100f;
            float radius = b.extents.magnitude;
            Vector3 dir = it.IconDir.normalized;
            cam.transform.position = b.center + dir * (radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * 1.12f;
            cam.transform.LookAt(b.center);
            pru.lights[0].intensity = 1.3f; pru.lights[0].transform.rotation = Quaternion.Euler(40f, -35f, 0f);
            pru.lights[1].intensity = 0.7f; pru.lights[1].transform.rotation = Quaternion.Euler(340f, 150f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            pru.BeginStaticPreview(new Rect(0, 0, 256, 256));
            pru.Render(true);
            var tex = pru.EndStaticPreview();
            string path = Art + "/Icons/" + it.Icon + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(inst);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.assetBundleName = Bundle;
            imp.SaveAndReimport();
        }
        finally { pru.Cleanup(); }
    }
}
