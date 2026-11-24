using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Domain;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// D-182: which part of a Mobile Cannon the local player looks at - the handle (the cart's own E: grab and push) or the rest of the cannon (E loads it).
    /// Uses the same camera ray as the game's hover search (Player.FindHoverObject: camera forward, 50 m), so it agrees with the hover text.
    /// </summary>
    internal static class MobileCannonLook
    {
        static readonly RaycastHit[] Hits = new RaycastHit[32];

        /// <summary>Distance (m) from the point the camera ray hits on <paramref name="cart"/> to its handle; NaN when the ray does not hit the cart.</summary>
        internal static float MetresFromHandle(Vagon cart)
        {
            if (cart == null || cart.m_attachPoint == null || GameCamera.instance == null) { if (RuleLog.Due("MobileCannonLook.MetresFromHandle:none", 2d)) ModLog.Every("MobileCannonLook.MetresFromHandle:none", 2d, "MobileCannonLook.MetresFromHandle: returns NaN because cart == null || cart.m_attachPoint == null || GameCamera.instance == null (line 19)"); return float.NaN; }
            Transform cam = GameCamera.instance.transform;
            int n = Physics.RaycastNonAlloc(cam.position, cam.forward, Hits, 50f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue, from = float.NaN;
            for (int i = 0; i < n; i++)
            {
                if (Hits[i].collider == null || Hits[i].collider.GetComponentInParent<Vagon>() != cart || Hits[i].distance >= best) { if (RuleLog.Due("MobileCannonLook.MetresFromHandle:skip", 2d)) ModLog.Every("MobileCannonLook.MetresFromHandle:skip", 2d, "MobileCannonLook.MetresFromHandle: skips to the next item (continue) because the hit is not on this cart or is farther (line 26)"); continue; }
                best = Hits[i].distance;
                from = Vector3.Distance(Hits[i].point, cart.m_attachPoint.position);
            }
            if (RuleLog.Due("MobileCannonLook.MetresFromHandle", 2d)) ModLog.Every("MobileCannonLook.MetresFromHandle", 2d, $"MobileCannonLook.MetresFromHandle: {n} hit(s), looked-at point {RuleLog.Shown(from)} m from the handle (line 30)");
            return from;
        }

        /// <summary>Whether the local player looks at the handle of <paramref name="cart"/>.</summary>
        internal static bool AtHandle(Vagon cart) => CartUseRules.OnHandle(MetresFromHandle(cart));
    }
}
