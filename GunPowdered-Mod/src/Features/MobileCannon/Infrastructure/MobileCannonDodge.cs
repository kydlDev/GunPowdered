using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.MobileCannon.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// User 2026-11-24: a towed Mobile Cannon slides past trees and rocks the Asksvin walked by, instead of catching on them (a19full mobilecannon-tow-trees:
    /// a wheel caught, the joint stretched and flung the cart over). Every physics step while towed, the static colliders near the cart each give a sideways
    /// push (<see cref="AsksvinTowRules.SidePush"/>); creatures, the Asksvin, items and the cart itself have a Rigidbody and are skipped.
    /// </summary>
    internal static class MobileCannonDodge
    {
        static readonly Collider[] Near = new Collider[32];
        static int _mask;
        static float _lastPush;

        /// <summary>Pushes <paramref name="body"/> sideways away from the obstacles near it. Returns the push (m/s^2, + = right; 0 when nothing is near).</summary>
        internal static float Apply(Rigidbody body, Transform cart)
        {
            if (_mask == 0) _mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece");
            Vector3 fwd = Vector3.ProjectOnPlane(cart.forward, Vector3.up).normalized, right = Vector3.Cross(Vector3.up, fwd);
            Vector3 middle = body.worldCenterOfMass;
            int n = Physics.OverlapSphereNonAlloc(middle + fwd * 1.5f, AsksvinTowRules.DodgeAhead, Near, _mask, QueryTriggerInteraction.Ignore);
            if (n == Near.Length && ModLog.IsDue("MobileCannonDodge.full", 5d)) ModLog.Warn("MobileCannonDodge: " + n + " colliders near the cart fill the buffer - some obstacles are not seen");
            float push = 0f;
            Collider strongest = null;
            float strongestPush = 0f, strongestSide = 0f, strongestAhead = 0f;
            for (int i = 0; i < n; i++)
            {
                Collider c = Near[i];
                if (c == null || c.attachedRigidbody != null || c.transform.IsChildOf(cart)) { if (RuleLog.Due("MobileCannonDodge.Apply:skip", 2d)) ModLog.Every("MobileCannonDodge.Apply:skip", 2d, "MobileCannonDodge.Apply: skips to the next item (continue) because c == null || c.attachedRigidbody != null || c.transform.IsChildOf(cart)"); continue; }
                // ClosestPoint fails on non-convex meshes. Their bounds' closest point was no answer either: a tree's mesh ("Lod0") bounds hold the whole
                // crown, so the cart was always "inside" and the push lost its side (a19play4 tow-trees: side 0.00, moved 3.6 m). The trunk stands at the
                // bounds' centre, so a non-convex mesh counts at its centre, at the cart's height.
                Vector3 at = c is MeshCollider mc && !mc.convex ? new Vector3(c.bounds.center.x, middle.y, c.bounds.center.z) : c.ClosestPoint(middle);
                Vector3 d = at - middle;
                float side = Vector3.Dot(d, right), ahead = Vector3.Dot(d, fwd);
                if (Mathf.Abs(side) < 0.001f && _lastPush != 0f) side = _lastPush < 0f ? 0.001f : -0.001f;      // a19cannon-rep01: the middle point sat inside a tree's box, the push flipped sides - keep going the way it was pushing
                float one = AsksvinTowRules.SidePush(side, ahead);
                push += one;
                if (Mathf.Abs(one) > Mathf.Abs(strongestPush)) { strongest = c; strongestPush = one; strongestSide = side; strongestAhead = ahead; }
            }
            _lastPush = push;
            if (push == 0f) { if (RuleLog.Due("MobileCannonDodge.Apply:clear", 2d)) ModLog.Every("MobileCannonDodge.Apply:clear", 2d, $"MobileCannonDodge.Apply: returns 0 because no obstacle is inside the field ({n} collider(s) near)"); return 0f; }
            push = Mathf.Clamp(push, -AsksvinTowRules.DodgeStrength, AsksvinTowRules.DodgeStrength);      // several colliders (one tree, two LODs) added up to 54 m/s2
            body.AddForce(right * push, ForceMode.Acceleration);
            if (ModLog.IsDue("MobileCannonDodge.push", 0.5d))
                ModLog.Decision("mobile cannon dodge", push > 0f ? "right" : "left", $"push {push:0.0} m/s2, strongest {strongest.name} side {strongestSide:0.00} m ahead {strongestAhead:0.00} m, colliders {n}");
            return push;
        }
    }
}