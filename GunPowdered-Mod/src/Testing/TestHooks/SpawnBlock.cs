using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TEST ONLY (D-185, after lessons\examples\TestHooks\MobSpawnBlock). While armed, the game's SpawnSystem.Spawn is skipped for every spawn point within
    /// the radius of the local player - creatures AND the Ashlands' falling lava rocks (prefab LavaRock, a19ash7: one hit the Mobile Cannon for 60 while the
    /// lava test measured it). Arming also removes the lava rocks already in the air. Nothing is armed outside a test that asks for it; the arena step
    /// disarms it, and it lifts itself after <see cref="MaxSeconds"/> so a failed or aborted test never leaves the game's spawns off.
    /// </summary>
    internal static class SpawnBlock
    {
        /// Longer than the longest test that arms it (mobilecannon-lava: 260 s).
        internal const float MaxSeconds = 300f;

        const string LavaRockPrefab = "LavaRock";

        static float _radius;
        static float _until;

        /// Spawns skipped since the block was armed.
        internal static int Blocked { get; private set; }

        internal static bool Active => _radius > 0f && Time.time < _until;

        /// Starts skipping spawns within <paramref name="radius"/> m of the local player for <see cref="MaxSeconds"/> and removes the falling lava rocks
        /// within it. Expected outcome: "spawns blocked within R m for S s, removed N falling lava rock(s)" when <see cref="Active"/>.
        internal static string Arm(float radius)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"SpawnBlock.Arm(radius={radius})");
            if (!Active) Blocked = 0;
            _radius = radius;
            _until = Time.time + MaxSeconds;
            int removed = RemoveLavaRocks(radius);
            string s = $"spawns blocked within {radius:0} m for {MaxSeconds:0} s, removed {removed} falling lava rock(s), active {Active}";
            ModLog.Info("SpawnBlock: " + s);
            return s;
        }

        /// Stops skipping spawns (the arena step calls it). Expected outcome: not <see cref="Active"/>; logged only when it was armed.
        internal static bool Disarm()
        {
            bool was = Active;
            _radius = 0f;
            if (was) ModLog.Info($"SpawnBlock: disarmed after {Blocked} skipped spawn(s)");
            { var returnedAt48 = !Active; if (RuleLog.IsOn()) ModLog.Verbose($"SpawnBlock.Disarm: returns {returnedAt48} (line 48)"); return returnedAt48; }
        }

        /// <summary>
        /// The patch's decision for one spawn: not armed, no local player or the point outside the radius = the game spawns it; else it is skipped (logged).
        /// Expected outcome: true = spawn, false = skip.
        /// </summary>
        internal static bool Allows(string prefab, Vector3 spawnPoint)
        {
            Player player = Player.m_localPlayer;
            float distance = player != null ? Flat(spawnPoint - player.transform.position) : float.PositiveInfinity;
            bool allowed = !Active || distance > _radius;
            if (allowed) { if (RuleLog.Due("SpawnBlock.Allows:60", 5d)) ModLog.Every("SpawnBlock.Allows:60", 5d, $"SpawnBlock.Allows: returns {allowed} because !Active || distance > _radius (line 60)"); return allowed; }
            Blocked++;
            ModLog.Info($"SpawnBlock: spawn of {prefab} at {spawnPoint:F0} skipped, {distance:F0} m from the player ({Blocked} since armed)");
            { var returnedAt63 = allowed; if (RuleLog.IsOn()) ModLog.Verbose($"SpawnBlock.Allows: returns {returnedAt63} (line 63)"); return returnedAt63; }
        }

        // the lava rocks already falling when the block is armed: they were spawned before it and would still land
        static int RemoveLavaRocks(float radius)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"SpawnBlock.RemoveLavaRocks(radius={radius})");
            Player player = Player.m_localPlayer;
            if (player == null || ZNetScene.instance == null) { if (RuleLog.IsOn()) ModLog.Verbose($"SpawnBlock.RemoveLavaRocks: returns 0 because player == null || ZNetScene.instance == null (line 71)"); return 0; }
            int n = 0;
            foreach (ZNetView nv in Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
            {
                if (nv == null || !nv.IsValid() || Utils.GetPrefabName(nv.gameObject) != LavaRockPrefab) { if (RuleLog.IsOn()) ModLog.Verbose($"SpawnBlock.RemoveLavaRocks: skips to the next object (continue) because nv == null || !nv.IsValid() || Utils.GetPrefabName(nv.gameObject) != LavaRockPrefab (line 75)"); continue; }
                if (Flat(nv.transform.position - player.transform.position) > radius) { if (RuleLog.IsOn()) ModLog.Verbose($"SpawnBlock.RemoveLavaRocks: skips to the next object (continue) because Flat(nv.transform.position - player.transform.position) > radius (line 76)"); continue; }
                ModLog.Verbose("SpawnBlock: removes a falling lava rock at " + nv.transform.position.ToString("0"));
                nv.ClaimOwnership();
                ZNetScene.instance.Destroy(nv.gameObject);
                n++;
            }
            { var returnedAt82 = n; if (RuleLog.IsOn()) ModLog.Verbose($"SpawnBlock.RemoveLavaRocks: returns {returnedAt82} (line 82)"); return returnedAt82; }
        }

        static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;
    }
}
