// kydl:read-only-observer - test-only: measures an Asksvin that backs up (D-144); it is driven by AsksvinReverse, not by these hooks
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>D-144 (T-014) lab hooks: mark the Asksvin, lead it by the chest rope, stand in front of the rope, and read how it backed up.</summary>
    internal static class AsksvinBackTestHooks
    {
        static Vector3 _start, _startFwd;

        static GameObject Animal => AsksvinTowTestHooks.Animal;

        /// <summary>Marks where the Asksvin stands and faces now and starts the back-speed probe. Returns "marked at ...".</summary>
        [TestHook("GunPowdered.asksvinBackMark")]
        static string Mark()
        {
            ModLog.Verbose("AsksvinBackTestHooks.Mark()");
            if (Animal == null) { ModLog.Verbose("AsksvinBackTestHooks.Mark: no Asksvin - returns why"); return "no Asksvin"; }
            _start = Animal.transform.position;
            _startFwd = Flat(Animal.transform.forward);
            AsksvinBackProbe probe = Animal.GetComponent<AsksvinBackProbe>();
            if (probe == null) probe = Animal.AddComponent<AsksvinBackProbe>();
            probe.Clear();
            string s = "marked at " + _start.ToString("0.0");
            ModLog.Verbose("AsksvinBackTestHooks.Mark: returns " + s);
            return s;
        }

        /// <summary>
        /// "back D m, max back speed S m/s (walk W m/s, ratio R), turned T deg, backing seen B, cannon off straight A deg, upright U". back is along the marked
        /// facing (negative: it went forward); the ratio is the top backwards speed over the animal's walk speed (D-144: half).
        /// </summary>
        [TestHook("GunPowdered.asksvinBackState")]
        static string State()
        {
            ModLog.Verbose("AsksvinBackTestHooks.State()");
            if (Animal == null) { ModLog.Verbose("AsksvinBackTestHooks.State: no Asksvin - returns why"); return "no Asksvin"; }
            Character c = Animal.GetComponent<Character>();
            AsksvinBackProbe probe = Animal.GetComponent<AsksvinBackProbe>();
            float back = Vector3.Dot(_start - Animal.transform.position, _startFwd);
            float walk = c != null ? c.m_walkSpeed : 0f, top = probe != null ? probe.TopBack : 0f;
            float turned = Vector3.SignedAngle(_startFwd, Flat(Animal.transform.forward), Vector3.up);
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            string cannonPart = "no cannon";
            if (tow != null && tow.Ring != null && tow.Towed)
            {
                float off = Vector3.Angle(Flat(tow.Ring.position - cannon.transform.position), Flat(Animal.transform.forward));
                cannonPart = $"cannon off straight {off:0} deg (worst {(probe != null ? probe.WorstOff : 0f):0}), upright {cannon.transform.up.y > 0.7f}";
            }
            string s = $"back {back:0.00} m, max back speed {top:0.00} m/s (walk {walk:0.00} m/s, ratio {(walk > 0f ? top / walk : 0f):0.00}), turned {turned:0} deg, backing seen {(probe != null && probe.Seen)}, {cannonPart}";
            ModLog.Info("AsksvinBackTestHooks.State: returns " + s);
            return s;
        }

        /// <summary>E on the Asksvin's chest rope as the local player (the rope's own Interact). Returns "lead sent True/False, rope at ..., leader me True/False" (leader read after the RPC).</summary>
        [TestHook("GunPowdered.asksvinLead")]
        static string Lead()
        {
            ModLog.Verbose("AsksvinBackTestHooks.Lead()");
            Player p = Player.m_localPlayer;
            Transform rope = Animal != null ? Animal.transform.Find("gp_lead_rope") : null;
            AsksvinLeadRope r = rope != null ? rope.GetComponent<AsksvinLeadRope>() : null;
            if (p == null || r == null) { ModLog.Verbose("AsksvinBackTestHooks.Lead: no player or no chest rope - returns why"); return "no player or no chest rope"; }
            string hover = r.GetHoverText().Replace("\n", " / ");                   // what the player saw before pressing E
            bool sent = r.Interact(p, false, false);
            AsksvinReverse rev = Animal.GetComponent<AsksvinReverse>();
            string s = "lead sent " + sent + ", rope at " + rope.localPosition.ToString("0.00") + ", hover '" + hover + "', leader me " + (rev != null && rev.Leader == p.GetZDOID());
            ModLog.Info("AsksvinBackTestHooks.Lead: returns " + s);
            return s;
        }

        /// <summary>Whether the local player leads the Asksvin by the chest rope. Returns "leader me True/False".</summary>
        [TestHook("GunPowdered.asksvinLeader")]
        static string Leader()
        {
            ModLog.Verbose("AsksvinBackTestHooks.Leader()");
            Player p = Player.m_localPlayer;
            AsksvinReverse rev = Animal != null ? Animal.GetComponent<AsksvinReverse>() : null;
            string s = "leader me " + (p != null && rev != null && rev.Leader == p.GetZDOID());
            ModLog.Verbose("AsksvinBackTestHooks.Leader: returns " + s);
            return s;
        }

        /// <summary>Puts the local player <paramref name="metres"/> in front of the chest rope (on the ground), facing the Asksvin. Returns "before the rope at ...".</summary>
        [TestHook("GunPowdered.playerBeforeRope")]
        static string BeforeRope(double metres)
        {
            ModLog.Verbose($"AsksvinBackTestHooks.BeforeRope(metres={metres})");
            Player p = Player.m_localPlayer;
            AsksvinReverse rev = Animal != null ? Animal.GetComponent<AsksvinReverse>() : null;
            if (p == null || rev == null) { ModLog.Verbose("AsksvinBackTestHooks.BeforeRope: no player or Asksvin - returns why"); return "no player or Asksvin"; }
            Vector3 fwd = Flat(Animal.transform.forward);
            Vector3 at = AsksvinLeadRope.WorldPoint(rev) + fwd * (float)metres;
            float h;
            at.y = ZoneSystem.instance != null && ZoneSystem.instance.GetSolidHeight(at, out h) ? h : Animal.transform.position.y;
            p.transform.SetPositionAndRotation(at, Quaternion.LookRotation(-fwd));
            Rigidbody body = p.GetComponent<Rigidbody>();
            if (body != null) { body.position = at; body.linearVelocity = Vector3.zero; }
            p.SetLookDir(-fwd);
            string s = "before the rope at " + at.ToString("0.0") + " (rope " + AsksvinLeadRope.WorldPoint(rev).ToString("0.0") + ", Asksvin " + Animal.transform.position.ToString("0.0") + ")";
            ModLog.Verbose("AsksvinBackTestHooks.BeforeRope: returns " + s);
            return s;
        }

        static Vector3 Flat(Vector3 v)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinBackTestHooks.Flat", 2d, "AsksvinBackTestHooks.Flat()");
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }

    /// <summary>Test-only: the top backwards speed of an Asksvin, whether it was seen backing, and the worst angle of a towed cannon off straight.</summary>
    internal sealed class AsksvinBackProbe : MonoBehaviour
    {
        internal float TopBack, WorstOff;
        internal bool Seen;

        internal void Clear() { TopBack = 0f; WorstOff = 0f; Seen = false; ModLog.Verbose("AsksvinBackProbe.Clear: cleared"); }

        void FixedUpdate()
        {
            Rigidbody b = GetComponent<Rigidbody>();
            AsksvinReverse rev = GetComponent<AsksvinReverse>();
            bool backing = rev != null && rev.IsBacking;
            Seen |= backing;
            if (b != null && backing) TopBack = Mathf.Max(TopBack, Vector3.Dot(b.linearVelocity, -transform.forward));
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            if (backing && tow != null && tow.Towed && tow.Ring != null)
            {
                Vector3 line = Vector3.ProjectOnPlane(tow.Ring.position - cannon.transform.position, Vector3.up);
                WorstOff = Mathf.Max(WorstOff, Vector3.Angle(line, Vector3.ProjectOnPlane(transform.forward, Vector3.up)));
            }
            ModLog.Every("asksvin-back-probe-" + GetInstanceID(), 2d, $"AsksvinBackProbe: backing {backing}, top back speed {TopBack:0.00} m/s, worst cannon angle {WorstOff:0} deg");
        }
    }
}
