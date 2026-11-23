using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Integrations.Valheim
{
    /// <summary>
    /// The game side of the station placement rules (D-140, StationPlacementRules): turns an extension's build ghost red (Invalid) when it would stand on a
    /// batch station, and keeps snapping to the allowed pairs (an extension to its own base station only; nothing else uses the stations' or extensions' points).
    /// Called from StationPlacementPatch after the game's own placement code ran.
    /// </summary>
    internal static class StationPlacement
    {
        static readonly AccessTools.FieldRef<Player, GameObject> Ghost = AccessTools.FieldRefAccess<Player, GameObject>("m_placementGhost");
        static readonly AccessTools.FieldRef<Player, Player.PlacementStatus> Status = AccessTools.FieldRefAccess<Player, Player.PlacementStatus>("m_placementStatus");
        static readonly AccessTools.FieldRef<Player, List<Transform>> GhostPoints = AccessTools.FieldRefAccess<Player, List<Transform>>("m_tempSnapPoints1");
        static readonly AccessTools.FieldRef<Player, List<Transform>> TargetPoints = AccessTools.FieldRefAccess<Player, List<Transform>>("m_tempSnapPoints2");
        static readonly AccessTools.FieldRef<Player, int> ManualPoint = AccessTools.FieldRefAccess<Player, int>("m_manualSnapPoint");
        static readonly System.Action<Player, bool> SetGhostValid = AccessTools.MethodDelegate<System.Action<Player, bool>>(AccessTools.Method(typeof(Player), "SetPlacementGhostValid"));

        /// <summary>After the game placed the ghost: an extension ghost standing on a batch station turns red. Expected outcome: the status is Invalid only for that case.</summary>
        internal static void CheckGhost(Player player)
        {
            GameObject ghost = Ghost(player);
            if (ghost == null || !ghost.activeSelf || Status(player) != Player.PlacementStatus.Valid) { if (RuleLog.Due("StationPlacement.CheckGhost:none", 2d)) ModLog.Every("StationPlacement.CheckGhost:none", 2d, "StationPlacement.CheckGhost: no ghost or not valid - returns"); return; }
            if (ghost.GetComponent<BatchStationExtension>() == null) { if (RuleLog.Due("StationPlacement.CheckGhost:other", 2d)) ModLog.Every("StationPlacement.CheckGhost:other", 2d, "StationPlacement.CheckGhost: not an extension - returns"); return; }
            BatchStation under = BatchStation.StandsOn(ghost.transform.position);
            if (under == null) { if (RuleLog.Due("StationPlacement.CheckGhost:free", 2d)) ModLog.Every("StationPlacement.CheckGhost:free", 2d, "StationPlacement.CheckGhost: on no station - returns"); return; }
            Status(player) = Player.PlacementStatus.Invalid;
            SetGhostValid(player, false);
            if (RuleLog.Due("station-placement-on-top", 5d)) ModLog.Every("station-placement-on-top", 5d, $"StationPlacement: {ghost.name} would stand on {under.StationKey} at {under.transform.position:0.0} - red (D-140)");
        }

        /// <summary>After the game chose the closest snap pair: a pair the rules forbid is replaced by the closest allowed pair, or no snap. Expected outcome: result/a/b describe an allowed pair or false.</summary>
        internal static void FilterSnap(Player player, Transform ghost, float maxDistance, ref Transform a, ref Transform b, ref bool result)
        {
            if (!result || b == null || ghost == null) { if (RuleLog.Due("StationPlacement.FilterSnap:none", 2d)) ModLog.Every("StationPlacement.FilterSnap:none", 2d, "StationPlacement.FilterSnap: no snap - returns"); return; }
            var ext = ghost.GetComponent<BatchStationExtension>();
            string ghostOf = ext != null ? ext.Station : null;
            if (Allowed(ghostOf, b)) { if (RuleLog.Due("StationPlacement.FilterSnap:ok", 2d)) ModLog.Every("StationPlacement.FilterSnap:ok", 2d, "StationPlacement.FilterSnap: pair allowed - returns"); return; }
            List<Transform> mine = GhostPoints(player), theirs = TargetPoints(player);
            int manual = ManualPoint(player);
            Transform bestA = null, bestB = null;
            float best = float.MaxValue;
            for (int i = 0; i < mine.Count; i++)
            {
                if (manual >= 0 && i != manual) { if (RuleLog.Due("StationPlacement.FilterSnap:49", 2d)) ModLog.Every("StationPlacement.FilterSnap:49", 2d, $"StationPlacement.FilterSnap: skips to the next item (continue) because manual >= 0 && i != manual (line 49)"); continue; }
                foreach (Transform t in theirs)
                {
                    float d = Vector3.Distance(mine[i].position, t.position);
                    if (d <= maxDistance && d < best && Allowed(ghostOf, t)) { best = d; bestA = mine[i]; bestB = t; }
                }
            }
            a = bestA; b = bestB; result = bestA != null;
            if (RuleLog.Due("StationPlacement.FilterSnap:redo", 2d)) ModLog.Every("StationPlacement.FilterSnap:redo", 2d, $"StationPlacement.FilterSnap({ghost.name}): forbidden pair replaced, {(result ? "snaps to " + bestB.parent.name : "no snap")}");
        }

        static bool Allowed(string ghostOf, Transform point)
        {
            var station = point.GetComponentInParent<BatchStation>();
            var ext = point.GetComponentInParent<BatchStationExtension>();
            bool ok = StationPlacementRules.SnapAllowed(ghostOf, station != null ? station.StationKey : null, ext != null ? ext.Station : null);
            if (RuleLog.Due("StationPlacement.Allowed", 2d)) ModLog.Every("StationPlacement.Allowed", 2d, $"StationPlacement.Allowed({ghostOf ?? "-"}, {point.name}): returns {ok}");
            return ok;
        }
    }
}
