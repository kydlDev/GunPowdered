using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Features.MobileCannon.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// Towing one Mobile Cannon behind a harnessed Asksvin (D-063, D-116). The towing animal is a ZDO value of the cannon (gp_mc_tow), so every client
    /// knows it. The joint (like the cart's own, from the cannon's tow ring to the hook behind the tail) lives only on the client that owns the Asksvin:
    /// the cannon's owner hands the cannon over to that client, so animal and cannon are simulated together, ridden or following. Unhitched when the
    /// animal dies, disappears or loses its harness, when the joint breaks, or with E on the tow ring.
    /// </summary>
    internal sealed class MobileCannonTow : MonoBehaviour
    {
        internal const string KeyTow = "gp_mc_towkey";        // the towing Asksvin's harness key (AsksvinHarness.TowKey), not a ZDOID: ZDOIDs change on world load
        const string RpcHitch = "GP_MC_Hitch", RpcUnhitch = "GP_MC_Unhitch", RingName = "gp_towring";

        ZNetView _nview;
        Vagon _vagon;
        Transform _ring;
        ConfigurableJoint _joint;
        AsksvinHarness _animal;
        Character _massOn;
        float _lostSince = -1f, _nextSearch;

        internal bool IsValid => _nview != null && _nview.IsValid();

        internal bool Towed => IsValid && _nview.GetZDO().GetLong(KeyTow, 0L) != 0L;

        internal bool Jointed => _joint != null;

        internal Transform Ring => _ring;

        internal AsksvinHarness Animal => _animal;

        /// <summary>Fastest the cannon moved while hitched here (m/s, horizontal), for the lab.</summary>
        internal float MaxSpeedSeen { get; private set; }

        /// <summary>Fastest the towing Asksvin moved meanwhile (m/s, horizontal), for the lab: the cannon should keep up with it.</summary>
        internal float AnimalMaxSpeed { get; private set; }

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _vagon = GetComponent<Vagon>();
            Transform point = _vagon != null && _vagon.m_attachPoint != null ? FindPoint() : null;
            if (point != null)
            {
                var go = new GameObject(RingName);
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                go.transform.position = point.position;
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(0.3f, 0.3f, 0.3f);
                go.AddComponent<MobileCannonTowRing>().Tow = this;
                _ring = go.transform;
            }
            if (IsValid)
            {
                _nview.Register<ZDOID>(RpcHitch, RPC_Hitch);          // the animal's current ZDOID; the owner stores its harness key
                _nview.Register(RpcUnhitch, RPC_Unhitch);
            }
            ModLog.Verbose("MobileCannonTow: " + name + " tow ring " + (_ring != null) + ", towed " + Towed);
        }

        Transform FindPoint()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTow.FindPoint()");
            foreach (Transform t in GetComponentsInChildren<Transform>(true)) if (t.name == MobileCannonSetup.TrailName) { var returnedAt71 = t; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTow.FindPoint: returns {RuleLog.Shown(returnedAt71)} (line 71)"); return returnedAt71; }
            { var returnedAt72 = _vagon.m_attachPoint; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTow.FindPoint: returns {RuleLog.Shown(returnedAt72)} (line 72)"); return returnedAt72; }
        }

        /// <summary>E on the tow ring: unhitches a towed cannon, else hitches it to the nearest harnessed Asksvin. Expected outcome: true when a request was sent.</summary>
        internal bool Use(Player p)
        {
            if (p == null || !IsValid) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Use:78", 2d, "MobileCannonTow.Use: returns false (line 78)"); return false; }
            if (Towed)
            {
                _nview.InvokeRPC(RpcUnhitch);
                p.Message(MessageHud.MessageType.Center, "Unhitched the Mobile Cannon");
                { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Use:83", 2d, "MobileCannonTow.Use: returns true (line 83)"); return true; }
            }
            AsksvinHarness near = Nearest();
            string why = AsksvinTowRules.WhyNotHitch(false, _vagon != null && _vagon.IsAttached(), near != null, Range());
            if (why != null)
            {
                p.Message(MessageHud.MessageType.Center, why);
                ModLog.Decision("mobile cannon hitch", "refused", why + "; " + Candidates());
                return false;
            }
            _nview.InvokeRPC(RpcHitch, near.View.GetZDO().m_uid);
            p.Message(MessageHud.MessageType.Center, "Hitched the Mobile Cannon to " + near.Animal.GetHoverName());
            { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Use:95", 2d, "MobileCannonTow.Use: returns true (line 95)"); return true; }
        }

        AsksvinHarness Nearest()
        {
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Nearest", 2d, $"MobileCannonTow.Nearest()");
            AsksvinHarness best = null;
            float bestDist = -1f;
            foreach (Character c in Character.GetAllCharacters())
            {
                AsksvinHarness h = c != null ? c.GetComponent<AsksvinHarness>() : null;
                if (h == null || !h.Harnessed || !c.IsTamed() || c.IsDead()) continue;
                // the nearer of the hook behind the tail and the body: the player need not line the tail up, the cannon snaps behind it
                float d = Mathf.Min(Vector3.Distance(_ring.position, c.transform.TransformPoint(AsksvinHarness.HookLocal)), Vector3.Distance(_ring.position, c.transform.position));
                if (AsksvinTowRules.Nearer(d, bestDist, Range())) { best = h; bestDist = d; }
            }
            { var returnedAt111 = best; if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Nearest:111", 2d, $"MobileCannonTow.Nearest: returns {RuleLog.Shown(returnedAt111)} (line 111)"); return returnedAt111; }
        }

        /// <summary>Diagnostics for a refused hitch: every Asksvin loaded here with its harness / tame / dead state and its hook and body distance from the tow ring.</summary>
        string Candidates()
        {
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Candidates", 2d, $"MobileCannonTow.Candidates()");
            var sb = new System.Text.StringBuilder("ring at " + (_ring != null ? _ring.position.ToString("0.0") : "?") + ", cart held " + (_vagon != null && _vagon.IsAttached()));
            foreach (Character c in Character.GetAllCharacters())
            {
                AsksvinHarness h = c != null ? c.GetComponent<AsksvinHarness>() : null;
                if (h == null || _ring == null) continue;
                sb.Append("; " + c.name + " harnessed " + h.Harnessed + " tamed " + c.IsTamed() + " dead " + c.IsDead() + " hook " + Vector3.Distance(_ring.position, c.transform.TransformPoint(AsksvinHarness.HookLocal)).ToString("0.0")
                    + " m body " + Vector3.Distance(_ring.position, c.transform.position).ToString("0.0") + " m up " + c.transform.up.y.ToString("0.00"));
            }
            { var returnedAt126 = sb.ToString(); if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Candidates:126", 2d, $"MobileCannonTow.Candidates: returns {RuleLog.Shown(returnedAt126)} (line 126)"); return returnedAt126; }
        }

        static float Range() => ModSettings.MobileCannonHitchRange.Value;

        void RPC_Hitch(long sender, ZDOID animal)
        {
            if (!_nview.IsOwner() || Towed) { if (RuleLog.IsOn()) ModLog.Verbose("MobileCannonTow.RPC_Hitch: returns (line 133)"); return; }
            GameObject go = ZNetScene.instance.FindInstance(animal);
            AsksvinHarness h = go != null ? go.GetComponent<AsksvinHarness>() : null;
            if (h == null || !h.Harnessed || h.TowKey == 0L || (_vagon != null && _vagon.IsAttached())) { if (RuleLog.IsOn()) ModLog.Verbose("MobileCannonTow.RPC_Hitch: returns (line 136)"); return; }
            _nview.GetZDO().Set(KeyTow, h.TowKey);
            _animal = h;
            PlaceBehind(h);
            ModLog.Info("MobileCannonTow: " + name + " hitched to " + go.name + " " + animal + ", tow key " + h.TowKey);
        }

        void RPC_Unhitch(long sender)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTow.RPC_Unhitch(sender={RuleLog.Shown(sender)})");
            if (!_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Verbose("MobileCannonTow.RPC_Unhitch: returns (line 146)"); return; }
            Unhitch("E on the tow ring");
        }

        /// <summary>
        /// The towing animal died or lost its harness (AsksvinHarness.Drop): every cannon towed by this key lets go now. On death the animal's object is gone
        /// a frame later, and a cannon would otherwise wait the 10 s "not loaded" grace with a joint to nothing (lab a9miss1). The owner unhitches; any other
        /// client drops its joint and asks the owner.
        /// </summary>
        internal static void AnimalGone(long key, string why)
        {
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.AnimalGone", 2d, $"MobileCannonTow.AnimalGone(key={RuleLog.Shown(key)}, why={RuleLog.Shown(why)})");
            if (key == 0L) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.AnimalGone:158", 2d, "MobileCannonTow.AnimalGone: returns (line 158)"); return; }
            foreach (MobileCannonTow t in FindObjectsByType<MobileCannonTow>(FindObjectsSortMode.None))
            {
                if (t == null || !t.IsValid || t._nview.GetZDO().GetLong(KeyTow, 0L) != key) continue;
                if (t._nview.IsOwner()) t.Unhitch(why);
                else { t.DropJoint(); t._nview.InvokeRPC(RpcUnhitch); }
            }
        }

        void Unhitch(string why)
        {
            DropJoint();
            if (IsValid && _nview.IsOwner()) _nview.GetZDO().Set(KeyTow, 0L);
            _animal = null;
            ModLog.Info("MobileCannonTow: " + name + " unhitched (" + why + ")");
        }

        /// <summary>Snaps the cannon so its tow ring sits on the hook behind the tail, the trail towards the animal.</summary>
        void PlaceBehind(AsksvinHarness h)
        {
            Transform a = h.transform;
            Vector3 forward = Vector3.ProjectOnPlane(a.forward, Vector3.up).normalized;
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
            Vector3 ringLocal = transform.InverseTransformPoint(_ring.position);
            Vector3 pos = a.TransformPoint(AsksvinHarness.HookLocal) - rot * ringLocal + Vector3.up * 0.15f;
            transform.SetPositionAndRotation(pos, rot);
            foreach (Rigidbody b in GetComponentsInChildren<Rigidbody>())
            {
                b.linearVelocity = Vector3.zero;
                b.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
            ModLog.Info("MobileCannonTow: " + name + " snapped behind " + h.name + ", " + ClevisReport(h.transform));
        }

        /// <summary>Diagnostics for the clevis (T-009i): the tow ring (between the two eye plates, x = 0) and the harness hook ring, both in the cannon's own space (x sideways, y up, z along the cannon), and the gap between them. The harness ring is 0.030 wide and the plates' inner faces are at x +-0.0155, so |dx| above about 0.01 means the ring is not between the plates.</summary>
        internal string ClevisReport(Transform animal)
        {
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.ClevisReport", 2d, $"MobileCannonTow.ClevisReport(animal={RuleLog.Shown(animal)})");
            if (_ring == null || animal == null) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.ClevisReport:197", 2d, "MobileCannonTow.ClevisReport: returns \"clevis: no ring or no animal\" (line 197)"); return "clevis: no ring or no animal"; }
            Vector3 ring = transform.InverseTransformPoint(_ring.position);
            Vector3 hook = transform.InverseTransformPoint(animal.TransformPoint(AsksvinHarness.HookLocal));
            Vector3 gap = hook - ring;
            { var returnedAt201 = "clevis ring " + ring.ToString("0.000") + ", harness hook " + hook.ToString("0.000") + ", gap " + gap.ToString("0.000") + " (" + gap.magnitude.ToString("0.000") + " m), plates " + (Mathf.Abs(gap.x) < 0.01f ? "hold the hook ring" : "MISS the hook ring"); if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.ClevisReport:201", 2d, $"MobileCannonTow.ClevisReport: returns {RuleLog.Shown(returnedAt201)} (line 201)"); return returnedAt201; }
        }

        void Update()
        {
            if (!IsValid) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Update:206", 2d, "MobileCannonTow.Update: returns (line 206)"); return; }
            long key = _nview.GetZDO().GetLong(KeyTow, 0L);
            if (key == 0L)
            {
                if (_joint != null) DropJoint();
                _animal = null;
                { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Update:212", 2d, "MobileCannonTow.Update: returns (line 212)"); return; }
            }
            if ((_animal == null || _animal.TowKey != key) && Time.time >= _nextSearch)
            {
                _nextSearch = Time.time + 0.5f;                      // searching all characters is cheap, but not every frame
                AsksvinHarness found = AsksvinHarness.WithKey(key);
                if (found != _animal && _joint != null) DropJoint();
                _animal = found;
            }
            if (_animal != null && _animal.TowKey != key) _animal = null;
            if (_animal != null) _animal.ShowHitched(_ring);
            LogOwners();
            if (!_nview.IsOwner() && _animal != null && _animal.View != null && _animal.View.IsValid()
                && AsksvinTowRules.OwnerMove(ZDOMan.GetSessionID(), _nview.GetZDO().GetOwner(), _animal.View.GetZDO().GetOwner()) == "pull")
            {
                // this client moves the animal: it takes the cannon too, also when the client that had it never hands it back (left, lagging, a lab puppet)
                _nview.ClaimOwnership();
                ModLog.Info("MobileCannonTow: " + name + " taken over from " + _lastCannonOwner + " because this client owns the towing animal");
            }
            if (!_nview.IsOwner())
            {
                if (_joint != null) DropJoint();
                { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Update:234", 2d, "MobileCannonTow.Update: returns (line 234)"); return; }
            }
            OwnerTick();
            ModLog.Every("mc-tow-" + GetInstanceID(), 5d, "MobileCannonTow: " + name + " towed by key " + key + (_animal != null ? " (" + _animal.name + ")" : " (not loaded)") + ", joint " + (_joint != null) + ", speed " + Speed().ToString("0.0") + " m/s");
        }

        // ---- upright keeper (T-009: no flips at full speed or in 90 degree turns) ----
        // The hook sits 3.5 m behind the Asksvin's root, 0.4 m up: when the animal turns, the hook swings sideways fast and yanks the trail end above the wheels'
        // contact, which rolls the cannon over. While towed, the owner holds roll (about the cannon's length) towards the ground's sideways tilt and pitch inside
        // a band: a spring and damper first, a hard limit (the excess turned back, the rate taken out) when a yank still gets past it. Every hard hold is logged.

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
            if (_joint == null || _animal == null || !IsValid || !_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.FixedUpdate:259", 2d, "MobileCannonTow.FixedUpdate: returns (line 259)"); return; }
            if (_body == null) _body = GetComponent<Rigidbody>();
            if (_body == null) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.FixedUpdate:261", 2d, "MobileCannonTow.FixedUpdate: returns (line 261)"); return; }
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
            if (rollOver == 0f && pitchOver == 0f) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.FixedUpdate:286", 2d, "MobileCannonTow.FixedUpdate: returns (line 286)"); return; }
            Quaternion back = Quaternion.AngleAxis(-rollOver, fwd) * Quaternion.AngleAxis(-pitchOver, right);
            _body.rotation = back * _body.rotation;          // at once: MoveRotation waits a step, and a tree or the joint pushes it past again meanwhile (a9miss1: 56 deg)
            if (rollOver != 0f && rollRate * rollOver > 0f) w -= fwd * rollRate;
            if (pitchOver != 0f && pitchRate * pitchOver > 0f) w -= right * pitchRate;
            if (rollOver != 0f) w -= fwd * Vector3.Dot(w, fwd);          // past the limit: no more roll spin at all (the 0.6 damping still flipped it for a frame, a9g16g)
            _body.angularVelocity = w * 0.6f;          // a held yank loses its spin too, or the next step tips it again
            Holds++;
            if (Time.time - _lastHoldLog < 0.5f) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.FixedUpdate:294", 2d, "MobileCannonTow.FixedUpdate: returns (line 294)"); return; }
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
            if (cannon == _lastCannonOwner && animal == _lastAnimalOwner) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.LogOwners:311", 2d, "MobileCannonTow.LogOwners: returns (line 311)"); return; }
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
                { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.OwnerTick:325", 2d, "MobileCannonTow.OwnerTick: returns (line 325)"); return; }
            }
            _lostSince = -1f;
            if (!AsksvinTowRules.KeepsTowing(_animal.View != null && _animal.View.IsValid(), _animal.Animal == null || _animal.Animal.IsDead(), _animal.Harnessed))
            {
                Unhitch("animal dead or without harness");
                { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.OwnerTick:331", 2d, "MobileCannonTow.OwnerTick: returns (line 331)"); return; }
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
            if (_ring != null && ModLog.IsDue("tow-align-" + GetInstanceID(), 2d))
                ModLog.Every("tow-align-" + GetInstanceID(), 2d, "MobileCannonTow: " + name + " " + _animal.AlignReport(_ring.position));      // D-143: cart tip vs harness end
            Rigidbody b = GetComponent<Rigidbody>();
            if (b != null) MaxSpeedSeen = Mathf.Max(MaxSpeedSeen, Vector3.ProjectOnPlane(b.linearVelocity, Vector3.up).magnitude);
            Rigidbody ab = _animal.GetComponent<Rigidbody>();
            if (ab != null) AnimalMaxSpeed = Mathf.Max(AnimalMaxSpeed, Vector3.ProjectOnPlane(ab.linearVelocity, Vector3.up).magnitude);
        }

        void MakeJoint()
        {
            Rigidbody animalBody = _animal.GetComponent<Rigidbody>();
            if (animalBody == null || _ring == null) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.MakeJoint:352", 2d, "MobileCannonTow.MakeJoint: returns (line 352)"); return; }
            _joint = gameObject.AddComponent<ConfigurableJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = transform.InverseTransformPoint(_ring.position);
            _joint.connectedAnchor = AsksvinHarness.HookLocal;
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
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.DropJoint", 2d, $"MobileCannonTow.DropJoint()");
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
            if (_joint == null || !IsValid || !_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Verbose("MobileCannonTow.OnJointBreak: returns (line 390)"); return; }
            _joint = null;
            if (_massOn != null) _massOn.SetExtraMass(0f);
            _massOn = null;
            Unhitch("the tow joint broke at " + force.ToString("0") + " N");
        }

        float Speed()
        {
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Speed", 2d, $"MobileCannonTow.Speed()");
            Rigidbody b = GetComponent<Rigidbody>();
            { var returnedAt401 = b != null ? b.linearVelocity.magnitude : 0f; if (RuleLog.IsOn()) ModLog.Every("MobileCannonTow.Speed:401", 2d, $"MobileCannonTow.Speed: returns {RuleLog.Shown(returnedAt401)} (line 401)"); return returnedAt401; }
        }
    }

    /// <summary>The cannon's tow ring at the end of the trail: E hitches the cannon to a harnessed Asksvin or unhitches it.</summary>
    internal sealed class MobileCannonTowRing : MonoBehaviour, Hoverable, Interactable
    {
        internal MobileCannonTow Tow { get; set; }

        public string GetHoverName() => "Mobile Cannon tow ring";

        public string GetHoverText()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTowRing.GetHoverText()");
            string what = Tow != null && Tow.Towed ? "Unhitch from the Asksvin" : "Hitch to a harnessed Asksvin";
            { var returnedAt416 = Localization.instance.Localize("Mobile Cannon tow ring\n[<color=yellow><b>$KEY_Use</b></color>] " + what); if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonTowRing.GetHoverText: returns {RuleLog.Shown(returnedAt416)} (line 416)"); return returnedAt416; }
        }

        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt) => !hold && Tow != null && Tow.Use(user as Player);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
