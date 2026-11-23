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
    /// <summary>CannonBallFlight, part 2 (G04 class size): arming, damage, hit data, sparks, stopping and the small helpers.</summary>
    internal sealed partial class CannonBallFlight
    {
        /// <summary>
        /// What fired this ball (D-145): the cannon's part of the damage (the Mobile Cannon's heavier one, D-107, or the Hand Cannon's) and the ball
        /// whose part adds to it; the same ball lies where the flight stops. Call before <see cref="Launch"/>.
        /// </summary>
        internal void Arm(bool mobileCannon, string ball)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"CannonBallFlight.Arm(mobileCannon={RuleLog.Shown(mobileCannon)}, ball={RuleLog.Shown(ball)})");
            _mobile = mobileCannon;
            _ball = CannonAmmoRules.LoadedOrOld(ball);
            LastBall = _ball;
            LastShotDamage = Damage().ToString();
            ModLog.Verbose("CannonBallFlight: armed by the " + (_mobile ? "Mobile Cannon" : "Hand Cannon") + " with " + _ball + ": " + LastShotDamage);
        }

        // D-145: the shot = the cannon's part + the ball's part
        ShotDamage Damage()
        {
            if (RuleLog.Due("CannonBallFlight.Damage", 2d)) ModLog.Every("CannonBallFlight.Damage", 2d, $"CannonBallFlight.Damage()");
            var cannon = _mobile
                ? new ShotDamage(0f, ModSettings.MobileCannonBlunt.Value * _blunt, ModSettings.MobileCannonPickaxe.Value, ModSettings.MobileCannonFire.Value, 0)
                : new ShotDamage(0f, ModSettings.CannonBlunt.Value * _blunt, ModSettings.CannonPickaxe.Value, ModSettings.CannonFire.Value, 0);
            { var returnedAt40 = ShotDamage.Sum(cannon, BallDamage(_ball)); if (RuleLog.Due("CannonBallFlight.Damage:40", 2d)) ModLog.Every("CannonBallFlight.Damage:40", 2d, $"CannonBallFlight.Damage: returns {returnedAt40} (line 40)"); return returnedAt40; }
        }

        /// <summary>A ball's part of the shot (D-145), from its Tunables.</summary>
        internal static ShotDamage BallDamage(string ball)
        {
            if (RuleLog.Due("CannonBallFlight.BallDamage", 2d)) ModLog.Every("CannonBallFlight.BallDamage", 2d, $"CannonBallFlight.BallDamage(ball={RuleLog.Shown(ball)})");
            if (ball == GunPowderedPrefabs.FlametalCannonBall) return new ShotDamage(ModSettings.FlametalBallPierce.Value, 0f, 0f, 0f, Mathf.RoundToInt(ModSettings.FlametalBallToolTier.Value));
            { var returnedAt48 = new ShotDamage(ModSettings.IronBallPierce.Value, 0f, 0f, 0f, Mathf.RoundToInt(ModSettings.IronBallToolTier.Value)); if (RuleLog.Due("CannonBallFlight.BallDamage:48", 2d)) ModLog.Every("CannonBallFlight.BallDamage:48", 2d, $"CannonBallFlight.BallDamage: returns {returnedAt48} (line 48)"); return returnedAt48; }
        }

        HitData MakeHit(Vector3 at)
        {
            if (RuleLog.Due("CannonBallFlight.MakeHit", 2d)) ModLog.Every("CannonBallFlight.MakeHit", 2d, $"CannonBallFlight.MakeHit(at={RuleLog.Shown(at)})");
            var hit = new HitData();
            ShotDamage d = Damage().Times(_momentum);
            hit.m_damage.m_pierce = d.Pierce;
            hit.m_damage.m_blunt = d.Blunt;
            hit.m_damage.m_pickaxe = d.Pickaxe;
            hit.m_damage.m_fire = GunPowdered.Core.BurnRules.Total(d.FirePerSecond);
            hit.m_point = at;
            hit.m_dir = _vel.sqrMagnitude > 0f ? _vel.normalized : _flatDir;
            hit.m_pushForce = 30f * _momentum;
            hit.m_toolTier = (short)d.ToolTier;
            if (_shooter != null) hit.SetAttacker(_shooter);
            { var returnedAt65 = hit; if (RuleLog.Due("CannonBallFlight.MakeHit:65", 2d)) ModLog.Every("CannonBallFlight.MakeHit:65", 2d, $"CannonBallFlight.MakeHit: returns {returnedAt65} (line 65)"); return returnedAt65; }
        }

        void Spark(Vector3 at, Vector3 normal)
        {
            if (RuleLog.Due("CannonBallFlight.Spark", 2d)) ModLog.Every("CannonBallFlight.Spark", 2d, $"CannonBallFlight.Spark(at={RuleLog.Shown(at)}, normal={RuleLog.Shown(normal)})");
            CannonEffects.PlayImpact(at, normal);
        }

        // the flight ends: a Cannon Ball item lies there to be picked up and used again (D-053), the flying ball goes away a moment later
        void Stop(Vector3 at, string why)
        {
            if (!_flying) { if (RuleLog.Due("CannonBallFlight.Stop:77", 2d)) ModLog.Every("CannonBallFlight.Stop:77", 2d, $"CannonBallFlight.Stop: returns because !_flying (line 77)"); return; }
            _flying = false;
            InFlight = false;
            CannonInventory.Drop(_ball, 1, at + (Vector3.up * 0.05f));
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
            if (RuleLog.Due("CannonBallFlight.Remove", 2d)) ModLog.Every("CannonBallFlight.Remove", 2d, $"CannonBallFlight.Remove()");
            if (_nview != null && _nview.IsValid() && _nview.IsOwner()) ZNetScene.instance.Destroy(gameObject);
        }

        static float GroundY(Vector3 spot)
        {
            if (RuleLog.Due("CannonBallFlight.GroundY", 2d)) ModLog.Every("CannonBallFlight.GroundY", 2d, $"CannonBallFlight.GroundY(spot={RuleLog.Shown(spot)})");
            RaycastHit h;
            int ground = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
            if (Physics.Raycast(spot + (Vector3.up * 60f), Vector3.down, out h, 200f, ground, QueryTriggerInteraction.Ignore)) { var returnedAt103 = h.point.y; if (RuleLog.Due("CannonBallFlight.GroundY:103", 2d)) ModLog.Every("CannonBallFlight.GroundY:103", 2d, $"CannonBallFlight.GroundY: returns {returnedAt103} because Physics.Raycast(spot + (Vector3.up * 60f), Vector3.down, out h, 200f, ground, QueryTriggerInteraction.Ignore) (line 103)"); return returnedAt103; }
            { var returnedAt104 = ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(spot) : spot.y; if (RuleLog.Due("CannonBallFlight.GroundY:104", 2d)) ModLog.Every("CannonBallFlight.GroundY:104", 2d, $"CannonBallFlight.GroundY: returns {returnedAt104} (line 104)"); return returnedAt104; }
        }

        internal static int Mask()
        {
            if (RuleLog.Due("CannonBallFlight.Mask", 2d)) ModLog.Every("CannonBallFlight.Mask", 2d, $"CannonBallFlight.Mask()");
            if (_mask == 0)
                _mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle", "character",
                    "character_net", "character_ghost", "character_noenv", "hitbox");
            { var returnedAt113 = _mask; if (RuleLog.Due("CannonBallFlight.Mask:113", 2d)) ModLog.Every("CannonBallFlight.Mask:113", 2d, $"CannonBallFlight.Mask: returns {returnedAt113} (line 113)"); return returnedAt113; }
        }

        static int RootId(Collider c)
        {
            if (RuleLog.Due("CannonBallFlight.RootId", 2d)) ModLog.Every("CannonBallFlight.RootId", 2d, $"CannonBallFlight.RootId(c={RuleLog.Shown(c)})");
            Component owner = (Component)c.GetComponentInParent<Character>() ?? (Component)c.GetComponentInParent<TreeBase>()
                ?? (Component)c.GetComponentInParent<TreeLog>() ?? c.transform.root;
            { var returnedAt121 = owner.gameObject.GetInstanceID(); if (RuleLog.Due("CannonBallFlight.RootId:121", 2d)) ModLog.Every("CannonBallFlight.RootId:121", 2d, $"CannonBallFlight.RootId: returns {returnedAt121} (line 121)"); return returnedAt121; }
        }

        static string Name(Collider c)
        {
            if (RuleLog.Due("CannonBallFlight.Name", 2d)) ModLog.Every("CannonBallFlight.Name", 2d, $"CannonBallFlight.Name(c={RuleLog.Shown(c)})");
            Character ch = c.GetComponentInParent<Character>();
            GameObject go = ch != null ? ch.gameObject : (c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.transform.root.gameObject);
            { var returnedAt129 = Utils.GetPrefabName(go.name); if (RuleLog.Due("CannonBallFlight.Name:129", 2d)) ModLog.Every("CannonBallFlight.Name:129", 2d, $"CannonBallFlight.Name: returns {returnedAt129} (line 129)"); return returnedAt129; }
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
