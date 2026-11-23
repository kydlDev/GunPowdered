using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Musket.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>Musket test hooks, part 2 (G04 class size): hands, lift, dodge, grip, trace and blocker.</summary>
    internal static partial class MusketTestHooks
    {
        /// <summary>The local player's hands: "right R, left L, musket held M, other item in the left hand T".</summary>
        [TestHook("GunPowdered.musketHands")]
        static string Hands()
        {
            ModLog.Verbose("MusketTestHooks.Hands()");
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("MusketTestHooks.Hands: no player - returns why"); return "no player"; }
            VisEquipment vis = p.GetComponent<VisEquipment>();
            GameObject other = vis != null ? HarmonyLib.Traverse.Create(vis).Field("m_rightItemInstance").GetValue<GameObject>() : null;
            bool inLeft = other != null && vis.m_leftHand != null && other.transform.IsChildOf(vis.m_leftHand);
            string s = "right " + (p.RightItem != null ? p.RightItem.m_dropPrefab.name : "nothing") + ", left " + (p.LeftItem != null ? p.LeftItem.m_dropPrefab.name : "nothing")
                + ", musket held " + (MusketGun.Held(p) != null) + ", other item in the left hand " + inLeft;
            ModLog.Info("MusketTestHooks.Hands: returns " + s);
            return s;
        }

        /// <summary>
        /// Where the local player's hand items sit: "right MODEL under BONE at (x, y, z) rot (p, y, r); left ..." - the right and left item models
        /// (VisEquipment), the hand bone they hang under and their local place there (D-155: the torch next to the musket compared with a normal torch).
        /// </summary>
        [TestHook("GunPowdered.handModels")]
        static string HandModels()
        {
            ModLog.Verbose("MusketTestHooks.HandModels()");
            Player p = Player.m_localPlayer;
            VisEquipment vis = p != null ? p.GetComponent<VisEquipment>() : null;
            if (vis == null) { ModLog.Verbose("MusketTestHooks.HandModels: no player - returns why"); return "no player"; }
            var t = HarmonyLib.Traverse.Create(vis);
            string s = "right " + Place(t.Field("m_rightItemInstance").GetValue<GameObject>(), vis) + "; left " + Place(t.Field("m_leftItemInstance").GetValue<GameObject>(), vis);
            ModLog.Info("MusketTestHooks.HandModels: returns " + s);
            return s;
        }

        static string Place(GameObject go, VisEquipment vis)
        {
            if (go == null) { ModLog.Verbose("MusketTestHooks.Place: no model"); return "nothing"; }
            Transform tr = go.transform;
            string hand = tr.IsChildOf(vis.m_leftHand) ? "left hand" : tr.IsChildOf(vis.m_rightHand) ? "right hand" : (tr.parent != null ? tr.parent.name : "no parent");
            string s = go.name.Replace("(Clone)", "") + " in the " + hand + " (" + (tr.parent != null ? tr.parent.name : "-") + ") at " + tr.localPosition.ToString("0.000") + " rot " + tr.localEulerAngles.ToString("0");
            ModLog.Verbose("MusketTestHooks.Place: returns " + s);
            return s;
        }

        /// <summary>
        /// Lifts the local player <paramref name="metres"/> up (in the air, like a jump; the game sees it at its next physics step, so wait a moment before the
        /// next step). Returns "lifted to Y".
        /// </summary>
        [TestHook("GunPowdered.musketLift")]
        static string Lift(double metres)
        {
            ModLog.Verbose("MusketTestHooks.Lift(metres=" + metres + ")");
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("MusketTestHooks.Lift: no player - returns why"); return "no player"; }
            p.transform.position += Vector3.up * (float)metres;
            Rigidbody body = p.GetComponent<Rigidbody>();
            if (body != null) body.position = p.transform.position;
            Physics.SyncTransforms();
            string s = "lifted to " + p.transform.position.y.ToString("0.0");
            ModLog.Info("MusketTestHooks.Lift: returns " + s);
            return s;
        }

        /// <summary>Dodge tests (user 2026-11-23): the local player dodges forward through the game's own dodge (Player.Dodge, queued like the dodge key). Returns "dodge queued".</summary>
        [TestHook("GunPowdered.playerDodge")]
        static string Dodge()
        {
            ModLog.Verbose("MusketTestHooks.Dodge()");
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("MusketTestHooks.Dodge: no player - returns why"); return "no player"; }
            p.AddStamina(100f);
            HarmonyLib.Traverse.Create(p).Method("Dodge", new[] { typeof(Vector3) }).GetValue(p.transform.forward);
            ModLog.Info("MusketTestHooks.Dodge: returns dodge queued");
            return "dodge queued";
        }

        /// <summary>Dodge tests: "dodging D; musket under P, X m from the right hand, Y m from the left hand; offhand under O" - where the musket model and the other hand item sit right now.</summary>
        [TestHook("GunPowdered.musketGrip")]
        static string Grip()
        {
            ModLog.Verbose("MusketTestHooks.Grip()");
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("MusketTestHooks.Grip: no player - returns why"); return "no player"; }
            Animator anim = p.GetComponentInChildren<Animator>();
            Transform right = anim != null ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null, left = anim != null ? anim.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            VisEquipment vis = p.GetComponent<VisEquipment>();
            Transform model = null;
            if (vis != null && vis.m_rightHand != null) foreach (Transform t in vis.m_rightHand.GetComponentsInChildren<Transform>(true)) if (t.name == "gp_model" && t.parent != null && t.parent.name.StartsWith("attach")) { model = t; break; }
            GameObject other = vis != null ? HarmonyLib.Traverse.Create(vis).Field("m_rightItemInstance").GetValue<GameObject>() : null;
            string s = "dodging " + p.InDodge() + "; musket " + (model == null ? "not in the right hand" : "under " + model.parent.parent?.name + ", " + (right != null ? Vector3.Distance(model.position, right.position).ToString("0.00") : "?") + " m from the right hand, "
                + (left != null ? Vector3.Distance(model.position, left.position).ToString("0.00") : "?") + " m from the left hand") + "; offhand " + (other != null && other.transform.parent != null ? "under " + other.transform.parent.name : "none");
            ModLog.Info("MusketTestHooks.Grip: returns " + s);
            return s;
        }

        /// <summary>Diagnostic: logs the next N frames of every other player's musket rig (MusketRig.Trace): "tracing N frames".</summary>
        [TestHook("GunPowdered.musketTrace")]
        static string Trace(double frames)
        {
            ModLog.Verbose("MusketTestHooks.Trace(frames=" + frames + ")");
            MusketRig.TraceFrames = (int)frames;
            string s = "tracing " + MusketRig.TraceFrames + " frames";
            ModLog.Info("MusketTestHooks.Trace: returns " + s);
            return s;
        }

        /// <summary>D-155: the item the local player blocks with now: "blocker NAME" (prefab name) or "blocker nothing".</summary>
        [TestHook("GunPowdered.musketBlocker")]
        static string Blocker()
        {
            ModLog.Verbose("MusketTestHooks.Blocker()");
            Player p = Player.m_localPlayer;
            if (p == null) { ModLog.Verbose("MusketTestHooks.Blocker: no player - returns why"); return "no player"; }
            ItemDrop.ItemData b = GunPowdered.Integrations.HarmonyPatches.MusketOffhandBlockerPatch.Blocker(p);
            string s = "blocker " + (b != null && b.m_dropPrefab != null ? b.m_dropPrefab.name : "nothing");
            ModLog.Info("MusketTestHooks.Blocker: returns " + s);
            return s;
        }
    }
}
