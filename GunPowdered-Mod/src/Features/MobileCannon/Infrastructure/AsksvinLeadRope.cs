using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// D-144 (T-014): the saddle's chest rope as something to take hold of: a small trigger box in front of the saddle (child "gp_lead_rope" of the Asksvin)
    /// that answers the player's look like any piece ("[E] Lead ..."). E toggles leading the Asksvin by it (<see cref="AsksvinReverse.ToggleLead"/>). Only an
    /// Asksvin with a saddle has it; the saddle itself (mount) and the animal (pet) keep their own E.
    /// </summary>
    internal sealed class AsksvinLeadRope : MonoBehaviour, Hoverable, Interactable
    {
        const string ObjectName = "gp_lead_rope";

        /// <summary>Where the rope sits when the saddle cannot be measured (Asksvin space: centre line, chest height, in front of the saddle).</summary>
        internal static readonly Vector3 FallbackLocal = new Vector3(0f, 1.0f, 0.6f);

        /// <summary>From the rider's seat to the chest rope, in the Asksvin's space (forward and down to the chest).</summary>
        internal static readonly Vector3 SeatToRope = new Vector3(0f, -0.7f, 0.8f);

        AsksvinReverse _reverse;
        bool _placed;
        BoxCollider _box;

        /// <summary>Puts the rope box on the Asksvin (once). Expected outcome: the child exists, a trigger, on a layer the player's look can hit.</summary>
        internal static void AddTo(AsksvinReverse reverse)
        {
            if (reverse == null || reverse.transform.Find(ObjectName) != null) { if (RuleLog.IsOn()) ModLog.Verbose("AsksvinLeadRope.AddTo: none or already there - returns"); return; }
            var go = new GameObject(ObjectName);
            go.layer = LayerMask.NameToLayer("piece_nonsolid");
            go.transform.SetParent(reverse.transform, false);
            go.transform.localPosition = FallbackLocal;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.9f, 0.7f, 0.35f);
            go.AddComponent<AsksvinLeadRope>()._reverse = reverse;
            ModLog.Verbose("AsksvinLeadRope.AddTo: rope box on " + reverse.name + " - returns");
        }

        /// <summary>The rope's world position (the point the leading player is measured from). Expected outcome: the box's centre, or the fallback place.</summary>
        internal static Vector3 WorldPoint(AsksvinReverse reverse)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinLeadRope.WorldPoint", 2d, "AsksvinLeadRope.WorldPoint(reverse=" + RuleLog.Shown(reverse) + ")");
            Transform t = reverse != null ? reverse.transform.Find(ObjectName) : null;
            { var returnedAt44 = t != null ? t.position : reverse != null ? reverse.transform.TransformPoint(FallbackLocal) : Vector3.zero; if (RuleLog.IsOn()) ModLog.Every("AsksvinLeadRope.WorldPoint:44", 2d, $"AsksvinLeadRope.WorldPoint: returns {returnedAt44} (line 44)"); return returnedAt44; }
        }

        void Update()
        {
            bool saddled = _reverse != null && _reverse.Saddled;
            if (_box == null) _box = GetComponent<BoxCollider>();
            if (_box != null && _box.enabled != saddled) _box.enabled = saddled;       // without a saddle the box must not catch the look meant for the animal
            ModLog.Every("asksvin-rope-" + GetInstanceID(), 10d, "AsksvinLeadRope: rope box " + (_box != null && _box.enabled ? "on" : "off") + ", saddled " + saddled + ", at " + transform.localPosition.ToString("0.00"));
            if (_placed || !saddled) return;
            _placed = true;
            // the rider's seat (Sadle.m_attachPoint) is on the saddle; the chest rope runs round the chest just in front of and below it. (The saddle's skinned
            // renderer bounds were tried first, a14back1: they are inflated, the rope landed 3 m ahead of the root, in front of the head.)
            Transform a = _reverse.transform;
            Tameable tame = _reverse.GetComponent<Tameable>();
            Transform seat = tame != null && tame.m_saddle != null ? tame.m_saddle.m_attachPoint : null;
            if (seat == null) { ModLog.Info("AsksvinLeadRope: no saddle seat on " + _reverse.name + ", the chest rope stays at " + FallbackLocal); return; }
            Vector3 s = a.InverseTransformPoint(seat.position);
            transform.localPosition = new Vector3(0f, s.y + SeatToRope.y, s.z + SeatToRope.z);
            Collider body = _reverse.GetComponent<Collider>();
            ModLog.Info($"AsksvinLeadRope: chest rope of {_reverse.name} at {transform.localPosition:0.00} (seat {s:0.00}; body collider "
                + (body != null ? a.InverseTransformPoint(body.bounds.min).ToString("0.00") + ".." + a.InverseTransformPoint(body.bounds.max).ToString("0.00") : "none") + ")");
        }

        public string GetHoverText()
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinLeadRope.GetHoverText", 2d, "AsksvinLeadRope.GetHoverText()");
            if (_reverse == null || !_reverse.Saddled) return "";
            Player p = Player.m_localPlayer;
            bool mine = p != null && _reverse.Leader == p.GetZDOID();
            { var returnedAt78 = Localization.instance.Localize("Chest rope\n[<color=yellow><b>$KEY_Use</b></color>] " + (mine ? AsksvinReverseRules.LetGoHover : AsksvinReverseRules.LeadHover)); if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinLeadRope.GetHoverText: returns {returnedAt78} (line 78)"); return returnedAt78; }
        }

        public string GetHoverName() => "Chest rope";

        public float GetHoverOffset() => 0f;

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || _reverse == null) { if (RuleLog.IsOn()) ModLog.Verbose("AsksvinLeadRope.Interact: held or no animal - returns false"); return false; }
            bool done = _reverse.ToggleLead(user as Player);
            ModLog.Verbose("AsksvinLeadRope.Interact: returns " + done);
            return done;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    }
}
