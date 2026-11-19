using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The Iron Musket as an equippable weapon (D-087 step 1, D-088 step 0): the item is cloned from the vanilla Arbalest (two-handed, shoulder-aimed, with the
    /// game's reload) and its hand model is the bundle's musket put under the clone's "attach" child, where the game takes what the player holds. The arms get
    /// the musket's own poses from MusketRig (D-092); the model sits in the right hand at the pose authored with them. Firing and the magazine are not wired yet.
    /// </summary>
    internal static class MusketSetup
    {
        static GameObject _item;
        static Vector3 _handPosition = new Vector3(MusketPoses.GripHold[0], MusketPoses.GripHold[1], MusketPoses.GripHold[2]);           // authored with the arm poses (D-092)
        static Vector3 _handEuler = new Vector3(MusketPoses.GripHold[3], MusketPoses.GripHold[4], MusketPoses.GripHold[5]);

        /// <summary>The musket item prefab once configured (null before).</summary>
        internal static GameObject Item => _item;

        /// <summary>Called by the ItemRegistry right after the musket item exists. Expected outcome: true when the hand model sits under "attach".</summary>
        internal static bool Configure(GameObject itemPrefab)
        {
            _item = itemPrefab;
            bool hand = AttachHandModel(itemPrefab);
            ModLog.Verbose("MusketSetup: configured - hand model " + hand);
            return hand;
        }

        /// <summary>
        /// Moves and turns the hand model on the item prefab (the next equip shows it; pictures decide the numbers). Expected outcome: true when the model exists and
        /// now sits at the asked pose.
        /// </summary>
        internal static bool SetHandPose(Vector3 position, Vector3 euler)
        {
            _handPosition = position;
            _handEuler = euler;
            Transform copy = _item != null ? _item.transform.Find("attach/gp_model") : null;
            if (copy == null)
            {
                ModLog.Warn("MusketSetup: no hand model to pose");
                return false;
            }
            copy.localPosition = position;
            copy.localEulerAngles = euler;
            ModLog.Verbose("MusketSetup: hand pose " + position.ToString("0.000") + " rotation " + euler.ToString("0.0"));
            return copy.localPosition == position;
        }

        static bool AttachHandModel(GameObject item)
        {
            Transform attach = item.transform.Find("attach");
            Transform model = item.transform.Find("gp_model");
            if (attach == null || model == null)
            {
                ModLog.Warn("MusketSetup: no hand model - attach " + (attach != null) + ", bundle model " + (model != null));
                return false;
            }
            if (attach.Find("gp_model") != null) return true;
            foreach (Renderer old in attach.GetComponentsInChildren<Renderer>(true)) old.enabled = false;
            GameObject copy = Object.Instantiate(model.gameObject, attach);
            copy.name = "gp_model";
            copy.transform.localPosition = _handPosition;
            copy.transform.localEulerAngles = _handEuler;
            copy.transform.localScale = Vector3.one;
            foreach (Transform t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = attach.gameObject.layer;
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            ModLog.Verbose("MusketSetup: hand model under attach (" + attach.childCount + " children), the vanilla look hidden");
            return attach.Find("gp_model") != null;
        }
    }
}
