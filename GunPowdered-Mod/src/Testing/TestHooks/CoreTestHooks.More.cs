using System;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Water.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>Core test hooks, second half (G04 split).</summary>
    internal static partial class CoreTestHooks
    {
        /// "visible" when the deposit shows its model, "hidden" when it does not (it hides while it is not full), "no deposit" otherwise.
        [TestHook("GunPowdered.depositVisible")]
        static string DepositVisible(GameObject target)
        {
            Pickable pick = target != null ? target.GetComponentInChildren<Pickable>() : null;
            Transform model = pick != null ? pick.transform.Find("gp_model") : null;
            if (model == null) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.DepositVisible: returns \"no deposit\" because model == null (line 237)"); return "no deposit"; }
            ModLog.Verbose("CoreTestHooks: deposit " + target.name + " model active " + model.gameObject.activeInHierarchy);
            return model.gameObject.activeInHierarchy ? "visible" : "hidden";
        }

        static float _markedGround = float.NaN;

        /// Remembers the terrain height at (x, z) before a test digs there (a generated ruin levels and digs its own ground). Returns the height, or -1 when the ground is not loaded.
        [TestHook("GunPowdered.terrainMark")]
        static double TerrainMark(double x, double z)
        {
            float h;
            if (ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(new Vector3((float)x, 0f, (float)z), out h)) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.TerrainMark: returns -1d because ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(new Vector3((float)x, 0f, (float)z), out h) (line 249)"); return -1d; }
            _markedGround = h;
            ModLog.Verbose("CoreTestHooks: terrain height marked " + h.ToString("0.00") + " at (" + x + ", " + z + ")");
            return System.Math.Round(h, 2);
        }

        /// Puts the terrain near (x, z) back to the lab template with KydlCore's terrain restore (D-175: the one restore of the lab, zone-keyed, also run at
        /// every test start). `radius` is kept for the test files; KydlCore restores every changed zone within its own range. Returns "reset N zone(s) ...".
        [TestHook("GunPowdered.terrainReset")]
        static string TerrainReset(double x, double z, double radius)
        {
            int restored = TestTerrainRestore.Run(ModContexts.For(typeof(CoreTestHooks)), new Vector3((float)x, 0f, (float)z));
            string result = "reset " + restored + " zone(s) put back to the template";
            ModLog.Verbose("CoreTestHooks: terrain reset near (" + x + ", " + z + ") asked radius " + radius + " m: " + result);
            return result;
        }

        /// "back" when the terrain height at (x, z) is within 5 cm of the height terrainMark remembered; otherwise says by how much it differs.
        [TestHook("GunPowdered.terrainBack")]
        static string TerrainBack(double x, double z)
        {
            float h;
            if (float.IsNaN(_markedGround)) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.TerrainBack: returns \"nothing marked\" because float.IsNaN(_markedGround) (line 276)"); return "nothing marked"; }
            if (ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(new Vector3((float)x, 0f, (float)z), out h)) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.TerrainBack: returns \"ground not loaded\" because ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(new Vector3((float)x, 0f, (float)z), out h) (line 277)"); return "ground not loaded"; }
            float diff = h - _markedGround;
            ModLog.Verbose("CoreTestHooks: terrain at (" + x + ", " + z + ") is " + h.ToString("0.00") + ", marked " + _markedGround.ToString("0.00"));
            return Mathf.Abs(diff) <= 0.05f ? "back" : "differs by " + diff.ToString("0.00") + " m";
        }
        /// Runs the real Draugr-pile mud placement (MudPlacer.NearPile) for a pile standing on the ground at (x, z); returns how many deposits it placed.
        [TestHook("GunPowdered.mudNearPile")]
        static int MudNearPile(double x, double z)
        {
            float ground;
            var pos = new Vector3((float)x, 0f, (float)z);
            if (ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(pos, out ground)) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.MudNearPile: returns -1 because ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(pos, out ground) (line 288)"); return -1; }
            pos.y = ground;
            int placed = GunPowdered.Features.Sulfur.Infrastructure.MudPlacer.NearPile(pos, (int)x * 7919 + (int)z);
            ModLog.Verbose("CoreTestHooks: mud next to a pile at " + pos.ToString("0.0") + " -> " + placed + " deposit(s)");
            return placed;
        }

        /// How many Draugr piles of generated locations asked for mud since the game started.
        [TestHook("GunPowdered.pilesSeen")]
        static int PilesSeen()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.PilesSeen()");
            return GunPowdered.Features.Sulfur.Infrastructure.MudPlacer.PilesSeen;
        }

        static float _lastArmAngle = float.NaN;

        /// "turning" when the mill's sweep arm moved since the last call, "still" when it did not, "first" on the first call; reads the real model part.
        [TestHook("GunPowdered.millArm")]
        static string MillArm(GameObject target)
        {
            Transform arm = null;
            if (target != null) foreach (Transform t in target.GetComponentsInChildren<Transform>(true)) if (t.name == "mill_arm") { arm = t; { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.MillArm: leaves the loop / case (break) because t.name == \"mill_arm\" (line 310)"); break; } }
            if (arm == null) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.MillArm: returns \"no arm\" because arm == null (line 311)"); return "no arm"; }
            float angle = arm.localEulerAngles.y;
            string state = float.IsNaN(_lastArmAngle) ? "first" : Mathf.Abs(Mathf.DeltaAngle(angle, _lastArmAngle)) > 1f ? "turning" : "still";
            ModLog.Verbose("CoreTestHooks: mill arm at " + angle.ToString("0.0") + " deg (before " + _lastArmAngle.ToString("0.0") + ") -> " + state);
            _lastArmAngle = angle;
            { var returnedAt316 = state; if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.MillArm: returns {returnedAt316} (line 316)"); return returnedAt316; }
        }

        /// Generates a real location at a world position with the game's TestSpawnLocation (e.g. a swamp ruin in the test world's lake). "spawned" or why not.
        [TestHook("GunPowdered.spawnLocationAt")]
        static string SpawnLocationAt(string name, double x, double z)
        {
            if (ZoneSystem.instance == null) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.SpawnLocationAt: returns \"no ZoneSystem\" because ZoneSystem.instance == null (line 323)"); return "no ZoneSystem"; }
            var pos = new Vector3((float)x, 0f, (float)z);
            float ground;
            if (!ZoneSystem.instance.GetGroundHeight(pos, out ground)) { var returnedAt326 = "ground at (" + x + ", " + z + ") not loaded"; if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.SpawnLocationAt: returns {returnedAt326} because !ZoneSystem.instance.GetGroundHeight(pos, out ground) (line 326)"); return returnedAt326; }
            pos.y = ground;
            bool ok = ZoneSystem.instance.TestSpawnLocation(name, pos, true);
            ModLog.Verbose("CoreTestHooks: " + name + " at " + pos.ToString("0.0") + " -> " + ok);
            return ok ? "spawned" : "refused";
        }

        /// Removes what a generated location left near (x, z): every object within `radius` m whose prefab is one of the location's own pieces (a swamp ruin's stone walls,
        /// pillars ...), so the next test finds the arena empty (run a5final-1: leftover ruin walls blocked visual-pieces' camera), AND the location's own LocationProxy.
        /// The proxy holds the location's non-networked body with its TerrainModifier (SwampRuin1: level radius 10, smooth 12); the game applies a TerrainModifier
        /// whenever a heightmap is rebuilt and never stores it in TerrainComp, so a terrain restore cannot undo it (a11gate 1-3: the ground stayed 6.76 m
        /// above the mark). Destroying the proxy destroys the modifier, whose OnDestroy re-pokes the heightmaps. Only proxies whose ZDO names THIS location are
        /// touched (the KydlFlat world's own locations stay). Returns "removed N object(s), P proxy(ies)".
        [TestHook("GunPowdered.removeLocationObjects")]
        static string RemoveLocationObjects(string name, double x, double z, double radius)
        {
            ModLog.Verbose("CoreTestHooks.RemoveLocationObjects(name=" + name + ", x=" + x + ", z=" + z + ", radius=" + radius + ")");
            if (ZoneSystem.instance == null || ZNetScene.instance == null) { ModLog.Verbose("CoreTestHooks.RemoveLocationObjects: no world - returns 'no world'"); return "no world"; }
            ZoneSystem.ZoneLocation loc = ZoneSystem.instance.m_locations.Find(l => l.m_prefabName == name);
            GameObject root = loc != null && loc.m_prefab != null ? loc.m_prefab.Asset : null;
            if (root == null) { ModLog.Verbose("CoreTestHooks.RemoveLocationObjects: the game knows no location " + name + " - returns 'no location'"); return "no location " + name; }
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (ZNetView v in root.GetComponentsInChildren<ZNetView>(true)) names.Add(Utils.GetPrefabName(v.gameObject));
            int locationHash = loc.m_prefab.Name.GetStableHashCode();
            var centre = new Vector2((float)x, (float)z);
            int removed = 0, proxies = 0;
            foreach (ZNetView nv in UnityEngine.Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
            {
                if (nv == null || !nv.IsValid() || nv.GetComponent<Player>() != null || nv.GetComponent<TerrainComp>() != null) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.RemoveLocationObjects: skips to the next item (continue) because nv == null || !nv.IsValid() || nv.GetComponent<Player>() != null || nv.GetComponent<TerrainComp>() != null (line 354)"); continue; }
                Vector3 p = nv.transform.position;
                if (Vector2.Distance(new Vector2(p.x, p.z), centre) > radius) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.RemoveLocationObjects: skips to the next item (continue) because Vector2.Distance(new Vector2(p.x, p.z), centre) > radius (line 356)"); continue; }
                bool ownProxy = nv.GetComponent<LocationProxy>() != null && nv.GetZDO().GetInt(ZDOVars.s_location) == locationHash;
                if (!ownProxy && !names.Contains(Utils.GetPrefabName(nv.gameObject))) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.RemoveLocationObjects: skips to the next item (continue) because !ownProxy && !names.Contains(Utils.GetPrefabName(nv.gameObject)) (line 358)"); continue; }
                if (ownProxy) ModLog.Info("CoreTestHooks: removing the LocationProxy of " + name + " at (" + p.x.ToString("0.0") + ", " + p.z.ToString("0.0") + ") with its terrain levelling");
                nv.ClaimOwnership();
                ZNetScene.instance.Destroy(nv.gameObject);
                if (ownProxy) proxies++; else removed++;
            }
            ModLog.Check("LocationProxy objects of " + name + " removed near (" + x + ", " + z + ")", proxies, 1, 0, "proxies");
            ModLog.Verbose("CoreTestHooks: removed " + removed + " object(s) and " + proxies + " proxy(ies) of location " + name + " near (" + x + ", " + z + "), " + names.Count + " prefab name(s)");
            return "removed " + removed + " object(s), " + proxies + " proxy(ies)";
        }
        static string _claimAnswer = "none";

        /// Asks the owner of a tagged location proxy to let THIS client fill the cave (the real CaveClaim path); read the answer with caveClaimAnswer.
        [TestHook("GunPowdered.claimCave")]
        static string ClaimCave(GameObject target)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.ClaimCave(target={target})");
            var claim = target != null ? target.GetComponentInParent<GunPowdered.Features.Guano.Infrastructure.CaveClaim>() : null;
            if (claim == null) { var returnedAt376 = "no CaveClaim on " + (target != null ? target.name : "nothing"); if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.ClaimCave: returns {returnedAt376} because claim == null (line 376)"); return returnedAt376; }
            _claimAnswer = "pending";
            claim.Request(granted => _claimAnswer = granted ? "granted" : "refused");
            { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.ClaimCave: returns \"asked\" (line 379)"); return "asked"; }
        }

        /// The owner's last answer to claimCave: "granted", "refused", "pending" or "none".
        [TestHook("GunPowdered.caveClaimAnswer")]
        static string CaveClaimAnswer() { return _claimAnswer; }

        /// How many of an item the player carries plus how many lie loose on the ground within 10 m (a pick drops items that are auto-picked a moment later).
        [TestHook("GunPowdered.countWithInventory")]
        static int CountWithInventory(string prefab)
        {
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.IsOn()) ModLog.Verbose($"CoreTestHooks.CountWithInventory: returns -1 because p == null (line 391)"); return -1; }
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            ItemDrop drop = go != null ? go.GetComponent<ItemDrop>() : null;
            int carried = drop != null ? p.GetInventory().CountItems(drop.m_itemData.m_shared.m_name) : 0;
            int loose = 0;
            foreach (ItemDrop d in UnityEngine.Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None))
                if (d != null && Utils.GetPrefabName(d.gameObject) == prefab && Vector3.Distance(d.transform.position, p.transform.position) < 10f)
                    loose += d.m_itemData.m_stack;
            ModLog.Verbose("CoreTestHooks: " + prefab + " carried " + carried + ", loose nearby " + loose);
            return carried + loose;
        }
    }
}
