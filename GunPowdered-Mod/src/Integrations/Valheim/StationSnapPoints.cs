using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Integrations.Valheim
{
    /// <summary>
    /// Builds the placement shape of a batch station or an extension piece (D-140, user 2026-11-20): snap points at ground level in the middle of each side
    /// of the bundle model (StationPlacementRules.SideSnapPoints) so the pieces glue side by side only, and the game's "no clipping" test, so they touch but
    /// never overlap. Snap points left over from the vanilla placeholder (chest / workbench) are removed first. Also measures a piece's footprint in its own space.
    /// </summary>
    internal static class StationSnapPoints
    {
        internal const string SnapTag = "snappoint", NamePrefix = "gp_snap_";
        const string BundleModelChild = "gp_model";                        // BundleVisuals' name for the bundle model
        static readonly string[] Sides = { "left", "right", "back", "front" };

        /// <summary>Gives a station / extension prefab its side snap points and the no-clipping test. Expected outcome: 4 snap points, m_noClipping on; nothing when the prefab has no piece.</summary>
        internal static void Configure(GameObject prefab)
        {
            Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
            if (piece == null) { ModLog.Warn("StationSnapPoints.Configure: no Piece on " + (prefab != null ? prefab.name : "null") + " - no snap points"); return; }
            int removed = 0;
            for (int i = prefab.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = prefab.transform.GetChild(i);
                if (child.CompareTag(SnapTag)) { Object.DestroyImmediate(child.gameObject); removed++; }
            }
            Bounds foot = Footprint(prefab);
            float[] p = StationPlacementRules.SideSnapPoints(foot.min.x, foot.max.x, foot.min.z, foot.max.z);
            for (int i = 0; i < Sides.Length; i++)
            {
                var go = new GameObject(NamePrefix + Sides[i]) { tag = SnapTag, layer = prefab.layer };
                go.transform.SetParent(prefab.transform, false);
                go.transform.localPosition = new Vector3(p[i * 2], 0f, p[i * 2 + 1]);
                go.SetActive(false);                                           // like the game's own snap points: found by tag, never drawn
            }
            piece.m_noClipping = true;
            ModLog.Verbose($"StationSnapPoints.Configure({prefab.name}): {removed} placeholder snap point(s) removed, 4 side points on footprint x {foot.min.x:0.00}..{foot.max.x:0.00}, z {foot.min.z:0.00}..{foot.max.z:0.00}, no clipping");
        }

        /// <summary>
        /// The footprint of a piece in its own space: the bounds of its bundle model's meshes ("gp_model"; the 'fire' part left out). Only the bundle model
        /// counts: a placed Powder Mill also carries the workbench placeholder's 20 m range ring (segments made at run time), which made a placed mill
        /// measure 40 x 40 m (found by the station-placement lab test, 2026-11-20). Expected outcome: a zero box at the origin when the piece has no visible mesh.
        /// </summary>
        internal static Bounds Footprint(GameObject root)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
            Transform model = root.transform.Find(BundleModelChild);
            foreach (MeshFilter mf in (model != null ? model : root.transform).GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<Renderer>();
                if (mf.sharedMesh == null || r == null || !r.enabled || mf.name == "fire") continue;
                Bounds b = mf.sharedMesh.bounds;
                Matrix4x4 m = toRoot * mf.transform.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                {
                    var corner = new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 local = m.MultiplyPoint3x4(corner);
                    if (first) { bounds = new Bounds(local, Vector3.zero); first = false; }
                    else bounds.Encapsulate(local);
                }
            }
            if (RuleLog.IsOn()) ModLog.Verbose($"StationSnapPoints.Footprint({root.name}): returns {bounds.min:0.00} .. {bounds.max:0.00}{(first ? " (no mesh)" : "")}");
            return bounds;
        }
    }
}
