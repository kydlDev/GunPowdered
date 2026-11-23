// kydl:read-only-observer - test-only: the local player is moved to cave entrances and uses them like a player would
using System.Collections;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Features.Guano.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab helper for D-033: visits the REAL caves of the lab world (SpotsFor, user 2026-11-23: no generated caves in tests), walks the player to each
    /// cave's ground entrance and uses it through Teleport.Interact - the same path a player takes - so CaveEntryPatch places the guano on arrival.
    /// The player is moved with the game's own long-distance teleport, which waits until the area has loaded (setting the position directly into an
    /// unloaded area made the player fall through and be respawned: run entry2, 2026-11-18). Every step is logged.
    /// </summary>
    internal static class CaveEntryRun
    {
        const float WaitLocation = 20f, WaitFill = 25f, WaitBack = 3f, WaitTeleport = 30f, WaitPlayer = 60f, Cooldown = 2.2f;
        static int _left, _count, _spawned, _withEntrance, _entered, _plannedAtStart, _placedAtStart, _filledAtStart;
        static string _name = "";

        internal static int Left => _left;

        /// <summary>The arena step forgets the last cave run (a18g16-1: meta-cave-verdict-fails read an earlier test's "ok" in a full run). Not while one runs.</summary>
        internal static void Forget()
        {
            if (_left > 0) { ModLog.Verbose("CaveEntryRun.Forget: a run is still going - kept"); return; }
            _count = 0; _spawned = 0; _withEntrance = 0; _entered = 0; _name = "";
            ModLog.Verbose("CaveEntryRun.Forget: last cave run forgotten");
        }

        internal static string Result() =>
            _name + ": caves " + _spawned + ", entrance " + _withEntrance + ", entered " + _entered + " of " + _withEntrance + ", " + CaveEntryPlacer.Stats.Text();

        /// <summary>"ok" when the last run generated every cave, found and used every entrance, filled every cave and placed every planned deposit;
        /// otherwise "bad: " and the first thing that is wrong. (A plain text the lab compares with expect: a check step ignored regex, run entry1.)</summary>
        internal static string Verdict()
        {
            int planned = CaveEntryPlacer.Stats.Planned - _plannedAtStart, placed = CaveEntryPlacer.Stats.Placed - _placedAtStart, filled = CaveEntryPlacer.Stats.Filled - _filledAtStart;
            string why = _count == 0 ? "no cave run yet"
                : _left > 0 ? "still running, " + _left + " left"
                : _spawned < _count ? "location found at " + _spawned + " of " + _count + " known spots"
                : _withEntrance < _count ? "entrance found in " + _withEntrance + " of " + _count + " caves"
                : _entered < _count ? "entrance used in " + _entered + " of " + _count + " caves"
                : filled < _count ? "guano placement ran in " + filled + " of " + _count + " caves"
                : placed < planned ? "placed " + placed + " of " + planned + " planned deposits"
                : "";
            string verdict = why.Length == 0 ? "ok" : "bad: " + why;
            ModLog.Verbose("CaveEntryRun: verdict " + verdict + " (" + Result() + ", this run filled " + filled + ", planned " + planned + ", placed " + placed + ")");
            return verdict;
        }

        internal static string Start(string name, int count)
        {
            if (_left > 0) return "FAIL: a cave run is still going (" + _left + " left)";
            _left = count; _count = count; _spawned = 0; _withEntrance = 0; _entered = 0; _name = name;
            _plannedAtStart = CaveEntryPlacer.Stats.Planned; _placedAtStart = CaveEntryPlacer.Stats.Placed; _filledAtStart = CaveEntryPlacer.Stats.Filled;
            Plugin.Instance.StartCoroutine(Run(name, count));
            return "started " + count;
        }

        /// <summary>
        /// The real caves of the lab world (seed TestWorld, user 2026-11-23, checked by survey-world-spots a18spots2; Claude\shared\TestLab\templates\README.md
        /// "Known spots"): the run visits these instead of generating caves next to the arena spot, whose place moved with the lab start (a17/a18:
        /// a generated crypt on a hill had no entrance, 7 of 8). Same caves every run. Position = the location's own position (x, ground y, z).
        /// </summary>
        internal static Vector3[] SpotsFor(string name) =>
            name.StartsWith("TrollCave") ? new[] { new Vector3(-2169, 37, -2883), new Vector3(-1913, 46, -3142), new Vector3(-2172, 54, -3261), new Vector3(-1657, 37, -3330),
                                                   new Vector3(-1157, 35, -4860), new Vector3(133, 41, -3646), new Vector3(1032, 33, -1145), new Vector3(-261, 55, -827) }
            : name.StartsWith("Crypt") ? new[] { new Vector3(-575, 31, 323), new Vector3(-770, 33, 117), new Vector3(-1090, 48, 327), new Vector3(2299, 54, 5374),   // #3 Hildir_crypt
                                               new Vector3(-1015, 50, 67), new Vector3(-1082, 44, -71), new Vector3(-1223, 50, 118), new Vector3(-1410, 58, 201) }
            : name.StartsWith("MountainCave") ? new[] { new Vector3(-1216, 132, 960), new Vector3(-1408, 315, 960), new Vector3(-1344, 270, 832),   // #2 Hildir_cave
                                                      new Vector3(-1344, 236, 768), new Vector3(-1408, 130, 1216), new Vector3(-1600, 226, 768), new Vector3(-1728, 132, 896), new Vector3(-1472, 146, 384) }
            : new Vector3[0];

        static IEnumerator Run(string name, int count)
        {
            Vector3[] spots = SpotsFor(name);
            for (int i = 0; i < count; i++)
            {
                string tag = name + " #" + i;
                bool known = i < spots.Length;
                Vector3 pos = known ? spots[i] : Vector3.zero;
                if (known) yield return TravelTo(tag + " spot", pos + Vector3.up * 3f);
                Teleport entrance = null;
                float waited = 0f;
                while (known && entrance == null && waited < WaitLocation)
                {
                    entrance = Entrance(pos);
                    if (entrance == null) { waited += 0.5f; yield return new WaitForSeconds(0.5f); }
                }
                Location loc = known ? Location.GetLocation(pos, false) : null;
                if (loc != null) _spawned++;
                ModLog.Verbose("CaveEntryRun: " + tag + " at " + pos.ToString("0") + (known ? "" : " (no known spot)") + ": location " + (loc != null ? loc.name : "none")
                    + ", entrance " + (entrance != null ? entrance.transform.position.ToString("0") : "none") + " after " + waited.ToString("0.0") + " s");
                if (entrance != null)
                {
                    _withEntrance++;
                    yield return UseEntrance(tag, entrance);
                }
                TestArena.MovePlayer();
                yield return new WaitForSeconds(WaitBack);
                _left = count - i - 1;
            }
            ModLog.Verbose("CaveEntryRun: done - " + Result());
            _left = 0;
        }

        static IEnumerator UseEntrance(string tag, Teleport entrance)
        {
            int filledBefore = CaveEntryPlacer.Stats.Filled;
            yield return TravelTo(tag + " entrance", entrance.transform.position + entrance.transform.forward * 1.5f);
            Player p = Player.m_localPlayer;
            float ready = 0f;
            while (p != null && (p.IsTeleporting() || CooldownOf(p) < Cooldown) && ready < WaitTeleport) { ready += 0.25f; yield return new WaitForSeconds(0.25f); }
            ModLog.Verbose("CaveEntryRun: " + tag + " before the entrance: player " + (p != null) + ", teleporting " + (p != null && p.IsTeleporting()) + ", cooldown " + (p != null ? CooldownOf(p).ToString("0.0") : "-") + " s (needs 2), waited " + ready.ToString("0.0") + " s, at " + (p != null ? p.transform.position.ToString("0") : "-"));
            bool used = p != null && entrance.Interact(p, false, false);
            if (used) _entered++;
            float waited = 0f;
            while (used && waited < WaitFill && CaveEntryPlacer.Stats.Filled == filledBefore) { waited += 0.25f; yield return new WaitForSeconds(0.25f); }
            ModLog.Verbose("CaveEntryRun: " + tag + " entrance used " + used + ", filled " + (CaveEntryPlacer.Stats.Filled > filledBefore) + " after " + waited.ToString("0.0") + " s, player at " + (Player.m_localPlayer != null ? Player.m_localPlayer.transform.position.ToString("0") : "-"));
        }

        // The game's own teleport: it waits until the target area is loaded and has a floor. Waits for a player to exist first (after a respawn).
        internal static IEnumerator TravelTo(string what, Vector3 to)
        {
            float waited = 0f;
            while (Player.m_localPlayer == null && waited < WaitPlayer) { waited += 0.5f; yield return new WaitForSeconds(0.5f); }
            Player p = Player.m_localPlayer;
            while (p != null && (p.IsTeleporting() || CooldownOf(p) < Cooldown) && waited < WaitPlayer) { waited += 0.25f; yield return new WaitForSeconds(0.25f); }
            bool asked = p != null && p.TeleportTo(to, p.transform.rotation, true);
            float travel = 0f;
            while (asked && p != null && p.IsTeleporting() && travel < WaitTeleport) { travel += 0.25f; yield return new WaitForSeconds(0.25f); }
            ModLog.Verbose("CaveEntryRun: travel to " + what + " " + to.ToString("0") + ": player " + (p != null) + ", teleport asked " + asked + ", took " + travel.ToString("0.0") + " s (waited " + waited.ToString("0.0") + " s before), now at " + (p != null ? p.transform.position.ToString("0") : "-"));
        }

        static float CooldownOf(Player p)
        {
            object v = HarmonyLib.AccessTools.Field(typeof(Player), "m_teleportCooldown")?.GetValue(p);
            return v is float f ? f : 0f;
        }

        // The ground entrance of the cave at that spot: a Teleport whose target point is in the interior band (~5000 m up).
        static Teleport Entrance(Vector3 pos)
        {
            Location loc = Location.GetLocation(pos, false);
            if (loc == null) return null;
            foreach (Teleport t in loc.GetComponentsInChildren<Teleport>())
                if (t.m_targetPoint != null && t.transform.position.y < 3000f && t.m_targetPoint.transform.position.y > 3000f) return t;
            return null;
        }
    }
}
