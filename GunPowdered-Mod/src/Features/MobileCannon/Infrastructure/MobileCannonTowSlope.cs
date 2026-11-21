using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// The towed cannon on slopes and while the Asksvin backs up (owner side, every physics step of MobileCannonTow), and its clevis diagnostics; split out of MobileCannonTow for the class size (G04).
    /// D-143: the joint's hook follows the ground under it, so on a slope the cannon's trail tip stays down with the harness traces.
    /// D-144: while the Asksvin backs up, the cannon is pushed at its trail and would jack-knife; it is turned to stay straight behind the animal, so it goes
    /// back along the path it came (the user's "same path backwards").
    /// </summary>
    internal static class MobileCannonTowSlope
    {
        /// <summary>Where the joint's hook is, in the animal's space, for a hook drop (m). Expected outcome: <see cref="AsksvinHarness.HookLocal"/> lowered by it.</summary>
        internal static Vector3 HookAt(float hookDrop) => AsksvinHarness.HookLocal + Vector3.down * hookDrop;

        /// <summary>Moves the joint's hook with the ground under it. Expected outcome: the new hook drop (m), set on the joint; unchanged without a joint.</summary>
        internal static float FollowGround(ConfigurableJoint joint, Transform animal, float hookDrop)
        {
            if (joint == null || animal == null || ZoneSystem.instance == null) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTowSlope.FollowGround:none", 2d, "MobileCannonTowSlope.FollowGround: no joint, animal or zone system - returns " + hookDrop); return hookDrop; }
            float underAnimal = ZoneSystem.instance.GetGroundHeight(animal.position);
            float underHook = ZoneSystem.instance.GetGroundHeight(animal.TransformPoint(AsksvinHarness.HookLocal));
            float drop = AsksvinTowRules.HookDrop(underAnimal, underHook, hookDrop, AsksvinTowRules.HookDropSpeed * Time.fixedDeltaTime);
            joint.connectedAnchor = HookAt(drop);
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTowSlope.FollowGround:end", 2d, $"MobileCannonTowSlope.FollowGround: ground under animal {underAnimal:0.00}, under hook {underHook:0.00}, hook drop {drop:0.00} m - returns");
            return drop;
        }

        /// <summary>Turns a backing Asksvin's cannon straight behind it. Expected outcome: the cannon's yaw spin set while the animal backs, nothing otherwise.</summary>
        internal static void KeepStraightBacking(Rigidbody b, Transform cannon, Transform ring, Transform animal, AsksvinReverse reverse)
        {
            if (b == null || cannon == null || ring == null || animal == null || reverse == null || !reverse.IsBacking) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTowSlope.KeepStraightBacking:none", 2d, "MobileCannonTowSlope.KeepStraightBacking: not backing - returns"); return; }
            Vector3 line = Vector3.ProjectOnPlane(ring.position - cannon.position, Vector3.up);
            Vector3 fwd = Vector3.ProjectOnPlane(animal.forward, Vector3.up);
            float angle = Vector3.SignedAngle(line, fwd, Vector3.up);
            Vector3 spin = b.angularVelocity;
            spin.y = AsksvinReverseRules.StraightSpin(angle, AsksvinTowRules.MaxTowSpin);
            b.angularVelocity = spin;
            ModLog.Every("tow-back-" + cannon.GetInstanceID(), 2d, $"MobileCannonTowSlope: {cannon.name} backing, {angle:0} deg off straight, spin {spin.y:0.00} rad/s");
        }

        /// <summary>
        /// Diagnostics for the clevis (T-009i): the tow ring (between the two eye plates, x = 0) and the harness hook ring, both in the cannon's own space (x
        /// sideways, y up, z along the cannon), and the gap between them. The harness ring is 0.030 wide and the plates' inner faces are at x +-0.0155, so
        /// |dx| above about 0.01 means the ring is not between the plates. Expected outcome: one line.
        /// </summary>
        internal static string ClevisReport(Transform cannon, Transform towRing, Transform animal, Vector3 hookLocal)
        {
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTowSlope.ClevisReport", 2d, $"MobileCannonTowSlope.ClevisReport(animal={RuleLog.Shown(animal)})");
            if (cannon == null || towRing == null || animal == null) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTowSlope.ClevisReport:none", 2d, "MobileCannonTowSlope.ClevisReport: returns \"clevis: no ring or no animal\""); return "clevis: no ring or no animal"; }
            Vector3 ring = cannon.InverseTransformPoint(towRing.position);
            Vector3 hook = cannon.InverseTransformPoint(animal.TransformPoint(hookLocal));
            Vector3 gap = hook - ring;
            string s = "clevis ring " + ring.ToString("0.000") + ", harness hook " + hook.ToString("0.000") + ", gap " + gap.ToString("0.000") + " (" + gap.magnitude.ToString("0.000") + " m), plates "
                + (Mathf.Abs(gap.x) < 0.01f ? "hold the hook ring" : "MISS the hook ring");
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTowSlope.ClevisReport:end", 2d, "MobileCannonTowSlope.ClevisReport: returns " + s);
            return s;
        }
    }
}