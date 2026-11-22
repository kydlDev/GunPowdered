// kydl:read-only-observer - test-only: the local player throws a grenade like a player would; the hooks read where it lands
using System.Collections;
using System.Globalization;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Features.Grenades.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the Spiked Grenade (D-047). The throw goes through the game's own attack (Humanoid.StartAttack with the grenade in hand), so the
    /// animation, the attack's spawn point and the flight are the real ones; the hooks then compare where the blast appeared with where the drawn arc
    /// said it would land (user: "have them throw the explosives and it to follow the arch").
    /// </summary>
    internal static class GrenadeTestHooks
    {
        /// How far the real blast may be from the drawn landing point (the attack starts a few frames after the arc was read; the player turns to the aim).
        const float Tolerance = 0.75f, WaitBlast = 10f;
        static Vector3? _predicted;
        static Vector3? _blast;
        static bool _running;
        static float _thrownAt;
        // the throw flew with grenadeTestDrag on (the negative test meta-grenade-arc-check-fails): its miss is intended, so it is not a CHECK OFF
        // (a11gate: that one intended miss was the only CHECK OFF of every session and turned G06 / G12 into WARN)
        static float _throwTestDrag;
        static string _note = "no throw yet";
        static readonly System.Collections.Generic.List<GameObject> Placed = new System.Collections.Generic.List<GameObject>();
        // blasts that already existed at the throw (an earlier test's blast lives 3 s): never taken for this throw's (runs gren1: meta + creature test read a stale one)
        static readonly System.Collections.Generic.HashSet<int> OldBlasts = new System.Collections.Generic.HashSet<int>();

        /// What the arc shows right now: "hidden", or "landing at (x, z), N m, red=True/False".
        [TestHook("GunPowdered.arcShown")]
        static string ArcShown()
        {
            Vector3? l = ThrowArcView.LastLanding;
            Player p = Player.m_localPlayer;
            if (!l.HasValue || p == null) return "hidden";
            return "landing at (" + l.Value.x.ToString("0.0", CultureInfo.InvariantCulture) + ", " + l.Value.z.ToString("0.0", CultureInfo.InvariantCulture) + "), "
                + Vector3.Distance(p.transform.position, l.Value).ToString("0.0", CultureInfo.InvariantCulture) + " m, red=" + ThrowArcView.LastInBlast;
        }

        /// Throws the held grenade through the game's attack and follows its flight. Expected outcome: true when the attack started.
        [TestHook("GunPowdered.throwGrenade")]
        static bool ThrowGrenade()
        {
            Player p = Player.m_localPlayer;
            if (p == null || !GrenadeSetup.IsGrenade(p.GetCurrentWeapon())) { _note = "no grenade in hand"; ModLog.Verbose("GrenadeTestHooks: throw refused - " + _note); return false; }
            BeginWatch(ThrowArcView.LastLanding);
            bool started = p.StartAttack(null, false);
            _note = started ? "thrown" : "the attack did not start";
            ModLog.Verbose("GrenadeTestHooks: throw " + _note);
            if (started) WatchBlastOnce();
            return started;
        }

        /// Starts following a throw that is about to happen (the aimed throw lets the game start the attack itself): remembers where it should land and which blasts are old.
        internal static void BeginWatch(Vector3? predicted)
        {
            _predicted = predicted;
            _blast = null;
            _thrownAt = Time.time;
            _throwTestDrag = GrenadeSetup.TestFlightDrag;
            OldBlasts.Clear();
            foreach (Aoe a in Object.FindObjectsByType<Aoe>(FindObjectsSortMode.None)) OldBlasts.Add(a.GetInstanceID());
            ModLog.Verbose("GrenadeTestHooks: watching a throw, predicted landing " + (predicted.HasValue ? predicted.Value.ToString("0.00") : "none") + ", " + OldBlasts.Count + " older blast(s) ignored");
        }

        /// Starts the blast watcher unless it already runs.
        internal static void WatchBlastOnce()
        {
            if (!_running) Plugin.Instance.StartCoroutine(WatchBlast());
            else ModLog.Verbose("GrenadeTestHooks: the blast watcher already runs");
        }

        /// Test only: gives the grenade's flight a drag the drawn arc does not model ("0" switches it off again). Returns the drag now in use.
        [TestHook("GunPowdered.grenadeTestDrag")]
        static string GrenadeTestDrag(string value)
        {
            GrenadeSetup.TestFlightDrag = float.Parse(value, CultureInfo.InvariantCulture);
            bool applied = GrenadeSetup.ApplyIfChanged();
            ModLog.Verbose("GrenadeTestHooks: test flight drag " + GrenadeSetup.TestFlightDrag + ", applied " + applied);
            return GrenadeSetup.Projectile != null ? GrenadeSetup.Projectile.GetComponent<global::Projectile>().m_drag.ToString("0.###", CultureInfo.InvariantCulture) : "no flight prefab";
        }

        /// Is the last throw's blast known yet (for a wait-until step).
        [TestHook("GunPowdered.grenadeBlastSeen")]
        static bool BlastSeen() => _blast.HasValue;

        /// "ok" when the blast appeared within the tolerance of the drawn landing point; else "bad: " and why (with the numbers).
        [TestHook("GunPowdered.grenadeFollowedArc")]
        static string FollowedArc()
        {
            if (!_predicted.HasValue) return "bad: no landing was drawn when it was thrown (" + _note + ")";
            if (!_blast.HasValue) return "bad: no blast seen (" + _note + ")";
            float flat = Vector2.Distance(new Vector2(_predicted.Value.x, _predicted.Value.z), new Vector2(_blast.Value.x, _blast.Value.z));
            string nums = "drawn " + _predicted.Value.ToString("0.00") + ", blast " + _blast.Value.ToString("0.00") + ", " + flat.ToString("0.00", CultureInfo.InvariantCulture) + " m apart";
            if (_throwTestDrag > 0f) ModLog.Info("GrenadeTestHooks: intended miss of the negative test (test flight drag " + _throwTestDrag.ToString("0.###", CultureInfo.InvariantCulture) + " at the throw): " + nums);
            else ModLog.Check("grenade blast distance from the drawn landing", flat, 0d, Tolerance, "m");
            ModLog.Verbose("GrenadeTestHooks: " + nums);
            return flat <= Tolerance ? "ok" : "bad: " + nums;
        }

        /// <summary>
        /// D-100 diagnosis: what a blast of <paramref name="radius"/> m at the last burst point collects with the game's Aoe masks (characters, and
        /// props: Default, static_solid, Default_small, piece, hitbox, character_noenv, vehicle). The game's Aoe keeps only the first 100 colliders.
        /// Returns "chars C, props P, first: name, name, ...".
        /// </summary>
        [TestHook("GunPowdered.blastProbe")]
        static string BlastProbe(double radius)
        {
            if (!_blast.HasValue) return "no burst seen";
            int chars = Physics.OverlapSphere(_blast.Value, (float)radius, LayerMask.GetMask("character", "character_net", "character_ghost")).Length;
            Collider[] props = Physics.OverlapSphere(_blast.Value, (float)radius, LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "hitbox", "character_noenv", "vehicle"));
            var names = new System.Collections.Generic.Dictionary<string, int>();
            foreach (Collider c in props)
            {
                string n = LayerMask.LayerToName(c.gameObject.layer) + ":" + Utils.GetPrefabName(Projectile.FindHitObject(c) ?? c.gameObject);
                names[n] = names.TryGetValue(n, out int k) ? k + 1 : 1;
            }
            var top = new System.Collections.Generic.List<string>();
            foreach (var kv in names) top.Add(kv.Key + " x" + kv.Value);
            top.Sort((a, b) => string.CompareOrdinal(a, b));
            string r = "chars " + chars + ", props " + props.Length + ", first: " + string.Join(", ", top.GetRange(0, System.Math.Min(12, top.Count)).ToArray());
            ModLog.Info("GrenadeTestHooks: blastProbe " + r);
            return r;
        }

        /// <summary>True when the nearest piece of this prefab (within 20 m) has lost health (D-100: blasts damage buildings); false when it is whole or missing.</summary>
        [TestHook("GunPowdered.pieceDamaged")]
        static bool PieceDamaged(string prefab)
        {
            float pct = PieceHealth(prefab);
            return pct >= 0f && pct < 99.9f;
        }

        /// Health of the nearest piece of a prefab within 20 m, in percent (100 = untouched), or -1 when there is none.
        [TestHook("GunPowdered.pieceHealth")]
        static float PieceHealth(string prefab)
        {
            Player p = Player.m_localPlayer;
            WearNTear best = null;
            float bestDist = 20f;
            foreach (WearNTear w in Object.FindObjectsByType<WearNTear>(FindObjectsSortMode.None))
            {
                float d = p != null ? Vector3.Distance(p.transform.position, w.transform.position) : 0f;
                if (Utils.GetPrefabName(w.gameObject) == prefab && d < bestDist) { best = w; bestDist = d; }
            }
            float pct = best != null ? best.GetHealthPercentage() * 100f : -1f;
            ModLog.Verbose("GrenadeTestHooks: pieceHealth " + prefab + " = " + pct.ToString("0.0") + " % at " + bestDist.ToString("0.0") + " m");
            return pct;
        }

        /// Moves the nearest creature of a prefab onto the drawn landing point (the target of a damage test). Expected outcome: true when one was moved.
        [TestHook("GunPowdered.creatureToLanding")]
        static bool CreatureToLanding(string prefab)
        {
            Vector3? l = ThrowArcView.LastLanding;
            Player p = Player.m_localPlayer;
            if (!l.HasValue || p == null) { ModLog.Verbose("GrenadeTestHooks: creatureToLanding - no landing drawn"); return false; }
            Character best = null;
            float bestDist = 40f;
            foreach (Character c in Character.GetAllCharacters())
            {
                float d = Vector3.Distance(p.transform.position, c.transform.position);
                if (c != p && Utils.GetPrefabName(c.gameObject) == prefab && d < bestDist) { best = c; bestDist = d; }
            }
            if (best == null) { ModLog.Verbose("GrenadeTestHooks: creatureToLanding - no " + prefab + " within 40 m"); return false; }
            best.transform.position = l.Value + (Vector3.up * 0.1f);
            best.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            ModLog.Verbose("GrenadeTestHooks: " + prefab + " moved onto the landing " + l.Value.ToString("0.00"));
            return Vector3.Distance(best.transform.position, l.Value) < 0.5f;
        }

        /// Places a prefab (a piece, through the game's own ZNetScene prefab) on the drawn landing point. Returns "placed <prefab>" or why not.
        [TestHook("GunPowdered.spawnAtLanding")]
        static string SpawnAtLanding(string prefab)
        {
            Vector3? l = ThrowArcView.LastLanding;
            GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (!l.HasValue) return "no landing drawn";
            if (source == null) return "no prefab " + prefab;
            GameObject go = Object.Instantiate(source, l.Value, Quaternion.identity);
            if (go != null) Placed.Add(go);
            ModLog.Verbose("GrenadeTestHooks: " + prefab + " placed on the landing " + l.Value.ToString("0.00"));
            return go != null ? "placed " + prefab : "could not place " + prefab;
        }

        /// Removes what spawnAtLanding placed (pieces stay on the arena otherwise; cleanArena only removes creatures and loose items). Returns "removed N".
        [TestHook("GunPowdered.removeLandingPieces")]
        static string RemoveLandingPieces()
        {
            int n = 0;
            foreach (GameObject go in Placed)
            {
                if (go == null) continue;
                ZNetView nv = go.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid()) { nv.ClaimOwnership(); ZNetScene.instance.Destroy(go); }
                else Object.Destroy(go);
                n++;
            }
            Placed.Clear();
            ModLog.Verbose("GrenadeTestHooks: removed " + n + " piece(s) placed on landings");
            return "removed " + n;
        }

        /// Removes the pieces earlier tests left standing in front of the thrower (a Rain Barrel 4 m ahead stopped the drawn arc 1.8 m out in the full chain).
        /// Only pieces of this mod (prefab GP_*) within 20 m. Returns "removed N".
        [TestHook("GunPowdered.removeStationsNearby")]
        static string RemoveStationsNearby()
        {
            Player p = Player.m_localPlayer;
            if (p == null || ZNetScene.instance == null) return "removed 0 (no player)";
            int n = 0;
            foreach (Piece piece in Object.FindObjectsByType<Piece>(FindObjectsSortMode.None))
            {
                if (!Utils.GetPrefabName(piece.gameObject).StartsWith("GP_") || Vector3.Distance(p.transform.position, piece.transform.position) > 20f) continue;
                ZNetView nv = piece.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid()) { nv.ClaimOwnership(); ZNetScene.instance.Destroy(piece.gameObject); }
                else Object.Destroy(piece.gameObject);
                n++;
            }
            ModLog.Verbose("GrenadeTestHooks: removed " + n + " station piece(s) of the mod near the thrower");
            return "removed " + n;
        }

        /// <summary>D-157: the landed-grenade fuse: "fuse F s, bursts B, landed now L" (F = the last fuse this client set up from the thrower's Explosives level).</summary>
        [TestHook("GunPowdered.grenadeFuse")]
        static string Fuse()
        {
            ModLog.Verbose("GrenadeTestHooks.Fuse()");
            int landed = Object.FindObjectsByType<GrenadeFuse>(FindObjectsSortMode.None).Length;
            string s = "fuse " + GrenadeFuse.LastFuse.ToString("0.00", CultureInfo.InvariantCulture) + " s, bursts " + GrenadeFuse.Bursts + ", landed now " + landed;
            ModLog.Info("GrenadeTestHooks.Fuse: returns " + s);
            return s;
        }

        // the blast is the grenade's own explosion prefab: its first new instance after the throw is where the grenade burst
        static IEnumerator WatchBlast()
        {
            _running = true;
            float until = Time.time + WaitBlast;
            while (Time.time < until && !_blast.HasValue)
            {
                foreach (Aoe a in Object.FindObjectsByType<Aoe>(FindObjectsSortMode.None))
                {
                    if (a.name.StartsWith(GrenadeSetup.ExplosionId) && !OldBlasts.Contains(a.GetInstanceID())) { _blast = a.transform.position; break; }
                }
                yield return null;
            }
            _note = _blast.HasValue ? "burst at " + _blast.Value.ToString("0.00") + ", " + (Time.time - _thrownAt).ToString("0.00") + " s after the throw" : "no blast within " + WaitBlast + " s";
            ModLog.Verbose("GrenadeTestHooks: " + _note);
            _running = false;
        }
    }
}
