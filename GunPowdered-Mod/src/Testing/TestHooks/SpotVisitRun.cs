// kydl:read-only-observer - test-only: walks the player to real world spots and counts the mud the mod placed there; places nothing itself
using System.Collections;
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// User 2026-11-23: "all tests have to be made on all or a part of the coordinates I've given" - no generated geysers / piles. The player visits real
    /// spots of the lab world (seed TestWorld, Claude\shared\TestLab\templates\README.md "Known spots"). They lie far outside the baked area, so the first
    /// visit makes the game generate the zone (ZoneSystem.SpawnLocation, Full) - the same path a player triggers, where LocationSpawnPatch places the mud.
    /// After each visit it counts GP_SulfurousMud within <see cref="CountMetres"/> and how many lie under the water level.
    /// </summary>
    internal static class SpotVisitRun
    {
        const float WaitLocation = 20f, WaitPlace = 6f, WaitBack = 3f, CountMetres = 25f;
        static readonly Dictionary<string, Vector3[]> Spots = new Dictionary<string, Vector3[]>
        {
            ["geyser"] = new[] { new Vector3(-3575, 30, 971), new Vector3(-3573, 29, 872), new Vector3(-3271, 30, -745), new Vector3(-4415, 29, -964),
                                 new Vector3(-4532, 30, -1176), new Vector3(-4632, 31, -1196), new Vector3(-4607, 29, -1281), new Vector3(-1538, 29, -2663) },
            ["pileWet"] = new[] { new Vector3(-2423, 31, -11), new Vector3(-4427, 31, -1073), new Vector3(-4613, 31, -1141), new Vector3(-4718, 31, -1272),
                                  new Vector3(-3839, 31, 497), new Vector3(-3827, 31, 833), new Vector3(-3520, 31, 884), new Vector3(-3506, 31, 820) },
            ["pileDry"] = new[] { new Vector3(-1398, 192, 640), new Vector3(-1538, 259, 764), new Vector3(-1288, 183, 1209) },
        };

        static int _left;
        static string _kind = "";
        static readonly List<(string location, int mud, int wet)> _seen = new List<(string, int, int)>();

        /// <summary>Starts visiting the first <paramref name="count"/> spots of <paramref name="kind"/> (geyser, pileWet, pileDry). Returns "started N" or why not.</summary>
        [TestHook("GunPowdered.visitSpots")]
        static string Start(string kind, int count)
        {
            ModLog.Verbose("SpotVisitRun.Start(kind=" + kind + ", count=" + count + ")");
            if (_left > 0) return "FAIL: a spot visit is still going (" + _left + " left)";
            if (!Spots.TryGetValue(kind, out var spots)) return "FAIL: no spots for " + kind;
            int n = Mathf.Clamp(count, 1, spots.Length);
            _kind = kind; _left = n; _seen.Clear();
            Plugin.Instance.StartCoroutine(Run(spots, n));
            return "started " + n;
        }

        /// <summary>How many spots are left to visit (0 = done).</summary>
        [TestHook("GunPowdered.visitLeft")]
        static int Left() => _left;

        /// <summary>
        /// The verdict over the last visit: rule "mudEach" = every spot has mud; "mudEachWet" = every spot has mud and all of it lies under water;
        /// "noMud" = no spot has mud. Returns "ok" or "bad: " + the first spot that breaks the rule (with what was seen at every spot).
        /// </summary>
        [TestHook("GunPowdered.visitVerdict")]
        static string Verdict(string rule)
        {
            string why = _left > 0 ? "still visiting, " + _left + " left" : _seen.Count == 0 ? "no spot visited" : "";
            for (int i = 0; why.Length == 0 && i < _seen.Count; i++)
            {
                var s = _seen[i];
                if (s.location == "none") why = "spot " + i + ": no location found";
                else if (rule == "noMud" && s.mud > 0) why = "spot " + i + " (" + s.location + "): " + s.mud + " mud";
                else if (rule != "noMud" && s.mud == 0) why = "spot " + i + " (" + s.location + "): no mud";
                else if (rule == "mudEachWet" && s.wet < s.mud) why = "spot " + i + " (" + s.location + "): " + (s.mud - s.wet) + " of " + s.mud + " mud not under water";
            }
            string all = string.Join("; ", _seen.ConvertAll(s => s.location + " mud " + s.mud + " wet " + s.wet));
            string verdict = why.Length == 0 ? "ok" : "bad: " + why;
            ModLog.Info("SpotVisitRun: " + _kind + " verdict " + rule + " -> " + verdict + " (" + all + ")");
            return verdict;
        }

        static IEnumerator Run(Vector3[] spots, int n)
        {
            for (int i = 0; i < n; i++)
            {
                Vector3 pos = spots[i];
                string tag = _kind + " #" + i;
                yield return CaveEntryRun.TravelTo(tag, pos + Vector3.up * 3f);
                Location loc = null;
                float waited = 0f;
                while (loc == null && waited < WaitLocation) { loc = Location.GetLocation(pos, false); if (loc == null) { waited += 0.5f; yield return new WaitForSeconds(0.5f); } }
                yield return new WaitForSeconds(WaitPlace);      // LocationSpawnPatch places 2 s after the location is generated
                int mud = 0, wet = 0;
                float sea = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
                foreach (ZNetView nv in Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
                {
                    if (nv == null || !nv.name.StartsWith("GP_SulfurousMud") || Vector3.Distance(nv.transform.position, pos) > CountMetres) continue;
                    mud++;
                    if (nv.transform.position.y < sea) wet++;
                }
                _seen.Add((loc != null ? Utils.GetPrefabName(loc.gameObject) : "none", mud, wet));
                ModLog.Info("SpotVisitRun: " + tag + " at " + pos.ToString("0") + ": location " + (loc != null ? loc.name : "none") + " after " + waited.ToString("0.0") + " s, mud " + mud + " (under water " + wet + ") within " + CountMetres + " m");
                TestArena.MovePlayer();
                yield return new WaitForSeconds(WaitBack);
                _left = n - i - 1;
            }
            _left = 0;
        }
    }
}
