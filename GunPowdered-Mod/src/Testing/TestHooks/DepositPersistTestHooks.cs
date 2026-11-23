// kydl:read-only-observer - test-only: reads the saved records (ZDOs) of the mod's deposits and compares them; changes nothing
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;

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
            int mud = l.FindAll(r => r.prefab == Prefabs[0]).Count, guano = l.Count - mud;
            return mud + " mud, " + guano + " guano";
        }

        /// <summary>Remembers every deposit record. Returns "snapshot N mud, M guano".</summary>
        [TestHook("GunPowdered.depositSnapshot")]
        static string Snapshot()
        {
            _snap = Records();
            string s = "snapshot " + Count(_snap);
            ModLog.Info("DepositPersistTestHooks: " + s);
            return s;
        }

        /// <summary>"same N mud, M guano" when the records match the snapshot exactly (count and every position within 1 cm); otherwise "bad: " + the first difference.</summary>
        [TestHook("GunPowdered.depositCompare")]
        static string Compare()
        {
            var now = Records();
            string why = _snap.Count == 0 ? "no snapshot (or nothing was placed)" : now.Count != _snap.Count ? "count " + Count(now) + ", was " + Count(_snap) : "";
            for (int i = 0; why.Length == 0 && i < now.Count; i++)
                if (now[i].prefab != _snap[i].prefab || Vector3.Distance(now[i].pos, _snap[i].pos) > 0.01f)
                    why = _snap[i].prefab + " at " + _snap[i].pos.ToString("0.00") + " is now " + now[i].prefab + " at " + now[i].pos.ToString("0.00");
            string s = why.Length == 0 ? "same " + Count(now) : "bad: " + why;
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
            return "started";
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
