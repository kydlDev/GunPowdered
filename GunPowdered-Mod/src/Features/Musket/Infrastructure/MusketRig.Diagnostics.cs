using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>MusketRig, part 2 (G04 class size): the diagnostics (trace, shake measure) and the musket-model lookup in the hand.</summary>
    internal sealed partial class MusketRig : MonoBehaviour
    {
        /// <summary>
        /// Diagnostic switch (hook GunPowdered.musketTrace): the next N frames of every OTHER player's rig are logged one line each (skip / write, how far the
        /// right upper arm and the musket model moved since the last frame, model id and parent) - for the Bjornita vibration (user 2026-11-22). 0 = off.
        /// </summary>
        internal static int TraceFrames;

        // the game's timer while a player gets up after spawning or joining (Player.SetupAwake / UpdateAwake: 0 .. 1 s, -1 otherwise, every client)
        static readonly HarmonyLib.AccessTools.FieldRef<Player, float> WakeupTimer = HarmonyLib.AccessTools.FieldRefAccess<Player, float>("m_wakeupTimer");
        Quaternion _traceArm, _traceFore, _traceHand; Vector3 _traceModel, _traceHips, _traceRoot, _traceVisual; int _traceId; bool _traceSet; readonly Vector3[] _traceLinks = new Vector3[4];

        void Trace(string what)
        {
            if (TraceFrames <= 0 || IsOwnRig) { if (RuleLog.Due("MusketRig.Trace:off", 2d)) ModLog.Every("MusketRig.Trace:off", 2d, "MusketRig.Trace: off or own rig - returns"); return; }
            TraceFrames--;
            Transform arm = _animator != null ? _animator.GetBoneTransform(HumanBodyBones.RightUpperArm) : null;
            Quaternion a = arm != null ? arm.localRotation : Quaternion.identity;
            Vector3 m = _model != null ? _character.transform.InverseTransformPoint(_model.position) : Vector3.zero;
            int id = _model != null ? _model.GetInstanceID() : 0;
            Transform fore = _animator != null ? _animator.GetBoneTransform(HumanBodyBones.RightLowerArm) : null, hand = _animator != null ? _animator.GetBoneTransform(HumanBodyBones.RightHand) : null, hips = _animator != null ? _animator.GetBoneTransform(HumanBodyBones.Hips) : null;
            Quaternion fq = fore != null ? fore.localRotation : Quaternion.identity, hq = hand != null ? hand.localRotation : Quaternion.identity;
            Vector3 hp = hips != null ? _character.transform.InverseTransformPoint(hips.position) : Vector3.zero, rp = _character.transform.position, vp = _animator != null ? _character.transform.InverseTransformPoint(_animator.transform.position) : Vector3.zero;
            string more = _traceSet ? ", fore " + Quaternion.Angle(_traceFore, fq).ToString("0.0") + ", hand " + Quaternion.Angle(_traceHand, hq).ToString("0.0") + " deg, hips " + ((hp - _traceHips).magnitude * 1000f).ToString("0.0") + " mm, root " + ((rp - _traceRoot).magnitude * 1000f).ToString("0.0") + " mm, visual " + ((vp - _traceVisual).magnitude * 1000f).ToString("0.0") + " mm" : "";
            _traceFore = fq; _traceHand = hq; _traceHips = hp; _traceRoot = rp; _traceVisual = vp;
            // where each link from the hand to the model is, in the character's space (which one moves on a skipped frame)
            Transform attachHand = _vis != null ? _vis.m_rightHand : null, attach = _model != null ? _model.parent : null, hipsUp = hips != null ? hips.parent : null;
            Vector3 hb = hand != null ? _character.transform.InverseTransformPoint(hand.position) : Vector3.zero, ah = attachHand != null ? _character.transform.InverseTransformPoint(attachHand.position) : Vector3.zero;
            Vector3 at = attach != null ? _character.transform.InverseTransformPoint(attach.position) : Vector3.zero, hu = hipsUp != null ? _character.transform.InverseTransformPoint(hipsUp.position) : Vector3.zero;
            more += ", links mm: hand " + ((hb - _traceLinks[0]).magnitude * 1000f).ToString("0.0") + ", hand attach " + ((ah - _traceLinks[1]).magnitude * 1000f).ToString("0.0") + " (" + (attachHand != null ? attachHand.parent?.name : "-") + ")"
                + ", item attach " + ((at - _traceLinks[2]).magnitude * 1000f).ToString("0.0") + " local " + (attach != null ? attach.localPosition.ToString("0.000") : "-") + ", model local " + (_model != null ? _model.localPosition.ToString("0.000") : "-")
                + ", hips parent " + ((hu - _traceLinks[3]).magnitude * 1000f).ToString("0.0") + " (" + (hipsUp != null ? hipsUp.name : "-") + ")";
            _traceLinks[0] = hb; _traceLinks[1] = ah; _traceLinks[2] = at; _traceLinks[3] = hu;
            bool zLowered = _nview != null && _nview.IsValid() && _nview.GetZDO() != null && _nview.GetZDO().GetBool(LoweredKey);
            string line = "MusketRig.Trace: " + _character.name + " frame " + Time.frameCount + " " + what + more + ", zdo lowered " + zLowered + ", arm turned " + (_traceSet ? Quaternion.Angle(_traceArm, a).ToString("0.0") : "-") + " deg, model moved "
                + (_traceSet ? ((m - _traceModel).magnitude * 1000f).ToString("0.0") : "-") + " mm, model " + id + (id != _traceId && _traceSet ? " (NEW)" : "") + " under " + (_model != null && _model.parent != null ? _model.parent.name : "-")
                + ", carry " + _carry.ToString("0.00") + ", " + (_raised ? "raised" : "lowered") + ", action " + _action + ", left " + TraceFrames;
            _traceArm = a; _traceModel = m; _traceId = id; _traceSet = true;
            ModLog.Info(line);
        }
        bool AnimatorDidNotRun()
        {
            if (RuleLog.Due("MusketRig.AnimatorDidNotRun", 2d)) ModLog.Every("MusketRig.AnimatorDidNotRun", 2d, $"MusketRig.AnimatorDidNotRun()");
            if (!_wrote || _upperL == null || _upperR == null) return false;
            { var returnedAt53 = _upperL.localRotation == _wroteL && _upperR.localRotation == _wroteR; if (RuleLog.Due("MusketRig.AnimatorDidNotRun:53", 2d)) ModLog.Every("MusketRig.AnimatorDidNotRun:53", 2d, $"MusketRig.AnimatorDidNotRun: returns {returnedAt53} (line 53)"); return returnedAt53; }
        }

        void RememberWrite()
        {
            if (RuleLog.Due("MusketRig.RememberWrite", 2d)) ModLog.Every("MusketRig.RememberWrite", 2d, $"MusketRig.RememberWrite()");
            if (_upperL == null) _upperL = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (_upperR == null) _upperR = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (_upperL == null || _upperR == null) { if (RuleLog.Due("MusketRig.RememberWrite:61", 2d)) ModLog.Every("MusketRig.RememberWrite:61", 2d, $"MusketRig.RememberWrite: returns because _upperL == null || _upperR == null (line 61)"); return; }
            _wroteL = _upperL.localRotation;
            _wroteR = _upperR.localRotation;
            _wrote = true;
        }

        void Measure()
        {
            if (RuleLog.Due("MusketRig.Measure", 2d)) ModLog.Every("MusketRig.Measure", 2d, $"MusketRig.Measure()");
            float h = _character.transform.InverseTransformPoint(_model.position).y;
            if (!float.IsNaN(_lastHeight)) _maxStep = Mathf.Max(_maxStep, Mathf.Abs(h - _lastHeight));
            _lastHeight = h;
            _minHeight = Mathf.Min(_minHeight, h);
            _maxHeight = Mathf.Max(_maxHeight, h);
            _measured++;
            Transform body = _character.transform;
            Transform hips = _animator.GetBoneTransform(HumanBodyBones.Hips), chest = _animator.GetBoneTransform(HumanBodyBones.UpperChest), shoulder = _animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            if (hips != null) { float y = body.InverseTransformPoint(hips.position).y; _hipMin = Mathf.Min(_hipMin, y); _hipMax = Mathf.Max(_hipMax, y); }
            if (chest != null) { float y = body.InverseTransformPoint(chest.position).y; _chestMin = Mathf.Min(_chestMin, y); _chestMax = Mathf.Max(_chestMax, y); }
            if (shoulder != null) { float y = body.InverseTransformPoint(shoulder.position).y; _shoulderMin = Mathf.Min(_shoulderMin, y); _shoulderMax = Mathf.Max(_shoulderMax, y); }
        }

        /// <summary>Starts a new shake measurement.</summary>
        internal void ResetMeasure()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketRig.ResetMeasure()");
            _lastHeight = float.NaN;
            _minHeight = float.MaxValue;
            _maxHeight = float.MinValue;
            _maxStep = 0f;
            _measured = 0;
            _hipMin = _chestMin = _shoulderMin = float.MaxValue;
            _hipMax = _chestMax = _shoulderMax = float.MinValue;
            SkippedFrames = 0;
        }

        /// <summary>How much the musket moved up and down since ResetMeasure: "frames N, skipped S, range R mm, max step M mm".</summary>
        internal string ShakeReport()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketRig.ShakeReport()");
            float range = _measured > 0 ? (_maxHeight - _minHeight) * 1000f : 0f;
            string report = $"frames {_measured}, skipped {SkippedFrames}, range {range:0.0} mm, max step {_maxStep * 1000f:0.0} mm; hips {Range(_hipMin, _hipMax)}, chest {Range(_chestMin, _chestMax)}, shoulder {Range(_shoulderMin, _shoulderMax)}";
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketRig.ShakeReport: returns {report}");
            return report;
        }

        static string Range(float min, float max)
        {
            if (RuleLog.Due("MusketRig.Range", 2d)) ModLog.Every("MusketRig.Range", 2d, $"MusketRig.Range(min={RuleLog.Shown(min)}, max={RuleLog.Shown(max)})");
            return max >= min ? ((max - min) * 1000f).ToString("0.0") + " mm" : "n/a";
        }

        bool EnsureHandler()
        {
            if (_handler != null) { if (RuleLog.Due("MusketRig.EnsureHandler:115", 2d)) ModLog.Every("MusketRig.EnsureHandler:115", 2d, $"MusketRig.EnsureHandler: returns true because _handler != null (line 115)"); return true; }
            _animator = _character.GetComponentInChildren<Animator>();
            if (_animator == null || !_animator.isHuman || _animator.avatar == null)
            {
                ModLog.Warn($"MusketRig: {_character.name} has no humanoid animator - the musket keeps the crossbow hold");
                return _handler != null;
            }
            _handler = new HumanPoseHandler(_animator.avatar, _animator.transform);
            _pose = new HumanPose();
            ModLog.Verbose($"MusketRig: pose handler for {_character.name} (avatar {_animator.avatar.name})");
            return _handler != null;
        }

        static readonly HarmonyLib.AccessTools.FieldRef<VisEquipment, int> LeftHash = HarmonyLib.AccessTools.FieldRefAccess<VisEquipment, int>("m_currentLeftItemHash");
        static readonly HarmonyLib.AccessTools.FieldRef<VisEquipment, int> RightHash = HarmonyLib.AccessTools.FieldRefAccess<VisEquipment, int>("m_currentRightItemHash");
        static readonly int MusketHash = GunPowdered.Core.GunPowderedPrefabs.Musket.GetStableHashCode();

        /// <summary>
        /// True while this player holds the musket model. The game puts a Bow-type weapon in the LEFT hand; the musket poses are authored with the musket in the
        /// right hand (on the trigger), so the attach instance is moved over to the right hand once per equip (the game still destroys it on unequip).
        /// </summary>
        bool FindModel()
        {
            // D-147 / PROJECT_RULES 38: every player runs this every frame - without a musket equipped (the game's own item hashes) it ends at once
            if (LeftHash(_vis) != MusketHash && RightHash(_vis) != MusketHash) { _model = null; if (RuleLog.Due("MusketRig.FindModel:none", 2d)) ModLog.Every("MusketRig.FindModel:none", 2d, "MusketRig.FindModel: no musket equipped - returns false"); return false; }
            if (_model != null && _model.gameObject.activeInHierarchy && _model.IsChildOf(_vis.m_rightHand)) { if (RuleLog.Due("MusketRig.FindModel:140", 2d)) ModLog.Every("MusketRig.FindModel:140", 2d, $"MusketRig.FindModel: returns true because _model != null && _model.gameObject.activeInHierarchy && _model.IsChildOf(_vis.m_rightHand) (line 140)"); return true; }
            _model = null;
            Transform found = Search(_vis.m_rightHand) ?? Search(_vis.m_leftHand);
            if (found == null) { if (RuleLog.Due("MusketRig.FindModel:143", 2d)) ModLog.Every("MusketRig.FindModel:143", 2d, $"MusketRig.FindModel: returns false because found == null (line 143)"); return false; }
            Transform attach = found.parent;
            if (!attach.IsChildOf(_vis.m_rightHand))
            {
                attach.SetParent(_vis.m_rightHand, false);
                attach.localPosition = Vector3.zero;
                attach.localRotation = Quaternion.identity;
                ModLog.Verbose($"MusketRig: {_character.name} - musket moved from the left hand to the right hand ({attach.name})");
            }
            _model = found;
            _rod = null;
            foreach (Transform c in _model.GetComponentsInChildren<Transform>(true)) if (c.name.StartsWith(RodName)) _rod = c;
            if (_rod != null) _rodHome = _rod.localPosition;
            ModLog.Verbose($"MusketRig: {_character.name} musket found, ramrod {(_rod != null ? _rod.name + " at " + _rodHome.ToString("0.000") + " under " + _rod.parent.name : "missing")}");
            return _model.IsChildOf(_vis.m_rightHand);
        }

        static Transform Search(Transform hand)
        {
            if (RuleLog.Due("MusketRig.Search", 2d)) ModLog.Every("MusketRig.Search", 2d, $"MusketRig.Search(hand={RuleLog.Shown(hand)})");
            if (hand == null) return null;
            foreach (Transform attach in hand)
            {
                Transform m = attach.name.StartsWith("attach") ? attach.Find(ModelName) : null;
                if (m != null && HasChild(m, BarrelName)) { var returnedAt167 = m; if (RuleLog.Due("MusketRig.Search:167", 2d)) ModLog.Every("MusketRig.Search:167", 2d, $"MusketRig.Search: returns {returnedAt167} because m != null && HasChild(m, BarrelName) (line 167)"); return returnedAt167; }
            }
            { if (RuleLog.Due("MusketRig.Search:169", 2d)) ModLog.Every("MusketRig.Search:169", 2d, $"MusketRig.Search: returns null (line 169)"); return null; }
        }

        static bool HasChild(Transform root, string name)
        {
            if (RuleLog.Due("MusketRig.HasChild", 2d)) ModLog.Every("MusketRig.HasChild", 2d, $"MusketRig.HasChild(root={RuleLog.Shown(root)}, name={RuleLog.Shown(name)})");
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith(name)) return true;
            { if (RuleLog.Due("MusketRig.HasChild:176", 2d)) ModLog.Every("MusketRig.HasChild:176", 2d, $"MusketRig.HasChild: returns false (line 176)"); return false; }
        }
    }
}
