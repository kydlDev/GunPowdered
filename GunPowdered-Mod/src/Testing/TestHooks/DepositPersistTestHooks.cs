// kydl:read-only-observer - test-only: reads the saved records (ZDOs) of the mod's deposits and compares them; changes nothing
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// User 2026-11-23: "will the loaded mud ... / guano ... be saved after a log out? ... when the player goes very far away and comes back ... would the
    /// exact spots ... be constant". Snapshots every Sulfurous Mud and Bat Guano deposit record in the world (ZDOMan, loaded or not - a cave's guano
    /// sits ~5000 m up) and later compares: same count, every position within 1 cm, same amount.
    /// </summary>
    internal static class DepositPersistTestHooks
    {
        static readonly string[] Prefabs = { "GP_SulfurousMud", "GP_GuanoDeposit" };
        static List<(string prefab, Vector3 pos)> _snap = new List<(string, Vector3)>();
        // the zones the world had generated when the snapshot was taken (the game's ZoneSystem.m_generatedZones)
        static HashSet<Vector2s> _generatedAtSnap = new HashSet<Vector2s>();
        static readonly AccessTools.FieldRef<ZoneSystem, HashSet<Vector2s>> GeneratedZones = AccessTools.FieldRefAccess<ZoneSystem, HashSet<Vector2s>>("m_generatedZones");
        static bool _travelling;

        static List<(string prefab, Vector3 pos)> Records()
        {
            var list = new List<(string, Vector3)>();
            var all = AccessTools.Field(typeof(ZDOMan), "m_objectsByID")?.GetValue(ZDOMan.instance) as IDictionary;
            if (all == null) { ModLog.Verbose("DepositPersistTestHooks.Records: no ZDO table - returns empty"); return list; }
            var hashes = new Dictionary<int, string>();
            foreach (string p in Prefabs) hashes[p.GetStableHashCode()] = p;
            foreach (object o in all.Values)
                if (o is ZDO z && hashes.TryGetValue(z.GetPrefab(), out string name)) list.Add((name, z.GetPosition()));
            list.Sort((a, b) => { int c = a.Item2.x.CompareTo(b.Item2.x); if (c == 0) c = a.Item2.z.CompareTo(b.Item2.z); return c != 0 ? c : a.Item2.y.CompareTo(b.Item2.y); });   // Sonar S1244
            ModLog.Verbose("DepositPersistTestHooks.Records: returns " + list.Count + " record(s)");
            return list;
        }

        static string Count(List<(string prefab, Vector3 pos)> l)
        {
            if (RuleLog.Due("DepositPersistTestHooks.Count", 2d)) ModLog.Every("DepositPersistTestHooks.Count", 2d, $"DepositPersistTestHooks.Count(l={l})");
            int mud = l.FindAll(r => r.prefab == Prefabs[0]).Count, guano = l.Count - mud;
            { var returnedAt44 = mud + " mud, " + guano + " guano"; if (RuleLog.Due("DepositPersistTestHooks.Count:44", 2d)) ModLog.Every("DepositPersistTestHooks.Count:44", 2d, $"DepositPersistTestHooks.Count: returns {returnedAt44} (line 44)"); return returnedAt44; }
        }

        /// <summary>Remembers every deposit record. Returns "snapshot N mud, M guano".</summary>
        [TestHook("GunPowdered.depositSnapshot")]
        static string Snapshot()
        {
            _snap = Records();
            _generatedAtSnap = new HashSet<Vector2s>(ZoneSystem.instance != null ? GeneratedZones(ZoneSystem.instance) : new HashSet<Vector2s>());
            string s = "snapshot " + Count(_snap);
            ModLog.Info("DepositPersistTestHooks: " + s);
            return s;
        }

        /// <summary>
        /// "same N mud, M guano" when every snapshot record is still there (same prefab, position within 1 cm) and no record was added in a zone the
        /// world had already generated at the snapshot (ZoneSystem.m_generatedZones then); otherwise "bad: " + the first difference. a20g16 run 1: the
        /// trip 2000 m away generated a new Grave1 zone and the mod placed its first-visit pile mud there (2 deposits) - deposits in zones generated
        /// AFTER the snapshot are first placements, listed as ", K new in zones generated since the snapshot". a20g16b run 1: the first version (D-189)
        /// let through new mud in any zone without deposits - meta-deposits-compare-fails (mud spawned on the arena) caught it.
        /// </summary>
        [TestHook("GunPowdered.depositCompare")]
        static string Compare()
        {
            var now = Records();
            var left = new List<(string prefab, Vector3 pos)>(now);
            string why = _snap.Count == 0 ? "no snapshot (or nothing was placed)" : "";
            for (int i = 0; why.Length == 0 && i < _snap.Count; i++)
            {
                int at = left.FindIndex(r => r.prefab == _snap[i].prefab && Vector3.Distance(r.pos, _snap[i].pos) <= 0.01f);
                if (at >= 0) left.RemoveAt(at);
                else why = _snap[i].prefab + " at " + _snap[i].pos.ToString("0.00") + " is gone or moved (count " + Count(now) + ", was " + Count(_snap) + ")";
            }
            int fresh = 0;
            for (int i = 0; why.Length == 0 && i < left.Count; i++)
            {
                if (_generatedAtSnap.Contains(ZoneSystem.GetZone(left[i].pos))) why = "extra " + left[i].prefab + " at " + left[i].pos.ToString("0.00") + " in a zone that already existed at the snapshot (count " + Count(now) + ", was " + Count(_snap) + ")";
                else fresh++;
            }
            string s = why.Length == 0 ? "same " + Count(_snap) + (fresh > 0 ? ", " + fresh + " new in zones generated since the snapshot" : "") : "bad: " + why;
            ModLog.Info("DepositPersistTestHooks: compare -> " + s);
            return s;
        }

        /// <summary>Takes the player about 2 km from the arena (every zone near the spots unloads), waits there, and comes back. Returns "started"; poll travelDone.</summary>
        [TestHook("GunPowdered.travelFarAndBack")]
        static string TravelFarAndBack(double metres, double waitSeconds)
        {
            ModLog.Verbose("DepositPersistTestHooks.TravelFarAndBack(metres=" + metres + ", waitSeconds=" + waitSeconds + ")");
            if (_travelling) return "FAIL: still travelling";
            _travelling = true;
            Plugin.Instance.StartCoroutine(Travel((float)metres, (float)waitSeconds));
            { if (RuleLog.IsOn()) ModLog.Verbose($"DepositPersistTestHooks.TravelFarAndBack: returns \"started\" (line 96)"); return "started"; }
        }

        /// <summary>True when travelFarAndBack has come back.</summary>
        [TestHook("GunPowdered.travelDone")]
        static bool TravelDone() => !_travelling;

        static IEnumerator Travel(float metres, float wait)
        {
            Vector3 far = TestArena.Spot + new Vector3(metres, 0f, 0f);
            yield return CaveEntryRun.TravelTo("far away", far + Vector3.up * 80f);
            yield return new WaitForSeconds(wait);
            TestArena.MovePlayer();
            yield return new WaitForSeconds(3f);
            ModLog.Info("DepositPersistTestHooks: travelled " + metres.ToString("0") + " m away for " + wait.ToString("0") + " s and back");
            _travelling = false;
        }
    }
}
