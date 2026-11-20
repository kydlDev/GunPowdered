using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Dynamite.Infrastructure;

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
            if (vis == null) return "no player";
            GameObject inst = HarmonyLib.AccessTools.Field(typeof(VisEquipment), "m_rightItemInstance")?.GetValue(vis) as GameObject;      // private in the game
            if (inst == null) inst = HarmonyLib.AccessTools.Field(typeof(VisEquipment), "m_leftItemInstance")?.GetValue(vis) as GameObject;
            if (inst == null) return "nothing in the hand";
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

        /// <summary>Sets one stick down <paramref name="metres"/> in front of the local player with the real attack code. Returns "placed at (...)" or why not.</summary>
        [TestHook("GunPowdered.dynamitePlaceAt")]
        static string PlaceAt(double metres)
        {
            Player p = Player.m_localPlayer;
            string r = DynamiteSetup.Place(p, (float)metres);
            DynamiteCharge c = Nearest(p != null ? p.transform.position + p.transform.forward * (float)metres : Vector3.zero);
            _placed = c != null ? c.transform.position : (Vector3?)null;
            return r;
        }

        /// <summary>Uses the stick that was set down, as the local player (the real Interact). Returns "lit" or why not.</summary>
        [TestHook("GunPowdered.dynamiteLight")]
        static string Light()
        {
            DynamiteCharge c = _placed.HasValue ? Nearest(_placed.Value) : null;
            if (c == null) return "no stick";
            _blastsAtLight = DynamiteCharge.Blasts;
            string why = c.Light(Player.m_localPlayer);
            return why ?? "lit";
        }

        /// <summary>Sticks of dynamite within 20 m of where the test set one down.</summary>
        [TestHook("GunPowdered.dynamiteSticks")]
        static int Sticks()
        {
            int n = 0;
            foreach (DynamiteCharge c in Object.FindObjectsByType<DynamiteCharge>(FindObjectsSortMode.None))
                if (!_placed.HasValue || Vector3.Distance(c.transform.position, _placed.Value) < 20f) n++;
            return n;
        }

        /// <summary>Blasts since the test lit the stick.</summary>
        [TestHook("GunPowdered.dynamiteBlastsSinceLight")]
        static int BlastsSinceLight() => DynamiteCharge.Blasts - _blastsAtLight;

        /// <summary>Remembers the ground height where the stick stands. Returns "marked H".</summary>
        [TestHook("GunPowdered.dynamiteGroundMark")]
        static string GroundMark()
        {
            if (!_placed.HasValue) return "no stick";
            _ground = Ground(_placed.Value);
            SnapshotTerrain(_placed.Value);
            return float.IsNaN(_ground) ? "ground not loaded" : "marked " + _ground.ToString("0.00");
        }

        /// <summary>True when the ground where the stick stood is at least <paramref name="metres"/> lower than marked (the hole).</summary>
        [TestHook("GunPowdered.dynamiteHoleAtLeast")]
        static bool HoleAtLeast(double metres)
        {
            if (!_placed.HasValue || float.IsNaN(_ground)) return false;
            float now = Ground(_placed.Value);
            ModLog.Info("DynamiteTestHooks: ground " + _ground.ToString("0.00") + " -> " + now.ToString("0.00") + " at the stick");
            return !float.IsNaN(now) && _ground - now >= metres;
        }

        /// <summary>
        /// Undoes the hole: the terrain pieces near the stick get back the modifications they had when the ground was marked (the KydlFlat arena is itself a terrain
        /// modification, so clearing everything would drop the ground by metres - lab a8dyn1). Pieces made after the mark are cleared. Returns "reset N".
        /// </summary>
        [TestHook("GunPowdered.dynamiteTerrainReset")]
        static string TerrainReset()
        {
            if (!_placed.HasValue) return "no stick";
            int restored = 0, cleared = 0;
            foreach (TerrainComp tc in Object.FindObjectsByType<TerrainComp>(FindObjectsSortMode.None))
            {
                Vector3 p = tc.transform.position;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(_placed.Value.x, _placed.Value.z)) > TerrainNear) continue;
                ZNetView view = tc.GetComponent<ZNetView>();
                if (view != null && view.IsValid() && !view.IsOwner()) view.ClaimOwnership();
                bool known = Saved.TryGetValue(tc, out object[] saved);
                for (int i = 0; i < TerrainFields.Length; i++)
                {
                    var now = TerrainField(TerrainFields[i])?.GetValue(tc) as System.Array;
                    if (now == null) continue;
                    if (known && saved[i] is System.Array before && before.Length == now.Length) System.Array.Copy(before, now, now.Length);
                    else System.Array.Clear(now, 0, now.Length);
                }
                typeof(TerrainComp).GetMethod("Save", Flags)?.Invoke(tc, new object[] { false });
                Poke(TerrainField("m_hmap")?.GetValue(tc) as Heightmap);
                if (known) restored++; else cleared++;
            }
            ModLog.Info("DynamiteTestHooks: terrain reset - " + restored + " restored from the mark, " + cleared + " cleared");
            return "reset " + restored + " restored, " + cleared + " cleared";
        }

        const float TerrainNear = 46f;
        const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        static readonly string[] TerrainFields = { "m_modifiedHeight", "m_levelDelta", "m_smoothDelta", "m_modifiedPaint", "m_paintMask" };
        static readonly System.Collections.Generic.Dictionary<TerrainComp, object[]> Saved = new System.Collections.Generic.Dictionary<TerrainComp, object[]>();

        static System.Reflection.FieldInfo TerrainField(string name) => typeof(TerrainComp).GetField(name, Flags);

        /// <summary>Copies the modification arrays of the terrain pieces near the stick (the state the reset goes back to).</summary>
        static void SnapshotTerrain(Vector3 at)
        {
            Saved.Clear();
            foreach (TerrainComp tc in Object.FindObjectsByType<TerrainComp>(FindObjectsSortMode.None))
            {
                Vector3 p = tc.transform.position;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(at.x, at.z)) > TerrainNear) continue;
                var copy = new object[TerrainFields.Length];
                for (int i = 0; i < TerrainFields.Length; i++) copy[i] = (TerrainField(TerrainFields[i])?.GetValue(tc) as System.Array)?.Clone();
                Saved[tc] = copy;
            }
            ModLog.Info("DynamiteTestHooks: terrain marked - " + Saved.Count + " terrain piece(s) near the stick");
        }

        static void Poke(Heightmap hm)
        {
            var poke = typeof(Heightmap).GetMethod("Poke", Flags);
            if (hm == null || poke == null) return;
            var args = new object[poke.GetParameters().Length];
            for (int i = 0; i < args.Length; i++) args[i] = System.Type.Missing;
            poke.Invoke(hm, System.Reflection.BindingFlags.OptionalParamBinding | Flags, null, args, null);
        }

        /// <summary>"back" when the ground at the stick is again within 5 cm of the marked height, else how far it differs.</summary>
        [TestHook("GunPowdered.dynamiteGroundBack")]
        static string GroundBack()
        {
            if (!_placed.HasValue || float.IsNaN(_ground)) return "nothing marked";
            float now = Ground(_placed.Value);
            return Mathf.Abs(now - _ground) <= 0.05f ? "back" : "differs by " + (now - _ground).ToString("0.00") + " m";
        }

        /// <summary>Places a piece <paramref name="metres"/> to the right of the stick. Returns "placed &lt;prefab&gt;".</summary>
        [TestHook("GunPowdered.placePieceNearDynamite")]
        static string PlacePieceNear(string prefab, double metres)
        {
            Player p = Player.m_localPlayer;
            GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (!_placed.HasValue || p == null || source == null) return "no stick or no prefab " + prefab;
            Vector3 at = _placed.Value + p.transform.right * (float)metres;
            float g = Ground(at);
            if (!float.IsNaN(g)) at.y = g;
            GameObject go = Object.Instantiate(source, at, Quaternion.LookRotation(-p.transform.forward));
            if (go != null) Pieces.Add(go);
            return go != null ? "placed " + prefab : "could not place " + prefab;
        }

        /// <summary>True when the piece the test placed was damaged or destroyed by the blast.</summary>
        [TestHook("GunPowdered.dynamitePieceHurt")]
        static bool PieceHurt()
        {
            foreach (GameObject go in Pieces)
            {
                if (go == null) return true;                  // destroyed
                WearNTear w = go.GetComponent<WearNTear>();
                if (w != null && w.GetHealthPercentage() < 0.999f) return true;
            }
            return false;
        }

        /// <summary>Removes what placePieceNearDynamite placed. Returns "removed N".</summary>
        [TestHook("GunPowdered.dynamiteRemovePieces")]
        static string RemovePieces()
        {
            int n = 0;
            foreach (GameObject go in Pieces)
            {
                if (go == null) continue;
                ZNetView nv = go.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid()) { nv.ClaimOwnership(); ZNetScene.instance.Destroy(go); }
                else Object.Destroy(go);
                n++;
            }
            Pieces.Clear();
            return "removed " + n;
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
                if (c == null || c.IsDead() || Utils.GetPrefabName(c.gameObject) != prefab) continue;
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
            Player p = Player.m_localPlayer;
            int n = 0;
            foreach (DynamiteCharge c in Object.FindObjectsByType<DynamiteCharge>(FindObjectsSortMode.None))
            {
                if (p != null && Vector3.Distance(c.transform.position, p.transform.position) > 30f) continue;
                ZNetView v = c.GetComponent<ZNetView>();
                if (v != null && v.IsValid()) { v.ClaimOwnership(); ZNetScene.instance.Destroy(c.gameObject); } else Object.Destroy(c.gameObject);
                n++;
            }
            return "removed " + n;
        }

        /// <summary>What the local player holds in the right hand: the item's prefab name and whether its model is shown, or "none" (also no model).</summary>
        [TestHook("GunPowdered.rightHand")]
        static string RightHand()
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            ItemDrop.ItemData item = HarmonyLib.AccessTools.Field(typeof(Humanoid), "m_rightItem").GetValue(p) as ItemDrop.ItemData;      // private in the game
            VisEquipment vis = p.GetComponent<VisEquipment>();
            GameObject inst = vis != null ? HarmonyLib.AccessTools.Field(typeof(VisEquipment), "m_rightItemInstance")?.GetValue(vis) as GameObject : null;
            string s = item == null ? "none" : (item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name);
            return s + ", model " + (inst != null);
        }

        static DynamiteCharge Nearest(Vector3 at)
        {
            DynamiteCharge best = null;
            float bestDist = 3f;
            foreach (DynamiteCharge c in Object.FindObjectsByType<DynamiteCharge>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(c.transform.position, at);
                if (d < bestDist) { best = c; bestDist = d; }
            }
            return best;
        }

        static float Ground(Vector3 at)
        {
            float h;
            return ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out h) ? h : float.NaN;
        }
    }
}
