using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>Hooks of the arena-cleanup-terrain test (D-142): notes the ground height in front of the player and tells later how far it moved, so a test can check that
    /// the start-of-test cleanup (KydlCore TestArena.MovePlayer) put dug ground back.</summary>
    internal static class GroundNoteTestHooks
    {
        static float _noted;

        /// Notes the ground height `dz` m in front of the player. Returns the height ("12.34"), or "no ground" when it is not loaded.
        [TestHook("GunPowdered.groundNote")]
        static string GroundNote(double dz)
        {
            if (Player.m_localPlayer == null) { if (RuleLog.IsOn()) ModLog.Verbose($"GroundNoteTestHooks.GroundNote: returns \"no ground\" because Player.m_localPlayer == null (line 17)"); return "no ground"; }
            var at = Player.m_localPlayer.transform.position + Vector3.forward * (float)dz;
            if (!ZoneSystem.instance.GetGroundHeight(at, out float h)) { if (RuleLog.IsOn()) ModLog.Verbose($"GroundNoteTestHooks.GroundNote: returns \"no ground\" because !ZoneSystem.instance.GetGroundHeight(at, out float h) (line 19)"); return "no ground"; }
            _noted = h;
            ModLog.Verbose("GroundNoteTestHooks: ground noted " + h.ToString("0.00") + " at " + at.ToString("F0"));
            return h.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// How far the ground `dz` m in front of the player is above the noted height now ("-0.35" = lower): "0.00" when unchanged, "no ground" when not loaded.
        [TestHook("GunPowdered.groundDelta")]
        static string GroundDelta(double dz)
        {
            if (Player.m_localPlayer == null) { if (RuleLog.IsOn()) ModLog.Verbose($"GroundNoteTestHooks.GroundDelta: returns \"no ground\" because Player.m_localPlayer == null (line 29)"); return "no ground"; }
            var at = Player.m_localPlayer.transform.position + Vector3.forward * (float)dz;
            if (!ZoneSystem.instance.GetGroundHeight(at, out float h)) { if (RuleLog.IsOn()) ModLog.Verbose($"GroundNoteTestHooks.GroundDelta: returns \"no ground\" because !ZoneSystem.instance.GetGroundHeight(at, out float h) (line 31)"); return "no ground"; }
            string delta = (Mathf.Round((h - _noted) * 100f) / 100f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            ModLog.Verbose("GroundNoteTestHooks: ground now " + h.ToString("0.00") + ", " + delta + " from the note");
            return delta == "-0.00" ? "0.00" : delta;
        }
        /// Applies one of the game's own terrain operations (a path / cultivate / road prefab known to ObjectDB; the game looks an operation up by its prefab name, so a made-up object does nothing) `dz` m in front of the player. The change is stored in the zone's terrain data like a hoe or pickaxe use. Returns "dug with <prefab> at (x, y, z)" or "no terrain operation prefab".
        [TestHook("GunPowdered.digTerrain")]
        static string DigTerrain(double dz)
        {
            if (Player.m_localPlayer == null) { if (RuleLog.IsOn()) ModLog.Verbose($"GroundNoteTestHooks.DigTerrain: returns \"no player\" because Player.m_localPlayer == null (line 40)"); return "no player"; }
            string chosen = null;
            foreach (string name in new[] { "path_v2", "cultivate_v2", "paved_road_v2", "mud_road_v2", "raise_v2", "digg_v2" })
                if (ObjectDB.instance.TryGetTerrainOp(name.GetStableHashCode(), out _)) { chosen = name; { if (RuleLog.IsOn()) ModLog.Verbose($"GroundNoteTestHooks.DigTerrain: leaves the loop / case (break) because ObjectDB.instance.TryGetTerrainOp(name.GetStableHashCode(), out _) (line 43)"); break; } }
            if (chosen == null) { if (RuleLog.IsOn()) ModLog.Verbose($"GroundNoteTestHooks.DigTerrain: returns \"no terrain operation prefab\" because chosen == null (line 44)"); return "no terrain operation prefab"; }
            var at = Player.m_localPlayer.transform.position + Vector3.forward * (float)dz;
            at.y = ZoneSystem.instance.GetGroundHeight(at);
            var go = new GameObject(chosen);
            go.SetActive(false);                                 // Awake applies the operation: settings first
            go.transform.position = at;
            go.AddComponent<TerrainOp>();
            go.SetActive(true);
            ModLog.Info("GroundNoteTestHooks: terrain op " + chosen + " applied at " + at.ToString("F0"));
            return "dug with " + chosen + " at " + at.ToString("F0");
        }    }
}
