// kydl:read-only-observer - test-only: selects a build piece like the hammer menu and holds its real build ghost at chosen spots beside a placed station
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Integrations.Valheim;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the station placement (D-140). The player holds the Hammer (test step), <c>buildSelect</c> picks a piece in the build menu the
    /// way the menu does (the game makes the real ghost), and <c>ghostAt</c> holds that ghost at an exact spot next to a placed station and runs, in the same
    /// frame, the game's own snap search (with the mod's snap filter patched in), the game's no-clipping test and the mod's on-top check.
    /// </summary>
    internal static class StationPlacementTestHooks
    {
        static readonly AccessTools.FieldRef<Player, GameObject> Ghost = AccessTools.FieldRefAccess<Player, GameObject>("m_placementGhost");
        static readonly AccessTools.FieldRef<Player, PieceTable> BuildPieces = AccessTools.FieldRefAccess<Player, PieceTable>("m_buildPieces");
        static readonly AccessTools.FieldRef<Player, bool> NoCost = AccessTools.FieldRefAccess<Player, bool>("m_noPlacementCost");
        static readonly AccessTools.FieldRef<Player, Player.PlacementStatus> Status = AccessTools.FieldRefAccess<Player, Player.PlacementStatus>("m_placementStatus");
        static readonly AccessTools.FieldRef<Player, List<Piece>> TempPieces = AccessTools.FieldRefAccess<Player, List<Piece>>("m_tempPieces");

        /// Selects a piece of the Hammer's menu (build costs off for the test, like the game's "nocost"). Returns "selected &lt;prefab&gt;" or why not.
        [TestHook("GunPowdered.buildSelect")]
        static string BuildSelect(string prefab)
        {
            Player p = Player.m_localPlayer;
            PieceTable table = p != null ? BuildPieces(p) : null;
            GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (table == null || source == null) return "no hammer in hand or no prefab " + prefab;
            NoCost(p) = true;
            AccessTools.Method(typeof(Player), "UpdateAvailablePiecesList").Invoke(p, null);
            if (!table.GetPieceIndex(source.GetComponent<Piece>(), out Vector2Int index, out Piece.PieceCategory category)) return prefab + " is not in the build menu";
            table.SetCategory(category);
            table.SetSelected(index);
            AccessTools.Method(typeof(Player), "SetupPlacementGhost").Invoke(p, null);
            GameObject ghost = Ghost(p);
            ModLog.Verbose("StationPlacementTestHooks: selected " + prefab + ", ghost " + (ghost != null ? ghost.name : "none"));
            return ghost != null && ghost.name == prefab ? "selected " + prefab : "no ghost for " + prefab;
        }

        /// <summary>
        /// Holds the ghost at a spot next to the tagged station and reports what the game and the mod say: "side" = its -x side point 0.3 m from the
        /// station's +x side point (inside the game's 0.5 m snap reach), "top" = on the middle of the station's top, "inside" = the station's middle at ground level.
        /// Returns "snap &lt;station or none&gt;, gap &lt;m&gt;, clip &lt;bool&gt;, status &lt;placement status&gt;".
        /// </summary>
        [TestHook("GunPowdered.ghostAt")]
        static string GhostAt(GameObject target, string where)
        {
            Player p = Player.m_localPlayer;
            GameObject ghost = p != null ? Ghost(p) : null;
            if (ghost == null || target == null) return "no ghost or no station";
            ghost.SetActive(true);
            Bounds st = StationSnapPoints.Footprint(target), gh = StationSnapPoints.Footprint(ghost);
            Vector3 local;
            switch (where)
            {
                case "side": local = new Vector3(st.max.x - gh.min.x + 0.3f, 0f, st.center.z - gh.center.z); break;
                case "top": local = new Vector3(st.center.x, st.max.y, st.center.z); break;
                default: local = new Vector3(st.center.x, 0f, st.center.z); break;
            }
            ghost.transform.rotation = target.transform.rotation;
            ghost.transform.position = target.transform.TransformPoint(local);
            Physics.SyncTransforms();

            string snapped = "none";
            var args = new object[] { ghost.transform, 0.5f, null, null, TempPieces(p) };
            if ((bool)AccessTools.Method(typeof(Player), "FindClosestSnapPoints").Invoke(p, args))
            {
                var a = (Transform)args[2]; var b = (Transform)args[3];
                ghost.transform.position = b.position - (a.position - ghost.transform.position);   // what the game does with the pair
                Physics.SyncTransforms();
                snapped = Utils.GetPrefabName(b.parent.gameObject);
            }
            float gap = target.transform.InverseTransformPoint(ghost.transform.position).x + gh.min.x - st.max.x;
            bool clip = (bool)AccessTools.Method(typeof(Player), "TestGhostClipping").Invoke(p, new object[] { ghost, 0.2f });
            Status(p) = clip ? Player.PlacementStatus.Invalid : Player.PlacementStatus.Valid;
            StationPlacement.CheckGhost(p);
            string answer = string.Format(CultureInfo.InvariantCulture, "snap {0}, gap {1:0.00}, clip {2}, status {3}", snapped, gap, clip, Status(p));
            ModLog.Verbose("StationPlacementTestHooks: ghost " + ghost.name + " " + where + " of " + target.name + ": " + answer);
            return answer;
        }

        /// Moves a tagged piece by (dx, dy, dz) metres as its owner (the 5 m bubble above / below a station). Returns "moved" or why not.
        [TestHook("GunPowdered.movePiece")]
        static string MovePiece(GameObject target, double dx, double dy, double dz)
        {
            ZNetView nview = target != null ? target.GetComponent<ZNetView>() : null;
            if (nview == null || !nview.IsValid()) return "no piece";
            nview.ClaimOwnership();
            Vector3 to = target.transform.position + new Vector3((float)dx, (float)dy, (float)dz);
            target.transform.position = to;
            nview.GetZDO().SetPosition(to);
            ModLog.Verbose("StationPlacementTestHooks: moved " + target.name + " to " + to);
            return "moved";
        }

        /// Ends building: build costs back on. Returns "done".
        [TestHook("GunPowdered.buildDone")]
        static string BuildDone()
        {
            Player p = Player.m_localPlayer;
            if (p != null) { NoCost(p) = false; AccessTools.Method(typeof(Player), "UpdateAvailablePiecesList").Invoke(p, null); }
            return "done";
        }
    }
}
