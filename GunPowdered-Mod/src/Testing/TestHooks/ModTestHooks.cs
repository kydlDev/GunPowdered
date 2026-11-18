using KydlCore.Common;
using GunPowdered.Bootstrap;
using GunPowdered.Features.Example.Infrastructure;
using GunPowdered.Presentation.Hub.Commands;
using GunPowdered.Presentation.Hub.Registration;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// DrakesTestLab hooks (spec 8.7) for the suite in tests/TestLab/suites/GunPowdered. A hook is a static method with [TestHook("name")];
    /// DrakesTestLab finds it by the attribute's NAME (the mod has no reference to DrakesTestLab; TestHookAttribute comes from KydlCore.dll:
    /// KydlCore.Common.TestHookAttribute, KydlCore\KydlCore-Mod\src\Shared\Common\TestLabSupport.cs). Names start with "GunPowdered." so two mods never clash.
    /// Hooks call the REAL code paths (the command dispatcher, the feature registry), never a copy of a rule. Only add a hook where a normal
    /// player action or a built-in check cannot reach the behaviour. Reads return short values a suite compares with expect / contains / notContains / regex (NOT min / max: a `do: hook` step silently ignores them, PROJECT_RULES 21 - numeric limits go into `wait until`, a `check` step or `asserts`). A hook that returns false for a reason logs that reason (throttled, ModLog.IsDue): lessons\DIAGNOSTIC_LOGGING.md section 3.
    /// </summary>
    internal static class ModTestHooks
    {
        /// Every test starts here (MIGRATION_DECISIONS C26, lessons TESTLAB_PLAYBOOK): the player on the open arena spot 70 m from the lab start, facing +z, god mode on.
        [TestHook("GunPowdered.arena")]
        static string Arena() { return TestArena.MovePlayer(); }

        /// All Startup_* / World_* checks of InGameTests (the ones Kydl.GameProbe runs): "PASS: ..." or "FAIL: ...".
        [TestHook("GunPowdered.selftest")]
        static string SelfTest() { return TestLabSupport.RunInGameTests(typeof(InGameTests)); }

        /// Is the feature running (FeatureRegistry state after its Enable switches)?
        [TestHook("GunPowdered.featureRunning")]
        static bool FeatureRunning(string id) { return ModInstaller.Current != null && ModInstaller.Current.Features.IsRunning(id); }

        /// Is the example tab registered in the KY hub right now (no stale tab after the feature stops)?
        [TestHook("GunPowdered.exampleTab")]
        static bool ExampleTab() { return global::KydlCore.Features.Hub.Contracts.KyHub.HasTab(Plugin.ModGuid, HubTabs.ExampleId); }

        /// EXAMPLE: the same path as the hub slider (dispatcher -> handler -> rules -> executor). Returns the response, e.g. "Success: clamped into range".
        [TestHook("GunPowdered.example.set")]
        static string ExampleSet(double value)
        {
            if (ModInstaller.Current == null) return "Failed: mod not installed";
            return ExampleCommands.SetValue(ModInstaller.Current.Deps.Commands, (float)value).ToString();
        }

        /// Generates a real location (e.g. Crypt2, TrollCave02, MountainCave02, FireHole) several times through the game's own ZoneSystem.SpawnLocation (SpawnMode.Full), so the
        /// mod's placement patches run as in a real world. Returns "spawned N of M". Each copy is far from the others, in the interior band for dungeons.
        [TestHook("GunPowdered.spawnLocations")]
        static string SpawnLocations(string name, int count)
        {
            var zs = ZoneSystem.instance;
            if (zs == null) return "FAIL: no ZoneSystem";
            ZoneSystem.ZoneLocation loc = zs.m_locations.Find(l => l.m_prefabName == name);
            if (loc == null) return "FAIL: no location " + name;
            if (loc.m_prefab != null) loc.m_prefab.Load();
            var method = HarmonyLib.AccessTools.Method(typeof(ZoneSystem), "SpawnLocation");
            if (method == null) return "FAIL: ZoneSystem.SpawnLocation not found";
            var pars = method.GetParameters();
            bool interior = loc.m_prefab != null && loc.m_prefab.IsLoaded && loc.m_prefab.Asset.GetComponentInChildren<DungeonGenerator>(true) != null;
            int ok = 0;
            for (int i = 0; i < count; i++)
            {
                var args = new object[pars.Length];
                var pos = new UnityEngine.Vector3(-6000f + (i % 10) * 400f, interior ? 5000f : 40f, -6000f + (i / 10) * 400f);
                for (int p = 0; p < pars.Length; p++)
                {
                    var t = pars[p].ParameterType;
                    if (t == typeof(ZoneSystem.ZoneLocation)) args[p] = loc;
                    else if (t == typeof(int)) args[p] = 1000 + i;
                    else if (t == typeof(UnityEngine.Vector3)) args[p] = pos;
                    else if (t == typeof(UnityEngine.Quaternion)) args[p] = UnityEngine.Quaternion.identity;
                    else if (t == typeof(ZoneSystem.SpawnMode)) args[p] = ZoneSystem.SpawnMode.Full;
                    else if (t == typeof(System.Collections.Generic.List<UnityEngine.GameObject>)) args[p] = new System.Collections.Generic.List<UnityEngine.GameObject>();
                    else args[p] = pars[p].HasDefaultValue ? pars[p].DefaultValue : null;
                }
                if (method.Invoke(zs, args) != null) ok++;
            }
            string why = ok == 0 ? " (prefab loaded " + (loc.m_prefab != null && loc.m_prefab.IsLoaded) + ", valid " + (loc.m_prefab != null && loc.m_prefab.IsValid) + ", method returns " + method.ReturnType.Name + ", enabled " + loc.m_enable + ")" : "";
            return "spawned " + ok + " of " + count + why;
        }

        static int _spawnLeft;

        /// Like spawnLocations, but one copy at a time with the player standing at it (objects far from every player are never kept active): starts a coroutine, returns "started N".
        /// Poll GunPowdered.spawnLeft until it is 0; the player goes back to the arena afterwards.
        [TestHook("GunPowdered.spawnNear")]
        static string SpawnNear(string name, int count, double waitSeconds)
        {
            if (_spawnLeft > 0) return "FAIL: a spawn run is still going (" + _spawnLeft + " left)";
            _spawnLeft = count;
            Plugin.Instance.StartCoroutine(SpawnNearRun(name, count, (float)waitSeconds));
            return "started " + count;
        }

        [TestHook("GunPowdered.spawnLeft")]
        static int SpawnLeft() { return _spawnLeft; }

        static System.Collections.IEnumerator SpawnNearRun(string name, int count, float waitSeconds)
        {
            var zs = ZoneSystem.instance;
            ZoneSystem.ZoneLocation loc = zs != null ? zs.m_locations.Find(l => l.m_prefabName == name) : null;
            var method = HarmonyLib.AccessTools.Method(typeof(ZoneSystem), "SpawnLocation");
            if (loc == null || method == null) { ModLog.Warn("spawnNear: no location " + name + " or no SpawnLocation method"); _spawnLeft = 0; yield break; }
            if (loc.m_prefab != null) loc.m_prefab.Load();
            bool interior = loc.m_prefab != null && loc.m_prefab.IsLoaded && loc.m_prefab.Asset.GetComponentInChildren<DungeonGenerator>(true) != null;
            var info = HarmonyLib.Harmony.GetPatchInfo(method);
            if (info != null)
            {
                foreach (var pf in info.Prefixes) ModLog.Verbose("spawnNear: SpawnLocation prefix " + pf.owner + " " + pf.PatchMethod.DeclaringType.Name + "." + pf.PatchMethod.Name + " prio " + pf.priority);
                foreach (var pf in info.Postfixes) ModLog.Verbose("spawnNear: SpawnLocation postfix " + pf.owner + " " + pf.PatchMethod.DeclaringType.Name + "." + pf.PatchMethod.Name);
                foreach (var pf in info.Transpilers) ModLog.Verbose("spawnNear: SpawnLocation transpiler " + pf.owner + " " + pf.PatchMethod.DeclaringType.Name + "." + pf.PatchMethod.Name);
            }
            int made = 0;
            var start = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : UnityEngine.Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                UnityEngine.Vector3 pos;
                if (interior)
                {
                    pos = new UnityEngine.Vector3(-3000f + i * 300f, 5000f, 3000f);
                    var player = Player.m_localPlayer;
                    if (player != null) player.transform.position = pos + UnityEngine.Vector3.up * 3f;
                }
                else
                {
                    // outdoor locations: on the flat arena ground beside the player, where the terrain collider exists (the floor ray of the placers needs it)
                    var around = start + new UnityEngine.Vector3(-30f + (i % 6) * 12f, 0f, 25f + (i / 6) * 14f);
                    float ground;
                    zs.GetGroundHeight(around, out ground);
                    pos = new UnityEngine.Vector3(around.x, ground, around.z);
                }
                yield return new UnityEngine.WaitForSeconds(1f);
                if (zs.TestSpawnLocation(name, pos, true)) made++;
                yield return new UnityEngine.WaitForSeconds(waitSeconds);
                _spawnLeft = count - i - 1;
            }
            ModLog.Verbose("spawnNear: " + name + " " + made + " of " + count + " created");
            _spawnLeft = 0;
            TestArena.MovePlayer();
        }

        /// How many live game objects have a prefab name starting with the given text (counts the deposits the placement patches made).
        [TestHook("GunPowdered.countObjects")]
        static int CountObjects(string prefix)
        {
            int n = 0;
            foreach (var view in UnityEngine.Object.FindObjectsByType<ZNetView>(UnityEngine.FindObjectsSortMode.None))
                if (view != null && view.gameObject.name.StartsWith(prefix)) n++;
            return n;
        }

        /// Is the guano deposit one of the vegetation entries of an alt biome called Bat Swamp (BatSwampPatch ran)? Returns the number of entries.
        [TestHook("GunPowdered.batSwampEntries")]
        static int BatSwampEntries()
        {
            int n = 0;
            foreach (var list in ZoneSystem.instance.m_altBiomeLists)
                foreach (var alt in list.m_alts)
                    if (alt.m_name == "Bat Swamp") foreach (var v in alt.m_addVegetation) if (v.m_name == "GP_GuanoDeposit") n++;
            return n;
        }

        /// EXAMPLE: the current value.
        [TestHook("GunPowdered.example.value")]
        static double ExampleValue() { return ExampleFeature.Instance != null ? ExampleFeature.Instance.Value : double.NaN; }
    }
}
