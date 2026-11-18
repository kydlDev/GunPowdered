// kydl:read-only-observer - test-only: the local player is moved to cave entrances and uses them like a player would
using System.Collections;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Features.Guano.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab helper for D-033: generates real caves with the game's own TestSpawnLocation (Full mode, as the console does), walks the player to each
    /// cave's ground entrance and uses it through Teleport.Interact - the same path a player takes - so CaveEntryPatch places the guano on arrival.
    /// The player is moved with the game's own long-distance teleport, which waits until the area has loaded (setting the position directly into an
    /// unloaded area made the player fall through and be respawned: run entry2, 2026-11-18). Every step is logged.
    /// </summary>
    internal static class CaveEntryRun
    {
        const float Spacing = 128f, WaitSpawn = 3f, WaitFill = 25f, WaitBack = 3f, WaitTeleport = 30f, WaitPlayer = 60f, Cooldown = 2.2f;
        static int _left, _count, _spawned, _withEntrance, _entered, _plannedAtStart, _placedAtStart, _filledAtStart;
        static string _name = "";
        static int _runIndex;          // every run uses its own rows: caves of an earlier run stay in the world and overlapped the next run's (entry4)

        internal static int Left => _left;

        internal static string Result() =>
            _name + ": caves " + _spawned + ", entrance " + _withEntrance + ", entered " + _entered + " of " + _withEntrance + ", " + CaveEntryPlacer.Stats.Text();

        /// <summary>"ok" when the last run generated every cave, found and used every entrance, filled every cave and placed every planned deposit;
        /// otherwise "bad: " and the first thing that is wrong. (A plain text the lab compares with expect: a check step ignored regex, run entry1.)</summary>
        internal static string Verdict()
        {
            int planned = CaveEntryPlacer.Stats.Planned - _plannedAtStart, placed = CaveEntryPlacer.Stats.Placed - _placedAtStart, filled = CaveEntryPlacer.Stats.Filled - _filledAtStart;
            string why = _count == 0 ? "no cave run yet"
                : _left > 0 ? "still running, " + _left + " left"
                : _spawned < _count ? "only " + _spawned + " of " + _count + " caves generated"
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
            _runIndex++;
            _left = count; _count = count; _spawned = 0; _withEntrance = 0; _entered = 0; _name = name;
            _plannedAtStart = CaveEntryPlacer.Stats.Planned; _placedAtStart = CaveEntryPlacer.Stats.Placed; _filledAtStart = CaveEntryPlacer.Stats.Filled;
            Plugin.Instance.StartCoroutine(Run(name, count));
            return "started " + count;
        }

        static IEnumerator Run(string name, int count)
        {
            var zs = ZoneSystem.instance;
            Vector3 start = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                string tag = name + " #" + i;
                var around = start + new Vector3(-2f * Spacing + (i % 5) * Spacing, 0f, 2f * Spacing + (i / 5) * Spacing + (_runIndex - 1) * 3f * Spacing);
                yield return TravelTo(tag + " spot", around + Vector3.up * 3f);
                float ground;
                bool haveGround = zs.GetGroundHeight(around, out ground);
                var pos = new Vector3(around.x, ground, around.z);
                ModLog.Verbose("CaveEntryRun: " + tag + " ground " + ground.ToString("0.0") + " m (found " + haveGround + ")");
                bool made = haveGround && zs.TestSpawnLocation(name, pos, true);
                if (made) _spawned++;
                yield return new WaitForSeconds(WaitSpawn);
                Teleport entrance = made ? Entrance(pos) : null;
                ModLog.Verbose("CaveEntryRun: " + tag + " at " + pos.ToString("0") + " spawned " + made + ", entrance " + (entrance != null ? entrance.transform.position.ToString("0") : "none"));
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
        static IEnumerator TravelTo(string what, Vector3 to)
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
