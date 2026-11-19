using UnityEngine;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// Puts a character's hands on the Mobile Cannon's push handle (D-107) after the game's animation: each arm is bent (two-bone IK) so the palm lies on its
    /// grip, palm down, fingers over the bar towards the cannon.
    /// </summary>
    internal static class MobileCannonHands
    {
        /// <summary>Bends both arms onto the grips. Expected outcome: true when both hands were placed (false without a humanoid animator or grips).</summary>
        internal static bool Hold(Animator animator, Transform gripA, Transform gripB, Vector3 towardsCannon)
        {
            if (animator == null || !animator.isHuman || gripA == null || gripB == null) return false;
            Transform body = animator.transform;
            Vector3 right = body.right;
            // the grip on the character's right side goes to the right hand
            bool aIsRight = Vector3.Dot(gripA.position - body.position, right) > Vector3.Dot(gripB.position - body.position, right);
            bool r = Arm(animator, true, aIsRight ? gripA.position : gripB.position, towardsCannon, body);
            bool l = Arm(animator, false, aIsRight ? gripB.position : gripA.position, towardsCannon, body);
            return r && l;
        }

        static bool Arm(Animator a, bool right, Vector3 grip, Vector3 forward, Transform body)
        {
            Transform upper = a.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
            Transform lower = a.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
            Transform hand = a.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            Transform mid = a.GetBoneTransform(right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal);
            Transform thumb = a.GetBoneTransform(right ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal);
            if (upper == null || lower == null || hand == null || mid == null || thumb == null) return false;
            Vector3 fingers = (forward - body.up * 0.6f).normalized;          // over the bar and down
            Vector3 palm = -body.up;                                           // palm on the bar
            Vector3 elbowHint = upper.position - body.up * 0.5f + (right ? body.right : -body.right) * 0.3f;
            for (int it = 0; it < 3; it++)
            {
                Orient(hand, mid, thumb, right ? 1f : -1f, fingers, palm);
                Vector3 palmOffset = (mid.position - hand.position) * 0.5f;
                Vector3 wrist = grip - palmOffset + body.up * 0.03f;
                Quaternion keep = hand.rotation;
                Solve(upper, lower, hand, wrist, elbowHint);
                hand.rotation = keep;
            }
            return true;
        }

        /// <summary>Turns the hand so the fingers point along <paramref name="fingers"/> and the palm faces <paramref name="palm"/> (palm normal = cross(fingers, thumb) x side).</summary>
        static void Orient(Transform hand, Transform mid, Transform thumb, float side, Vector3 fingers, Vector3 palm)
        {
            Vector3 cf = (mid.position - hand.position).normalized;
            Vector3 cn = Vector3.ProjectOnPlane(Vector3.Cross(cf, (thumb.position - hand.position).normalized) * side, cf).normalized;
            Vector3 dn = Vector3.ProjectOnPlane(palm, fingers).normalized;
            if (cn.sqrMagnitude < 1e-6f || dn.sqrMagnitude < 1e-6f) return;
            hand.rotation = Quaternion.LookRotation(fingers, dn) * Quaternion.Inverse(Quaternion.LookRotation(cf, cn)) * hand.rotation;
        }

        /// <summary>Two-bone IK: the end bone on the target (or as close as the arm reaches), the elbow towards the hint.</summary>
        static void Solve(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 hint)
        {
            Vector3 a = upper.position;
            float ab = (lower.position - a).magnitude, bc = (end.position - lower.position).magnitude;
            if (ab < 1e-4f || bc < 1e-4f) return;
            Vector3 to = target - a;
            float at = Mathf.Clamp(to.magnitude, Mathf.Abs(ab - bc) + 1e-3f, ab + bc - 1e-3f);
            Vector3 dir = to.sqrMagnitude > 1e-8f ? to.normalized : upper.forward;
            Vector3 n = Vector3.Cross(dir, hint - a);
            if (n.sqrMagnitude < 1e-8f) n = Vector3.Cross(dir, Vector3.up);
            n.Normalize();
            float cosA = Mathf.Clamp((ab * ab + at * at - bc * bc) / (2f * ab * at), -1f, 1f);
            Vector3 elbow = a + Quaternion.AngleAxis(Mathf.Acos(cosA) * Mathf.Rad2Deg, n) * dir * ab;
            upper.rotation = Quaternion.FromToRotation(lower.position - a, elbow - a) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(end.position - lower.position, a + dir * at - lower.position) * lower.rotation;
        }
    }
}
