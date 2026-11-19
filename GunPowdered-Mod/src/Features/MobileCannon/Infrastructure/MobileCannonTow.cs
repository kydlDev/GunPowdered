using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Features.MobileCannon.Domain;

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
        internal const string KeyTow = "gp_mc_tow";
        const string RpcHitch = "GP_MC_Hitch", RpcUnhitch = "GP_MC_Unhitch", RingName = "gp_towring";

        ZNetView _nview;
        Vagon _vagon;
        Transform _ring;
        ConfigurableJoint _joint;
        AsksvinHarness _animal;
        Character _massOn;
        float _lostSince = -1f;

        internal bool IsValid => _nview != null && _nview.IsValid();

        internal bool Towed => IsValid && !_nview.GetZDO().GetZDOID(KeyTow).IsNone();

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
                _nview.Register<ZDOID>(RpcHitch, RPC_Hitch);
                _nview.Register(RpcUnhitch, RPC_Unhitch);
            }
            ModLog.Verbose("MobileCannonTow: " + name + " tow ring " + (_ring != null) + ", towed " + Towed);
        }

        Transform FindPoint()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true)) if (t.name == MobileCannonSetup.TrailName) return t;
            return _vagon.m_attachPoint;
        }

        /// <summary>E on the tow ring: unhitches a towed cannon, else hitches it to the nearest harnessed Asksvin. Expected outcome: true when a request was sent.</summary>
        internal bool Use(Player p)
        {
            if (p == null || !IsValid) return false;
            if (Towed)
            {
                _nview.InvokeRPC(RpcUnhitch);
                p.Message(MessageHud.MessageType.Center, "Unhitched the Mobile Cannon");
                return true;
            }
            AsksvinHarness near = Nearest();
            string why = AsksvinTowRules.WhyNotHitch(false, _vagon != null && _vagon.IsAttached(), near != null, Range());
            if (why != null)
            {
                p.Message(MessageHud.MessageType.Center, why);
                return false;
            }
            _nview.InvokeRPC(RpcHitch, near.View.GetZDO().m_uid);
            p.Message(MessageHud.MessageType.Center, "Hitched the Mobile Cannon to " + near.Animal.GetHoverName());
            return true;
        }

        AsksvinHarness Nearest()
        {
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
            return best;
        }

        static float Range() => ModSettings.MobileCannonHitchRange.Value;

        void RPC_Hitch(long sender, ZDOID animal)
        {
            if (!_nview.IsOwner() || Towed) return;
            GameObject go = ZNetScene.instance.FindInstance(animal);
            AsksvinHarness h = go != null ? go.GetComponent<AsksvinHarness>() : null;
            if (h == null || !h.Harnessed || (_vagon != null && _vagon.IsAttached())) return;
            _nview.GetZDO().Set(KeyTow, animal);
            PlaceBehind(h);
            ModLog.Info("MobileCannonTow: " + name + " hitched to " + go.name + " " + animal);
        }

        void RPC_Unhitch(long sender)
        {
            if (!_nview.IsOwner()) return;
            Unhitch("E on the tow ring");
        }

        void Unhitch(string why)
        {
            DropJoint();
            if (IsValid && _nview.IsOwner()) _nview.GetZDO().Set(KeyTow, ZDOID.None);
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
        }

        void Update()
        {
            if (!IsValid) return;
            ZDOID id = _nview.GetZDO().GetZDOID(KeyTow);
            if (id.IsNone())
            {
                if (_joint != null) DropJoint();
                _animal = null;
                return;
            }
            GameObject go = ZNetScene.instance.FindInstance(id);
            _animal = go != null ? go.GetComponent<AsksvinHarness>() : null;
            if (_animal != null) _animal.ShowHitched();
            if (!_nview.IsOwner())
            {
                if (_joint != null) DropJoint();
                return;
            }
            OwnerTick(id);
            ModLog.Every("mc-tow-" + GetInstanceID(), 5d, "MobileCannonTow: " + name + " towed by " + id + ", joint " + (_joint != null) + ", speed " + Speed().ToString("0.0") + " m/s");
        }

        void OwnerTick(ZDOID id)
        {
            bool exists = ZDOMan.instance.GetZDO(id) != null;
            if (_animal == null)
            {
                // the animal is not loaded here (yet): give it a few seconds before giving up, unless its ZDO is gone (dead, removed)
                if (_lostSince < 0f) _lostSince = Time.time;
                if (!exists || Time.time - _lostSince > 10f) Unhitch(exists ? "animal not loaded for 10 s" : "animal gone");
                return;
            }
            _lostSince = -1f;
            if (!AsksvinTowRules.KeepsTowing(exists, _animal.Animal == null || _animal.Animal.IsDead(), _animal.Harnessed))
            {
                Unhitch("animal dead or without harness");
                return;
            }
            long animalOwner = _animal.View.GetZDO().GetOwner();
            if (animalOwner != 0L && animalOwner != ZDOMan.GetSessionID())
            {
                // the client that moves the animal simulates the cannon too
                DropJoint();
                _nview.GetZDO().SetOwner(animalOwner);
                ModLog.Info("MobileCannonTow: " + name + " handed over to the animal's owner " + animalOwner);
                return;
            }
            if (_joint == null) MakeJoint();
            Rigidbody b = GetComponent<Rigidbody>();
            if (b != null) MaxSpeedSeen = Mathf.Max(MaxSpeedSeen, Vector3.ProjectOnPlane(b.linearVelocity, Vector3.up).magnitude);
            Rigidbody ab = _animal.GetComponent<Rigidbody>();
            if (ab != null) AnimalMaxSpeed = Mathf.Max(AnimalMaxSpeed, Vector3.ProjectOnPlane(ab.linearVelocity, Vector3.up).magnitude);
        }

        void MakeJoint()
        {
            Rigidbody animalBody = _animal.GetComponent<Rigidbody>();
            if (animalBody == null || _ring == null) return;
            _joint = gameObject.AddComponent<ConfigurableJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = transform.InverseTransformPoint(_ring.position);
            _joint.connectedAnchor = AsksvinHarness.HookLocal;
            _joint.breakForce = _vagon != null ? _vagon.m_breakForce * 4f : 40000f;
            _joint.xMotion = ConfigurableJointMotion.Limited;
            _joint.yMotion = ConfigurableJointMotion.Limited;
            _joint.zMotion = ConfigurableJointMotion.Locked;
            _joint.linearLimit = new SoftJointLimit { limit = 0.001f };
            _joint.linearLimitSpring = new SoftJointLimitSpring { spring = _vagon != null ? _vagon.m_spring : 5000f, damper = _vagon != null ? _vagon.m_springDamping : 1000f };
            _joint.connectedBody = animalBody;
            _massOn = _animal.Animal;
            if (_massOn != null) _massOn.SetExtraMass(ModSettings.MobileCannonTowExtraMass.Value);     // like the cart's m_playerExtraPullMass: the cannon must not hold the Asksvin back (D-105)
            ModLog.Info("MobileCannonTow: " + name + " joint to " + _animal.name + ", ring " + _joint.anchor.ToString("0.00") + ", hook " + _joint.connectedAnchor.ToString("0.00"));
        }

        void DropJoint()
        {
            if (_joint != null) Destroy(_joint);
            _joint = null;
            if (_massOn != null) _massOn.SetExtraMass(0f);
            _massOn = null;
        }

        void OnDestroy() => DropJoint();

        void OnJointBreak(float force)
        {
            if (_joint == null || !IsValid || !_nview.IsOwner()) return;
            _joint = null;
            if (_massOn != null) _massOn.SetExtraMass(0f);
            _massOn = null;
            Unhitch("the tow joint broke at " + force.ToString("0") + " N");
        }

        float Speed()
        {
            Rigidbody b = GetComponent<Rigidbody>();
            return b != null ? b.linearVelocity.magnitude : 0f;
        }
    }

    /// <summary>The cannon's tow ring at the end of the trail: E hitches the cannon to a harnessed Asksvin or unhitches it.</summary>
    internal sealed class MobileCannonTowRing : MonoBehaviour, Hoverable, Interactable
    {
        internal MobileCannonTow Tow { get; set; }

        public string GetHoverName() => "Mobile Cannon tow ring";

        public string GetHoverText()
        {
            string what = Tow != null && Tow.Towed ? "Unhitch from the Asksvin" : "Hitch to a harnessed Asksvin";
            return Localization.instance.Localize("Mobile Cannon tow ring\n[<color=yellow><b>$KEY_Use</b></color>] " + what);
        }

        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt) => !hold && Tow != null && Tow.Use(user as Player);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
