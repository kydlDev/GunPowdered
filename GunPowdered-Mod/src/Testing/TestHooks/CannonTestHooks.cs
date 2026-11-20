using System.Collections.Generic;
using System.Globalization;
using KydlCore.Common;
using KydlCore.Features.Diagnostics.Contracts;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Cannon.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the mounted Hand Cannon (D-051 .. D-054). The player acts through the stock's own Interact (mount, load, aim, take off) and
    /// the game's attack (StartAttack, which the CannonAttackPatch turns into lighting the fuse), so the real paths run; only the aimed spot is fixed
    /// (a distance and side angle instead of the camera). Targets put on the shot line stand still (their AI is switched off and they are tracked).
    /// </summary>
    internal static class CannonTestHooks
    {
        static CannonMount _stock;
        static Vector3 _stockAt;
        static int _fuse0, _muzzle0, _impact0;
        static readonly List<GameObject> Placed = new List<GameObject>();

        /// Places a Cannon Stock dz metres in front of the player, facing the way the player faces. Returns "stock at (x, z)".
        [TestHook("GunPowdered.cannonSpawnStock")]
        static string SpawnStock(double dz)
        {
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.CannonStock) : null;
            if (p == null || prefab == null) return "no player or no stock prefab";
            RemoveStock();
            Vector3 fwd = p.transform.forward;
            fwd.y = 0f;
            fwd.Normalize();
            Vector3 at = p.transform.position + (fwd * (float)dz);
            at.y = Ground(at);
            GameObject go = Object.Instantiate(prefab, at, Quaternion.LookRotation(fwd));
            _stock = go.GetComponent<CannonMount>();
            // built by this player, as with the hammer (a piece nobody placed gives back only a third of its resources, run cannonB)
            Piece piece = go.GetComponent<Piece>();
            ZNetView pnv = go.GetComponent<ZNetView>();
            if (piece != null && pnv != null && pnv.IsValid())
            {
                pnv.GetZDO().Set(ZDOVars.s_creator, p.GetPlayerID());
                HarmonyLib.AccessTools.Field(typeof(Piece), "m_creator").SetValue(piece, p.GetPlayerID());
            }
            _stockAt = at;
            _fuse0 = CannonEffects.FuseCount;
            _muzzle0 = CannonEffects.MuzzleCount;
            _impact0 = CannonEffects.ImpactCount;
            CannonAim.TestAim = null;
            ModLog.Verbose("CannonTestHooks: stock placed at " + at.ToString("0.0") + ", behaviour " + (_stock != null));
            return _stock != null ? "stock at (" + F(at.x) + ", " + F(at.z) + ")" : "stock without CannonMount";
        }

        /// Takes the Cannon Stock nearest to the player (placed by the lab's spawn step) as the stock the other hooks use. Returns "adopted at N m".
        [TestHook("GunPowdered.cannonAdoptStock")]
        static string AdoptStock()
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            CannonMount best = null;
            float bestDist = 30f;
            foreach (CannonMount m in Object.FindObjectsByType<CannonMount>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(p.transform.position, m.transform.position);
                if (d < bestDist) { best = m; bestDist = d; }
            }
            if (best == null) return "no stock within 30 m";
            _stock = best;
            _stockAt = best.transform.position;
            _fuse0 = CannonEffects.FuseCount;
            _muzzle0 = CannonEffects.MuzzleCount;
            _impact0 = CannonEffects.ImpactCount;
            return "adopted at " + F(bestDist) + " m";
        }

        /// The player interacts with the stock (alt = shift + use). Returns what Interact answered.
        [TestHook("GunPowdered.cannonUse")]
        static bool Use(bool alt)
        {
            Player p = Player.m_localPlayer;
            if (p == null || _stock == null) return false;
            bool r = _stock.Interact(p, false, alt);
            ModLog.Verbose("CannonTestHooks: use (alt " + alt + ") -> " + r + ", hover now: " + _stock.GetHoverText().Replace("\n", " | "));
            return r;
        }

        /// "mounted X, loaded Y, barrel shown Z, loading W, aiming V".
        [TestHook("GunPowdered.cannonState")]
        static string State()
        {
            if (_stock == null) return "no stock";
            CannonStatus s = _stock.Status();
            bool barrel = _stock.Barrel != null && _stock.Barrel.gameObject.activeSelf;
            return "mounted " + s.Mounted + ", loaded " + s.Loaded + ", barrel shown " + barrel + ", loading " + _stock.Loading + ", aiming " + (CannonAim.Current == _stock);
        }

        /// Is the cast ball in the player's right hand right now (the game's own hand visual, as everybody sees it)?
        [TestHook("GunPowdered.cannonBallInHand")]
        static bool BallInHand()
        {
            Player p = Player.m_localPlayer;
            ZNetView nv = p != null ? p.GetComponent<ZNetView>() : null;
            return nv != null && nv.IsValid() && nv.GetZDO().GetInt(ZDOVars.s_rightItem, 0) == GunPowderedPrefabs.CannonBall.GetStableHashCode();
        }

        /// Starts aiming (Interact) at a spot distance metres out and side degrees off the stock's forward. Returns "aiming True at D m".
        [TestHook("GunPowdered.cannonAim")]
        static string Aim(double distance, double side)
        {
            Player p = Player.m_localPlayer;
            if (p == null || _stock == null) return "no stock";
            CannonAim.TestAim = new Vector2((float)distance, (float)side);
            if (CannonAim.Current != _stock) _stock.Interact(p, false, false);
            return "aiming " + (CannonAim.Current == _stock) + " at " + F(CannonAim.TargetDistance) + " m";
        }

        /// Presses attack the way the game does (Humanoid.StartAttack). Returns the cannon's answer: "lit" or why not.
        [TestHook("GunPowdered.cannonLight")]
        static string Light()
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            CannonBallFlight.ClearLast();
            p.StartAttack(null, false);
            return CannonAim.LastLight;
        }

        /// What the last shot did ("landed at ... m (aimed ... m), passed N, stopped by ..., hits: ...").
        [TestHook("GunPowdered.cannonShot")]
        static string Shot() => CannonBallFlight.LastReport;

        /// Has the last fired ball come to rest?
        [TestHook("GunPowdered.cannonShotDone")]
        static bool ShotDone() => !CannonBallFlight.InFlight && CannonBallFlight.LastStop.HasValue;

        /// Did the last ball stop within tolerance metres of distance (flat, from the muzzle)?
        [TestHook("GunPowdered.cannonLandedNear")]
        static bool LandedNear(double distance, double tolerance)
        {
            bool ok = ShotDone() && Mathf.Abs(CannonBallFlight.LastDistance - (float)distance) <= (float)tolerance;
            ModLog.Verbose("CannonTestHooks: landed " + F(CannonBallFlight.LastDistance) + " m, wanted " + F((float)distance) + " +- " + F((float)tolerance) + " -> " + ok);
            return ok;
        }

        /// Did the last ball land where the landing rule says after its pass-through (D-053 addendum 2), within tolerance? "held, expected X, landed Y" or "broken, ...".
        [TestHook("GunPowdered.cannonLandingRule")]
        static string LandingRule(double tolerance)
        {
            if (!ShotDone() || CannonBallFlight.LastPassed < 1) return "no pass-through shot";
            float want = CannonRules.PassThroughLanding(CannonBallFlight.LastAimed, CannonBallFlight.LastPassAt);
            bool ok = Mathf.Abs(CannonBallFlight.LastDistance - want) <= (float)tolerance;
            return (ok ? "held" : "broken") + ", expected " + F(want) + ", landed " + F(CannonBallFlight.LastDistance);
        }

        /// The damage of each hit of the last ball, in order: "130.0, 65.0" ("none").
        [TestHook("GunPowdered.cannonDamages")]
        static string Damages()
        {
            if (CannonBallFlight.LastDamages.Count == 0) return "none";
            var parts = new List<string>();
            foreach (float d in CannonBallFlight.LastDamages) parts.Add(F(d));
            return string.Join(", ", parts.ToArray());
        }

        /// How many trees / creatures the last ball passed.
        [TestHook("GunPowdered.cannonPassed")]
        static int Passed() => CannonBallFlight.LastPassed;

        /// Effect bursts since the stock was placed: "fuse True, muzzle 1, impact 2" (fuse = any spark at the touch hole).
        [TestHook("GunPowdered.cannonEffects")]
        static string Effects() =>
            "fuse " + (CannonEffects.FuseCount > _fuse0) + ", muzzle " + (CannonEffects.MuzzleCount - _muzzle0) + ", impact " + (CannonEffects.ImpactCount - _impact0);

        /// Cannon Ball items lying within 1.5 m of where the last ball stopped.
        [TestHook("GunPowdered.cannonBallsAtStop")]
        static int BallsAtStop() => CannonBallFlight.LastStop.HasValue ? Drops(GunPowderedPrefabs.CannonBall, CannonBallFlight.LastStop.Value, 1.5f) : 0;

        /// The player picks up the Cannon Ball lying where the last ball stopped. Returns true when it went into the inventory.
        [TestHook("GunPowdered.cannonPickBall")]
        static bool PickBall()
        {
            Player p = Player.m_localPlayer;
            if (p == null || !CannonBallFlight.LastStop.HasValue) return false;
            foreach (ItemDrop d in Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None))
                if (Utils.GetPrefabName(d.gameObject) == GunPowderedPrefabs.CannonBall && Vector3.Distance(d.transform.position, CannonBallFlight.LastStop.Value) < 1.5f)
                    return p.Pickup(d.gameObject, false, false);
            return false;
        }

        /// How many of a prefab the player carries (all stacks).
        [TestHook("GunPowdered.invCount")]
        static int InvCount(string prefab) => Player.m_localPlayer != null ? CannonInventory.Count(Player.m_localPlayer.GetInventory(), prefab) : -1;

        /// Removes the stock with the hammer's own remove (WearNTear.Remove: resources and the mounted cannon drop). Returns "removed".
        [TestHook("GunPowdered.cannonDismantle")]
        static string Dismantle()
        {
            WearNTear w = _stock != null ? _stock.GetComponent<WearNTear>() : null;
            if (w == null) return "no stock";
            w.Remove();
            _stock = null;
            return "removed";
        }

        /// Items of a prefab lying within 4 m of where the stock stood plus those the player already carries (the auto pickup takes drops at the feet).
        [TestHook("GunPowdered.recoveredNearStock")]
        static int RecoveredNearStock(string prefab) => Drops(prefab, _stockAt, 4f) + InvCount(prefab);

        /// Puts a prefab (creature, tree or piece) on the shot line, distance metres in front of the stock, facing it. Creatures stand still.
        [TestHook("GunPowdered.cannonPlaceOnLine")]
        static string PlaceOnLine(string prefab, double distance)
        {
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (go == null || _stock == null) return "no prefab " + prefab + " or no stock";
            Vector3 fwd = _stock.transform.forward;
            Vector3 at = _stock.transform.position + (fwd * (float)distance);
            at.y = Ground(at);
            GameObject made = Object.Instantiate(go, at, Quaternion.LookRotation(-fwd));
            Placed.Add(made);
            Character c = made.GetComponent<Character>();
            if (c != null)
            {
                foreach (BaseAI ai in made.GetComponentsInChildren<BaseAI>()) ai.enabled = false;
                FlightRecorder.Track(c, "cannon test target " + prefab, () => "AI off, stands on the shot line, health " + F(c.GetHealth()));
            }
            string standing = c != null ? "creature" : made.GetComponent<Piece>() != null ? StandOnGround(made, at.y) : "not a building piece, left as placed";
            ModLog.Info("CannonTestHooks: placed " + prefab + " at " + F((float)distance) + " m on the line, " + at.ToString("0.0") + ", " + standing);
            return "placed " + prefab;
        }

        /// Lifts a placed piece whose colliders reach below the ground (a pivot in the middle, like the wood wall) so it stands on the ground. Returns the collider bounds for the log.
        static string StandOnGround(GameObject made, float groundY)
        {
            Collider[] cols = made.GetComponentsInChildren<Collider>();
            if (cols.Length == 0) return "no collider";
            Bounds b = cols[0].bounds;
            foreach (Collider col in cols) b.Encapsulate(col.bounds);
            float lift = groundY - b.min.y;
            if (lift > 0.01f) made.transform.position += Vector3.up * lift;
            return "colliders y " + F(b.min.y) + ".." + F(b.max.y) + ", lifted " + F(lift > 0.01f ? lift : 0f) + " m";
        }

        /// How many of the placed creatures of a prefab have lost health.
        [TestHook("GunPowdered.cannonHurt")]
        static int Hurt(string prefab)
        {
            int n = 0;
            foreach (GameObject g in Placed)
            {
                Character c = g != null ? g.GetComponent<Character>() : null;
                if (c != null && Utils.GetPrefabName(g) == prefab && (c.IsDead() || c.GetHealth() < c.GetMaxHealth() - 0.5f)) n++;
            }
            return n;
        }

        /// What the right hand shows: "N renderers, enabled M, size X m, at Y m from the hand, lossy scale S" of the objects under the hand joint.
        [TestHook("GunPowdered.rightHandInfo")]
        static string RightHandInfo()
        {
            Player p = Player.m_localPlayer;
            VisEquipment vis = p != null ? p.GetComponent<VisEquipment>() : null;
            if (vis == null || vis.m_rightHand == null) return "no hand";
            int n = 0, on = 0;
            Bounds b = new Bounds(vis.m_rightHand.position, Vector3.zero);
            foreach (Renderer r in vis.m_rightHand.GetComponentsInChildren<Renderer>(true))
            {
                n++;
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                on++;
                b.Encapsulate(r.bounds);
            }
            var names = new List<string>();
            foreach (Transform c in vis.m_rightHand) names.Add(c.name);
            string info = n + " renderers, enabled " + on + ", size " + F(b.size.magnitude) + " m, centre " + F(Vector3.Distance(b.center, vis.m_rightHand.position))
                + " m from the hand, hand lossy scale " + vis.m_rightHand.lossyScale.ToString("0.00") + ", children: " + string.Join(", ", names.ToArray());
            ModLog.Info("CannonTestHooks: right hand " + info);
            return info;
        }

        /// Sets the third-person camera's distance from the player in metres (close-up pictures; the game's default is 4). Returns the old distance.
        [TestHook("GunPowdered.cameraDistance")]
        static float CameraDistance(double metres)
        {
            if (GameCamera.instance == null) return -1f;
            var field = HarmonyLib.AccessTools.Field(typeof(GameCamera), "m_distance");
            float old = (float)field.GetValue(GameCamera.instance);
            field.SetValue(GameCamera.instance, (float)metres);
            return old;
        }

        /// How many of the objects placed on the line with that prefab still exist (a rock the ball broke is gone).
        [TestHook("GunPowdered.cannonPlacedLeft")]
        static int PlacedLeft(string prefab)
        {
            int n = 0;
            foreach (GameObject g in Placed)
                if (g != null && Utils.GetPrefabName(g) == prefab) n++;
            ModLog.Verbose("CannonTestHooks: " + n + " placed " + prefab + " left");
            return n;
        }

        /// Tames the creatures of that prefab placed on the line (the game's own Character.SetTamed). Returns how many are tamed now.
        [TestHook("GunPowdered.cannonTame")]
        static int Tame(string prefab)
        {
            int n = 0;
            foreach (GameObject g in Placed)
            {
                Character c = g != null && Utils.GetPrefabName(g) == prefab ? g.GetComponent<Character>() : null;
                if (c == null) continue;
                c.SetTamed(true);
                if (c.IsTamed()) n++;
            }
            ModLog.Verbose("CannonTestHooks: tamed " + n + " " + prefab);
            return n;
        }

        /// Turns the local player's PvP (the game's friendly-fire switch) on or off. Returns the state the game reports afterwards.
        [TestHook("GunPowdered.setPvp")]
        static bool SetPvp(bool on)
        {
            Player p = Player.m_localPlayer;
            if (p == null) throw new System.InvalidOperationException("no local player");
            p.SetPVP(on);
            ModLog.Verbose("CannonTestHooks: PvP asked " + on + ", now " + p.IsPVPEnabled());
            return p.IsPVPEnabled();
        }

        /// Removes the stock, what was placed on the line, rock fragments the ball left, the aim override and PvP (the end of every cannon test,
        /// before cleanArena; killall / removedrops do not touch rocks).
        [TestHook("GunPowdered.cannonCleanup")]
        static string Cleanup()
        {
            CannonAim.End("test cleanup");
            CannonAim.TestAim = null;
            Player me = Player.m_localPlayer;
            if (me != null && me.IsPVPEnabled()) me.SetPVP(false);
            RemoveStock();
            int removed = 0;
            foreach (GameObject g in Placed)
            {
                ZNetView nv = g != null ? g.GetComponent<ZNetView>() : null;
                if (nv != null && nv.IsValid()) { nv.ClaimOwnership(); ZNetScene.instance.Destroy(g); removed++; }
            }
            Placed.Clear();
            int fragments = RemoveRockFragments(me != null ? me.transform.position : Vector3.zero, 60f);
            return "cleaned " + removed + ", rock fragments " + fragments;
        }

        // a rock the ball breaks leaves fragments (a "..._frac" prefab or a "___MineRock5" object, run a5cannon1); the arena has no rocks of its own,
        // so every MineRock5 / fragment near the player is debris and the next test must not find it on the line
        static int RemoveRockFragments(Vector3 around, float radius)
        {
            int n = 0;
            foreach (ZNetView nv in Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
            {
                if (nv == null || !nv.IsValid() || Vector3.Distance(nv.transform.position, around) > radius) continue;
                bool debris = nv.GetComponent<MineRock5>() != null || Utils.GetPrefabName(nv.gameObject).EndsWith("_frac");
                if (!debris) continue;
                nv.ClaimOwnership();
                ZNetScene.instance.Destroy(nv.gameObject);
                n++;
            }
            ModLog.Verbose("CannonTestHooks: removed " + n + " rock fragment(s) within " + F(radius) + " m");
            return n;
        }

        static void RemoveStock()
        {
            if (_stock == null) return;
            ZNetView nv = _stock.GetComponent<ZNetView>();
            if (nv != null && nv.IsValid()) { nv.ClaimOwnership(); ZNetScene.instance.Destroy(_stock.gameObject); }
            _stock = null;
        }

        static int Drops(string prefab, Vector3 at, float radius)
        {
            int n = 0;
            foreach (ItemDrop d in Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None))
                if (Utils.GetPrefabName(d.gameObject) == prefab && Vector3.Distance(d.transform.position, at) <= radius) n += d.m_itemData.m_stack;
            return n;
        }

        static float Ground(Vector3 at)
        {
            RaycastHit h;
            if (Physics.Raycast(at + (Vector3.up * 50f), Vector3.down, out h, 120f, LayerMask.GetMask("Default", "static_solid", "terrain"), QueryTriggerInteraction.Ignore)) return h.point.y;
            return at.y;
        }

        static string F(float f) => f.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
