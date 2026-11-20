using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// The Mobile Cannon piece is a clone of the game's Cart (D-104): the cart's physics stay (body, two wheel rigidbodies on joints, Vagon pull joint,
    /// network sync), its looks, its load and its storage go, and the bundle's cannon model rides on it. The model is turned so the TRAIL points to the
    /// cart's pull side (+Z): the player grabs the trail end and the muzzle points back while it is pulled, like a field gun. The visible wheels follow the
    /// cart's physics wheels (MobileCannonWheels).
    /// </summary>
    internal static class MobileCannonSetup
    {
        internal const string ModelName = "gp_model", PushName = "PushPoint", TrailName = "HarnessPoint", GripA = "HandleGripA", GripB = "HandleGripB";

        /// <summary>The model sits 13 cm back so its axle lies on the cart's wheel axle (cart wheels at z -0.13, the cannon's at z 0), turned 180 degrees.</summary>
        internal static readonly Vector3 ModelOffset = new Vector3(0f, 0f, -0.13f);

        static readonly AccessTools.FieldRef<Vagon, Container> ContainerRef = AccessTools.FieldRefAccess<Vagon, Container>("m_container");

        /// <summary>Called by the ItemRegistry after the bundle look is in place. Expected outcome: true when the cart physics, the model and the pull point are set.</summary>
        internal static bool Configure(GameObject piece)
        {
            Vagon vagon = piece != null ? piece.GetComponent<Vagon>() : null;
            Transform model = piece != null ? piece.transform.Find(ModelName) : null;
            if (vagon == null || model == null)
            {
                ModLog.Warn("MobileCannonSetup: not a cart clone with a bundle model - vagon " + (vagon != null) + ", model " + (model != null));
                return false;
            }
            int colliders = RestoreCartColliders(piece, model);
            int hidden = HideCartLooks(piece, model);
            RemoveCartLoad(piece, vagon);

            model.localRotation = Quaternion.Euler(0f, 180f, 0f);
            model.localPosition = ModelOffset;
            Transform trail = model.Find(PushName) ?? model.Find(TrailName);     // D-107: the player stands behind the push handle
            bool pull = trail != null && vagon.m_attachPoint != null;
            if (pull)
            {
                Vector3 local = piece.transform.InverseTransformPoint(trail.position);
                vagon.m_attachPoint.localPosition = local;
                if (vagon.m_lineAttachPoints0 != null) vagon.m_lineAttachPoints0.localPosition = local + new Vector3(0.12f, 0f, 0f);
                if (vagon.m_lineAttachPoints1 != null) vagon.m_lineAttachPoints1.localPosition = local - new Vector3(0.12f, 0f, 0f);
            }
            AddTrailFoot(piece);
            vagon.m_name = "Mobile Cannon";
            if (piece.GetComponent<MobileCannonWheels>() == null) piece.AddComponent<MobileCannonWheels>();
            if (piece.GetComponent<MobileCannonPull>() == null) piece.AddComponent<MobileCannonPull>();
            if (piece.GetComponent<MobileCannonAim>() == null) piece.AddComponent<MobileCannonAim>();
            if (piece.GetComponent<MobileCannonGun>() == null) piece.AddComponent<MobileCannonGun>();
            if (piece.GetComponent<MobileCannonTow>() == null) piece.AddComponent<MobileCannonTow>();
            ModLog.Verbose("MobileCannonSetup: cart colliders on " + colliders + ", cart looks hidden " + hidden + ", pull point "
                + (pull ? vagon.m_attachPoint.localPosition.ToString("0.00") : "missing") + ", wheels " + (vagon.m_wheels != null ? vagon.m_wheels.Length : 0));
            { var returnedAt58 = pull && colliders > 0; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonSetup.Configure: returns {RuleLog.Shown(returnedAt58)} (line 58)"); return returnedAt58; }
        }

        /// <summary>BundleVisuals switched every solid collider off; the cart needs its body boxes and its wheel colliders back. The model's own colliders stay off.</summary>
        static int RestoreCartColliders(GameObject piece, Transform model)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonSetup.RestoreCartColliders(piece={RuleLog.Shown(piece)}, model={RuleLog.Shown(model)})");
            int n = 0;
            foreach (Collider c in piece.GetComponentsInChildren<Collider>(true))
            {
                if (c.transform.IsChildOf(model) || c.isTrigger) continue;
                if (c.transform == piece.transform && c is BoxCollider)
                {
                    Object.DestroyImmediate(c);           // the bundle prefab's own box: the cart's body boxes do that job
                    continue;
                }
                c.enabled = true;
                n++;
            }
            { var returnedAt77 = n; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonSetup.RestoreCartColliders: returns {RuleLog.Shown(returnedAt77)} (line 77)"); return returnedAt77; }
        }

        /// <summary>Switches off every renderer of the cart itself (body, wheels, worn / broken states); the cannon model is the only look.</summary>
        static int HideCartLooks(GameObject piece, Transform model)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonSetup.HideCartLooks(piece={RuleLog.Shown(piece)}, model={RuleLog.Shown(model)})");
            int n = 0;
            foreach (Renderer r in piece.GetComponentsInChildren<Renderer>(true))
            {
                if (r.transform.IsChildOf(model) || r is LineRenderer) continue;
                r.enabled = false;
                n++;
            }
            { var returnedAt91 = n; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonSetup.HideCartLooks: returns {RuleLog.Shown(returnedAt91)} (line 91)"); return returnedAt91; }
        }

        /// <summary>The cart's boxes-and-barrels load and its storage chest are not part of a cannon.</summary>
        static void RemoveCartLoad(GameObject piece, Vagon vagon)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonSetup.RemoveCartLoad(piece={RuleLog.Shown(piece)}, vagon={RuleLog.Shown(vagon)})");
            Transform load = piece.transform.Find("load");
            if (load != null) Object.DestroyImmediate(load.gameObject);
            vagon.m_loadVis = new List<Vagon.LoadData>();
            Container chest = ContainerRef(vagon);
            if (chest != null) Object.DestroyImmediate(chest.gameObject);
            ContainerRef(vagon) = null;
        }
        /// <summary>
        /// The cart rests on its shafts and tips about 12 degrees (lab a8crank2: barrel 27.5 deg at crank 15). A field gun rests on its trail: a box under the
        /// trail end reaches the ground at the same height as the wheels, so a parked cannon stands level; the cart's own shaft colliders go.
        /// </summary>
        static void AddTrailFoot(GameObject piece)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonSetup.AddTrailFoot(piece={RuleLog.Shown(piece)})");
            foreach (Collider c in piece.GetComponentsInChildren<Collider>(true))
                if (c.name.StartsWith("handle")) c.enabled = false;
            if (piece.transform.Find("gp_trail_foot") != null) { if (RuleLog.IsOn()) ModLog.Verbose("MobileCannonSetup.AddTrailFoot: returns (line 114)"); return; }
            var foot = new GameObject("gp_trail_foot");
            foot.layer = piece.layer;
            foot.transform.SetParent(piece.transform, false);
            foot.transform.localPosition = new Vector3(0f, 0.17f, 1.52f);
            BoxCollider box = foot.AddComponent<BoxCollider>();
            box.size = new Vector3(0.5f, 0.34f, 0.3f);
        }
    }
}
