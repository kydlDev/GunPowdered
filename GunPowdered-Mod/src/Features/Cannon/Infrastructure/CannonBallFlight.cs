using GunPowdered.Features.Skilling.Domain;
using GunPowdered.Features.Skilling.Infrastructure;
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
    internal sealed partial class CannonBallFlight : MonoBehaviour
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
            if (RuleLog.IsOn()) ModLog.Verbose($"CannonBallFlight.ClearLast()");
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
            if (RuleLog.IsOn()) ModLog.Verbose($"CannonBallFlight.Awake()");
            _nview = GetComponent<ZNetView>();
        }

        /// <summary>
        /// Starts the flight from the muzzle (the object's position) with the launch velocity. targetFlat is the flat distance of the aimed spot from
        /// the muzzle (for the landing rule). Expected outcome: true when the ball flies (false without a valid network object).
        /// </summary>
        internal bool Launch(Character shooter, Transform ignoreRoot, Vector3 velocity, float targetFlat)
        {
            if (_nview == null || !_nview.IsValid()) { if (RuleLog.Due("CannonBallFlight.Launch:80", 2d)) ModLog.Every("CannonBallFlight.Launch:80", 2d, "CannonBallFlight.Launch: returns false (line 80)"); return false; }
            _shooter = shooter;
            // D-157: the shooter's Cannons skill makes the cannon's blunt part harder (x1.0 at level 0 .. x1.5 at level 100); this runs on the shooter's client
            _blunt = SkillRules.CannonBluntFactor(GunSkills.Level(shooter, GunSkills.Cannons));
            GunSkills.Raise(shooter, GunSkills.Cannons, SkillRules.GainCannonShot);
            LastShotDamage = Damage().ToString();
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
                if (!_flying || _nview == null || !_nview.IsValid() || !_nview.IsOwner()) { if (RuleLog.Due("CannonBallFlight.FixedUpdate:104", 2d)) ModLog.Every("CannonBallFlight.FixedUpdate:104", 2d, "CannonBallFlight.FixedUpdate: returns (line 104)"); return; }
                float dt = Time.fixedDeltaTime;
                _age += dt;
                Vector3 from = _pos;
                ArcVector p = ToArc(_pos), v = ToArc(_vel);
                ThrowArcRules.Step(ref p, ref v, ModSettings.CannonGravity.Value, 0f, dt);
                _pos = ToVec(p);
                _vel = ToVec(v);
                if (RuleLog.Due("cannon-ball-flight", 2d)) ModLog.Every("cannon-ball-flight", 2d, "CannonBallFlight: ball at " + _pos.ToString("0.0") + ", " + Fmt(_vel.magnitude) + " m/s, "
                    + Fmt(Flat(_origin, _pos)) + " m out, passed " + _passed + ", age " + Fmt(_age) + " s");
                if (Segment(from, _pos)) { if (RuleLog.Due("CannonBallFlight.FixedUpdate:114", 2d)) ModLog.Every("CannonBallFlight.FixedUpdate:114", 2d, "CannonBallFlight.FixedUpdate: returns (line 114)"); return; }
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
            if (RuleLog.Due("CannonBallFlight.Segment", 2d)) ModLog.Every("CannonBallFlight.Segment", 2d, $"CannonBallFlight.Segment(from={RuleLog.Shown(from)}, to={RuleLog.Shown(to)})");
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len <= 0f) { if (RuleLog.Due("CannonBallFlight.Segment:131", 2d)) ModLog.Every("CannonBallFlight.Segment:131", 2d, "CannonBallFlight.Segment: returns false (line 131)"); return false; }
            int n = Physics.SphereCastNonAlloc(from, BallRadius, d / len, Hits, len, Mask(), QueryTriggerInteraction.Ignore);
            Array.Sort(Hits, 0, n, HitOrder.Instance);
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = Hits[i];
                if (Ignored(h.collider)) continue;
                Vector3 at = h.distance > 0f ? h.point : from;
                if (Impact(h.collider, at, h.normal.sqrMagnitude > 0f ? h.normal : -d / len)) { if (RuleLog.Due("CannonBallFlight.Segment:139", 2d)) ModLog.Every("CannonBallFlight.Segment:139", 2d, "CannonBallFlight.Segment: returns true (line 139)"); return true; }
            }
            { if (RuleLog.Due("CannonBallFlight.Segment:141", 2d)) ModLog.Every("CannonBallFlight.Segment:141", 2d, "CannonBallFlight.Segment: returns false (line 141)"); return false; }
        }

        bool Ignored(Collider c)
        {
            if (RuleLog.Due("CannonBallFlight.Ignored", 2d)) ModLog.Every("CannonBallFlight.Ignored", 2d, $"CannonBallFlight.Ignored(c={RuleLog.Shown(c)})");
            if (c == null) { if (RuleLog.Due("CannonBallFlight.Ignored:147", 2d)) ModLog.Every("CannonBallFlight.Ignored:147", 2d, "CannonBallFlight.Ignored: returns true (line 147)"); return true; }
            Transform t = c.transform;
            if (_ignore != null && t.IsChildOf(_ignore)) { if (RuleLog.Due("CannonBallFlight.Ignored:149", 2d)) ModLog.Every("CannonBallFlight.Ignored:149", 2d, "CannonBallFlight.Ignored: returns true (line 149)"); return true; }
            if (_shooter != null && t.IsChildOf(_shooter.transform)) { if (RuleLog.Due("CannonBallFlight.Ignored:150", 2d)) ModLog.Every("CannonBallFlight.Ignored:150", 2d, "CannonBallFlight.Ignored: returns true (line 150)"); return true; }
            if (t.IsChildOf(transform)) { if (RuleLog.Due("CannonBallFlight.Ignored:151", 2d)) ModLog.Every("CannonBallFlight.Ignored:151", 2d, "CannonBallFlight.Ignored: returns true (line 151)"); return true; }
            Character ch = c.GetComponentInParent<Character>();
            if (ch != null && !HitsCreature(ch)) { if (RuleLog.Due("CannonBallFlight.Ignored:153", 2d)) ModLog.Every("CannonBallFlight.Ignored:153", 2d, "CannonBallFlight.Ignored: returns true (line 153)"); return true; }                    // a friend while PvP is off: the ball flies through (D-055 addendum 2)
            // a tree the ball breaks drops its logs right where it stood: they are still that tree, not a second target (run cannonA)
            if (_passedTreeAt.HasValue && c.GetComponentInParent<TreeLog>() != null && Flat(_passedTreeAt.Value, c.transform.position) < TreeLogReach) { if (RuleLog.Due("CannonBallFlight.Ignored:155", 2d)) ModLog.Every("CannonBallFlight.Ignored:155", 2d, "CannonBallFlight.Ignored: returns true (line 155)"); return true; }
            { var returnedAt156 = _passedRoots.Contains(RootId(c)); if (RuleLog.Due("CannonBallFlight.Ignored:156", 2d)) ModLog.Every("CannonBallFlight.Ignored:156", 2d, $"CannonBallFlight.Ignored: returns {RuleLog.Shown(returnedAt156)} (line 156)"); return returnedAt156; }
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
            { var returnedAt169 = hits; if (RuleLog.Due("CannonBallFlight.HitsCreature:169", 2d)) ModLog.Every("CannonBallFlight.HitsCreature:169", 2d, $"CannonBallFlight.HitsCreature: returns {RuleLog.Shown(returnedAt169)} (line 169)"); return returnedAt169; }
        }

        // one impact; true when the step ends here (the ball stopped or flies on along a new arc)
        bool Impact(Collider c, Vector3 at, Vector3 normal)
        {
            if (RuleLog.Due("CannonBallFlight.Impact", 2d)) ModLog.Every("CannonBallFlight.Impact", 2d, $"CannonBallFlight.Impact(c={RuleLog.Shown(c)}, at={RuleLog.Shown(at)}, normal={RuleLog.Shown(normal)})");
            Character creature = c.GetComponentInParent<Character>();
            IDestructible tree = (IDestructible)c.GetComponentInParent<TreeBase>() ?? c.GetComponentInParent<TreeLog>();
            bool passable = creature != null || tree != null;
            IDestructible solid = passable ? null : SolidOf(c);
            ImpactOutcome outcome = CannonRules.OnImpact(KindOf(passable, solid), _passed);
            Spark(at, normal);
            if (outcome == ImpactOutcome.Stop)
            {
                Stop(at + (normal * (BallRadius + 0.02f)), "stopped by " + Name(c));
                { if (RuleLog.Due("CannonBallFlight.Impact:185", 2d)) ModLog.Every("CannonBallFlight.Impact:185", 2d, "CannonBallFlight.Impact: returns true (line 185)"); return true; }
            }
            float dealt = Damage(creature, tree, solid, c, at);
            _hits.Add(Name(c) + " " + Fmt(dealt));
            LastDamages.Add(dealt);
            if (outcome == ImpactOutcome.StopWithDamage)
            {
                Vector3 rest = solid != null ? at + (normal * (BallRadius + 0.02f)) : at;
                Stop(rest, "stopped by " + Name(c) + (solid != null ? " (building / rock, damaged)" : " (second target)"));
                { if (RuleLog.Due("CannonBallFlight.Impact:194", 2d)) ModLog.Every("CannonBallFlight.Impact:194", 2d, "CannonBallFlight.Impact: returns true (line 194)"); return true; }
            }
            if (tree != null) _passedTreeAt = at;
            PassThrough(c, at);
            { if (RuleLog.Due("CannonBallFlight.Impact:198", 2d)) ModLog.Every("CannonBallFlight.Impact:198", 2d, "CannonBallFlight.Impact: returns true (line 198)"); return true; }
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
            if (RuleLog.Due("CannonBallFlight.HitCreature", 2d)) ModLog.Every("CannonBallFlight.HitCreature", 2d, $"CannonBallFlight.HitCreature(creature={RuleLog.Shown(creature)}, at={RuleLog.Shown(at)})");
            HitData hit = MakeHit(at);
            creature.Damage(hit);
            string label = "cannon ball hit " + creature.m_name;
            float total = hit.m_damage.m_pierce + hit.m_damage.m_blunt;
            FlightRecorder.Track(creature, label, () => "hit by a cannon ball for " + Fmt(total) + " (pierce + blunt), pass " + _passed);
            { var returnedAt227 = total; if (RuleLog.Due("CannonBallFlight.HitCreature:227", 2d)) ModLog.Every("CannonBallFlight.HitCreature:227", 2d, $"CannonBallFlight.HitCreature: returns {RuleLog.Shown(returnedAt227)} (line 227)"); return returnedAt227; }
        }

        float HitTree(IDestructible tree, Vector3 at)
        {
            if (RuleLog.Due("CannonBallFlight.HitTree", 2d)) ModLog.Every("CannonBallFlight.HitTree", 2d, $"CannonBallFlight.HitTree(tree={RuleLog.Shown(tree)}, at={RuleLog.Shown(at)})");
            HitData hit = MakeHit(at);
            hit.m_damage.m_chop = hit.m_damage.m_pierce + hit.m_damage.m_blunt;     // a tree only cares for chopping; the ball breaks it like an axe
            tree.Damage(hit);
            { var returnedAt236 = hit.m_damage.m_pierce + hit.m_damage.m_blunt; if (RuleLog.Due("CannonBallFlight.HitTree:236", 2d)) ModLog.Every("CannonBallFlight.HitTree:236", 2d, $"CannonBallFlight.HitTree: returns {RuleLog.Shown(returnedAt236)} (line 236)"); return returnedAt236; }
        }

        static ImpactKind KindOf(bool passable, IDestructible solid)
        {
            if (RuleLog.Due("CannonBallFlight.KindOf", 2d)) ModLog.Every("CannonBallFlight.KindOf", 2d, $"CannonBallFlight.KindOf(passable={RuleLog.Shown(passable)}, solid={RuleLog.Shown(solid)})");
            if (passable) { var returnedAt242 = ImpactKind.Passable; if (RuleLog.Due("CannonBallFlight.KindOf:242", 2d)) ModLog.Every("CannonBallFlight.KindOf:242", 2d, $"CannonBallFlight.KindOf: returns {RuleLog.Shown(returnedAt242)} (line 242)"); return returnedAt242; }
            { var returnedAt243 = solid != null ? ImpactKind.Solid : ImpactKind.Ground; if (RuleLog.Due("CannonBallFlight.KindOf:243", 2d)) ModLog.Every("CannonBallFlight.KindOf:243", 2d, $"CannonBallFlight.KindOf: returns {RuleLog.Shown(returnedAt243)} (line 243)"); return returnedAt243; }
        }

        // the damage of one impact on whatever was struck (exactly one of creature / tree / solid is set)
        float Damage(Character creature, IDestructible tree, IDestructible solid, Collider c, Vector3 at)
        {
            if (RuleLog.Due("CannonBallFlight.Damage", 2d)) ModLog.Every("CannonBallFlight.Damage", 2d, $"CannonBallFlight.Damage(creature={RuleLog.Shown(creature)}, tree={RuleLog.Shown(tree)}, solid={RuleLog.Shown(solid)}, c={RuleLog.Shown(c)}, at={RuleLog.Shown(at)})");
            if (creature != null) { var returnedAt250 = HitCreature(creature, at); if (RuleLog.Due("CannonBallFlight.Damage:250", 2d)) ModLog.Every("CannonBallFlight.Damage:250", 2d, $"CannonBallFlight.Damage: returns {RuleLog.Shown(returnedAt250)} (line 250)"); return returnedAt250; }
            { var returnedAt251 = tree != null ? HitTree(tree, at) : HitSolid(solid, c, at); if (RuleLog.Due("CannonBallFlight.Damage:251", 2d)) ModLog.Every("CannonBallFlight.Damage:251", 2d, $"CannonBallFlight.Damage: returns {RuleLog.Shown(returnedAt251)} (line 251)"); return returnedAt251; }
        }

        // a building piece (WearNTear) or a rock (MineRock, MineRock5, Destructible) under the collider; null for bare ground
        static IDestructible SolidOf(Collider c)
        {
            if (RuleLog.Due("CannonBallFlight.SolidOf", 2d)) ModLog.Every("CannonBallFlight.SolidOf", 2d, $"CannonBallFlight.SolidOf(c={RuleLog.Shown(c)})");
            IDestructible piece = c.GetComponentInParent<WearNTear>();
            return piece ?? (IDestructible)c.GetComponentInParent<MineRock5>() ?? (IDestructible)c.GetComponentInParent<MineRock>()
                ?? c.GetComponentInParent<Destructible>();
        }

        // D-055 addendum 1: a building or rock takes the ball's damage (the reduced share after a pass, via the momentum in MakeHit) and still
        // ends the flight. Rocks only care for pickaxe damage: both balls carry their own pickaxe number (D-108: hand 150, mobile 250); the hit
        // collider tells a MineRock(5) which part was struck.
        float HitSolid(IDestructible solid, Collider c, Vector3 at)
        {
            HitData hit = MakeHit(at);
            hit.m_hitCollider = c;
            float total = hit.m_damage.m_pierce + hit.m_damage.m_blunt + hit.m_damage.m_pickaxe;
            bool building = solid is WearNTear;
            solid.Damage(hit);
            ModLog.Verbose("CannonBallFlight: " + (building ? "building piece " : "rock ") + Name(c) + " takes " + Fmt(total) + " (momentum "
                + Fmt(_momentum) + ")");
            { var returnedAt275 = total; if (RuleLog.Due("CannonBallFlight.HitSolid:275", 2d)) ModLog.Every("CannonBallFlight.HitSolid:275", 2d, $"CannonBallFlight.HitSolid: returns {RuleLog.Shown(returnedAt275)} (line 275)"); return returnedAt275; }
        }

        bool _mobile;
        string _ball = GunPowderedPrefabs.CannonBall;

        /// <summary>The ball prefab the last shot flew (test hooks).</summary>
        internal static string LastBall { get; private set; } = "";
        /// <summary>The last shot's damage before the momentum (cannon part + ball part, D-145; test hooks).</summary>
        internal static string LastShotDamage { get; private set; } = "";

        // D-157: the factor on the cannon's blunt part from the shooter's Cannons skill (1 until launched)
        float _blunt = 1f;

    }
}
