// kydl:read-only-observer - test-only reads and writes on the mod's own pieces and the local player's inventory
using System;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Water.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the core behaviours (Rain Barrel, Leaching Station, bucket, deposits). They only read state, set up a starting state the game
    /// would reach slowly (water in a barrel, a batch that started a day ago) or call the mod's REAL code path (BucketUse.Use). The tests themselves use
    /// the game's own interaction, crafting and Bjornita's RPCs.
    /// </summary>
    internal static class CoreTestHooks
    {
        /// The value of one ZDO key of a tagged object as text ("int:1", "float:30", "long:123"; "none" when the key is not set, "no object" without one).
        [TestHook("GunPowdered.zdoValue")]
        static string ZdoValue(GameObject target, string key)
        {
            ZNetView view = target != null ? target.GetComponentInParent<ZNetView>() : null;
            if (view == null || !view.IsValid()) return "no object";
            ZDO zdo = view.GetZDO();
            string value = zdo.GetInt(key, int.MinValue) != int.MinValue ? zdo.GetInt(key).ToString()
                : !float.IsNaN(zdo.GetFloat(key, float.NaN)) ? zdo.GetFloat(key).ToString("0.###")
                : zdo.GetLong(key, long.MinValue) != long.MinValue ? zdo.GetLong(key).ToString()
                : "none";
            ModLog.Verbose("CoreTestHooks: zdo " + key + " of " + target.name + " = " + value + ", owner me " + view.IsOwner());
            return value;
        }

        /// Writes one ZDO value on a tagged object (type int / float / long) as its owner would: a starting state for a test. Returns "set" or why not.
        [TestHook("GunPowdered.setZdo")]
        static string SetZdo(GameObject target, string key, string type, double value)
        {
            ZNetView view = target != null ? target.GetComponentInParent<ZNetView>() : null;
            if (view == null || !view.IsValid()) return "no object";
            if (!view.IsOwner()) view.ClaimOwnership();
            ZDO zdo = view.GetZDO();
            switch (type)
            {
                case "int": zdo.Set(key, (int)value); break;
                case "float": zdo.Set(key, (float)value); break;
                case "long": zdo.Set(key, (long)value); break;
                default: return "unknown type " + type;
            }
            ModLog.Verbose("CoreTestHooks: set " + key + " (" + type + ") of " + target.name + " to " + value);
            return view.IsOwner() ? "set" : "not owner";
        }

        /// Uses the held item through the mod's real bucket code (BucketUse.Use). Returns "handled <prefab before>" or "not a bucket".
        [TestHook("GunPowdered.bucketUse")]
        static string BucketUseHook()
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            ItemDrop.ItemData held = p.GetCurrentWeapon();
            string before = BucketUse.PrefabOf(held);
            bool handled = BucketUse.Use(p, held);
            ModLog.Verbose("CoreTestHooks: bucket use with " + before + " -> handled " + handled);
            return handled ? "handled " + before : "not a bucket (" + before + ")";
        }

        /// The prefab of the item in the player's hand ("" with an empty hand).
        [TestHook("GunPowdered.heldPrefab")]
        static string HeldPrefab()
        {
            Player p = Player.m_localPlayer;
            return p != null ? BucketUse.PrefabOf(p.GetCurrentWeapon()) : "no player";
        }

        /// One ZDO value of a tagged object as a number (float, then int), for checks with min / max; -1 without an object.
        [TestHook("GunPowdered.zdoNumber")]
        static double ZdoNumber(GameObject target, string key)
        {
            ZNetView view = target != null ? target.GetComponentInParent<ZNetView>() : null;
            if (view == null || !view.IsValid()) return -1d;
            float f = view.GetZDO().GetFloat(key, float.NaN);
            double value = !float.IsNaN(f) ? f : view.GetZDO().GetInt(key, 0);
            ModLog.Verbose("CoreTestHooks: zdo number " + key + " of " + target.name + " = " + value.ToString("0.###"));
            return value;
        }

        /// Sets a float Tunable of ModSettings by its field name for the test ("reset" puts every touched one back). Returns the value now in use.
        [TestHook("GunPowdered.setTunable")]
        static string SetTunable(string field, string value)
        {
            var info = HarmonyLib.AccessTools.Field(typeof(GunPowdered.Configuration.ModSettings), field);
            if (!(info?.GetValue(null) is Tunable<float> t)) return "no float Tunable " + field;
            if (value == "reset") t.Reset();
            else t.Value = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            ModLog.Verbose("CoreTestHooks: Tunable " + field + " = " + t.Value);
            return t.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// Kills the nearest creature of a prefab within 30 m through the game's own damage (so its death drops run). Returns "killed <name>" or why not.
        [TestHook("GunPowdered.killNearest")]
        static string KillNearest(string prefab)
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            Character best = null;
            float bestDist = 30f;
            foreach (Character c in Character.GetAllCharacters())
            {
                float d = Vector3.Distance(c.transform.position, p.transform.position);
                if (c != p && Utils.GetPrefabName(c.gameObject) == prefab && d < bestDist) { best = c; bestDist = d; }
            }
            if (best == null) return "no " + prefab + " within 30 m";
            var hit = new HitData { m_point = best.transform.position, m_dir = Vector3.down };
            hit.m_damage.m_blunt = best.GetHealth() * 10f + 1000f;
            hit.SetAttacker(p);
            best.Damage(hit);
            ModLog.Verbose("CoreTestHooks: killed " + prefab + " at " + bestDist.ToString("0.0") + " m (health was " + best.GetHealth().ToString("0") + ")");
            return "killed " + prefab;
        }

        /// How many living creatures of a prefab are within 30 m of the player (e.g. the Blobs an Oozer splits into when it dies).
        [TestHook("GunPowdered.countCreatures")]
        static int CountCreatures(string prefab)
        {
            Player p = Player.m_localPlayer;
            if (p == null) return -1;
            int n = 0;
            foreach (Character c in Character.GetAllCharacters())
                if (c != null && !c.IsDead() && Utils.GetPrefabName(c.gameObject) == prefab && Vector3.Distance(c.transform.position, p.transform.position) < 30f) n++;
            ModLog.Verbose("CoreTestHooks: " + n + " living " + prefab + " within 30 m");
            return n;
        }

        /// Removes (without a death, so without drops) every creature of a prefab within 30 m: the Blobs an earlier Oozer test left alive (run regress1).
        [TestHook("GunPowdered.removeCreatures")]
        static int RemoveCreatures(string prefab)
        {
            Player p = Player.m_localPlayer;
            if (p == null || ZNetScene.instance == null) return -1;
            var doomed = new System.Collections.Generic.List<Character>();
            foreach (Character c in Character.GetAllCharacters())
                if (c != null && c != p && Utils.GetPrefabName(c.gameObject) == prefab && Vector3.Distance(c.transform.position, p.transform.position) < 30f) doomed.Add(c);
            foreach (Character c in doomed)
            {
                ZNetView view = c.GetComponent<ZNetView>();
                if (view != null && view.IsValid() && !view.IsOwner()) view.ClaimOwnership();
                ZNetScene.instance.Destroy(c.gameObject);
            }
            ModLog.Verbose("CoreTestHooks: removed " + doomed.Count + " " + prefab + " within 30 m");
            return doomed.Count;
        }

        /// Moves the player to the nearest shore within `radius` m and turns them to the water: open water lies 1.5-2.5 m ahead (where the bucket looks).
        /// Uses the game's own teleport. Returns "shore at (x, z), water ahead" or "no water within N m".
        [TestHook("GunPowdered.goToWater")]
        static string GoToWater(double radius)
        {
            Player p = Player.m_localPlayer;
            if (p == null || ZoneSystem.instance == null) return "no player";
            Vector3 from = p.transform.position, bestStand = Vector3.zero, bestDir = Vector3.zero;
            float best = float.MaxValue;
            bool found = false;   // Sonar S1244: no float equality on the sentinel
            for (float x = -(float)radius; x <= radius; x += 3f)
                for (float z = -(float)radius; z <= radius; z += 3f)
                {
                    Vector3 stand = new Vector3(from.x + x, 0f, from.z + z);
                    float ground;
                    if (!ZoneSystem.instance.GetGroundHeight(stand, out ground)) continue;     // only loaded ground: an unloaded point reads 0 m and looked like water (run core4)
                    if (ground <= ZoneSystem.instance.m_waterLevel + 0.2f) continue;            // must stand on dry land
                    for (int a = 0; a < 8; a++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, a * 45f, 0f) * Vector3.forward;
                        Vector3 ahead = stand + dir * 2f;
                        float aheadGround;
                        if (!ZoneSystem.instance.GetGroundHeight(ahead, out aheadGround) || aheadGround > ZoneSystem.instance.m_waterLevel - 0.3f) continue;   // 2 m ahead must be under water
                        float dist = new Vector2(x, z).magnitude;
                        if (dist < best) { found = true; best = dist; bestStand = new Vector3(stand.x, ground + 0.2f, stand.z); bestDir = dir; }
                    }
                }
            if (!found) return "no water within " + radius.ToString("0") + " m";
            p.TeleportTo(bestStand, Quaternion.LookRotation(bestDir), true);
            ModLog.Verbose("CoreTestHooks: shore at " + bestStand.ToString("0.0") + ", facing " + bestDir.ToString("0.0") + ", " + best.ToString("0") + " m away, water level " + ZoneSystem.instance.m_waterLevel);
            return "shore at (" + bestStand.x.ToString("0") + ", " + bestStand.z.ToString("0") + "), water ahead";
        }

        /// Removes loose items of a prefab within 30 m of the player (leftovers of an earlier test on the same arena spot: the lab's clearArea left them,
        /// run core3). Returns how many stacks were removed.
        [TestHook("GunPowdered.clearLoose")]
        static int ClearLoose(string prefab)
        {
            Player p = Player.m_localPlayer;
            if (p == null || ZNetScene.instance == null) return -1;
            int removed = 0;
            foreach (ItemDrop d in UnityEngine.Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None))
            {
                ZNetView view = d != null ? d.GetComponent<ZNetView>() : null;
                if (view == null || !view.IsValid() || Utils.GetPrefabName(d.gameObject) != prefab || Vector3.Distance(d.transform.position, p.transform.position) > 30f) continue;
                if (!view.IsOwner()) view.ClaimOwnership();
                ZNetScene.instance.Destroy(d.gameObject);
                removed++;
            }
            ModLog.Verbose("CoreTestHooks: removed " + removed + " loose stack(s) of " + prefab);
            return removed;
        }

        /// Refill test: runs the deposit's own respawn check now (the game runs it about once a minute) after the test moved world time on with the
        /// `skiptime` command. Returns "empty" or "refilled" (or why it could not check); logs the time since the pick against the respawn time.
        /// (Moving the pick time back instead fails in a young world: the game builds a DateTime from it, which throws below 0 ticks; run extra1.)
        [TestHook("GunPowdered.depositRespawnCheck")]
        static string DepositRespawnCheck(GameObject target)
        {
            Pickable pick = target != null ? target.GetComponentInChildren<Pickable>() : null;
            ZNetView view = pick != null ? pick.GetComponent<ZNetView>() : null;
            if (pick == null || view == null || !view.IsValid() || ZNet.instance == null) return "no deposit";
            if (!view.IsOwner()) view.ClaimOwnership();
            try
            {
                HarmonyLib.AccessTools.Method(typeof(Pickable), "UpdateRespawn").Invoke(pick, null);
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                ModLog.Verbose("CoreTestHooks: deposit respawn check threw " + e.InnerException);
                return "check threw " + (e.InnerException != null ? e.InnerException.GetType().Name + ": " + e.InnerException.Message : e.Message);
            }
            bool picked = view.GetZDO().GetBool(ZDOVars.s_picked);
            long pickedAt = view.GetZDO().GetLong(ZDOVars.s_pickedTime, 0L);
            double minutes = pickedAt > 1 ? (ZNet.instance.GetTime() - new DateTime(pickedAt)).TotalMinutes : -1d;
            ModLog.Verbose("CoreTestHooks: deposit " + target.name + " picked " + minutes.ToString("0.0") + " min ago, respawn after "
                + pick.m_respawnTimeMinutes.ToString("0") + " min -> " + (picked ? "empty" : "refilled"));
            return picked ? "empty" : "refilled";
        }

        /// "visible" when the deposit shows its model, "hidden" when it does not (it hides while it is not full), "no deposit" otherwise.
        [TestHook("GunPowdered.depositVisible")]
        static string DepositVisible(GameObject target)
        {
            Pickable pick = target != null ? target.GetComponentInChildren<Pickable>() : null;
            Transform model = pick != null ? pick.transform.Find("gp_model") : null;
            if (model == null) return "no deposit";
            ModLog.Verbose("CoreTestHooks: deposit " + target.name + " model active " + model.gameObject.activeInHierarchy);
            return model.gameObject.activeInHierarchy ? "visible" : "hidden";
        }

        static float _markedGround = float.NaN;

        /// Remembers the terrain height at (x, z) and copies the terrain pieces within TerrainMarkRadius m (TerrainSnapshot) before a test digs there (a generated ruin levels and digs its own ground). Returns the height, or -1 when the ground is not loaded.
        [TestHook("GunPowdered.terrainMark")]
        static double TerrainMark(double x, double z)
        {
            float h;
            if (ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(new Vector3((float)x, 0f, (float)z), out h)) return -1d;
            _markedGround = h;
            int pieces = TerrainSnapshot.Mark(new Vector3((float)x, 0f, (float)z), TerrainMarkRadius);
            ModLog.Verbose("CoreTestHooks: terrain height marked " + h.ToString("0.00") + " at (" + x + ", " + z + "), " + pieces + " terrain piece(s) copied");
            return System.Math.Round(h, 2);
        }

        /// How far round the marked spot terrain pieces are copied and restored (m).
        const float TerrainMarkRadius = 80f;

        /// Puts the terrain near (x, z) back the way terrainMark found it (TerrainSnapshot): the marked pieces get their old modifications back, pieces the test
        /// made are cleared. It never clears a piece it did not see at the mark, so the KydlFlat arena's own levelling stays (a11full1: the old blind clear dropped
        /// the arena ground and the player fell through it in every later test). `radius` is kept for the test files; the mark's radius decides. Returns
        /// "reset N restored, M cleared".
        [TestHook("GunPowdered.terrainReset")]
        static string TerrainReset(double x, double z, double radius)
        {
            string result = TerrainSnapshot.Restore(new Vector3((float)x, 0f, (float)z), TerrainMarkRadius);
            ModLog.Verbose("CoreTestHooks: terrain reset near (" + x + ", " + z + ") asked radius " + radius + " m: " + result);
            return result;
        }

        /// "back" when the terrain height at (x, z) is within 5 cm of the height terrainMark remembered; otherwise says by how much it differs.
        [TestHook("GunPowdered.terrainBack")]
        static string TerrainBack(double x, double z)
        {
            float h;
            if (float.IsNaN(_markedGround)) return "nothing marked";
            if (ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(new Vector3((float)x, 0f, (float)z), out h)) return "ground not loaded";
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
            if (ZoneSystem.instance == null || !ZoneSystem.instance.GetGroundHeight(pos, out ground)) return -1;
            pos.y = ground;
            int placed = GunPowdered.Features.Sulfur.Infrastructure.MudPlacer.NearPile(pos, (int)x * 7919 + (int)z);
            ModLog.Verbose("CoreTestHooks: mud next to a pile at " + pos.ToString("0.0") + " -> " + placed + " deposit(s)");
            return placed;
        }

        /// How many Draugr piles of generated locations asked for mud since the game started.
        [TestHook("GunPowdered.pilesSeen")]
        static int PilesSeen()
        {
            return GunPowdered.Features.Sulfur.Infrastructure.MudPlacer.PilesSeen;
        }

        static float _lastArmAngle = float.NaN;

        /// Setup for the mill test: writes the "window open" stamp a crafting player's client writes (PowderMillSpin.Stamp) with the current world time.
        [TestHook("GunPowdered.millStampNow")]
        static string MillStampNow(GameObject target)
        {
            ZNetView view = target != null ? target.GetComponentInParent<ZNetView>() : null;
            if (view == null || !view.IsValid() || ZNet.instance == null) return "no mill";
            view.GetZDO().Set("gp_mill_use", (long)(ZNet.instance.GetTimeSeconds() * 1000d));
            return "stamped";
        }

        /// "turning" when the mill's sweep arm moved since the last call, "still" when it did not, "first" on the first call; reads the real model part.
        [TestHook("GunPowdered.millArm")]
        static string MillArm(GameObject target)
        {
            Transform arm = null;
            if (target != null) foreach (Transform t in target.GetComponentsInChildren<Transform>(true)) if (t.name == "mill_arm") { arm = t; break; }
            if (arm == null) return "no arm";
            float angle = arm.localEulerAngles.y;
            string state = float.IsNaN(_lastArmAngle) ? "first" : Mathf.Abs(Mathf.DeltaAngle(angle, _lastArmAngle)) > 1f ? "turning" : "still";
            ModLog.Verbose("CoreTestHooks: mill arm at " + angle.ToString("0.0") + " deg (before " + _lastArmAngle.ToString("0.0") + ") -> " + state);
            _lastArmAngle = angle;
            return state;
        }

        /// Generates a real location at a world position with the game's TestSpawnLocation (e.g. a swamp ruin in the test world's lake). "spawned" or why not.
        [TestHook("GunPowdered.spawnLocationAt")]
        static string SpawnLocationAt(string name, double x, double z)
        {
            if (ZoneSystem.instance == null) return "no ZoneSystem";
            var pos = new Vector3((float)x, 0f, (float)z);
            float ground;
            if (!ZoneSystem.instance.GetGroundHeight(pos, out ground)) return "ground at (" + x + ", " + z + ") not loaded";
            pos.y = ground;
            bool ok = ZoneSystem.instance.TestSpawnLocation(name, pos, true);
            ModLog.Verbose("CoreTestHooks: " + name + " at " + pos.ToString("0.0") + " -> " + ok);
            return ok ? "spawned" : "refused";
        }

        /// Removes what a generated location left near (x, z): every object within `radius` m whose prefab is one of the location's own pieces (a swamp ruin's stone walls,
        /// pillars ...), so the next test finds the arena empty (run a5final-1: leftover ruin walls blocked visual-pieces' camera), AND the location's own LocationProxy.
        /// The proxy holds the location's non-networked body with its TerrainModifier (SwampRuin1: level radius 10, smooth 12); the game applies a TerrainModifier
        /// whenever a heightmap is rebuilt and never stores it in TerrainComp, so TerrainSnapshot.Restore cannot undo it (a11gate 1-3: the ground stayed 6.76 m
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
                if (nv == null || !nv.IsValid() || nv.GetComponent<Player>() != null || nv.GetComponent<TerrainComp>() != null) continue;
                Vector3 p = nv.transform.position;
                if (Vector2.Distance(new Vector2(p.x, p.z), centre) > radius) continue;
                bool ownProxy = nv.GetComponent<LocationProxy>() != null && nv.GetZDO().GetInt(ZDOVars.s_location) == locationHash;
                if (!ownProxy && !names.Contains(Utils.GetPrefabName(nv.gameObject))) continue;
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
            var claim = target != null ? target.GetComponentInParent<GunPowdered.Features.Guano.Infrastructure.CaveClaim>() : null;
            if (claim == null) return "no CaveClaim on " + (target != null ? target.name : "nothing");
            _claimAnswer = "pending";
            claim.Request(granted => _claimAnswer = granted ? "granted" : "refused");
            return "asked";
        }

        /// The owner's last answer to claimCave: "granted", "refused", "pending" or "none".
        [TestHook("GunPowdered.caveClaimAnswer")]
        static string CaveClaimAnswer() { return _claimAnswer; }

        /// How many of an item the player carries plus how many lie loose on the ground within 10 m (a pick drops items that are auto-picked a moment later).
        [TestHook("GunPowdered.countWithInventory")]
        static int CountWithInventory(string prefab)
        {
            Player p = Player.m_localPlayer;
            if (p == null) return -1;
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
