using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The musket animations (D-092). Sits on every player; while that player holds the Iron Musket it writes the arm and finger muscles after the game's
    /// own animation each frame (<see cref="MusketPoseRules"/>): the musket at the right shoulder, the right hand on the trigger, the left hand under the
    /// fore-stock - or across the chest while running, the recoil after a shot, the ramrod strokes of a reload. Legs, hips and spine keep the game's crossbow
    /// animation, so walking, running and turning still work, and the chest follows where the player looks (up / down). Runs on every client for every
    /// player, so all players with the mod see it.
    /// </summary>
    internal sealed class MusketRig : MonoBehaviour
    {
        /// <summary>How far the chest turns the musket up or down with the look, at most (degrees).</summary>
        internal const float MaxAimPitch = 40f;

        const string ModelName = "gp_model", BarrelName = "musket_barrel", RodName = "musket_ramrod";
        // D-098: the shooter's action (hold / fire / reload) reaches the other clients through the player's own ZDO: the action and a counter that
        // grows with every Play, so a second shot of the same kind still restarts the pose there
        internal const string ActionKey = "gp_musket_action", ActionSeqKey = "gp_musket_seq";

        readonly float[] _muscles = new float[MusketPoses.Count];
        Character _character;
        VisEquipment _vis;
        ZNetView _nview;
        int _seq, _seenSeq = -1;
        // D-099: the arm bones as this rig left them; equal next frame = the animator did not run (game paused with ESC, frozen frames), so the
        // pose must NOT be read back and turned again (it wound the upper body down and back while paused, user 2026-11-19 12:32)
        Transform _upperL, _upperR;
        Quaternion _wroteL, _wroteR;
        bool _wrote;
        /// <summary>Frames left alone because the animator did not run (for test hooks).</summary>
        internal int SkippedFrames { get; private set; }
        /// <summary>Test switch: true = the old behaviour (pose every frame) to compare (hook GunPowdered.musketSkipGuard).</summary>
        internal static bool NoSkipGuard;
        // the musket's height in the player's space, frame by frame (shake measurement)
        float _lastHeight = float.NaN, _minHeight = float.MaxValue, _maxHeight = float.MinValue, _maxStep;
        int _measured;
        // D-099: the idle animation bends the spine with every breath, which bobs a shouldered musket about 6 cm; while aiming, the spine muscles
        // (0..8: spine, chest, upper chest) follow the animation only slowly, so the posture stays and the breathing tilt goes
        internal const int SpineMuscles = 9;
        internal const float SpineSeconds = 1.5f;
        readonly float[] _spine = new float[SpineMuscles];
        bool _spineSet;
        float _lastPosed;
        float _hipMin = float.MaxValue, _hipMax = float.MinValue, _chestMin = float.MaxValue, _chestMax = float.MinValue, _shoulderMin = float.MaxValue, _shoulderMax = float.MinValue;
        Animator _animator;
        HumanPoseHandler _handler;
        HumanPose _pose;
        Transform _model, _rod;
        Vector3 _rodHome;
        float _carry, _actionStarted;
        bool _wasHolding;
        MusketAction _action = MusketAction.Hold;

        /// <summary>What the rig did in the last frame (for test hooks).</summary>
        internal string LastReport { get; private set; } = "not posed yet";

        /// <summary>The action the arms play now.</summary>
        internal MusketAction CurrentAction => _action;

        /// <summary>Puts the rig on a player (once). Expected outcome: the player has exactly one rig.</summary>
        internal static MusketRig AttachTo(Character character)
        {
            if (character == null) { if (RuleLog.On) ModLog.Every("MusketRig.AttachTo:68", 2d, "MusketRig.AttachTo: returns null (line 68)"); return null; }
            MusketRig rig = character.GetComponent<MusketRig>() ?? character.gameObject.AddComponent<MusketRig>();
            rig._character = character;
            rig._vis = character.GetComponent<VisEquipment>();
            rig._nview = character.GetComponent<ZNetView>();
            ModLog.Verbose($"MusketRig: on {character.name}, visuals {(rig._vis != null)}");
            return rig;
        }

        /// <summary>Where the muzzle of the musket in this player's hand is (world). Expected outcome: false while the player holds no musket model.</summary>
        internal bool TryMuzzle(out Vector3 at)
        {
            if (RuleLog.On) ModLog.Every("MusketRig.TryMuzzle", 2d, $"MusketRig.TryMuzzle()");
            at = Vector3.zero;
            if (_model == null || !_wasHolding) { if (RuleLog.On) ModLog.Every("MusketRig.TryMuzzle:82", 2d, "MusketRig.TryMuzzle: returns false (line 82)"); return false; }
            at = _model.TransformPoint(new Vector3(0f, 0f, MusketPoses.MuzzleZ));
            { if (RuleLog.On) ModLog.Every("MusketRig.TryMuzzle:84", 2d, "MusketRig.TryMuzzle: returns true (line 84)"); return true; }
        }

        /// <summary>Starts an action (a shot's recoil, a reload) for this player; Hold ends it.</summary>
        internal void Play(MusketAction action)
        {
            ModLog.Verbose($"MusketRig: {_character?.name} {_action} -> {action}");
            _action = action;
            _actionStarted = Time.time;
            Share(action);
        }

        // the owner (the player's own client) writes the action for everyone else
        void Share(MusketAction action)
        {
            if (RuleLog.On) ModLog.Every("MusketRig.Share", 2d, $"MusketRig.Share(action={RuleLog.Shown(action)})");
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner()) { if (RuleLog.On) ModLog.Every("MusketRig.Share:100", 2d, "MusketRig.Share: returns (line 100)"); return; }
            ZDO zdo = _nview.GetZDO();
            _seq = zdo.GetInt(ActionSeqKey) + 1;
            zdo.Set(ActionKey, (int)action);
            zdo.Set(ActionSeqKey, _seq);
            _seenSeq = _seq;
        }

        // another player's rig: start the action that player's client wrote (once per counter step). Expected outcome: true when a new action began.
        bool FollowOwner()
        {
            if (_nview == null || !_nview.IsValid() || _nview.IsOwner()) { if (RuleLog.On) ModLog.Every("MusketRig.FollowOwner:111", 2d, "MusketRig.FollowOwner: returns false (line 111)"); return false; }
            ZDO zdo = _nview.GetZDO();
            int seq = zdo.GetInt(ActionSeqKey, -1);
            if (seq < 0 || seq == _seenSeq) { if (RuleLog.On) ModLog.Every("MusketRig.FollowOwner:114", 2d, "MusketRig.FollowOwner: returns false (line 114)"); return false; }
            bool first = _seenSeq < 0;
            _seenSeq = seq;
            if (first) { if (RuleLog.On) ModLog.Every("MusketRig.FollowOwner:117", 2d, "MusketRig.FollowOwner: returns false (line 117)"); return false; }                 // joining late: the past action is not replayed
            _action = (MusketAction)zdo.GetInt(ActionKey);
            _actionStarted = Time.time;
            ModLog.Verbose($"MusketRig: {_character?.name} plays {_action} from its owner (step {seq})");
            return true;
        }

        void LateUpdate()
        {
            if (_character != null && _character == Player.m_localPlayer)
            {
                MusketAmmoHud.Refresh(Player.m_localPlayer);
                MusketInput.Poll(Player.m_localPlayer);
            }
            bool holding = _character != null && _vis != null && _vis.m_rightHand != null && FindModel();
            if (holding != _wasHolding) ModLog.Verbose($"MusketRig: {_character?.name} {(holding ? "holds" : "no longer holds")} the musket");
            _wasHolding = holding;
            if (!holding || !EnsureHandler())
            {
                _wrote = false;
                _spineSet = false;
                { if (RuleLog.On) ModLog.Every("MusketRig.LateUpdate:138", 2d, "MusketRig.LateUpdate: returns (line 138)"); return; }
            }
            if (!NoSkipGuard && AnimatorDidNotRun())
            {
                SkippedFrames++;
                { if (RuleLog.On) ModLog.Every("MusketRig.LateUpdate:143", 2d, "MusketRig.LateUpdate: returns (line 143)"); return; }
            }

            FollowOwner();
            float seconds = Time.time - _actionStarted;
            if (_action == MusketAction.Fire && seconds > MusketPoseRules.FireSeconds) _action = MusketAction.Hold;
            Vector3 v = _character.GetVelocity();
            _carry = MusketPoseRules.NextCarry(_carry, new Vector3(v.x, 0f, v.z).magnitude, Time.deltaTime);
            if (!MusketPoseRules.Evaluate(_action, seconds, _carry, _muscles)) { if (RuleLog.On) ModLog.Every("MusketRig.LateUpdate:151", 2d, "MusketRig.LateUpdate: returns (line 151)"); return; }

            _handler.GetHumanPose(ref _pose);
            System.Array.Copy(_muscles, 0, _pose.muscles, MusketPoses.First, MusketPoses.Count);
            SteadySpine(MusketPoseRules.SpineSteadiness(_action, _carry));
            _handler.SetHumanPose(ref _pose);
            float pitch = AimChest();
            float w = MusketPoseRules.ReloadWeight(_action, seconds);
            PlaceModel(w, seconds);
            RememberWrite();
            Measure();
            LastReport = $"{_action} {seconds:0.00} s, carry {_carry:0.00}, chest pitch {pitch:0.0}, reload grip {w:0.00}";
            ModLog.Every("musket-rig-" + _character.GetInstanceID(), 2f, "MusketRig: " + _character.name + " " + LastReport);
        }

        /// <summary>Turns the upper chest so the shouldered musket follows the look up or down (not while running or reloading). Returns the pitch used.</summary>
        float AimChest()
        {
            if (RuleLog.On) ModLog.Every("MusketRig.AimChest", 2d, $"MusketRig.AimChest()");
            if (_action == MusketAction.Reload || _carry > 0.5f) { if (RuleLog.On) ModLog.Every("MusketRig.AimChest:170", 2d, "MusketRig.AimChest: returns 0f (line 170)"); return 0f; }
            Transform chest = _animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? _animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null) { if (RuleLog.On) ModLog.Every("MusketRig.AimChest:172", 2d, "MusketRig.AimChest: returns 0f (line 172)"); return 0f; }
            Transform body = _character.transform;
            Vector3 look = _character.GetLookDir();
            Vector3 flat = Vector3.ProjectOnPlane(look, body.up);
            if (flat.sqrMagnitude < 1e-4f) { if (RuleLog.On) ModLog.Every("MusketRig.AimChest:176", 2d, "MusketRig.AimChest: returns 0f (line 176)"); return 0f; }
            float pitch = Mathf.Clamp(Vector3.SignedAngle(flat.normalized, look, body.right), -MaxAimPitch, MaxAimPitch) * (1f - _carry);
            chest.rotation = Quaternion.AngleAxis(pitch, body.right) * chest.rotation;
            { var returnedAt179 = pitch; if (RuleLog.On) ModLog.Every("MusketRig.AimChest:179", 2d, $"MusketRig.AimChest: returns {RuleLog.Shown(returnedAt179)} (line 179)"); return returnedAt179; }
        }

        /// <summary>Puts the musket in the right hand: the aiming grip, or (w = 1) the reload grip on the barrel with the ramrod in the bore, its top in the left fist.</summary>
        void PlaceModel(float w, float seconds)
        {
            if (RuleLog.On) ModLog.Every("MusketRig.PlaceModel", 2d, $"MusketRig.PlaceModel(w={RuleLog.Shown(w)}, seconds={RuleLog.Shown(seconds)})");
            float[] a = MusketPoses.GripHold, b = MusketPoses.GripReload;
            _model.localPosition = Vector3.Lerp(new Vector3(a[0], a[1], a[2]), new Vector3(b[0], b[1], b[2]), w);
            _model.localRotation = Quaternion.Slerp(Quaternion.Euler(a[3], a[4], a[5]), Quaternion.Euler(b[3], b[4], b[5]), w);
            if (_rod == null) { if (RuleLog.On) ModLog.Every("MusketRig.PlaceModel:189", 2d, "MusketRig.PlaceModel: returns (line 189)"); return; }
            var rammed = new Vector3(_rodHome.x, _rodHome.y - MusketPoses.RodPipeY, _rodHome.z + MusketPoses.MuzzleZ + MusketPoseRules.RodAboveMuzzle(seconds) - MusketPoses.RodTopZ);
            _rod.localPosition = w > 0.5f ? rammed : _rodHome;
        }

        // steadiness 0 = the animation's spine as it is, 1 = only its slow average (the breathing filtered out)
        void SteadySpine(float steadiness)
        {
            if (RuleLog.On) ModLog.Every("MusketRig.SteadySpine", 2d, $"MusketRig.SteadySpine(steadiness={RuleLog.Shown(steadiness)})");
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

        bool AnimatorDidNotRun()
        {
            if (RuleLog.On) ModLog.Every("MusketRig.AnimatorDidNotRun", 2d, $"MusketRig.AnimatorDidNotRun()");
            if (!_wrote || _upperL == null || _upperR == null) { if (RuleLog.On) ModLog.Every("MusketRig.AnimatorDidNotRun:213", 2d, "MusketRig.AnimatorDidNotRun: returns false (line 213)"); return false; }
            { var returnedAt214 = _upperL.localRotation == _wroteL && _upperR.localRotation == _wroteR; if (RuleLog.On) ModLog.Every("MusketRig.AnimatorDidNotRun:214", 2d, $"MusketRig.AnimatorDidNotRun: returns {RuleLog.Shown(returnedAt214)} (line 214)"); return returnedAt214; }
        }

        void RememberWrite()
        {
            if (RuleLog.On) ModLog.Every("MusketRig.RememberWrite", 2d, $"MusketRig.RememberWrite()");
            if (_upperL == null) _upperL = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (_upperR == null) _upperR = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (_upperL == null || _upperR == null) { if (RuleLog.On) ModLog.Every("MusketRig.RememberWrite:222", 2d, "MusketRig.RememberWrite: returns (line 222)"); return; }
            _wroteL = _upperL.localRotation;
            _wroteR = _upperR.localRotation;
            _wrote = true;
        }

        void Measure()
        {
            if (RuleLog.On) ModLog.Every("MusketRig.Measure", 2d, $"MusketRig.Measure()");
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
            if (RuleLog.On) ModLog.Verbose($"MusketRig.ResetMeasure()");
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
            if (RuleLog.On) ModLog.Verbose($"MusketRig.ShakeReport()");
            float range = _measured > 0 ? (_maxHeight - _minHeight) * 1000f : 0f;
            string report = $"frames {_measured}, skipped {SkippedFrames}, range {range:0.0} mm, max step {_maxStep * 1000f:0.0} mm; hips {Range(_hipMin, _hipMax)}, chest {Range(_chestMin, _chestMax)}, shoulder {Range(_shoulderMin, _shoulderMax)}";
            if (RuleLog.On) ModLog.Verbose($"MusketRig.ShakeReport: returns {report}");
            return report;
        }

        static string Range(float min, float max)
        {
            if (RuleLog.On) ModLog.Every("MusketRig.Range", 2d, $"MusketRig.Range(min={RuleLog.Shown(min)}, max={RuleLog.Shown(max)})");
            { var returnedAt269 = max >= min ? ((max - min) * 1000f).ToString("0.0") + " mm" : "n/a"; if (RuleLog.On) ModLog.Every("MusketRig.Range:269", 2d, $"MusketRig.Range: returns {RuleLog.Shown(returnedAt269)} (line 269)"); return returnedAt269; }
        }

        bool EnsureHandler()
        {
            if (_handler != null) { if (RuleLog.On) ModLog.Every("MusketRig.EnsureHandler:274", 2d, "MusketRig.EnsureHandler: returns true (line 274)"); return true; }
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

        /// <summary>
        /// True while this player holds the musket model. The game puts a Bow-type weapon in the LEFT hand; the musket poses are authored with the musket in the
        /// right hand (on the trigger), so the attach instance is moved over to the right hand once per equip (the game still destroys it on unequip).
        /// </summary>
        bool FindModel()
        {
            if (_model != null && _model.gameObject.activeInHierarchy && _model.IsChildOf(_vis.m_rightHand)) { if (RuleLog.On) ModLog.Every("MusketRig.FindModel:293", 2d, "MusketRig.FindModel: returns true (line 293)"); return true; }
            _model = null;
            Transform found = Search(_vis.m_rightHand) ?? Search(_vis.m_leftHand);
            if (found == null) { if (RuleLog.On) ModLog.Every("MusketRig.FindModel:296", 2d, "MusketRig.FindModel: returns false (line 296)"); return false; }
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
            if (RuleLog.On) ModLog.Every("MusketRig.Search", 2d, $"MusketRig.Search(hand={RuleLog.Shown(hand)})");
            if (hand == null) { if (RuleLog.On) ModLog.Every("MusketRig.Search:316", 2d, "MusketRig.Search: returns null (line 316)"); return null; }
            foreach (Transform attach in hand)
            {
                Transform m = attach.name.StartsWith("attach") ? attach.Find(ModelName) : null;
                if (m != null && HasChild(m, BarrelName)) { var returnedAt320 = m; if (RuleLog.On) ModLog.Every("MusketRig.Search:320", 2d, $"MusketRig.Search: returns {RuleLog.Shown(returnedAt320)} (line 320)"); return returnedAt320; }
            }
            { if (RuleLog.On) ModLog.Every("MusketRig.Search:322", 2d, "MusketRig.Search: returns null (line 322)"); return null; }
        }

        static bool HasChild(Transform root, string name)
        {
            if (RuleLog.On) ModLog.Every("MusketRig.HasChild", 2d, $"MusketRig.HasChild(root={RuleLog.Shown(root)}, name={RuleLog.Shown(name)})");
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith(name)) { if (RuleLog.On) ModLog.Every("MusketRig.HasChild:328", 2d, "MusketRig.HasChild: returns true (line 328)"); return true; }
            { if (RuleLog.On) ModLog.Every("MusketRig.HasChild:329", 2d, "MusketRig.HasChild: returns false (line 329)"); return false; }
        }

        void OnDestroy() => _handler?.Dispose();
    }
}
