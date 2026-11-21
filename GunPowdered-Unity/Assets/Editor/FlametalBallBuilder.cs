// D-145 (user 2026-11-21): the Flametal Cannon Ball. Its item prefab and its flying prefab are the Iron Cannon Ball's (GP_CannonBall,
// GP_CannonBall_projectile: same colliders, same sizes) with the mesh of FlametalCannonBall.fbx (make_flametal_cannon_ball.py) and the flametal
// material ext_flametal_mat (dark metal with orange veins, D-138). The icon FlametalCannonBall.png gets the Iron Cannon Ball icon's import settings.
// Run from the menu or in batch mode: Unity -batchmode -quit -projectPath <GunPowdered-Unity> -executeMethod FlametalBallBuilder.BuildAndBundle
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FlametalBallBuilder
{
    const string Art = "Assets/GunPowdered_Art";
    const string Bundle = "gunpowdered";

    [MenuItem("GunPowdered/Build Flametal Cannon Ball")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        // the mod reads the mesh at runtime (InGameTests World_BundleModels, colliders): Read/Write on, like the iron ball's CannonBall.fbx
        var model = (ModelImporter)AssetImporter.GetAtPath(Art + "/Models/FlametalCannonBall/FlametalCannonBall.fbx");
        if (model != null && !model.isReadable) { model.isReadable = true; model.SaveAndReimport(); }
        Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(Art + "/Models/FlametalCannonBall/FlametalCannonBall.fbx").OfType<Mesh>().FirstOrDefault();
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/ext_flametal_mat.mat");
        if (mesh == null || mat == null) { Debug.LogError("[FlametalBallBuilder] mesh " + (mesh != null) + ", material " + (mat != null) + " - nothing built"); Fail(); return; }
        Copy("GP_CannonBall", "GP_FlametalCannonBall", mesh, mat);
        Copy("GP_CannonBall_projectile", "GP_FlametalCannonBall_projectile", mesh, mat);
        Icon();
        AssetDatabase.SaveAssets();
        Debug.Log("[FlametalBallBuilder] built GP_FlametalCannonBall + GP_FlametalCannonBall_projectile, mesh " + mesh.name + " (" + mesh.vertexCount + " vertices)");
    }

    // batch mode: build the prefabs, then the bundle (copied into the mod's assets like the menu "Valheim/Build AssetBundles")
    public static void BuildAndBundle()
    {
        Build();
        GunPowdered.EditorTools.ModBundleExporter.BuildAndCopy();
    }

    static void Copy(string from, string to, Mesh mesh, Material mat)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Prefabs/" + from + ".prefab");
        if (source == null) { Debug.LogError("[FlametalBallBuilder] no " + from); Fail(); return; }
        GameObject copy = Object.Instantiate(source);        // a plain copy: the nested FBX instance of the iron ball becomes ordinary objects
        copy.name = to;
        int meshes = 0, renderers = 0;
        foreach (MeshFilter f in copy.GetComponentsInChildren<MeshFilter>(true)) { f.sharedMesh = mesh; meshes++; }
        foreach (MeshRenderer r in copy.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterials = Enumerable.Repeat(mat, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            renderers++;
        }
        string path = Art + "/Prefabs/" + to + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(copy, path);
        Object.DestroyImmediate(copy);
        AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(Bundle, "");
        Debug.Log("[FlametalBallBuilder] " + path + ": " + meshes + " mesh(es), " + renderers + " renderer(s) set to flametal");
    }

    static void Icon()
    {
        var iron = (TextureImporter)AssetImporter.GetAtPath(Art + "/Icons/CannonBall.png");
        var flametal = (TextureImporter)AssetImporter.GetAtPath(Art + "/Icons/FlametalCannonBall.png");
        if (iron == null || flametal == null) { Debug.LogError("[FlametalBallBuilder] icon importer missing"); Fail(); return; }
        var settings = new TextureImporterSettings();
        iron.ReadTextureSettings(settings);
        flametal.SetTextureSettings(settings);
        flametal.textureType = iron.textureType;
        flametal.spriteImportMode = iron.spriteImportMode;
        flametal.alphaIsTransparency = iron.alphaIsTransparency;
        flametal.maxTextureSize = iron.maxTextureSize;
        flametal.SetAssetBundleNameAndVariant(Bundle, "");
        flametal.SaveAndReimport();
    }

    static void Fail()
    {
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
