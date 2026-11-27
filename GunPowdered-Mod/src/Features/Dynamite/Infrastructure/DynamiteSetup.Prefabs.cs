using System.Linq;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Dynamite.Domain;

namespace GunPowdered.Features.Dynamite.Infrastructure
{
    /// <summary>DynamiteSetup, part 2 (G04 class size): the charge, sparkle, explosion and crater prefabs and the hand model.</summary>
    internal static partial class DynamiteSetup
    {
        static GameObject MakeCharge(ChargeKind kind, GameObject item)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.MakeCharge(kind={RuleLog.Shown(kind)}, item={RuleLog.Shown(item)})");
            Transform model = item.transform.Find("gp_model");
            GameObject stick = KyPrefabs.CreateEmpty(kind.ChargeId, true);
            if (stick == null) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteSetup.MakeCharge: returns null because stick == null (line 20)"); return null; }
            ZNetView nv = stick.GetComponent<ZNetView>();
            if (nv != null) nv.m_persistent = true;
            foreach (Collider c in stick.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (Renderer r in stick.GetComponentsInChildren<Renderer>(true)) Object.DestroyImmediate(r);
            foreach (MeshFilter f in stick.GetComponentsInChildren<MeshFilter>(true)) Object.DestroyImmediate(f);
            Bounds b = new Bounds(Vector3.up * 0.15f, new Vector3(0.1f, 0.3f, 0.1f));
            if (model != null)
            {
                GameObject look = Object.Instantiate(model.gameObject, stick.transform);
                look.name = "gp_model";
                look.transform.localPosition = Vector3.zero;
                look.transform.localRotation = Quaternion.identity;
                foreach (Collider c in look.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                foreach (Rigidbody rb in look.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
                bool any = false;
                foreach (Renderer r in look.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                    Bounds rb = r.bounds;
                    rb.center = stick.transform.InverseTransformPoint(rb.center);
                    if (!any) { b = rb; any = true; } else b.Encapsulate(rb);
                }
            }
            AddSparkle(stick, b);
            var box = stick.AddComponent<BoxCollider>();      // for the hover and the use key
            box.center = b.center;
            box.size = Vector3.Max(b.size, new Vector3(0.12f, 0.12f, 0.12f));
            int layer = LayerMask.NameToLayer("piece_nonsolid");
            if (layer >= 0) foreach (Transform t in stick.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            stick.AddComponent<DynamiteCharge>().m_kindId = kind.ChargeId;
            KyPrefabs.Add(stick);
            { var returnedAt52 = stick; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteSetup.MakeCharge: returns {returnedAt52} (line 52)"); return returnedAt52; }
        }

        /// <summary>D-158: the child that holds the placed stick's sparkle (DynamiteCharge hides it once the fuse burns).</summary>
        internal const string SparkleName = "gp_sparkle";

        /// <summary>D-158: the sparkle's colour, light red (user 2026-11-23: "if you can you could change its color to a light red").</summary>
        internal static readonly Color SparkleColor = new Color(1f, 0.4f, 0.4f, 1f);

        /// <summary>
        /// D-158 (user 2026-11-23: "add the sparkling effect, that dropped items have, to the placed dynamite ... so the player can better identify where the
        /// dynamite has been placed"): the game's dropped-item sparkle is a particle system on every item prefab's root (Wood.prefab, Coins.prefab ...). A copy
        /// of the Wood one, without anything else of the item, goes on the placed stick, light red. Expected outcome: true when the sparkle was added.
        /// </summary>
        static bool AddSparkle(GameObject stick, Bounds bounds)
        {
            GameObject wood = KyPrefabs.Asset<GameObject>("Wood");
            if (wood == null || wood.GetComponent<ParticleSystem>() == null) { ModLog.Warn("DynamiteSetup: no Wood item sparkle found - the placed dynamite shows none"); return false; }
            // the charge prefab lies under Jotunn's inactive prefab root, so the copy's game scripts (ItemDrop, ZNetView ...) never wake up before they go
            GameObject sparkle = Object.Instantiate(wood, stick.transform);
            sparkle.name = SparkleName;
            foreach (Transform child in sparkle.transform.Cast<Transform>().ToList()) Object.DestroyImmediate(child.gameObject);
            Component[] parts = sparkle.GetComponents<Component>();
            // scripts first (they need the rest), then physics, then everything but the transform and the particles
            foreach (Component c in parts) if (c is MonoBehaviour) Object.DestroyImmediate(c);
            foreach (Component c in sparkle.GetComponents<Component>()) if (c is Collider || c is Rigidbody) Object.DestroyImmediate(c);
            foreach (Component c in sparkle.GetComponents<Component>()) if (!(c is Transform) && !(c is ParticleSystem) && !(c is ParticleSystemRenderer)) Object.DestroyImmediate(c);
            sparkle.transform.localPosition = bounds.center;
            sparkle.transform.localRotation = Quaternion.identity;
            sparkle.transform.localScale = Vector3.one;
            ParticleSystem ps = sparkle.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = SparkleColor;
            ModLog.Info("DynamiteSetup: light red sparkle added to " + stick.name + " (copied from the Wood item, " + sparkle.GetComponents<Component>().Length + " components)");
            return true;
        }

        static GameObject MakeExplosion(ChargeKind kind)
        {
            if (KyPrefabs.Get(ExplosionBase) == null) { ModLog.Error("DynamiteSetup: no vanilla " + ExplosionBase + " - the " + kind.Name + " has no blast"); return null; }
            GameObject clone = KyPrefabs.Clone(kind.ExplosionId, ExplosionBase);
            if (clone == null) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteSetup.MakeExplosion: returns null because clone == null (line 93)"); return null; }
            foreach (TerrainOp op in clone.GetComponentsInChildren<TerrainOp>(true)) Object.DestroyImmediate(op);
            var child = new GameObject(CreatureChild);
            child.transform.SetParent(clone.transform, false);
            child.layer = clone.layer;
            child.AddComponent<Aoe>();
            if (!Mathf.Approximately(kind.ExplosionScale, 1f))        // the pack's bang looks bigger; the Aoe reach is its radius, not the scale
                foreach (Transform t in clone.transform) if (t.name != CreatureChild) t.localScale *= kind.ExplosionScale;
            KyPrefabs.Add(clone);
            { var returnedAt102 = clone; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteSetup.MakeExplosion: returns {returnedAt102} (line 102)"); return returnedAt102; }
        }

        /// <summary>Craters of other blasts (D-182: the grenade's) that are listed as terrain operations with the charges' own.</summary>
        static readonly System.Collections.Generic.List<GameObject> ExtraCraters = new System.Collections.Generic.List<GameObject>();

        /// <summary>
        /// D-182: a crater prefab for another blast (the grenade), listed as a terrain operation like the charges' craters. Expected outcome: the prefab
        /// (made once per id), or null when the game's crater base is missing.
        /// </summary>
        internal static GameObject OtherCrater(string id)
        {
            foreach (GameObject known in ExtraCraters) if (known != null && known.name == id) { ModLog.Verbose($"DynamiteSetup.OtherCrater: {id} already made - returns it"); return known; }
            GameObject crater = MakeCrater(id);
            if (crater == null) { ModLog.Warn($"DynamiteSetup.OtherCrater: {id} could not be made - that blast digs no hole"); return null; }
            ExtraCraters.Add(crater);
            bool listed = ObjectDB.instance != null && ListOne(ObjectDB.instance, crater);
            ModLog.Verbose($"DynamiteSetup.OtherCrater: {id} made, listed as a terrain operation now {listed} (later ObjectDB rebuilds: DynamiteTerrainPatch)");
            return crater;
        }

        /// <summary>
        /// Shapes a crater's terrain operation: a round hole <paramref name="radius"/> m wide and <paramref name="depth"/> m deep, smoothed, the ground around
        /// painted bare. Expected outcome: true when the prefab has a TerrainOp and carries the values.
        /// </summary>
        internal static bool ShapeCrater(GameObject crater, float radius, float depth)
        {
            TerrainOp dig = crater != null ? crater.GetComponent<TerrainOp>() : null;
            if (dig == null) { ModLog.Verbose($"DynamiteSetup.ShapeCrater: returns false because there is no TerrainOp on {(crater != null ? crater.name : "<none>")}"); return false; }
            TerrainOp.Settings s = dig.m_settings;
            s.m_level = false;
            s.m_raise = true;
            s.m_raiseRadius = radius;
            s.m_raiseDelta = -depth;
            s.m_square = false;                  // round
            s.m_smooth = true;
            s.m_smoothRadius = radius * 1.4f;
            s.m_paintCleared = true;
            s.m_paintType = TerrainModifier.PaintType.Dirt;    // the burnt, bare ground around the hole
            s.m_paintRadius = radius * 1.5f;
            dig.m_settings = s;                  // the same object (Settings is a class); kept explicit
            bool shaped = Mathf.Approximately(s.m_raiseRadius, radius) && Mathf.Approximately(s.m_raiseDelta, -depth);
            { if (RuleLog.Due("DynamiteSetup.ShapeCrater:" + crater.name, 2d)) ModLog.Every("DynamiteSetup.ShapeCrater:" + crater.name, 2d, $"DynamiteSetup.ShapeCrater: {crater.name} radius {radius:0.##} m, depth {depth:0.##} m - returns {shaped} (line 145)"); return shaped; }
        }

        static GameObject MakeCrater(string id)
        {
            if (KyPrefabs.Get(CraterBase) == null) { ModLog.Error("DynamiteSetup: no vanilla " + CraterBase + " - " + id + " digs no hole"); return null; }
            GameObject clone = KyPrefabs.Clone(id, CraterBase);
            if (clone != null) KyPrefabs.Add(clone);
            { var returnedAt110 = clone; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteSetup.MakeCrater: returns {returnedAt110} (line 110)"); return returnedAt110; }
        }

        static bool AttachHandModel(GameObject item, bool stickGrip)
        {
            Transform attach = item.transform.Find("attach");
            Transform model = item.transform.Find("gp_model");
            if (attach == null || model == null)
            {
                ModLog.Warn("DynamiteSetup: no hand model - attach " + (attach != null) + ", bundle model " + (model != null));
                return false;
            }
            if (attach.Find("gp_model") != null) { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteSetup.AttachHandModel: returns true because attach.Find(\"gp_model\") != null (line 122)"); return true; }
            foreach (Renderer old in attach.GetComponentsInChildren<Renderer>(true)) old.enabled = false;
            GameObject copy = Object.Instantiate(model.gameObject, attach);
            copy.name = "gp_model";
            copy.transform.localPosition = stickGrip ? HandGripPosition : PackGripPosition;      // values fitted on the vanilla Player + Club attach in the AssetRipper project (T-009j)
            copy.transform.localRotation = Quaternion.Euler(stickGrip ? HandGripEuler : PackGripEuler);
            copy.transform.localScale = Vector3.one;
            foreach (Collider c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (Transform t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = attach.gameObject.layer;
            Bounds b = default;
            bool any = false;
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
                if (any) b.Encapsulate(r.bounds); else { b = r.bounds; any = true; }
            }
            ModLog.Info("DynamiteSetup: hand model on the attach point, local position " + copy.transform.localPosition.ToString("0.000") + " rot " + HandGripEuler.ToString("0") + " (expected: centre near (0, 0, 0.02), long axis along +Z"
                + "), bounds centre in attach space " + (any ? attach.InverseTransformPoint(b.center).ToString("0.000") : "none") + ", size " + (any ? b.size.ToString("0.000") : "none"));
            { if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteSetup.AttachHandModel: returns true (line 140)"); return true; }
        }
    }
}
