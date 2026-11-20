using KydlCore.Common;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using UnityEngine;

namespace GunPowdered.Testing
{
    /// <summary>
    /// In-game tests run by the user's Kydl.GameProbe (dev tool, never a dependency: it finds this class by its NAME `InGameTests`).
    ///   Startup_*  run when the main menu is reached;   World_*  run 5 s after a world is loaded; both again on GameProbe's RetestKey (PageDown).
    /// Each method: static, no parameters, returns a text starting with PASS / FAIL / SKIP and the detail with numbers + units,
    /// e.g. "FAIL: coil radius 0.900 m, expected 0.180 m". null or an exception counts as FAIL. Results land in the session report.
    /// Keep them cheap and read-only: they also run in players' games when GameProbe is installed there (it never is for players).
    /// </summary>
    internal static class InGameTests
    {
        /// The player config is bound and the mod is switched on.
        static string Startup_ConfigBound()
        {
            if (ModSettings.EnableMod == null) return "FAIL: ModSettings.Bind did not run (EnableMod is null)";
            return ModSettings.On ? "PASS: EnableMod on" : "SKIP: EnableMod is off in the config";
        }

        /// The installer ran and the feature registry holds the mod's features (each started per its Enable switch).
        static string Startup_FeaturesInstalled()
        {
            var inst = ModInstaller.Current;
            if (inst == null) return "FAIL: ModInstaller.Install did not run";
            int running = 0;
            foreach (var f in inst.Features.All) if (inst.Features.IsRunning(f.Id)) running++;
            ModLog.Verbose("InGameTests: Startup_FeaturesInstalled " + inst.Features.Count + " feature(s), " + running + " running");
            return inst.Features.Count > 0 ? "PASS: " + inst.Features.Count + " feature(s), " + running + " running" : "FAIL: no feature registered";
        }

        /// KydlCore (hard dependency, T-070) is loaded: it owns the KY hub (F6) with this mod's tabs.
        static string Startup_CoreLoaded()
        {
            bool loaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(KydlCore.Bootstrap.Plugin.ModGuid);
            ModLog.Verbose("InGameTests: Startup_CoreLoaded " + loaded);
            return loaded ? "PASS: " + KydlCore.Bootstrap.Plugin.ModGuid + " is loaded" : "FAIL: " + KydlCore.Bootstrap.Plugin.ModGuid + " is not loaded (this mod requires it)";
        }

        /// The AssetBundle was loaded (embedded in the DLL) and holds the prefabs and icons (D-027 / D-028).
        static string Startup_BundleLoaded()
        {
            var b = Plugin.Bundle;
            if (b == null) return "FAIL: Plugin.Bundle is null, the mod runs on placeholder looks";
            int n = b.GetAllAssetNames().Length;
            ModLog.Verbose("InGameTests: Startup_BundleLoaded " + n + " assets");
            return n >= 22 ? "PASS: bundle has " + n + " assets" : "FAIL: bundle has " + n + " assets, expected at least 22 (11 prefabs + 11 icons)";
        }

        /// Every item, piece and deposit prefab carries the bundle model (child gp_model with a renderer and a real shader), and items an icon.
        static string World_BundleModels()
        {
            var bad = new System.Text.StringBuilder();
            int ok = 0;
            foreach (var s in ItemCatalog.All)
            {
                var go = Jotunn.Managers.PrefabManager.Instance.GetPrefab(s.Id);
                if (go == null) { bad.Append(s.Id).Append(" missing; "); continue; }
                var model = WorldModel(go.transform);
                if (model == null) { bad.Append(s.Id).Append(" no gp_model; "); continue; }
                var rends = model.GetComponentsInChildren<UnityEngine.Renderer>(true);
                if (rends.Length == 0) { bad.Append(s.Id).Append(" no renderer; "); continue; }
                foreach (var r in rends)
                    foreach (var m in r.sharedMaterials)
                        if (m == null || m.shader == null || m.shader.name.Contains("InternalErrorShader")) bad.Append(s.Id).Append("/").Append(r.name).Append(" bad shader; ");
                // the game's navmesh builder needs readable meshes on pieces ("does not allow read access" errors, GameProbe 2026-11-18)
                foreach (var mf in model.GetComponentsInChildren<UnityEngine.MeshFilter>(true))
                    if (mf.sharedMesh != null && !mf.sharedMesh.isReadable) bad.Append(s.Id).Append("/").Append(mf.name).Append(" mesh not readable; ");
                var drop = go.GetComponent<ItemDrop>();
                if (drop != null && (drop.m_itemData.m_shared.m_icons == null || drop.m_itemData.m_shared.m_icons.Length == 0 || drop.m_itemData.m_shared.m_icons[0] == null)) bad.Append(s.Id).Append(" no icon; ");
                ok++;
            }
            ModLog.Verbose("InGameTests: World_BundleModels " + ok + " of " + ItemCatalog.All.Count + " ok");
            return bad.Length == 0 ? "PASS: " + ok + " prefabs use the bundle model" : "FAIL: " + bad;
        }

        // the prefab's own bundle model: a direct child gp_model, or one a feature moved under a pivot of its own (the Cannon Stock turns its look
        // on "pivot", D-052; run cannonA showed the root-only lookup failing for it). The hand copy under "attach" does not count as the world model.
        static UnityEngine.Transform WorldModel(UnityEngine.Transform root)
        {
            var direct = root.Find("gp_model");
            if (direct != null) return direct;
            foreach (UnityEngine.Transform child in root)
            {
                if (child.name == "attach") continue;
                var nested = child.Find("gp_model");
                if (nested != null)
                {
                    ModLog.Verbose("InGameTests: " + root.name + " carries its model under " + child.name);
                    return nested;
                }
            }
            return null;
        }

        /// The public API (GunPowderedApi) names prefabs that exist in the game: what another mod gets from it can be spawned and used.
        static string World_ApiPrefabs()
        {
            var missing = new System.Text.StringBuilder();
            foreach (string id in GunPowderedApi.AllPrefabs)
                if (ZNetScene.instance == null || ZNetScene.instance.GetPrefab(id) == null) missing.Append(id).Append(' ');
            int n = GunPowderedApi.AllPrefabs.Count;
            ModLog.Verbose("InGameTests: World_ApiPrefabs " + n + " names, missing: " + (missing.Length == 0 ? "none" : missing.ToString()));
            return missing.Length == 0 ? "PASS: all " + n + " API prefab names exist (API version " + GunPowderedApi.ApiVersion + ")" : "FAIL: not registered: " + missing;
        }

        /// The Powder Mill is automatic (D-137): its prefab carries the PowderMill batch station and the turning arm, and no crafting station any more
        /// (no Craft menu; D-040 "works in the open" holds because nothing asks for a roof).
        static string World_PowderMillAutomatic()
        {
            var go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedApi.Prefabs.PowderMill) : null;
            if (go == null) { ModLog.Verbose("InGameTests: World_PowderMillAutomatic prefab missing"); return "FAIL: GP_PowderMill is not registered"; }
            bool mill = go.GetComponent<GunPowdered.Features.Gunpowder.Infrastructure.PowderMill>() != null;
            bool spin = go.GetComponent<GunPowdered.Features.Gunpowder.Infrastructure.PowderMillSpin>() != null;
            bool station = go.GetComponent<CraftingStation>() != null;
            ModLog.Verbose($"InGameTests: World_PowderMillAutomatic mill {mill}, arm {spin}, crafting station {station}");
            if (!mill || !spin) return $"FAIL: the Powder Mill lacks its automatic parts (mill {mill}, arm {spin})";
            return station ? "FAIL: the Powder Mill still has a crafting station" : "PASS: the Powder Mill grinds by itself (no crafting station)";
        }

        /// The Spiked Grenade (D-047) is wired as approved: its own flight and blast prefabs are registered, the throw has no random spread, the blast
        /// damages buildings, trees and rocks normally (D-100) but not the terrain, and the hand shows the clay pot.
        static string World_GrenadeReady()
        {
            var scene = ZNetScene.instance;
            GameObject item = scene != null ? scene.GetPrefab(GunPowderedPrefabs.SpikedGrenade) : null;
            GameObject flight = scene != null ? scene.GetPrefab(Features.Grenades.Infrastructure.GrenadeSetup.ProjectileId) : null;
            GameObject blast = scene != null ? scene.GetPrefab(Features.Grenades.Infrastructure.GrenadeSetup.ExplosionId) : null;
            Attack attack = item != null ? item.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack : null;
            Aoe aoe = blast != null ? blast.GetComponent<Aoe>() : null;
            Transform attach = item != null ? item.transform.Find("attach") : null;
            ModLog.Verbose("InGameTests: World_GrenadeReady item " + (item != null) + ", flight " + (flight != null) + ", blast " + (blast != null)
                + (attack != null ? ", accuracy " + attack.m_projectileAccuracy + "/" + attack.m_projectileAccuracyMin + ", projectile " + (attack.m_attackProjectile != null ? attack.m_attackProjectile.name : "none") : "")
                + (aoe != null ? ", blast hits props " + aoe.m_hitProps + ", radius " + aoe.m_radius : ""));
            if (item == null || flight == null || blast == null) return "FAIL: missing prefab - item " + (item != null) + ", flight " + (flight != null) + ", blast " + (blast != null);
            if (attack.m_attackProjectile != flight) return "FAIL: the grenade throws " + (attack.m_attackProjectile != null ? attack.m_attackProjectile.name : "nothing") + ", not its own flight";
            if (attack.m_projectileAccuracy > 0f || attack.m_projectileAccuracyMin > 0f || attack.m_randomVelocity) return "FAIL: the throw has random spread, it would not follow the arc";
            if (aoe == null || !aoe.m_hitProps || aoe.m_hitTerrain) return "FAIL: the blast " + (aoe == null ? "has no Aoe" : (!aoe.m_hitProps ? "spares buildings (D-100: it must damage them)" : "changes the terrain"));
            if (attack.m_recoilPushback <= 0f) return "FAIL: the throw has no recoil (D-045: every weapon has some)";
            if (attach == null || attach.Find("gp_model") == null) return "FAIL: no clay pot in the hand model (attach)";
            return "PASS: grenade, flight and blast ready (radius " + aoe.m_radius + " m, speed " + attack.m_projectileVel + " m/s, no spread, recoil " + attack.m_recoilPushback + ", buildings spared)";
        }

        /// The local player exists once a world is loaded (example of a World_ test; replace with the mod's own checks).
        static string World_PlayerPresent()
        {
            var p = Player.m_localPlayer;
            ModLog.Verbose("InGameTests: World_PlayerPresent player " + (p != null ? p.GetPlayerName() : "null"));
            return p != null ? "PASS: " + p.GetPlayerName() + " at " + p.transform.position.ToString("F1") : "FAIL: no local player 5 s after the world loaded";
        }
    }
}
