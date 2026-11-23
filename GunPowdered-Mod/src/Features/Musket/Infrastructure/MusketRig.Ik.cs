using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>MusketRig, part 3 (G04 class size): the arm IK, hand orientation, model placement and spine steadying.</summary>
    internal sealed partial class MusketRig
    {
        void SideArm(float weight)
        {
            if (weight <= 0.001f) { if (RuleLog.Due("MusketRig.SideArm:off", 2d)) ModLog.Every("MusketRig.SideArm:off", 2d, "MusketRig.SideArm: musket shouldered - no side IK"); return; }
            if (_rUpper == null) { _rUpper = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm); _rLower = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm); _rHand = _animator.GetBoneTransform(HumanBodyBones.RightHand); _rMid = _animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal); _rThumb = _animator.GetBoneTransform(HumanBodyBones.RightThumbProximal); }
            if (_rUpper == null || _rLower == null || _rHand == null || _rMid == null || _rThumb == null) { if (RuleLog.Due("MusketRig.SideArm:bones", 2d)) ModLog.Every("MusketRig.SideArm:bones", 2d, "MusketRig.SideArm: arm bones missing - no side IK"); return; }
            Transform body = _character.transform;
            Vector3 f = body.forward, u = body.up, r = body.right;
            Vector3 palmNow = (_rMid.position + _rHand.position) * 0.5f;
            Vector3 palmSide = _rUpper.position + r * MusketPoses.SidePalm[0] + u * MusketPoses.SidePalm[1] + f * MusketPoses.SidePalm[2];
            Vector3 palm = Vector3.Lerp(palmNow, palmSide, weight);
            Vector3 hint = _rUpper.position - u * 0.30f - f * 0.15f + r * 0.20f;
            Quaternion handNow = _rHand.rotation;
            for (int it = 0; it < 3; it++)
            {
                OrientHand(-u + f * 0.15f, -r);                            // trail arms: fingers down (curled under the stock), palm towards the thigh, wrist straight
                Quaternion side = _rHand.rotation;
                _rHand.rotation = Quaternion.Slerp(handNow, side, weight);
                Vector3 wrist = palm - (_rMid.position - _rHand.position) * 0.5f;
                Quaternion keep = _rHand.rotation;
                TwoBoneIk(wrist, hint);
                _rHand.rotation = keep;
            }
            if (RuleLog.Due("MusketRig.SideArm", 2d)) ModLog.Every("MusketRig.SideArm", 2d, "MusketRig.SideArm: weight " + weight.ToString("0.00") + ", palm miss " + ((_rMid.position + _rHand.position) * 0.5f - palm).magnitude.ToString("0.000") + " m");
        }

        // turns the right hand so its fingers point along fingers and its palm faces palmNormal (the authoring script's Orient)
        void OrientHand(Vector3 fingers, Vector3 palmNormal)
        {
            Vector3 cf = (_rMid.position - _rHand.position).normalized;
            Vector3 ct = (_rThumb.position - _rHand.position).normalized;
            Vector3 cn = Vector3.ProjectOnPlane(Vector3.Cross(cf, ct), cf).normalized;
            Vector3 dn = Vector3.ProjectOnPlane(palmNormal, fingers).normalized;
            if (cf.sqrMagnitude < 1e-6f || cn.sqrMagnitude < 1e-6f || dn.sqrMagnitude < 1e-6f) { if (RuleLog.Due("MusketRig.OrientHand:none", 2d)) ModLog.Every("MusketRig.OrientHand:none", 2d, "MusketRig.OrientHand: degenerate hand - left as it is"); return; }
            _rHand.rotation = Quaternion.LookRotation(fingers.normalized, dn) * Quaternion.Inverse(Quaternion.LookRotation(cf, cn)) * _rHand.rotation;
            if (RuleLog.Due("MusketRig.OrientHand", 2d)) ModLog.Every("MusketRig.OrientHand", 2d, "MusketRig.OrientHand: turned");
        }

        // bends upper arm and forearm so the wrist reaches target, the elbow towards hint (the authoring script's IK)
        void TwoBoneIk(Vector3 target, Vector3 hint)
        {
            Vector3 a = _rUpper.position;
            float ab = (_rLower.position - a).magnitude, bc = (_rHand.position - _rLower.position).magnitude;
            Vector3 tt = target - a;
            float at = Mathf.Clamp(tt.magnitude, Mathf.Abs(ab - bc) + 0.001f, ab + bc - 0.001f);
            Vector3 dir = tt.normalized;
            Vector3 n = Vector3.Cross(dir, hint - a).normalized;
            if (n.sqrMagnitude < 1e-6f) { if (RuleLog.Due("MusketRig.TwoBoneIk:none", 2d)) ModLog.Every("MusketRig.TwoBoneIk:none", 2d, "MusketRig.TwoBoneIk: hint on the line - no bend"); return; }
            float cosA = Mathf.Clamp((ab * ab + at * at - bc * bc) / (2f * ab * at), -1f, 1f);
            Vector3 elbow = a + Quaternion.AngleAxis(Mathf.Acos(cosA) * Mathf.Rad2Deg, n) * dir * ab;
            _rUpper.rotation = Quaternion.FromToRotation(_rLower.position - a, elbow - a) * _rUpper.rotation;
            _rLower.rotation = Quaternion.FromToRotation(_rHand.position - _rLower.position, a + dir * at - _rLower.position) * _rLower.rotation;
            if (RuleLog.Due("MusketRig.TwoBoneIk", 2d)) ModLog.Every("MusketRig.TwoBoneIk", 2d, "MusketRig.TwoBoneIk: reach " + at.ToString("0.00") + " of " + (ab + bc).ToString("0.00") + " m");
        }

        /// <summary>Puts the musket in the right hand: the aiming grip, or (w = 1) the reload grip on the barrel with the ramrod in the bore, its top in the left fist.</summary>
        void PlaceModel(float w, float seconds)
        {
            if (RuleLog.Due("MusketRig.PlaceModel", 2d)) ModLog.Every("MusketRig.PlaceModel", 2d, $"MusketRig.PlaceModel(w={RuleLog.Shown(w)}, seconds={RuleLog.Shown(seconds)})");
            float[] a = MusketPoses.GripHold, s = MusketPoses.GripSide, b = MusketPoses.GripReload;
            // shouldered -> at the side (D-155) by the carry, then -> the reload grip by the reload weight
            Vector3 p = Vector3.Lerp(new Vector3(a[0], a[1], a[2]), new Vector3(s[0], s[1], s[2]), _carry);
            Quaternion q = Quaternion.Slerp(Quaternion.Euler(a[3], a[4], a[5]), Quaternion.Euler(s[3], s[4], s[5]), _carry);
            _model.localPosition = Vector3.Lerp(p, new Vector3(b[0], b[1], b[2]), w);
            _model.localRotation = Quaternion.Slerp(q, Quaternion.Euler(b[3], b[4], b[5]), w);
            if (_rod == null) { if (RuleLog.Due("MusketRig.PlaceModel:75", 2d)) ModLog.Every("MusketRig.PlaceModel:75", 2d, $"MusketRig.PlaceModel: returns because _rod == null (line 75)"); return; }
            var rammed = new Vector3(_rodHome.x, _rodHome.y - MusketPoses.RodPipeY, _rodHome.z + MusketPoses.MuzzleZ + MusketPoseRules.RodAboveMuzzle(seconds, _stroke) - MusketPoses.RodTopZ);
            _rod.localPosition = w > 0.5f ? rammed : _rodHome;
        }

        // steadiness 0 = the animation's spine as it is, 1 = only its slow average (the breathing filtered out)
        void SteadySpine(float steadiness)
        {
            if (RuleLog.Due("MusketRig.SteadySpine", 2d)) ModLog.Every("MusketRig.SteadySpine", 2d, $"MusketRig.SteadySpine(steadiness={RuleLog.Shown(steadiness)})");
            float now = Time.time, dt = _spineSet ? now - _lastPosed : 0f;
            _lastPosed = now;
            float follow = MusketPoseRules.FollowFactor(dt, SpineSeconds);
            for (int i = 0; i < SpineMuscles; i++)
            {
                float anim = _pose.muscles[i];
                _spine[i] = _spineSet ? _spine[i] + (anim - _spine[i]) * follow : anim;
                _pose.muscles[i] = anim + (_spine[i] - anim) * steadiness;
            }
            _spineSet = true;
        }

    }
}
