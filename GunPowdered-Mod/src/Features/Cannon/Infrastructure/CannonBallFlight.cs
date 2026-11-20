using System;
using System.Collections.Generic;
using System.Globalization;
using KydlCore.Common;
using KydlCore.Features.Diagnostics.Contracts;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Features.Cannon.Infrastructure
{
    /// <summary>
    /// The flight of a fired Cannon Ball (D-053, D-054): a real ballistic ball, stepped like the game's projectiles (ThrowArcRules) and tested segment
    /// by segment against the world. It passes the FIRST tree or creature it meets (damage, then speed and damage times the pass factor, and a new
    /// arc that lands half way between that impact and the aimed spot); a second tree or creature stops it after the damage; rocks and buildings
    /// take its damage and end the flight, bare terrain only ends it (D-055 addendum 1). Players and tamed animals are hit only by the game's
    /// friendly-fire (PvP) rule, else the ball flies through them (D-055 addendum 2). Every impact shows a spark burst; where the ball stops, a
    /// Cannon Ball item lies to be picked up again. Only the client that fired (the owner of the network object) flies the ball; the others see it
    /// through the synced transform.
    /// </summary>
    internal sealed class CannonBallFlight : MonoBehaviour
    {
        const float BallRadius = 0.05f, RemoveDelay = 0.4f, TreeLogReach = 4f;
        static readonly RaycastHit[] Hits = new RaycastHit[32];
        static int _mask;

        ZNetView _nview;
        bool _flying;
        Vector3 _pos, _vel, _origin, _flatDir;
        float _age, _target, _momentum = 1f, _speed;
        int _passed;
        Character _shooter;
        Transform _ignore;
        Vector3? _passedTreeAt;
        readonly HashSet<int> _passedRoots = new HashSet<int>();
        readonly List<string> _hits = new List<string>();

        /// <summary>What the last shot did (test hooks): "landed at 24.8 m (aimed 30.0 m), passed 1, stopped by Lox, hits: ...".</summary>
        internal static string LastReport { get; private set; } = "no shot yet";
        /// <summary>Where the last ball stopped (null while none has).</summary>
        internal static Vector3? LastStop { get; private set; }
        /// <summary>Flat distance of the last stop from where the ball left the muzzle.</summary>
        internal static float LastDistance { get; private set; }
        /// <summary>How many trees / creatures the last ball passed and how many it hit at all.</summary>
        internal static int LastPassed { get; private set; }
        internal static int LastHitCount { get; private set; }
        /// <summary>The damage (pierce + blunt, before resistances) each hit of the last ball dealt, in order.</summary>
        internal static readonly List<float> LastDamages = new List<float>();
        /// <summary>The aimed flat distance of the last ball and where (flat, from the muzzle) it passed its tree / creature (0 = no pass).</summary>
        internal static float LastAimed { get; private set; }
        internal static float LastPassAt { get; private set; }
        /// <summary>Test only: forgets the last shot (a test that is about to fire must not read the shot of the test before).</summary>
        internal static void ClearLast()
        {
            LastStop = null;
            LastReport = "not fired yet";
            LastDamages.Clear();
            LastPassed = 0;
            LastPassAt = 0f;
        }

        /// <summary>A ball is in the air now.</summary>
        internal static bool InFlight { get; private set; }

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        /// <summary>
        /// Starts the flight from the muzzle (the object's position) with the launch velocity. targetFlat is the flat distance of the aimed spot from
        /// the muzzle (for the landing rule). Expected outcome: true when the ball flies (false without a valid network object).
        /// </summary>
        internal bool Launch(Character shooter, Transform ignoreRoot, Vector3 velocity, float targetFlat)
        {
            if (_nview == null || !_nview.IsValid()) return false;
            _shooter = shooter;
            _ignore = ignoreRoot;
            _pos = _origin = transform.position;
            _vel = velocity;
            _speed = velocity.magnitude;
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            _flatDir = flat.sqrMagnitude > 0f ? flat.normalized : Vector3.forward;
            _target = targetFlat;
            _flying = true;
            InFlight = true;
            LastStop = null;
            LastDamages.Clear();
            LastAimed = targetFlat;
            LastPassAt = 0f;
            LastReport = "in flight";
            ModLog.Verbose("CannonBallFlight: launched at " + Fmt(_speed) + " m/s towards " + Fmt(targetFlat) + " m, from " + _origin.ToString("0.0"));
            return true;
        }

        void FixedUpdate()
        {
            try
            {
                if (!_flying || _nview == null || !_nview.IsValid() || !_nview.IsOwner()) return;
                float dt = Time.fixedDeltaTime;
                _age += dt;
                Vector3 from = _pos;
                ArcVector p = ToArc(_pos), v = ToArc(_vel);
                ThrowArcRules.Step(ref p, ref v, ModSettings.CannonGravity.Value, 0f, dt);
                _pos = ToVec(p);
                _vel = ToVec(v);
                ModLog.Every("cannon-ball-flight", 2d, "CannonBallFlight: ball at " + _pos.ToString("0.0") + ", " + Fmt(_vel.magnitude) + " m/s, "
                    + Fmt(Flat(_origin, _pos)) + " m out, passed " + _passed + ", age " + Fmt(_age) + " s");
                if (Segment(from, _pos)) return;
                transform.position = _pos;
                if (_age >= ModSettings.CannonMaxFlightSeconds.Value) Stop(_pos, "time ran out in the air");
            }
            catch (Exception e)
            {
                ModLog.Warn("CannonBallFlight.FixedUpdate: " + e.GetType().Name + ": " + e.Message + " - the ball drops where it is");
                Stop(_pos, "error");
            }
        }

        // tests the step's segment against the world, nearest first; true when the ball stopped or turned (the step is done)
        bool Segment(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len <= 0f) return false;
            int n = Physics.SphereCastNonAlloc(from, BallRadius, d / len, Hits, len, Mask(), QueryTriggerInteraction.Ignore);
            Array.Sort(Hits, 0, n, HitOrder.Instance);
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = Hits[i];
                if (Ignored(h.collider)) continue;
                Vector3 at = h.distance > 0f ? h.point : from;
                if (Impact(h.collider, at, h.normal.sqrMagnitude > 0f ? h.normal : -d / len)) return true;
            }
            return false;
        }

        bool Ignored(Collider c)
        {
            if (c == null) return true;
            Transform t = c.transform;
            if (_ignore != null && t.IsChildOf(_ignore)) return true;
            if (_shooter != null && t.IsChildOf(_shooter.transform)) return true;
            if (t.IsChildOf(transform)) return true;
            Character ch = c.GetComponentInParent<Character>();
            if (ch != null && !HitsCreature(ch)) return true;                    // a friend while PvP is off: the ball flies through (D-055 addendum 2)
            // a tree the ball breaks drops its logs right where it stood: they are still that tree, not a second target (run cannonA)
            if (_passedTreeAt.HasValue && c.GetComponentInParent<TreeLog>() != null && Flat(_passedTreeAt.Value, c.transform.position) < TreeLogReach) return true;
            return _passedRoots.Contains(RootId(c));
        }

        // the game's own friendly-fire rule for a player's projectile (Projectile.OnHit + Character.RPC_Damage, 1.0.21), decided by CannonRules
        bool HitsCreature(Character ch)
        {
            bool enemy = _shooter != null
                ? BaseAI.IsEnemy(_shooter, ch) || (ch.GetBaseAI() != null && ch.GetBaseAI().IsAggravatable() && _shooter.IsPlayer())
                : !(ch.IsPlayer() || ch.IsTamed());
            var check = new HitCheck(enemy, _shooter != null && _shooter.IsPVPEnabled(), ch.IsPlayer(), ch.IsPVPEnabled());
            bool hits = CannonRules.HitsCreature(check);
            ModLog.Decision("cannon ball target", hits ? "hit" : "flies through", Utils.GetPrefabName(ch.gameObject.name) + " (" + check
                + ", tamed " + ch.IsTamed() + ")");
            return hits;
        }

        // one impact; true when the step ends here (the ball stopped or flies on along a new arc)
        bool Impact(Collider c, Vector3 at, Vector3 normal)
        {
            Character creature = c.GetComponentInParent<Character>();
            IDestructible tree = (IDestructible)c.GetComponentInParent<TreeBase>() ?? c.GetComponentInParent<TreeLog>();
            bool passable = creature != null || tree != null;
            IDestructible solid = passable ? null : SolidOf(c);
            ImpactOutcome outcome = CannonRules.OnImpact(KindOf(passable, solid), _passed);
            Spark(at, normal);
            if (outcome == ImpactOutcome.Stop)
            {
                Stop(at + (normal * (BallRadius + 0.02f)), "stopped by " + Name(c));
                return true;
            }
            float dealt = Damage(creature, tree, solid, c, at);
            _hits.Add(Name(c) + " " + Fmt(dealt));
            LastDamages.Add(dealt);
            if (outcome == ImpactOutcome.StopWithDamage)
            {
                Vector3 rest = solid != null ? at + (normal * (BallRadius + 0.02f)) : at;
                Stop(rest, "stopped by " + Name(c) + (solid != null ? " (building / rock, damaged)" : " (second target)"));
                return true;
            }
            if (tree != null) _passedTreeAt = at;
            PassThrough(c, at);
            return true;
        }

        void PassThrough(Collider c, Vector3 at)
        {
            _passed++;
            _passedRoots.Add(RootId(c));
            _momentum = CannonRules.MomentumAfter(_passed, ModSettings.CannonPassFactor.Value);
            float hit = Flat(_origin, at), land = CannonRules.PassThroughLanding(_target, hit);
            LastPassAt = hit;
            Vector3 spot = _origin + (_flatDir * land);
            spot.y = GroundY(spot);
            float speed = Mathf.Max(0.5f, _speed * _momentum);
            LaunchSolution s = LaunchSolver.Solve(ToArc(at), ToArc(spot), speed, ModSettings.CannonGravity.Value, Time.fixedDeltaTime);
            _pos = at;
            _vel = ToVec(s.Direction) * speed;
            transform.position = _pos;
            ModLog.Verbose("CannonBallFlight: passed " + Name(c) + " at " + Fmt(hit) + " m, now " + Fmt(speed) + " m/s, lands at " + Fmt(land)
                + " m (aimed " + Fmt(_target) + " m), reachable " + s.Reachable);
        }

        float HitCreature(Character creature, Vector3 at)
        {
            HitData hit = MakeHit(at);
            creature.Damage(hit);
            string label = "cannon ball hit " + creature.m_name;
            float total = hit.m_damage.m_pierce + hit.m_damage.m_blunt;
            FlightRecorder.Track(creature, label, () => "hit by a cannon ball for " + Fmt(total) + " (pierce + blunt), pass " + _passed);
            return total;
        }

        float HitTree(IDestructible tree, Vector3 at)
        {
            HitData hit = MakeHit(at);
            hit.m_damage.m_chop = hit.m_damage.m_pierce + hit.m_damage.m_blunt;     // a tree only cares for chopping; the ball breaks it like an axe
            tree.Damage(hit);
            return hit.m_damage.m_pierce + hit.m_damage.m_blunt;
        }

        static ImpactKind KindOf(bool passable, IDestructible solid)
        {
            if (passable) return ImpactKind.Passable;
            return solid != null ? ImpactKind.Solid : ImpactKind.Ground;
        }

        // the damage of one impact on whatever was struck (exactly one of creature / tree / solid is set)
        float Damage(Character creature, IDestructible tree, IDestructible solid, Collider c, Vector3 at)
        {
            if (creature != null) return HitCreature(creature, at);
            return tree != null ? HitTree(tree, at) : HitSolid(solid, c, at);
        }

        // a building piece (WearNTear) or a rock (MineRock, MineRock5, Destructible) under the collider; null for bare ground
        static IDestructible SolidOf(Collider c)
        {
            IDestructible piece = c.GetComponentInParent<WearNTear>();
            return piece ?? (IDestructible)c.GetComponentInParent<MineRock5>() ?? (IDestructible)c.GetComponentInParent<MineRock>()
                ?? c.GetComponentInParent<Destructible>();
        }

        // D-055 addendum 1: a building or rock takes the ball's damage (the reduced share after a pass, via the momentum in MakeHit) and still
        // ends the flight. Rocks only care for pickaxe damage, so the ball breaks them like a pickaxe (as a tree like an axe); the hit collider
        // tells a MineRock(5) which part was struck.
        float HitSolid(IDestructible solid, Collider c, Vector3 at)
        {
            HitData hit = MakeHit(at);
            hit.m_hitCollider = c;
            float total = hit.m_damage.m_pierce + hit.m_damage.m_blunt + hit.m_damage.m_pickaxe;
            bool building = solid is WearNTear;
            if (!building && !_mobile) hit.m_damage.m_pickaxe = total;
            solid.Damage(hit);
            ModLog.Verbose("CannonBallFlight: " + (building ? "building piece " : "rock ") + Name(c) + " takes " + Fmt(total) + " (momentum "
                + Fmt(_momentum) + ")");
            return total;
        }

        bool _mobile;

        /// <summary>D-107: this ball was fired by a Mobile Cannon: its own, heavier numbers (blunt + pickaxe + pierce + burning, tool tier 5).</summary>
        internal void UseMobileCannonDamage() => _mobile = true;

        HitData MakeHit(Vector3 at)
        {
            var hit = new HitData();
            if (_mobile)
            {
                hit.m_damage.m_pierce = ModSettings.MobileCannonPierce.Value * _momentum;
                hit.m_damage.m_blunt = ModSettings.MobileCannonBlunt.Value * _momentum;
                hit.m_damage.m_pickaxe = ModSettings.MobileCannonPickaxe.Value * _momentum;
                hit.m_damage.m_fire = GunPowdered.Core.BurnRules.Total(ModSettings.MobileCannonFire.Value) * _momentum;
            }
            else
            {
                hit.m_damage.m_pierce = ModSettings.CannonPierce.Value * _momentum;
                hit.m_damage.m_blunt = ModSettings.CannonBlunt.Value * _momentum;
                hit.m_damage.m_fire = GunPowdered.Core.BurnRules.Total(ModSettings.CannonFire.Value) * _momentum;
            }
            hit.m_point = at;
            hit.m_dir = _vel.sqrMagnitude > 0f ? _vel.normalized : _flatDir;
            hit.m_pushForce = 30f * _momentum;
            hit.m_toolTier = (short)Mathf.RoundToInt(_mobile ? ModSettings.MobileCannonToolTier.Value : ModSettings.CannonToolTier.Value);
            if (_shooter != null) hit.SetAttacker(_shooter);
            return hit;
        }

        void Spark(Vector3 at, Vector3 normal)
        {
            CannonEffects.PlayImpact(at, normal);
        }

        // the flight ends: a Cannon Ball item lies there to be picked up and used again (D-053), the flying ball goes away a moment later
        void Stop(Vector3 at, string why)
        {
            if (!_flying) return;
            _flying = false;
            InFlight = false;
            CannonInventory.Drop(GunPowderedPrefabs.CannonBall, 1, at + (Vector3.up * 0.05f));
            foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            LastStop = at;
            LastDistance = Flat(_origin, at);
            LastPassed = _passed;
            LastHitCount = _hits.Count;
            LastReport = "landed at " + Fmt(LastDistance) + " m (aimed " + Fmt(_target) + " m), passed " + _passed + ", " + why + ", hits: "
                + (_hits.Count == 0 ? "none" : string.Join(", ", _hits.ToArray()));
            ModLog.Info("CannonBallFlight: " + LastReport);
            Invoke(nameof(Remove), RemoveDelay);
        }

        void Remove()
        {
            if (_nview != null && _nview.IsValid() && _nview.IsOwner()) ZNetScene.instance.Destroy(gameObject);
        }

        static float GroundY(Vector3 spot)
        {
            RaycastHit h;
            int ground = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
            if (Physics.Raycast(spot + (Vector3.up * 60f), Vector3.down, out h, 200f, ground, QueryTriggerInteraction.Ignore)) return h.point.y;
            return ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(spot) : spot.y;
        }

        internal static int Mask()
        {
            if (_mask == 0)
                _mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle", "character",
                    "character_net", "character_ghost", "character_noenv", "hitbox");
            return _mask;
        }

        static int RootId(Collider c)
        {
            Component owner = (Component)c.GetComponentInParent<Character>() ?? (Component)c.GetComponentInParent<TreeBase>()
                ?? (Component)c.GetComponentInParent<TreeLog>() ?? c.transform.root;
            return owner.gameObject.GetInstanceID();
        }

        static string Name(Collider c)
        {
            Character ch = c.GetComponentInParent<Character>();
            GameObject go = ch != null ? ch.gameObject : (c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.transform.root.gameObject);
            return Utils.GetPrefabName(go.name);
        }

        static float Flat(Vector3 a, Vector3 b) => new Vector2(b.x - a.x, b.z - a.z).magnitude;
        static ArcVector ToArc(Vector3 v) => new ArcVector(v.x, v.y, v.z);
        static Vector3 ToVec(ArcVector v) => new Vector3(v.X, v.Y, v.Z);
        static string Fmt(float f) => f.ToString("0.0", CultureInfo.InvariantCulture);

        sealed class HitOrder : IComparer<RaycastHit>
        {
            internal static readonly HitOrder Instance = new HitOrder();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
