using GunPowdered.Core;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// D-184 (user 2026-11-24 09:10: "lava and fire damage ... Can you do tests for this?", 09:11: protect against ash damage too): the Mobile Cannon beside a
    /// plain wooden wall in the game's own damage: lava (WearNTear.UpdateAshlandsDamage: 70 % x lava of the max health every 2 s within 1.5 m of lava),
    /// Ashlands ash (Game.m_ashDamage % every 5 s without a roof) and spreading fire (the Fire object: chop + fire hits on burnable pieces). The wall must
    /// lose health - that proves the spot really damages - and the cannon must not.
    /// </summary>
    internal static class AshlandsTestHooks
    {
        const string WallPrefab = "woodwall";
        static GameObject _wall;
        static float _cannonBefore = float.NaN, _wallBefore = float.NaN;
        static Vector3 _spot;
        static float _ashBefore = float.NaN;

        /// <summary>
        /// D-185: no game spawn within <paramref name="radius"/> m of the player for the rest of the test (creatures and falling lava rocks; a19ash7: a lava rock
        /// hit the cannon for 60 during the measurement). Call it at the test spot, before the pieces are placed. Returns SpawnBlock.Arm's text.
        /// </summary>
        [TestHook("GunPowdered.spawnBlock")]
        static string SpawnBlockArm(double radius) => SpawnBlock.Arm((float)radius);

        /// <summary>Puts the game's ash damage back to what it was before the first ashDamage call. Returns "ash damage back to V" or "unchanged".</summary>
        [TestHook("GunPowdered.ashDamageReset")]
        static string AshDamageReset()
        {
            if (Game.instance == null || float.IsNaN(_ashBefore)) { ModLog.Verbose("AshlandsTestHooks.AshDamageReset: returns \"unchanged\" because Game.instance == null || float.IsNaN(_ashBefore)"); return "unchanged"; }
            Game.instance.m_ashDamage = _ashBefore;
            string s = $"ash damage back to {_ashBefore:0.##}";
            _ashBefore = float.NaN;
            ModLog.Info("AshlandsTestHooks: " + s);
            return s;
        }

        /// <summary>
        /// Teleports the player into the Ashlands, to a place the world generator says is lava (<paramref name="lava"/> true: its lava mask 0.7 or more at
        /// the spot and 2.5 m beside it) or dry open ground (false: no lava within 8 m), well inside the biome (all four points 48 m around are Ashlands, so
        /// the 64 m heightmap there is no biome edge - Heightmap.GetLava answers 0 on an edge). Returns "going to (x, z), lava L" or why not.
        /// </summary>
        [TestHook("GunPowdered.ashlandsGo")]
        static string AshlandsGo(bool lava)
        {
            Player p = Player.m_localPlayer;
            WorldGenerator g = WorldGenerator.instance;
            if (p == null || g == null) { ModLog.Verbose("AshlandsTestHooks.AshlandsGo: returns \"no player or no world generator\" because p == null || g == null (line 50)"); return "no player or no world generator"; }
            for (float z = -6000f; z >= -10000f; z -= 37f)
                for (float x = -2000f; x <= 2000f; x += 41f)
                {
                    if (!DeepInAshlands(g, x, z)) { if (RuleLog.Due("AshlandsTestHooks.AshlandsGo:biome", 2d)) ModLog.Every("AshlandsTestHooks.AshlandsGo:biome", 2d, "AshlandsTestHooks.AshlandsGo: skips to the next item (continue) because the point is not deep in the Ashlands (line 54)"); continue; }
                    float here = MaskLava(g, x, z), side = MaskLava(g, x - 2.5f, z);
                    // a19ash6: the first dry place was sea floor (ground 1.7 m, the sea at 30 m): under water nothing wears, so dry land well above the sea
                    bool fits = lava ? here >= 0.7f && side >= 0.7f : g.GetHeight(x, z) > AboveSea && g.GetHeight(x - 2.5f, z) > AboveSea && DryAround(g, x, z, 8f);
                    if (!fits) { if (RuleLog.Due("AshlandsTestHooks.AshlandsGo:fit", 2d)) ModLog.Every("AshlandsTestHooks.AshlandsGo:fit", 2d, "AshlandsTestHooks.AshlandsGo: skips to the next item (continue) because the point does not fit (line 58)"); continue; }
                    var to = new Vector3(x, g.GetHeight(x, z) + 3f, z - 4f);       // 4 m south of the spot, facing it (north)
                    _goal = new Vector3(x, 0f, z);
                    // a19ash4: one teleport did not happen (the player stayed in the Meadows); the game refuses while teleporting or within 2 s of the last one
                    if (!p.TeleportTo(to, Quaternion.identity, true)) { ModLog.Verbose($"AshlandsTestHooks.AshlandsGo: returns \"teleport refused\" because the game's TeleportTo said no (teleporting {p.IsTeleporting()})"); return "teleport refused (teleporting " + p.IsTeleporting() + ")"; }
                    string s = $"going to ({x:0}, {z:0}), lava {here:0.00}";
                    ModLog.Info("AshlandsTestHooks: " + s);
                    return s;
                }
            ModLog.Verbose("AshlandsTestHooks.AshlandsGo: returns \"no place found\" because the scan found none (line 67)");
            return "no place found";
        }

        static Vector3 _goal;

        /// <summary>Ground height (m) a dry test place needs: 6 m above the game's sea level (30 m).</summary>
        const float AboveSea = 36f;

        static bool DeepInAshlands(WorldGenerator g, float x, float z)
        {
            bool deep = g.GetBiome(x, z) == Heightmap.Biome.AshLands && g.GetBiome(x + 48f, z) == Heightmap.Biome.AshLands && g.GetBiome(x - 48f, z) == Heightmap.Biome.AshLands
                && g.GetBiome(x, z + 48f) == Heightmap.Biome.AshLands && g.GetBiome(x, z - 48f) == Heightmap.Biome.AshLands;
            if (RuleLog.Due("AshlandsTestHooks.DeepInAshlands", 2d)) ModLog.Every("AshlandsTestHooks.DeepInAshlands", 2d, $"AshlandsTestHooks.DeepInAshlands: returns {deep} at ({x:0}, {z:0})");
            return deep;
        }

        static float MaskLava(WorldGenerator g, float x, float z)
        {
            g.GetHeight(x, z, out Color mask);
            if (RuleLog.Due("AshlandsTestHooks.MaskLava", 2d)) ModLog.Every("AshlandsTestHooks.MaskLava", 2d, $"AshlandsTestHooks.MaskLava: returns {mask.a:0.00} at ({x:0}, {z:0})");
            return mask.a;
        }

        static bool DryAround(WorldGenerator g, float x, float z, float r)
        {
            for (float dx = -r; dx <= r; dx += 2f)
                for (float dz = -r; dz <= r; dz += 2f)
                    if (MaskLava(g, x + dx, z + dz) > 0.02f) { if (RuleLog.Due("AshlandsTestHooks.DryAround:no", 2d)) ModLog.Every("AshlandsTestHooks.DryAround:no", 2d, "AshlandsTestHooks.DryAround: returns false because there is lava nearby (line 95)"); return false; }
            if (RuleLog.Due("AshlandsTestHooks.DryAround:yes", 2d)) ModLog.Every("AshlandsTestHooks.DryAround:yes", 2d, $"AshlandsTestHooks.DryAround: returns true - no lava within {r} m (line 96)");
            return true;
        }

        /// <summary>"biome B, teleporting T, zone loaded L": whether the player has arrived (test steps wait until "biome AshLands, teleporting False, zone loaded True").</summary>
        [TestHook("GunPowdered.whereAmI")]
        static string WhereAmI()
        {
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("AshlandsTestHooks.WhereAmI: returns \"no player\" because p == null (line 105)"); return "no player"; }
            Vector3 at = p.transform.position;
            bool loaded = ZoneSystem.instance != null && ZoneSystem.instance.IsZoneLoaded(at);
            { var s = $"biome {Heightmap.FindBiome(at)}, teleporting {p.IsTeleporting()}, zone loaded {loaded}"; if (RuleLog.Due("AshlandsTestHooks.WhereAmI", 2d)) ModLog.Every("AshlandsTestHooks.WhereAmI", 2d, $"AshlandsTestHooks.WhereAmI: returns {s}"); return s; }
        }

        /// <summary>
        /// Finds ground on lava (lava ≥ 0.6) near the player where a second point 2.5 m to the side is lava too, and stands the player 4 m in front of it,
        /// facing it (mobileCannonPlace 4 puts the cannon on it). <paramref name="lava"/> false finds plain Ashlands ground with no lava within 6 m instead
        /// (the ash test). Returns "lava spot (x, y, z) lava L" / "dry spot ..." or why not.
        /// </summary>
        [TestHook("GunPowdered.ashSpot")]
        static string AshSpot(bool lava)
        {
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("AshlandsTestHooks.AshSpot: returns \"no player\" because p == null (line 120)"); return "no player"; }
            Vector3 c = _goal != Vector3.zero ? new Vector3(_goal.x, p.transform.position.y, _goal.z) : p.transform.position;
            for (float r = 0f; r <= 60f; r += 2f)
                for (float a = 0f; a < 360f; a += (r == 0f ? 360f : 20f))
                {
                    Vector3 at = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * r;
                    Vector3 side = at + Vector3.left * 2.5f;     // the wall stands 2.5 m to the west (the player faces north)
                    float l1 = Lava(at), l2 = Lava(side);
                    if (float.IsNaN(l1) || float.IsNaN(l2)) { if (RuleLog.Due("AshlandsTestHooks.AshSpot:nan", 2d)) ModLog.Every("AshlandsTestHooks.AshSpot:nan", 2d, "AshlandsTestHooks.AshSpot: skips to the next item (continue) because the ground there is not loaded (line 128)"); continue; }
                    bool fits = lava ? l1 >= 0.6f && l2 >= 0.6f : Dry(at, 6f) && Dry(side, 6f) && OpenSky(at) && OpenSky(side) && Ground(at) > AboveSea - 3f && Ground(side) > AboveSea - 3f;     // a19ash3: the wall stood under a rock arch - an "ash roof"
                    if (!fits) { if (RuleLog.Due("AshlandsTestHooks.AshSpot:no", 2d)) ModLog.Every("AshlandsTestHooks.AshSpot:no", 2d, "AshlandsTestHooks.AshSpot: skips to the next item (continue) because the spot does not fit (line 130)"); continue; }
                    _spot = at;
                    Vector3 dir = Vector3.forward;
                    Vector3 stand = at - dir * 4f;
                    stand.y = Ground(stand) + 0.2f;
                    p.transform.position = stand;
                    p.transform.rotation = Quaternion.LookRotation(dir);
                    p.SetLookDir(dir);
                    string s = $"{(lava ? "lava" : "dry")} spot {at:F1} lava {l1:0.00}/{l2:0.00}, biome {Heightmap.FindBiome(at)}";
                    ModLog.Info("AshlandsTestHooks: " + s);
                    return s;
                }
            ModLog.Verbose("AshlandsTestHooks.AshSpot: returns \"no spot\" because the scan found none within 60 m of the goal");
            return "no spot within 60 m";
        }

        /// <summary>Nothing above the ground there for 60 m (the game's ash damage spares pieces under a roof, WearNTear.HaveAshRoof).</summary>
        static bool OpenSky(Vector3 at)
        {
            Vector3 from = new Vector3(at.x, Ground(at) + 0.5f, at.z);
            bool open = !Physics.Raycast(from, Vector3.up, 60f, LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain"), QueryTriggerInteraction.Ignore);
            if (RuleLog.Due("AshlandsTestHooks.OpenSky", 2d)) ModLog.Every("AshlandsTestHooks.OpenSky", 2d, $"AshlandsTestHooks.OpenSky: returns {open} at {from:F0}");
            return open;
        }

        static float Lava(Vector3 at)
        {
            Heightmap h = Heightmap.FindHeightmap(at);
            { float l = h != null ? h.GetLava(at) : float.NaN; if (RuleLog.Due("AshlandsTestHooks.Lava", 2d)) ModLog.Every("AshlandsTestHooks.Lava", 2d, $"AshlandsTestHooks.Lava: returns {l} at {at:F0}"); return l; }
        }

        static bool Dry(Vector3 at, float radius)
        {
            for (float x = -radius; x <= radius; x += 2f)
                for (float z = -radius; z <= radius; z += 2f)
                {
                    float l = Lava(at + new Vector3(x, 0f, z));
                    if (float.IsNaN(l) || l > 0.05f) { if (RuleLog.Due("AshlandsTestHooks.Dry:no", 2d)) ModLog.Every("AshlandsTestHooks.Dry:no", 2d, "AshlandsTestHooks.Dry: returns false because there is lava (or no ground) nearby (line 167)"); return false; }
                }
            { if (RuleLog.Due("AshlandsTestHooks.Dry:yes", 2d)) ModLog.Every("AshlandsTestHooks.Dry:yes", 2d, $"AshlandsTestHooks.Dry: returns true - no lava within {radius} m of {at:F0}"); return true; }
        }

        /// <summary>
        /// The surface a player standing there stands on. a19ash2 / a19ash5: in the Ashlands both ZoneSystem.GetGroundHeight and the heightmap answered 1.7 m
        /// where the player stood at 28.7 m, and the wall went in under the ground; a ray down from 40 m above the given height onto terrain and solid rock
        /// finds the real surface.
        /// </summary>
        internal static float Ground(Vector3 at)
        {
            // a20ash2 rep02-mobilecannon-ash: the ray hit the crown of an AshlandsTree1 (51.8 m over ground at 36.6 m): the wall stood in the tree, the game
            // counted the tree as its ash roof and the wall never wore. Trees are skipped: the nearest hit that is not part of a tree is the ground.
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(at.x, at.y + 40f, at.z), Vector3.down, 200f, LayerMask.GetMask("terrain", "static_solid", "Default"), QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (IsTree(hit.collider)) { if (RuleLog.Due("AshlandsTestHooks.Ground:tree", 2d)) ModLog.Every("AshlandsTestHooks.Ground:tree", 2d, $"AshlandsTestHooks.Ground: skips to the next item (continue) because {hit.collider.name} at {hit.point.y:0.0} m is part of a tree (line 185)"); continue; }
                if (RuleLog.Due("AshlandsTestHooks.Ground:ray", 2d)) ModLog.Every("AshlandsTestHooks.Ground:ray", 2d, $"AshlandsTestHooks.Ground: returns {hit.point.y:0.0} - the ray down hit {hit.collider.name}");
                return hit.point.y;
            }
            float h = Heightmap.GetHeight(at, out float m) ? m : ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out float g) ? g : at.y;
            if (RuleLog.Due("AshlandsTestHooks.Ground:map", 2d)) ModLog.Every("AshlandsTestHooks.Ground:map", 2d, $"AshlandsTestHooks.Ground: returns {h:0.0} from the heightmap - the ray down hit nothing");
            return h;
        }

        /// <summary>A collider of a choppable tree (the game's TreeBase), or of an object whose prefab name says tree (AshlandsTree1: its roof collider "default").</summary>
        static bool IsTree(Collider c)
        {
            bool tree = c.GetComponentInParent<TreeBase>() != null || c.transform.root.name.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (RuleLog.Due("AshlandsTestHooks.IsTree", 2d)) ModLog.Every("AshlandsTestHooks.IsTree", 2d, $"AshlandsTestHooks.IsTree: returns {tree} for {c.transform.root.name}");
            return tree;
        }

        /// <summary>Places a plain wooden wall 2.5 m beside the cannon spot (the comparison piece). Returns "wall placed at (x, y, z)" or why not.</summary>
        [TestHook("GunPowdered.testWallPlace")]
        static string TestWallPlace()
        {
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(WallPrefab) : null;
            if (p == null || prefab == null) { ModLog.Verbose("AshlandsTestHooks.TestWallPlace: returns \"no player or no wall prefab\" because p == null || prefab == null (line 208)"); return "no player or no wall prefab"; }
            Vector3 at = _spot == Vector3.zero ? p.transform.position + p.transform.forward * 4f + p.transform.rotation * Vector3.left * 2.5f : _spot + Vector3.left * 2.5f;
            at.y = Ground(at);
            _wall = Object.Instantiate(prefab, at, p.transform.rotation);
            Piece piece = _wall.GetComponent<Piece>();
            ZNetView pnv = _wall.GetComponent<ZNetView>();
            if (piece != null && pnv != null && pnv.IsValid())      // the creator the game's build gives (as CannonTestHooks does)
            {
                pnv.GetZDO().Set(ZDOVars.s_creator, p.GetPlayerID());
                HarmonyLib.AccessTools.Field(typeof(Piece), "m_creator").SetValue(piece, p.GetPlayerID());
            }
            string s = "wall placed at " + at.ToString("F1");
            ModLog.Info("AshlandsTestHooks: " + s);
            return s;
        }

        /// <summary>Notes the health of the cannon and the wall now. Returns "marked cannon C, wall W".</summary>
        [TestHook("GunPowdered.healthMark")]
        static string HealthMark()
        {
            _cannonBefore = Health(MobileCannonTestHooks.Cannon);
            _wallBefore = Health(_wall);
            string s = $"marked cannon {_cannonBefore:0}, wall {_wallBefore:0}";
            ModLog.Info("AshlandsTestHooks: " + s);
            return s;
        }

        /// <summary>"cannon same|lost X, wall same|lost Y" since healthMark (current health from the piece's ZDO, the game's own record).</summary>
        [TestHook("GunPowdered.healthCompare")]
        static string HealthCompare()
        {
            float c = Health(MobileCannonTestHooks.Cannon), w = Health(_wall);
            string s = "cannon " + Lost(_cannonBefore, c) + ", wall " + Lost(_wallBefore, w);
            ModLog.Info("AshlandsTestHooks: " + s + $" (cannon {_cannonBefore:0} -> {c:0}, wall {_wallBefore:0} -> {w:0}; wall {WearState(_wall)}; cannon {WearState(MobileCannonTestHooks.Cannon)})");
            return s;
        }

        /// <summary>The game's own Ashlands bookkeeping of a piece (private WearNTear fields): biome, in Ashlands, ash roof and what counts as its roof.</summary>
        static string WearState(GameObject piece)
        {
            WearNTear w = piece != null ? piece.GetComponent<WearNTear>() : null;
            if (w == null) { if (RuleLog.Due("AshlandsTestHooks.WearState:none", 2d)) ModLog.Every("AshlandsTestHooks.WearState:none", 2d, "AshlandsTestHooks.WearState: returns \"gone\" because there is no WearNTear"); return "gone"; }
            var t = HarmonyLib.Traverse.Create(w);
            GameObject roof = t.Field("m_ashroof").GetValue<GameObject>();
            string s = $"biome {t.Field("m_biome").GetValue<Heightmap.Biome>()}, in Ashlands {t.Field("m_inAshlands").GetValue<bool>()}, ash roof {t.Field("m_haveAshRoof").GetValue<bool>()} ({(roof != null ? roof.name + " under " + (roof.transform.parent != null ? roof.transform.parent.name : "-") : "none")}), lava {t.Field("m_lavaValue").GetValue<float>():0.00}";
            { if (RuleLog.Due("AshlandsTestHooks.WearState", 2d)) ModLog.Every("AshlandsTestHooks.WearState", 2d, $"AshlandsTestHooks.WearState: returns {s}"); return s; }
        }

        // a piece that burned down is gone (null, NaN health): it lost all it had (a19ash3: the wall on lava, 270 -> destroyed within a second)
        static string Lost(float before, float now) => float.IsNaN(before) ? "unknown" : float.IsNaN(now) ? "lost " + before.ToString("0") + " (destroyed)" : before - now < 0.5f ? "same" : "lost " + (before - now).ToString("0");

        static float Health(GameObject piece)
        {
            if (piece == null) { if (RuleLog.Due("AshlandsTestHooks.Health:none", 2d)) ModLog.Every("AshlandsTestHooks.Health:none", 2d, "AshlandsTestHooks.Health: returns NaN because piece == null (line 261)"); return float.NaN; }
            WearNTear w = piece.GetComponent<WearNTear>();
            ZNetView v = piece.GetComponent<ZNetView>();
            { float h = w == null ? float.NaN : v != null && v.IsValid() ? v.GetZDO().GetFloat(ZDOVars.s_health, w.m_health) : w.m_health; if (RuleLog.Due("AshlandsTestHooks.Health", 2d)) ModLog.Every("AshlandsTestHooks.Health", 2d, $"AshlandsTestHooks.Health: returns {h} for {piece.name}"); return h; }
        }

        /// <summary>
        /// Runs the game's own roof check (WearNTear.UpdateCover, public) on the cannon and the wall now. The game runs it with a frame's delta about once a
        /// second and acts after 4 s summed (WearNTearUpdater), so a new piece's "ash roof" stays at its start value (true) for minutes (a19ash8: wall and
        /// cannon "ash roof True (none)" after 50 s). Returns "roof checked: wall roof R, cannon roof C".
        /// </summary>
        [TestHook("GunPowdered.coverCheckNow")]
        static string CoverCheckNow()
        {
            WearNTear wall = _wall != null ? _wall.GetComponent<WearNTear>() : null;
            WearNTear cannon = MobileCannonTestHooks.Cannon != null ? MobileCannonTestHooks.Cannon.GetComponent<WearNTear>() : null;
            if (wall == null || cannon == null) { ModLog.Verbose("AshlandsTestHooks.CoverCheckNow: returns why not because wall == null || cannon == null"); return $"wall {wall != null}, cannon {cannon != null}"; }
            wall.UpdateCover(100f);
            cannon.UpdateCover(100f);
            string s = $"roof checked: wall roof {HarmonyLib.Traverse.Create(wall).Field("m_haveAshRoof").GetValue<bool>()}, cannon roof {HarmonyLib.Traverse.Create(cannon).Field("m_haveAshRoof").GetValue<bool>()}";
            ModLog.Info("AshlandsTestHooks: " + s);
            return s;
        }

        /// <summary>Sets the game's Ashlands ash damage (% of the max health every 5 s without a roof). Returns "ash damage OLD -> NEW".</summary>
        [TestHook("GunPowdered.ashDamage")]
        static string AshDamage(double value)
        {
            if (Game.instance == null) { ModLog.Verbose("AshlandsTestHooks.AshDamage: returns \"no game\" because Game.instance == null (line 289)"); return "no game"; }
            float old = Game.instance.m_ashDamage;
            if (float.IsNaN(_ashBefore)) _ashBefore = old;
            Game.instance.m_ashDamage = (float)value;
            string s = $"ash damage {old:0.##} -> {value:0.##}";
            ModLog.Info("AshlandsTestHooks: " + s);
            return s;
        }

        /// <summary>
        /// Lights the game's own spreading fire (the prefab the Ashlands cinders start, found by its Fire component) at the foot of the cannon and of the
        /// wall. Returns "fire PREFAB at cannon and wall" or why not.
        /// </summary>
        [TestHook("GunPowdered.fireAtBoth")]
        static string FireAtBoth()
        {
            GameObject fire = null;
            if (ZNetScene.instance != null)
                foreach (GameObject g in ZNetScene.instance.m_prefabs)
                    if (g != null && g.GetComponent<Fire>() != null) { fire = g; ModLog.Verbose("AshlandsTestHooks.FireAtBoth: stops the search (break) because " + g.name + " has the game's Fire"); break; }
            GameObject cannon = MobileCannonTestHooks.Cannon;
            if (fire == null || cannon == null || _wall == null) { ModLog.Verbose($"AshlandsTestHooks.FireAtBoth: returns why not because fire == null || cannon == null || _wall == null (fire {fire != null}, cannon {cannon != null}, wall {_wall != null}) (line 310)"); return $"fire {fire != null}, cannon {cannon != null}, wall {_wall != null}"; }
            Object.Instantiate(fire, cannon.transform.position + Vector3.up * 0.4f, Quaternion.identity);
            Object.Instantiate(fire, _wall.transform.position + Vector3.up * 0.4f, Quaternion.identity);
            string s = "fire " + fire.name + " at cannon and wall";
            ModLog.Info("AshlandsTestHooks: " + s);
            return s;
        }

        /// <summary>Removes the comparison wall. Returns "wall removed" or "no wall".</summary>
        [TestHook("GunPowdered.testWallRemove")]
        static string TestWallRemove()
        {
            // a test that failed half way left its spot, its goal or the raised ash damage behind (a19ash2: the next test's wall went to the old Ashlands spot)
            if (Game.instance != null && !float.IsNaN(_ashBefore)) { Game.instance.m_ashDamage = _ashBefore; _ashBefore = float.NaN; }
            _spot = Vector3.zero;
            _goal = Vector3.zero;
            if (_wall == null) { ModLog.Verbose("AshlandsTestHooks.TestWallRemove: returns \"no wall\" because _wall == null (spot, goal and ash damage reset)"); return "no wall"; }
            ZNetView v = _wall.GetComponent<ZNetView>();
            if (v != null && v.IsValid()) { v.ClaimOwnership(); v.Destroy(); } else Object.Destroy(_wall);
            _wall = null;
            _spot = Vector3.zero;
            _goal = Vector3.zero;
            int fires = 0;
            Player p = Player.m_localPlayer;
            foreach (Fire f in Object.FindObjectsByType<Fire>(FindObjectsSortMode.None))
            {
                if (p != null && Vector3.Distance(f.transform.position, p.transform.position) > 40f) { if (RuleLog.Due("AshlandsTestHooks.TestWallRemove:far", 2d)) ModLog.Every("AshlandsTestHooks.TestWallRemove:far", 2d, "AshlandsTestHooks.TestWallRemove: skips to the next item (continue) because the fire is far away"); continue; }
                ZNetView fv = f.GetComponent<ZNetView>();
                if (fv != null && fv.IsValid()) { fv.ClaimOwnership(); fv.Destroy(); } else Object.Destroy(f.gameObject);
                fires++;
            }
            ModLog.Info($"AshlandsTestHooks: wall removed, {fires} fire(s) put out");
            ModLog.Verbose("AshlandsTestHooks.TestWallRemove: returns \"wall removed\" (line 342)");
            return "wall removed";
        }
    }
}
