using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Features.MobileCannon.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>Mobile cannon tow, second half (G04 split).</summary>
    internal partial class MobileCannonTow
    {
        /// <summary>Largest roll / pitch (deg, from the ground's tilt) and angular speed (rad/s) seen while towed here, and how often the hard limit held it.</summary>
        internal float MaxRollSeen { get; private set; }
        internal float MaxPitchSeen { get; private set; }
        internal float MaxSpinSeen { get; private set; }
        internal int Holds { get; private set; }
        internal int Flips { get; private set; }
        float _lastHoldLog = -99f, _lastAnimalYaw, _animalTurnRate;
        Rigidbody _body;
        float _spinBefore;

        internal void ResetTiltStats() { MaxRollSeen = 0f; MaxPitchSeen = 0f; MaxSpinSeen = 0f; Holds = 0; Flips = 0; }

        void FixedUpdate()
        {
            if (_joint == null || _animal == null || !IsValid || !_nview.IsOwner()) { if (RuleLog.Due("MobileCannonTow.FixedUpdate:251", 2d)) ModLog.Every("MobileCannonTow.FixedUpdate:251", 2d, $"MobileCannonTow.FixedUpdate: returns because _joint == null || _animal == null || !IsValid || !_nview.IsOwner() (line 251)"); return; }
            if (_body == null) _body = GetComponent<Rigidbody>();
            if (_body == null) { if (RuleLog.Due("MobileCannonTow.FixedUpdate:253", 2d)) ModLog.Every("MobileCannonTow.FixedUpdate:253", 2d, $"MobileCannonTow.FixedUpdate: returns because _body == null (line 253)"); return; }
            Vector3 ground = Vector3.up;
            RaycastHit hit;
            if (Physics.Raycast(_body.worldCenterOfMass + Vector3.up * 0.5f, Vector3.down, out hit, 3f, LayerMask.GetMask("terrain"), QueryTriggerInteraction.Ignore))
                ground = hit.normal;
            Vector3 fwd = transform.forward, right = transform.right, up = transform.up;
            float roll = Vector3.SignedAngle(Vector3.ProjectOnPlane(ground, fwd), Vector3.ProjectOnPlane(up, fwd), fwd);
            float pitch = Vector3.SignedAngle(Vector3.ProjectOnPlane(ground, right), Vector3.ProjectOnPlane(up, right), right);
            float animalYaw = _animal.transform.eulerAngles.y;
            _animalTurnRate = Mathf.DeltaAngle(_lastAnimalYaw, animalYaw) / Time.fixedDeltaTime;     // the animal turns by transform, its body reports no angular velocity
            _lastAnimalYaw = animalYaw;
            Vector3 w = _body.angularVelocity;
            float rollRate = Vector3.Dot(w, fwd), pitchRate = Vector3.Dot(w, right);
            MaxRollSeen = Mathf.Max(MaxRollSeen, Mathf.Abs(roll));
            MaxPitchSeen = Mathf.Max(MaxPitchSeen, Mathf.Abs(pitch));
            MaxSpinSeen = Mathf.Max(MaxSpinSeen, w.magnitude);
            if (up.y < 0.3f) Flips++;
            _body.AddTorque(fwd * AsksvinTowRules.UprightAccel(roll, rollRate), ForceMode.Acceleration);
            if (Mathf.Abs(pitch) > AsksvinTowRules.MaxPitch * 0.6f) _body.AddTorque(right * AsksvinTowRules.UprightAccel(pitch - Mathf.Sign(pitch) * AsksvinTowRules.MaxPitch * 0.6f, pitchRate), ForceMode.Acceleration);
            // the hard limit is against WORLD up as well: relative to the ground alone a 39 degree side slope allowed 25 + 39 = 64 degrees and the cannon went
            // over on the template's mountain cliff (a9cliff4: roll 61, pitch 75, 12 flipped frames). The tighter of the two wins.
            float rollW = Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up, fwd), Vector3.ProjectOnPlane(up, fwd), fwd);
            float pitchW = Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up, right), Vector3.ProjectOnPlane(up, right), right);
            float rollOver = AsksvinTowRules.Tighter(AsksvinTowRules.Excess(roll, AsksvinTowRules.MaxRoll), AsksvinTowRules.Excess(rollW, AsksvinTowRules.MaxWorldRoll));
            float pitchOver = AsksvinTowRules.Tighter(AsksvinTowRules.Excess(pitch, AsksvinTowRules.MaxPitch), AsksvinTowRules.Excess(pitchW, AsksvinTowRules.MaxWorldPitch));
            if (rollOver == 0f && pitchOver == 0f) { if (RuleLog.Due("MobileCannonTow.FixedUpdate:278", 2d)) ModLog.Every("MobileCannonTow.FixedUpdate:278", 2d, $"MobileCannonTow.FixedUpdate: returns because rollOver == 0f && pitchOver == 0f (line 278)"); return; }
            Quaternion back = Quaternion.AngleAxis(-rollOver, fwd) * Quaternion.AngleAxis(-pitchOver, right);
            _body.rotation = back * _body.rotation;          // at once: MoveRotation waits a step, and a tree or the joint pushes it past again meanwhile (a9miss1: 56 deg)
            if (rollOver != 0f && rollRate * rollOver > 0f) w -= fwd * rollRate;
            if (pitchOver != 0f && pitchRate * pitchOver > 0f) w -= right * pitchRate;
            if (rollOver != 0f) w -= fwd * Vector3.Dot(w, fwd);          // past the limit: no more roll spin at all (the 0.6 damping still flipped it for a frame, a9g16g)
            _body.angularVelocity = w * 0.6f;          // a held yank loses its spin too, or the next step tips it again
            Holds++;
            if (Time.time - _lastHoldLog < 0.5f) { if (RuleLog.Due("MobileCannonTow.FixedUpdate:286", 2d)) ModLog.Every("MobileCannonTow.FixedUpdate:286", 2d, $"MobileCannonTow.FixedUpdate: returns because Time.time - _lastHoldLog < 0.5f (line 286)"); return; }
            _lastHoldLog = Time.time;
            Rigidbody ab = _animal.GetComponent<Rigidbody>();
            ModLog.Info("MobileCannonTow: " + name + " tilt held - roll " + roll.ToString("0") + " deg (world " + rollW.ToString("0") + ") (rate " + (rollRate * Mathf.Rad2Deg).ToString("0") + " deg/s), pitch " + pitch.ToString("0") + " (world " + pitchW.ToString("0") + ")"
                + " deg, spin " + w.magnitude.ToString("0.0") + " rad/s, cannon " + Vector3.ProjectOnPlane(_body.linearVelocity, Vector3.up).magnitude.ToString("0.0") + " m/s, asksvin "
                + (ab != null ? Vector3.ProjectOnPlane(ab.linearVelocity, Vector3.up).magnitude.ToString("0.0") + " m/s turning " + _animalTurnRate.ToString("0") + " deg/s" : "?")
                + ", yaw to the animal " + Vector3.SignedAngle(Vector3.ProjectOnPlane(fwd, Vector3.up), Vector3.ProjectOnPlane(_animal.transform.forward, Vector3.up), Vector3.up).ToString("0")
                + " deg, joint force " + _joint.currentForce.magnitude.ToString("0") + " N, holds " + Holds);
        }

        long _lastCannonOwner = -1L, _lastAnimalOwner = -1L;

        /// <summary>Diagnostics for the two-client hand-over: logs every change of the cannon's or the towing animal's owner as this client sees it.</summary>
        void LogOwners()
        {
            long cannon = _nview.GetZDO().GetOwner();
            long animal = _animal != null && _animal.View != null && _animal.View.IsValid() ? _animal.View.GetZDO().GetOwner() : 0L;
            if (cannon == _lastCannonOwner && animal == _lastAnimalOwner) { if (RuleLog.Due("MobileCannonTow.LogOwners:303", 2d)) ModLog.Every("MobileCannonTow.LogOwners:303", 2d, $"MobileCannonTow.LogOwners: returns because cannon == _lastCannonOwner && animal == _lastAnimalOwner (line 303)"); return; }
            ModLog.Info("MobileCannonTow: " + name + " owners - cannon " + _lastCannonOwner + " -> " + cannon + ", animal " + _lastAnimalOwner + " -> " + animal
                + " (me " + ZDOMan.GetSessionID() + ", joint " + (_joint != null) + ")");
            _lastCannonOwner = cannon;
            _lastAnimalOwner = animal;
        }

        void OwnerTick()
        {
            if (_animal == null)
            {
                // the animal is not loaded here (yet, e.g. right after a world load): give it some seconds before giving up
                if (_lostSince < 0f) _lostSince = Time.time;
                if (Time.time - _lostSince > 10f) Unhitch("no Asksvin with this harness loaded for 10 s");
                { if (RuleLog.Due("MobileCannonTow.OwnerTick:317", 2d)) ModLog.Every("MobileCannonTow.OwnerTick:317", 2d, $"MobileCannonTow.OwnerTick: returns (line 317)"); return; }
            }
            _lostSince = -1f;
            if (!AsksvinTowRules.KeepsTowing(_animal.View != null && _animal.View.IsValid(), _animal.Animal == null || _animal.Animal.IsDead(), _animal.Harnessed))
            {
                Unhitch("animal dead or without harness");
                { if (RuleLog.Due("MobileCannonTow.OwnerTick:323", 2d)) ModLog.Every("MobileCannonTow.OwnerTick:323", 2d, $"MobileCannonTow.OwnerTick: returns (line 323)"); return; }
            }
            long animalOwner = _animal.View.GetZDO().GetOwner();
            if (AsksvinTowRules.OwnerMove(ZDOMan.GetSessionID(), ZDOMan.GetSessionID(), animalOwner) == "push")
            {
                // the client that moves the animal simulates the cannon too
                DropJoint();
                _nview.GetZDO().SetOwner(animalOwner);
                ModLog.Info("MobileCannonTow: " + name + " handed over to the animal's owner " + animalOwner);
                return;
            }
            if (_joint == null) MakeJoint();
            _hookDrop = MobileCannonTowSlope.FollowGround(_joint, _animal.transform, _hookDrop);      // D-143
            if (_ring != null && ModLog.IsDue("tow-align-" + GetInstanceID(), 2d))
                ModLog.Every("tow-align-" + GetInstanceID(), 2d, "MobileCannonTow: " + name + " " + _animal.AlignReport(_ring.position));      // D-143: cart tip vs harness end
            Rigidbody b = GetComponent<Rigidbody>();
            if (_reverse == null) _reverse = _animal.GetComponent<AsksvinReverse>();
            MobileCannonTowSlope.KeepStraightBacking(b, transform, _ring, _animal.transform, _reverse);      // D-144
            if (b != null) MaxSpeedSeen = Mathf.Max(MaxSpeedSeen, Vector3.ProjectOnPlane(b.linearVelocity, Vector3.up).magnitude);
            Rigidbody ab = _animal.GetComponent<Rigidbody>();
            if (ab != null) AnimalMaxSpeed = Mathf.Max(AnimalMaxSpeed, Vector3.ProjectOnPlane(ab.linearVelocity, Vector3.up).magnitude);
        }

        AsksvinReverse _reverse;
        float _hookDrop;

        /// <summary>Where the joint's hook is now, in the animal's space (D-143: lowered by the ground under it, MobileCannonTowSlope).</summary>
        internal Vector3 HookNow => MobileCannonTowSlope.HookAt(_hookDrop);

        void MakeJoint()
        {
            Rigidbody animalBody = _animal.GetComponent<Rigidbody>();
            if (animalBody == null || _ring == null) { if (RuleLog.Due("MobileCannonTow.MakeJoint:355", 2d)) ModLog.Every("MobileCannonTow.MakeJoint:355", 2d, $"MobileCannonTow.MakeJoint: returns because animalBody == null || _ring == null (line 355)"); return; }
            _joint = gameObject.AddComponent<ConfigurableJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = transform.InverseTransformPoint(_ring.position);
            _hookDrop = 0f;
            _joint.connectedAnchor = HookNow;
            _joint.breakForce = _vagon != null ? _vagon.m_breakForce * 4f : 40000f;
            // x and y stay Limited with the cart's soft spring (Locked was tried in a9fix1: the snap's 0.15 m correction kicked the cannon over at the hitch).
            // The few cm of sag are hidden: the harness model's hook ring and chain end ride the cannon's tow ring (AsksvinHarness skin, third bone).
            _joint.xMotion = ConfigurableJointMotion.Limited;
            _joint.yMotion = ConfigurableJointMotion.Limited;
            _joint.zMotion = ConfigurableJointMotion.Locked;
            _joint.linearLimit = new SoftJointLimit { limit = 0.001f };
            // 4x the cart's spring: with the cart's own the ring stretched up to 1 m off the hook in 90 degree turns at full speed (a9fix2 log)
            _joint.linearLimitSpring = new SoftJointLimitSpring { spring = (_vagon != null ? _vagon.m_spring : 5000f) * 4f, damper = (_vagon != null ? _vagon.m_springDamping : 1000f) * 2f };
            _joint.connectedBody = animalBody;
            Rigidbody own = GetComponent<Rigidbody>();
            if (own != null) { _spinBefore = own.maxAngularVelocity; own.maxAngularVelocity = AsksvinTowRules.MaxTowSpin; }      // a yank cannot spin it past the keeper in one physics step (a9g16g: roll 78 on the cliff)
            _massOn = _animal.Animal;
            if (_massOn != null) _massOn.SetExtraMass(ModSettings.MobileCannonTowExtraMass.Value);     // like the cart's m_playerExtraPullMass: the cannon must not hold the Asksvin back (D-105)
            ModLog.Info("MobileCannonTow: " + name + " joint to " + _animal.name + ", ring " + _joint.anchor.ToString("0.00") + ", hook " + _joint.connectedAnchor.ToString("0.00") + ", " + ClevisReport(_animal.transform));
        }

        void DropJoint()
        {
            if (RuleLog.Due("MobileCannonTow.DropJoint", 2d)) ModLog.Every("MobileCannonTow.DropJoint", 2d, $"MobileCannonTow.DropJoint()");
            if (_joint != null) Destroy(_joint);
            _joint = null;
            Rigidbody own = GetComponent<Rigidbody>();
            if (own != null && _spinBefore > 0f) own.maxAngularVelocity = _spinBefore;
            if (_massOn != null) _massOn.SetExtraMass(0f);
            _massOn = null;
        }

        void OnDestroy() => DropJoint();

        void OnJointBreak(float force)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTow.OnJointBreak(force={RuleLog.Shown(force)})");
            if (_joint == null || !IsValid || !_nview.IsOwner()) return;
            _joint = null;
            if (_massOn != null) _massOn.SetExtraMass(0f);
            _massOn = null;
            Unhitch("the tow joint broke at " + force.ToString("0") + " N");
        }

        float Speed()
        {
            if (RuleLog.Due("MobileCannonTow.Speed", 2d)) ModLog.Every("MobileCannonTow.Speed", 2d, $"MobileCannonTow.Speed()");
            Rigidbody b = GetComponent<Rigidbody>();
            { var returnedAt405 = b != null ? b.linearVelocity.magnitude : 0f; if (RuleLog.Due("MobileCannonTow.Speed:405", 2d)) ModLog.Every("MobileCannonTow.Speed:405", 2d, $"MobileCannonTow.Speed: returns {returnedAt405} (line 405)"); return returnedAt405; }
        }
    }
}
