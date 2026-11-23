using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Dynamite.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the Dynamite (D-100). Setting it down and lighting it call the real code (DynamiteSetup.Place, DynamiteCharge.Light) as the local
    /// player; only the distance is longer than a player would use, so the test player stands outside the blast.
    /// </summary>
    internal static class DynamiteTestHooks
    {
        static Vector3? _placed;
        static float _ground = float.NaN;
        static int _blastsAtLight;
        static readonly System.Collections.Generic.List<GameObject> Pieces = new System.Collections.Generic.List<GameObject>();

        /// <summary>"stick S, radius R, damage D tier T, creatures C, crater W x H".</summary>
        [TestHook("GunPowdered.dynamiteInfo")]
        static string Info()
        {
            if (RuleLog.Due("DynamiteTestHooks.Info", 2d)) ModLog.Every("DynamiteTestHooks.Info", 2d, $"DynamiteTestHooks.Info()");
            if (DynamiteSetup.Explosion == null) return "no blast";
            Aoe props = DynamiteSetup.Explosion.GetComponent<Aoe>();
            Transform child = DynamiteSetup.Explosion.transform.Find("gp_creature_blast");
            Aoe creatures = child != null ? child.GetComponent<Aoe>() : null;
            TerrainOp dig = DynamiteSetup.Crater != null ? DynamiteSetup.Crater.GetComponent<TerrainOp>() : null;
            return "stick " + (DynamiteSetup.Stick != null) + ", radius " + props.m_radius.ToString("0.#") + ", damage " + props.m_damage.m_blunt.ToString("0") + " tier " + props.m_toolTier
                + ", props " + props.m_hitProps + " chars " + props.m_hitCharacters
                + ", creatures " + (creatures != null ? creatures.m_damage.m_blunt.ToString("0") + " chars " + creatures.m_hitCharacters + " props " + creatures.m_hitProps : "none")
                + ", crater " + (dig != null ? (dig.m_settings.m_raiseRadius * 2f).ToString("0.0") + " x " + (-dig.m_settings.m_raiseDelta).ToString("0.0") : "none");
        }

        /// <summary>Diagnostics for the dynamite in the hand (T-009j): where the equipped stick's model is, in the world, in the hand joint's space and against the forearm. Logged too. Returns the same text.</summary>
        [TestHook("GunPowdered.dynamiteHandState")]
        static string HandState()
        {
            Player p = Player.m_localPlayer;
            VisEquipment vis = p != null ? p.GetComponent<VisEquipment>() : null;
            if (vis == null) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.HandState: returns \"no player\" because vis == null (line 41)"); return "no player"; }
            GameObject inst = HarmonyLib.AccessTools.Field(typeof(VisEquipment), "m_rightItemInstance")?.GetValue(vis) as GameObject;      // private in the game
            if (inst == null) inst = HarmonyLib.AccessTools.Field(typeof(VisEquipment), "m_leftItemInstance")?.GetValue(vis) as GameObject;
            if (inst == null) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.HandState: returns \"nothing in the hand\" because inst == null (line 44)"); return "nothing in the hand"; }
            Transform model = inst.transform.Find("gp_model");
            if (model == null) model = inst.GetComponentInChildren<Renderer>() != null ? inst.GetComponentInChildren<Renderer>().transform : null;
            Transform hand = inst.transform.parent;
            string s = "hand item " + inst.name + ", on " + (hand != null ? hand.name : "?") + " local " + inst.transform.localPosition.ToString("0.000") + " rot " + inst.transform.localEulerAngles.ToString("0")
                + ", model " + (model != null ? model.name : "none");
            if (model != null)
            {
                Bounds b = default;
                bool any = false;
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
                    if (r.enabled) { if (any) b.Encapsulate(r.bounds); else { b = r.bounds; any = true; } }
                Vector3 fore = hand != null && hand.parent != null ? (hand.position - hand.parent.position).normalized : Vector3.zero;
                Vector3 axis = model.up;
                s += ", model local " + model.localPosition.ToString("0.000") + ", centre in hand space " + (any && hand != null ? hand.InverseTransformPoint(b.center).ToString("0.000") : "?")
                    + ", length axis " + axis.ToString("0.00") + ", angle to forearm " + (fore != Vector3.zero ? Vector3.Angle(axis, fore).ToString("0") : "?") + " deg, hand world " + (hand != null ? hand.position.ToString("0.00") : "?");
            }
            ModLog.Info("DynamiteTestHooks: " + s);
            return s;
        }

        /// <summary>
        /// A stick ends up <paramref name="metres"/> in front of where the player stands, set down the way a PLAYER can (user 2026-11-24: "tests that test
        /// things that might not happen"): a player always sets it 1.2 m in front (GunPowdered.Features.Dynamite.Domain.DynamiteRules.PlaceDistance, the attack button), so the player first
        /// steps forward to 1.2 m short of the spot, presses attack (the game's StartAttack - DynamiteAttackPatch - kneel and all), and, unless
        /// <paramref name="stay"/>, steps back to where it stood. Returns what the attack's placement returned ("placed at (...)" or why not).
        /// </summary>
        [TestHook("GunPowdered.dynamitePlaceAt")]
        static string PlaceAt(double metres, bool stay = false)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.PlaceAt(metres={metres}, stay={stay})");
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("DynamiteTestHooks.PlaceAt: returns \"no player\" because Player.m_localPlayer == null"); return "no player"; }
            Vector3 start = p.transform.position, fwd = Vector3.ProjectOnPlane(p.transform.forward, Vector3.up).normalized;
            float walk = Mathf.Max(0f, (float)metres - GunPowdered.Features.Dynamite.Domain.DynamiteRules.PlaceDistance);
            if (walk > 0.01f) StepTo(p, start + fwd * walk);
            DynamiteSetup.LastPlace = "";
            p.StartAttack(null, false);      // the attack button: DynamiteAttackPatch -> DynamiteSetup.Place(player) at 1.2 m
            string r = DynamiteSetup.LastPlace.Length > 0 ? DynamiteSetup.LastPlace : "the attack did not reach the placement (dynamite not in hand?)";
            DynamiteCharge c = Nearest(p.transform.position + fwd * GunPowdered.Features.Dynamite.Domain.DynamiteRules.PlaceDistance);
            _placed = c != null ? c.transform.position : (Vector3?)null;
            if (!stay && walk > 0.01f) StepTo(p, start);
            ModLog.Verbose($"DynamiteTestHooks.PlaceAt: walked {walk:0.0} m, {(stay ? "stays there" : "walked back")}, placement: {r} - returns it");
            return r;
        }

        // Moves the player over the (loaded, level) arena ground a few metres, as walking there would; keeps the facing.
        static void StepTo(Player p, Vector3 to)
        {
            if (Heightmap.GetHeight(to, out float h)) to.y = h;
            p.transform.position = to;
            Rigidbody body = p.GetComponent<Rigidbody>();
            if (body != null) { body.position = to; body.linearVelocity = Vector3.zero; }
            Physics.SyncTransforms();
            ModLog.Verbose("DynamiteTestHooks.StepTo: player now at " + to.ToString("0.00"));
        }

        /// <summary>Uses the stick that was set down, as the local player (the real Interact). Returns "lit" or why not.</summary>
        [TestHook("GunPowdered.dynamiteLight")]
        static string Light()
        {
            if (RuleLog.Due("DynamiteTestHooks.Light", 2d)) ModLog.Every("DynamiteTestHooks.Light", 2d, $"DynamiteTestHooks.Light()");
            DynamiteCharge c = _placed.HasValue ? Nearest(_placed.Value) : null;
            if (c == null) { if (RuleLog.Due("DynamiteTestHooks.Light:107", 2d)) ModLog.Every("DynamiteTestHooks.Light:107", 2d, $"DynamiteTestHooks.Light: returns \"no stick\" because c == null (line 107)"); return "no stick"; }
            _blastsAtLight = DynamiteCharge.Blasts;
            string why = c.Light(Player.m_localPlayer);
            { var returnedAt110 = why ?? "lit"; if (RuleLog.Due("DynamiteTestHooks.Light:110", 2d)) ModLog.Every("DynamiteTestHooks.Light:110", 2d, $"DynamiteTestHooks.Light: returns {returnedAt110} (line 110)"); return returnedAt110; }
        }

        /// <summary>D-158: the light red sparkle of the stick the test set down: "sparkle shown|hidden, playing P, colour RGBA" or "no stick".</summary>
        [TestHook("GunPowdered.dynamiteSparkle")]
        static string Sparkle()
        {
            ModLog.Verbose("DynamiteTestHooks.Sparkle()");
            DynamiteCharge c = _placed.HasValue ? Nearest(_placed.Value) : null;
            string s = c == null ? "no stick" : c.SparkleReport();
            ModLog.Info("DynamiteTestHooks.Sparkle: returns " + s);
            return s;
        }

        /// <summary>D-158: the emote the local player is in (the game's synced emote): "emote NAME" or "emote none".</summary>
        [TestHook("GunPowdered.playerEmote")]
        static string Emote()
        {
            ModLog.Verbose("DynamiteTestHooks.Emote()");
            Player p = Player.m_localPlayer;
            ZNetView nv = p != null ? p.GetComponent<ZNetView>() : null;
            string e = nv != null && nv.IsValid() ? nv.GetZDO().GetString(ZDOVars.s_emote) : "";
            string s = "emote " + (string.IsNullOrEmpty(e) ? "none" : e);
            ModLog.Info("DynamiteTestHooks.Emote: returns " + s);
            return s;
        }

        /// <summary>Sticks of dynamite within 20 m of where the test set one down.</summary>
        [TestHook("GunPowdered.dynamiteSticks")]
        static int Sticks()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.Sticks()");
            int n = 0;
            foreach (DynamiteCharge c in Object.FindObjectsByType<DynamiteCharge>(FindObjectsSortMode.None))
                if (!_placed.HasValue || Vector3.Distance(c.transform.position, _placed.Value) < 20f) n++;
            { var returnedAt145 = n; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.Sticks: returns {returnedAt145} (line 145)"); return returnedAt145; }
        }

        /// <summary>Blasts since the test lit the stick.</summary>
        [TestHook("GunPowdered.dynamiteBlastsSinceLight")]
        static int BlastsSinceLight() => DynamiteCharge.Blasts - _blastsAtLight;

        /// <summary>Remembers the ground height where the stick stands. Returns "marked H".</summary>
        [TestHook("GunPowdered.dynamiteGroundMark")]
        static string GroundMark()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.GroundMark()");
            if (!_placed.HasValue) return "no stick";
            _ground = Ground(_placed.Value);
            TerrainSnapshot.Mark(_placed.Value, TerrainNear);
            { var returnedAt160 = float.IsNaN(_ground) ? "ground not loaded" : "marked " + _ground.ToString("0.00"); if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.GroundMark: returns {returnedAt160} (line 160)"); return returnedAt160; }
        }

        /// <summary>True when the ground where the stick stood is at least <paramref name="metres"/> lower than marked (the hole).</summary>
        [TestHook("GunPowdered.dynamiteHoleAtLeast")]
        static bool HoleAtLeast(double metres)
        {
            if (!_placed.HasValue || float.IsNaN(_ground)) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.HoleAtLeast: returns false because !_placed.HasValue || float.IsNaN(_ground) (line 167)"); return false; }
            float now = Ground(_placed.Value);
            ModLog.Info("DynamiteTestHooks: ground " + _ground.ToString("0.00") + " -> " + now.ToString("0.00") + " at the stick");
            return !float.IsNaN(now) && _ground - now >= metres;
        }

        /// <summary>
        /// Undoes the hole: the terrain pieces near the stick get back the modifications they had when the ground was marked (the KydlFlat arena is itself a
        /// terrain modification, so clearing everything would drop the ground by metres - lab a8dyn1). Pieces made after the mark are cleared. Returns "reset N".
        /// </summary>
        [TestHook("GunPowdered.dynamiteTerrainReset")]
        static string TerrainReset()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.TerrainReset()");
            if (!_placed.HasValue) return "no stick";
            { var returnedAt182 = TerrainSnapshot.Restore(_placed.Value, TerrainNear); if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.TerrainReset: returns {returnedAt182} (line 182)"); return returnedAt182; }
        }

        /// <summary>How far round the stick the terrain is marked and restored (m): the pack's 5 m crater with its smoothing and paint, and a margin.</summary>
        const float TerrainNear = 46f;

        /// <summary>"back" when the ground at the stick is again within 5 cm of the marked height, else how far it differs.</summary>
        [TestHook("GunPowdered.dynamiteGroundBack")]
        static string GroundBack()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.GroundBack()");
            if (!_placed.HasValue || float.IsNaN(_ground)) return "nothing marked";
            float now = Ground(_placed.Value);
            { var returnedAt195 = Mathf.Abs(now - _ground) <= 0.05f ? "back" : "differs by " + (now - _ground).ToString("0.00") + " m"; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.GroundBack: returns {returnedAt195} (line 195)"); return returnedAt195; }
        }

        /// <summary>Places a piece <paramref name="metres"/> to the right of the stick. Returns "placed &lt;prefab&gt;".</summary>
        [TestHook("GunPowdered.placePieceNearDynamite")]
        static string PlacePieceNear(string prefab, double metres)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.PlacePieceNear(prefab={prefab}, metres={metres})");
            Player p = Player.m_localPlayer;
            GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (!_placed.HasValue || p == null || source == null) { var returnedAt205 = "no stick or no prefab " + prefab; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.PlacePieceNear: returns {returnedAt205} because !_placed.HasValue || p == null || source == null (line 205)"); return returnedAt205; }
            Vector3 at = _placed.Value + p.transform.right * (float)metres;
            float g = Ground(at);
            if (!float.IsNaN(g)) at.y = g;
            GameObject go = Object.Instantiate(source, at, Quaternion.LookRotation(-p.transform.forward));
            if (go != null) Pieces.Add(go);
            { var returnedAt211 = go != null ? "placed " + prefab : "could not place " + prefab; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.PlacePieceNear: returns {returnedAt211} (line 211)"); return returnedAt211; }
        }

        /// <summary>True when the piece the test placed was damaged or destroyed by the blast.</summary>
        [TestHook("GunPowdered.dynamitePieceHurt")]
        static bool PieceHurt()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.PieceHurt()");
            foreach (GameObject go in Pieces)
            {
                if (go == null) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.PieceHurt: returns true because go == null (line 221)"); return true; }                  // destroyed
                WearNTear w = go.GetComponent<WearNTear>();
                if (w != null && w.GetHealthPercentage() < 0.999f) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.PieceHurt: returns true because w != null && w.GetHealthPercentage() < 0.999f (line 223)"); return true; }
            }
            { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.PieceHurt: returns false (line 225)"); return false; }
        }

        /// <summary>Removes what placePieceNearDynamite placed. Returns "removed N".</summary>
        [TestHook("GunPowdered.dynamiteRemovePieces")]
        static string RemovePieces()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.RemovePieces()");
            int n = 0;
            foreach (GameObject go in Pieces)
            {
                if (go == null) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.RemovePieces: skips to the next item (continue) because go == null (line 236)"); continue; }
                ZNetView nv = go.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid()) { nv.ClaimOwnership(); ZNetScene.instance.Destroy(go); }
                else Object.Destroy(go);
                n++;
            }
            Pieces.Clear();
            { var returnedAt243 = "removed " + n; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.RemovePieces: returns {returnedAt243} (line 243)"); return returnedAt243; }
        }

        /// <summary>
        /// True when the nearest living creature of this prefab lost between <paramref name="min"/> and <paramref name="max"/> health (the creatures' share
        /// of the blast is a fifth of the building damage). Logs the numbers.
        /// </summary>
        [TestHook("GunPowdered.creatureLostBetween")]
        static bool CreatureLostBetween(string prefab, double min, double max)
        {
            Player p = Player.m_localPlayer;
            Character best = null;
            float bestDist = 40f;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c.IsDead() || Utils.GetPrefabName(c.gameObject) != prefab) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.CreatureLostBetween: skips to the next item (continue) because c == null || c.IsDead() || Utils.GetPrefabName(c.gameObject) != prefab (line 258)"); continue; }
                float d = p != null ? Vector3.Distance(p.transform.position, c.transform.position) : 0f;
                if (d < bestDist) { best = c; bestDist = d; }
            }
            if (best == null) { ModLog.Info("DynamiteTestHooks: no living " + prefab); return false; }
            float lost = best.GetMaxHealth() - best.GetHealth();
            ModLog.Info("DynamiteTestHooks: " + prefab + " lost " + lost.ToString("0.0") + " of " + best.GetMaxHealth().ToString("0"));
            return lost >= min && lost <= max;
        }

        /// <summary>Removes every unlit or lit charge (stick or pack) within 30 m of the local player, so a visual test leaves none for the next test. Returns "removed N".</summary>
        [TestHook("GunPowdered.dynamiteRemoveSticks")]
        static string RemoveSticks()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.RemoveSticks()");
            Player p = Player.m_localPlayer;
            int n = 0;
            foreach (DynamiteCharge c in Object.FindObjectsByType<DynamiteCharge>(FindObjectsSortMode.None))
            {
                if (p != null && Vector3.Distance(c.transform.position, p.transform.position) > 30f) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.RemoveSticks: skips to the next item (continue) because p != null && Vector3.Distance(c.transform.position, p.transform.position) > 30f (line 277)"); continue; }
                ZNetView v = c.GetComponent<ZNetView>();
                if (v != null && v.IsValid()) { v.ClaimOwnership(); ZNetScene.instance.Destroy(c.gameObject); } else Object.Destroy(c.gameObject);
                n++;
            }
            { var returnedAt282 = "removed " + n; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.RemoveSticks: returns {returnedAt282} (line 282)"); return returnedAt282; }
        }

        /// <summary>What the local player holds in the right hand: the item's prefab name and whether its model is shown, or "none" (also no model).</summary>
        [TestHook("GunPowdered.rightHand")]
        static string RightHand()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.RightHand()");
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.RightHand: returns \"no player\" because p == null (line 291)"); return "no player"; }
            ItemDrop.ItemData item = HarmonyLib.AccessTools.Field(typeof(Humanoid), "m_rightItem").GetValue(p) as ItemDrop.ItemData;      // private in the game
            VisEquipment vis = p.GetComponent<VisEquipment>();
            GameObject inst = vis != null ? HarmonyLib.AccessTools.Field(typeof(VisEquipment), "m_rightItemInstance")?.GetValue(vis) as GameObject : null;
            string s = item == null ? "none" : (item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name);
            { var returnedAt296 = s + ", model " + (inst != null); if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.RightHand: returns {returnedAt296} (line 296)"); return returnedAt296; }
        }

        static DynamiteCharge Nearest(Vector3 at)
        {
            if (RuleLog.Due("DynamiteTestHooks.Nearest", 2d)) ModLog.Every("DynamiteTestHooks.Nearest", 2d, $"DynamiteTestHooks.Nearest(at={at})");
            DynamiteCharge best = null;
            float bestDist = 3f;
            foreach (DynamiteCharge c in Object.FindObjectsByType<DynamiteCharge>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(c.transform.position, at);
                if (d < bestDist) { best = c; bestDist = d; }
            }
            { var returnedAt309 = best; if (RuleLog.Due("DynamiteTestHooks.Nearest:309", 2d)) ModLog.Every("DynamiteTestHooks.Nearest:309", 2d, $"DynamiteTestHooks.Nearest: returns {returnedAt309} (line 309)"); return returnedAt309; }
        }

        static float Ground(Vector3 at)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.Ground(at={at})");
            float h;
            { var returnedAt316 = ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h) ? h : float.NaN; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteTestHooks.Ground: returns {returnedAt316} (line 316)"); return returnedAt316; }
        }
    }
}
