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
        internal const string ActionKey = "gp_musket_action", ActionSeqKey = "gp_musket_seq", StrokeKey = "gp_musket_stroke";

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
            if (character == null) { if (RuleLog.Due("MusketRig.AttachTo:68", 2d)) ModLog.Every("MusketRig.AttachTo:68", 2d, "MusketRig.AttachTo: returns null (line 68)"); return null; }
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
            if (RuleLog.Due("MusketRig.TryMuzzle", 2d)) ModLog.Every("MusketRig.TryMuzzle", 2d, $"MusketRig.TryMuzzle()");
            at = Vector3.zero;
            if (_model == null || !_wasHolding) { if (RuleLog.Due("MusketRig.TryMuzzle:82", 2d)) ModLog.Every("MusketRig.TryMuzzle:82", 2d, "MusketRig.TryMuzzle: returns false (line 82)"); return false; }
            at = _model.TransformPoint(new Vector3(0f, 0f, MusketPoses.MuzzleZ));
            { if (RuleLog.Due("MusketRig.TryMuzzle:84", 2d)) ModLog.Every("MusketRig.TryMuzzle:84", 2d, "MusketRig.TryMuzzle: returns true (line 84)"); return true; }
        }

        /// <summary>Starts an action (a shot's recoil, a reload) for this player; Hold ends it.</summary>
        internal void Play(MusketAction action, float strokeSeconds = MusketPoseRules.ReloadSecondsPerCartridge)
        {
            ModLog.Verbose($"MusketRig: {_character?.name} {_action} -> {action}" + (action == MusketAction.Reload ? " (" + strokeSeconds.ToString("0.00") + " s per cartridge)" : ""));
            _action = action;
            _stroke = strokeSeconds;
            _actionStarted = Time.time;
            Share(action);
        }

        // the owner (the player's own client) writes the action for everyone else
        void Share(MusketAction action)
        {
            if (RuleLog.Due("MusketRig.Share", 2d)) ModLog.Every("MusketRig.Share", 2d, $"MusketRig.Share(action={RuleLog.Shown(action)})");
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner()) { if (RuleLog.Due("MusketRig.Share:100", 2d)) ModLog.Every("MusketRig.Share:100", 2d, "MusketRig.Share: returns (line 100)"); return; }
            ZDO zdo = _nview.GetZDO();
            _seq = zdo.GetInt(ActionSeqKey) + 1;
            zdo.Set(ActionKey, (int)action);
            zdo.Set(StrokeKey, _stroke);                       // D-157: the reload stroke the Muskets skill gave (other clients play the same length)
            zdo.Set(ActionSeqKey, _seq);
            _seenSeq = _seq;
        }

        // another player's rig: start the action that player's client wrote (once per counter step). Expected outcome: true when a new action began.
        bool FollowOwner()
        {
            if (_nview == null || !_nview.IsValid() || _nview.IsOwner()) { if (RuleLog.Due("MusketRig.FollowOwner:111", 2d)) ModLog.Every("MusketRig.FollowOwner:111", 2d, "MusketRig.FollowOwner: returns false (line 111)"); return false; }
            ZDO zdo = _nview.GetZDO();
            int seq = zdo.GetInt(ActionSeqKey, MusketPoseRules.NoActionYet);
            // joining late: a past action is not replayed; but the shooter's very first action is (it was swallowed before, lab a17bjornita3)
            if (!MusketPoseRules.IsNewAction(seq, _seenSeq, out _seenSeq)) { if (RuleLog.Due("MusketRig.FollowOwner:114", 2d)) ModLog.Every("MusketRig.FollowOwner:114", 2d, "MusketRig.FollowOwner: returns false (line 114)"); return false; }
            _action = (MusketAction)zdo.GetInt(ActionKey);
            _stroke = zdo.GetFloat(StrokeKey, MusketPoseRules.ReloadSecondsPerCartridge);
            _actionStarted = Time.time;
            ModLog.Verbose($"MusketRig: {_character?.name} plays {_action} from its owner (step {seq})");
            return true;
        }

        // ---- D-155: raised (aimed at the shoulder) or lowered (at the right side). The owner decides; everyone else reads it from the player's ZDO.

        /// <summary>D-155: true while the musket hangs at the side, in the player's ZDO (written by the player's own client).</summary>
        internal const string LoweredKey = "gp_musket_lowered";

        // the left arm and left fingers in the pose arrays (MusketPoseRules.IsLeftArm), worked out once
        static readonly bool[] LeftMask = BuildLeftMask();
        bool _raised = true;
        static readonly HarmonyLib.AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> RightItem = HarmonyLib.AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_rightItem");

        static bool[] BuildLeftMask()
        {
            var mask = new bool[MusketPoses.Count];
            for (int i = 0; i < mask.Length; i++) mask[i] = MusketPoseRules.IsLeftArm(i);
            ModLog.Verbose("MusketRig.BuildLeftMask: " + MusketPoses.Count + " pose muscles mapped");
            return mask;
        }

        /// <summary>D-155: true while the musket is raised to the shoulder (it may be still coming up).</summary>
        internal bool Raised => _raised;

        /// <summary>D-155: the musket is raised and the raise animation has finished, so it may fire.</summary>
        internal bool ReadyToFire => MusketPoseRules.ReadyToFire(_raised, _carry);

        bool IsOwnRig => _nview == null || !_nview.IsValid() || _nview.IsOwner();

        /// <summary>Test switch (hook GunPowdered.musketTestSpeed): a ground speed (m/s) used instead of the player's real one; below 0 = off.</summary>
        internal static float TestSpeed = -1f;

        float _runningFor;
        float _stroke = MusketPoseRules.ReloadSecondsPerCartridge;
        bool _itemWasHeld;

        // fast right now (one frame) - Running() adds the time it has lasted
        bool FastNow()
        {
            Vector3 v = _character.GetVelocity();
            float speed = TestSpeed >= 0f && _character == Player.m_localPlayer ? TestSpeed : new Vector3(v.x, 0f, v.z).magnitude;
            bool fast = speed > MusketPoseRules.CarrySpeed;
            if (RuleLog.Due("MusketRig.FastNow", 2d)) ModLog.Every("MusketRig.FastNow", 2d, "MusketRig.FastNow: returns " + fast);
            return fast;
        }

        // running = fast for at least MusketPoseRules.LowerAfterSeconds (lab a17full1: a speed spike at a test's start lowered a fresh musket)
        bool Running()
        {
            bool running = MusketPoseRules.RunsLongEnough(_runningFor);
            if (RuleLog.Due("MusketRig.Running", 2d)) ModLog.Every("MusketRig.Running", 2d, "MusketRig.Running: returns " + running + " (fast for " + _runningFor.ToString("0.00") + " s)");
            return running;
        }

        // the owner lowers the musket while running; another player's rig reads the owner's choice from the ZDO
        void UpdateRaised()
        {
            if (IsOwnRig)
            {
                _runningFor = FastNow() ? _runningFor + Time.deltaTime : 0f;
                bool next = MusketPoseRules.NextRaised(_raised, Running(), false);
                if (next != _raised) { _raised = next; ShareRaised(); }
                if (RuleLog.Due("MusketRig.UpdateRaised:own", 2d)) ModLog.Every("MusketRig.UpdateRaised:own", 2d, "MusketRig.UpdateRaised: own rig, raised " + _raised);
                return;
            }
            bool raised = !_nview.GetZDO().GetBool(LoweredKey);
            if (raised != _raised) ModLog.Verbose($"MusketRig: {_character?.name} {(raised ? "raises" : "lowers")} the musket (from its owner)");
            _raised = raised;
        }

        void ShareRaised()
        {
            ModLog.Verbose($"MusketRig: {_character?.name} musket {(_raised ? "raised" : "lowered")}");
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner()) { if (RuleLog.Due("MusketRig.ShareRaised:none", 2d)) ModLog.Every("MusketRig.ShareRaised:none", 2d, "MusketRig.ShareRaised: not the owner - nothing written"); return; }
            _nview.GetZDO().Set(LoweredKey, !_raised);
            // the game's animation state follows: one-handed / left torch at the side (left arm free), the musket's own state when raised (synced by the game)
            GunPowdered.Integrations.HarmonyPatches.MusketAnimationStatePatch.Refresh(_character as Humanoid);
        }

        /// <summary>
        /// D-155: the aim key. Raises the musket from the side with the raise animation (not while running) and takes off whatever the left hand holds, so the
        /// musket is the only thing equipped. Expected outcome: "raising", or why not ("running", "already aimed", "not the owner").
        /// </summary>
        internal string Aim()
        {
            if (!IsOwnRig) { ModLog.Verbose("MusketRig.Aim: not the owner - returns why"); return "not the owner"; }
            if (_raised) { ModLog.Verbose("MusketRig.Aim: already aimed - returns why"); return "already aimed"; }
            if (Running()) { ModLog.Verbose("MusketRig.Aim: running - returns why"); return "running"; }
            _raised = MusketPoseRules.NextRaised(false, false, true);
            ShareRaised();
            string off = UnequipOffhand();
            ModLog.Info("MusketRig: " + _character.name + " raises the musket" + off);
            return "raising";
        }

        // whatever the player holds next to the musket (a torch, D-155) is taken off when the musket comes up
        string UnequipOffhand()
        {
            var player = _character as Humanoid;
            ItemDrop.ItemData right = player != null ? RightItem(player) : null;
            if (right == null || right == MusketGun.Held(player as Player)) { ModLog.Verbose("MusketRig.UnequipOffhand: nothing in the other hand"); return ""; }
            player.UnequipItem(right, true);
            ModLog.Verbose("MusketRig.UnequipOffhand: took off " + right.m_shared.m_name);
            return ", took off " + right.m_shared.m_name;
        }

        static readonly HarmonyLib.AccessTools.FieldRef<VisEquipment, GameObject> RightInstance = HarmonyLib.AccessTools.FieldRefAccess<VisEquipment, GameObject>("m_rightItemInstance");

        /// <summary>D-155: the item next to the musket (a torch) goes into the LEFT hand, because the musket's model is in the right hand.</summary>
        void MoveOffhandItem()
        {
            GameObject other = _vis != null ? RightInstance(_vis) : null;
            if (other == null || _vis.m_leftHand == null || other.transform.parent == _vis.m_leftHand || (_model != null && _model.IsChildOf(other.transform))) { if (RuleLog.Due("MusketRig.MoveOffhandItem", 2d)) ModLog.Every("MusketRig.MoveOffhandItem", 2d, "MusketRig.MoveOffhandItem: nothing to move"); return; }
            Vector3 lp = other.transform.localPosition;
            Quaternion lr = other.transform.localRotation;
            other.transform.SetParent(_vis.m_leftHand, false);
            other.transform.localPosition = lp;
            other.transform.localRotation = lr;
            ModLog.Verbose($"MusketRig: {_character?.name} - {other.name} moved to the left hand (the musket is in the right)");
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
            // the equipped musket ITEM (not its model: the game rebuilds the model when a torch or shield is equipped next to it)
            bool itemHeld = IsOwnRig && _character is Player owner && MusketGun.Held(owner) != null;
            bool takenUp = itemHeld && !_itemWasHeld;
            _itemWasHeld = itemHeld;
            if (takenUp && !_raised)
            {
                // a musket taken in hand starts raised, also when it was put away while it hung at the side (lab a17full1: the next test found it lowered)
                _raised = true;
                _carry = 0f;
                _runningFor = 0f;
                ShareRaised();
                ModLog.Info("MusketRig: " + _character.name + " took the musket in hand - raised");
            }
            _wasHolding = holding;
            if (!holding || !EnsureHandler())
            {
                Trace(holding ? "no pose handler" : "not holding");
                _wrote = false;
                _spineSet = false;
                { if (RuleLog.Due("MusketRig.LateUpdate:138", 2d)) ModLog.Every("MusketRig.LateUpdate:138", 2d, "MusketRig.LateUpdate: returns (line 138)"); return; }
            }
            // the action, the raised / lowered state and the carry follow the owner every frame, also in frames the pose itself is skipped below
            FollowOwner();
            UpdateRaised();
            _carry = MusketPoseRules.NextCarry(_carry, _raised, Time.deltaTime);
            MoveOffhandItem();
            if (MusketPoseRules.GameOwnsPose(_animator.GetBool(WakeupParam), _character.InCutscene()))
            {
                // getting up after joining / spawning (about 1 s) or a cutscene: the game's own animation keeps the arms; the musket stays in the hand
                _wrote = false;
                _spineSet = false;
                Trace("game pose (wakeup / cutscene)");
                { if (RuleLog.Due("MusketRig.LateUpdate:wakeup", 2d)) ModLog.Every("MusketRig.LateUpdate:wakeup", 2d, "MusketRig.LateUpdate: the game's own pose (getting up or cutscene) - returns"); return; }
            }
            if (!NoSkipGuard && AnimatorDidNotRun())
            {
                SkippedFrames++;
                // the pose is skipped, but the model is placed again: something resets it on such frames (lab a17bjc2: 34.7 mm jump on every first skipped frame)
                float skipSeconds = Time.time - _actionStarted;
                PlaceModel(MusketPoseRules.ReloadWeight(_action, skipSeconds), skipSeconds);
                Trace("skip");
                { if (RuleLog.Due("MusketRig.LateUpdate:143", 2d)) ModLog.Every("MusketRig.LateUpdate:143", 2d, "MusketRig.LateUpdate: returns (line 143)"); return; }
            }

            float seconds = Time.time - _actionStarted;
            if (_action == MusketAction.Fire && seconds > MusketPoseRules.FireSeconds) _action = MusketAction.Hold;
            if (!MusketPoseRules.Evaluate(_action, seconds, _carry, _muscles, _stroke)) { if (RuleLog.Due("MusketRig.LateUpdate:151", 2d)) ModLog.Every("MusketRig.LateUpdate:151", 2d, "MusketRig.LateUpdate: returns (line 151)"); return; }

            float w = MusketPoseRules.ReloadWeight(_action, seconds);
            float leftWeight = MusketPoseRules.LeftArmWeight(_carry, w);
            _handler.GetHumanPose(ref _pose);
            for (int i = 0; i < MusketPoses.Count; i++)
            {
                int m = MusketPoses.First + i;
                // D-155: the left arm keeps the game's animation while the musket hangs at the side (the left hand is free)
                _pose.muscles[m] = LeftMask[i] ? _pose.muscles[m] + (_muscles[i] - _pose.muscles[m]) * leftWeight : _muscles[i];
            }
            SteadySpine(MusketPoseRules.SpineSteadiness(_action, _carry));
            // SetHumanPose also puts bone POSITIONS back to the avatar's: the hips (bodyPosition round-trip error that piled up where the animator did not set
            // them again, lab a17bjc2: 10-33 mm per write) and the shoulders - the game moves both shoulders sideways every frame for a female character
            // (CharacterAnimEvent.m_femaleHack); undoing that moved her arms ~35 mm and, on frames her animator skipped, back again: the vibration the user saw on
            // Bjornita (user 2026-11-22). Those positions stay where the game put them.
            Transform hipsBone = _animator.GetBoneTransform(HumanBodyBones.Hips), lShoulder = _animator.GetBoneTransform(HumanBodyBones.LeftShoulder), rShoulder = _animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            Vector3 hipsPos = hipsBone != null ? hipsBone.localPosition : Vector3.zero, lsPos = lShoulder != null ? lShoulder.localPosition : Vector3.zero, rsPos = rShoulder != null ? rShoulder.localPosition : Vector3.zero;
            Quaternion hipsRot = hipsBone != null ? hipsBone.localRotation : Quaternion.identity;
            _handler.SetHumanPose(ref _pose);
            if (hipsBone != null) { hipsBone.localPosition = hipsPos; hipsBone.localRotation = hipsRot; }
            if (lShoulder != null) lShoulder.localPosition = lsPos;
            if (rShoulder != null) rShoulder.localPosition = rsPos;
            float pitch = AimChest();
            SideArm(_carry * (1f - w));
            PlaceModel(w, seconds);
            RememberWrite();
            Measure();
            Trace("write");
            LastReport = $"{_action} {seconds:0.00} s, {(_raised ? "raised" : "lowered")}, carry {_carry:0.00}, chest pitch {pitch:0.0}, reload grip {w:0.00}, left arm {leftWeight:0.00}";
            ModLog.Every("musket-rig-" + _character.GetInstanceID(), 2f, "MusketRig: " + _character.name + " " + LastReport);
        }

        /// <summary>Turns the upper chest so the shouldered musket follows the look up or down (not while running or reloading). Returns the pitch used.</summary>
        float AimChest()
        {
            if (RuleLog.Due("MusketRig.AimChest", 2d)) ModLog.Every("MusketRig.AimChest", 2d, $"MusketRig.AimChest()");
            if (_action == MusketAction.Reload || _carry > 0.5f) { if (RuleLog.Due("MusketRig.AimChest:170", 2d)) ModLog.Every("MusketRig.AimChest:170", 2d, "MusketRig.AimChest: returns 0f (line 170)"); return 0f; }
            Transform chest = _animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? _animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null) { if (RuleLog.Due("MusketRig.AimChest:172", 2d)) ModLog.Every("MusketRig.AimChest:172", 2d, "MusketRig.AimChest: returns 0f (line 172)"); return 0f; }
            Transform body = _character.transform;
            Vector3 look = _character.GetLookDir();
            Vector3 flat = Vector3.ProjectOnPlane(look, body.up);
            if (flat.sqrMagnitude < 1e-4f) { if (RuleLog.Due("MusketRig.AimChest:176", 2d)) ModLog.Every("MusketRig.AimChest:176", 2d, "MusketRig.AimChest: returns 0f (line 176)"); return 0f; }
            float pitch = Mathf.Clamp(Vector3.SignedAngle(flat.normalized, look, body.right), -MaxAimPitch, MaxAimPitch) * (1f - _carry);
            chest.rotation = Quaternion.AngleAxis(pitch, body.right) * chest.rotation;
            { var returnedAt179 = pitch; if (RuleLog.Due("MusketRig.AimChest:179", 2d)) ModLog.Every("MusketRig.AimChest:179", 2d, $"MusketRig.AimChest: returns {RuleLog.Shown(returnedAt179)} (line 179)"); return returnedAt179; }
        }

        // ---- D-155: the side carry placed in the body's space (the animation state - one-handed, left torch, crossbow, running - bends the spine and
        // shoulder differently, so the side pose's muscles alone pointed the musket a different way in each): the right palm goes to a point beside the hip,
        // palm up under the stock, fingers across towards the body, the arm bent to it with a two-bone IK (the same as the authoring script, D-094).
        Transform _rUpper, _rLower, _rHand, _rMid, _rThumb;

        void SideArm(float weight)
        {
            if (weight <= 0.001f) { if (RuleLog.Due("MusketRig.SideArm:off", 2d)) ModLog.Every("MusketRig.SideArm:off", 2d, "MusketRig.SideArm: musket shouldered - no side IK"); return; }
            if (_rUpper == null) { _rUpper = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm); _rLower = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm); _rHand = _animator.GetBoneTransform(HumanBodyBones.RightHand); _rMid = _animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal); _rThumb = _animator.GetBoneTransform(HumanBodyBones.RightThumbProximal); }
            if (_rUpper == null || _rLower == null || _rHand == null || _rMid == null || _rThumb == null) { if (RuleLog.Due("MusketRig.SideArm:bones", 2d)) ModLog.Every("MusketRig.SideArm:bones", 2d, "MusketRig.SideArm: arm bones missing - no side IK"); return; }
            Transform body = _character.transform;
            Vector3 f = body.forward, u = body.up, r = body.right;
            Vector3 palmNow = (_rMid.position + _rHand.position) * 0.5f;
            Vector3 palmSide = _rUpper.position + r * MusketPoses.SidePalm[0] + u * MusketPoses.SidePalm[1] + f * MusketPoses.SidePalm[2];
            Vector3 palm = Vector3.Lerp(palmNow, palmSide, weight);
            Vector3 hint = _rUpper.position - u * 0.30f - f * 0.15f + r * 0.20f;
            Quaternion handNow = _rHand.rotation;
            for (int it = 0; it < 3; it++)
            {
                OrientHand(-u + f * 0.15f, -r);                            // trail arms: fingers down (curled under the stock), palm towards the thigh, wrist straight
                Quaternion side = _rHand.rotation;
                _rHand.rotation = Quaternion.Slerp(handNow, side, weight);
                Vector3 wrist = palm - (_rMid.position - _rHand.position) * 0.5f;
                Quaternion keep = _rHand.rotation;
                TwoBoneIk(wrist, hint);
                _rHand.rotation = keep;
            }
            if (RuleLog.Due("MusketRig.SideArm", 2d)) ModLog.Every("MusketRig.SideArm", 2d, "MusketRig.SideArm: weight " + weight.ToString("0.00") + ", palm miss " + ((_rMid.position + _rHand.position) * 0.5f - palm).magnitude.ToString("0.000") + " m");
        }

        // turns the right hand so its fingers point along fingers and its palm faces palmNormal (the authoring script's Orient)
        void OrientHand(Vector3 fingers, Vector3 palmNormal)
        {
            Vector3 cf = (_rMid.position - _rHand.position).normalized;
            Vector3 ct = (_rThumb.position - _rHand.position).normalized;
            Vector3 cn = Vector3.ProjectOnPlane(Vector3.Cross(cf, ct), cf).normalized;
            Vector3 dn = Vector3.ProjectOnPlane(palmNormal, fingers).normalized;
            if (cf.sqrMagnitude < 1e-6f || cn.sqrMagnitude < 1e-6f || dn.sqrMagnitude < 1e-6f) { if (RuleLog.Due("MusketRig.OrientHand:none", 2d)) ModLog.Every("MusketRig.OrientHand:none", 2d, "MusketRig.OrientHand: degenerate hand - left as it is"); return; }
            _rHand.rotation = Quaternion.LookRotation(fingers.normalized, dn) * Quaternion.Inverse(Quaternion.LookRotation(cf, cn)) * _rHand.rotation;
            if (RuleLog.Due("MusketRig.OrientHand", 2d)) ModLog.Every("MusketRig.OrientHand", 2d, "MusketRig.OrientHand: turned");
        }

        // bends upper arm and forearm so the wrist reaches target, the elbow towards hint (the authoring script's IK)
        void TwoBoneIk(Vector3 target, Vector3 hint)
        {
            Vector3 a = _rUpper.position;
            float ab = (_rLower.position - a).magnitude, bc = (_rHand.position - _rLower.position).magnitude;
            Vector3 tt = target - a;
            float at = Mathf.Clamp(tt.magnitude, Mathf.Abs(ab - bc) + 0.001f, ab + bc - 0.001f);
            Vector3 dir = tt.normalized;
            Vector3 n = Vector3.Cross(dir, hint - a).normalized;
            if (n.sqrMagnitude < 1e-6f) { if (RuleLog.Due("MusketRig.TwoBoneIk:none", 2d)) ModLog.Every("MusketRig.TwoBoneIk:none", 2d, "MusketRig.TwoBoneIk: hint on the line - no bend"); return; }
            float cosA = Mathf.Clamp((ab * ab + at * at - bc * bc) / (2f * ab * at), -1f, 1f);
            Vector3 elbow = a + Quaternion.AngleAxis(Mathf.Acos(cosA) * Mathf.Rad2Deg, n) * dir * ab;
            _rUpper.rotation = Quaternion.FromToRotation(_rLower.position - a, elbow - a) * _rUpper.rotation;
            _rLower.rotation = Quaternion.FromToRotation(_rHand.position - _rLower.position, a + dir * at - _rLower.position) * _rLower.rotation;
            if (RuleLog.Due("MusketRig.TwoBoneIk", 2d)) ModLog.Every("MusketRig.TwoBoneIk", 2d, "MusketRig.TwoBoneIk: reach " + at.ToString("0.00") + " of " + (ab + bc).ToString("0.00") + " m");
        }

        /// <summary>Puts the musket in the right hand: the aiming grip, or (w = 1) the reload grip on the barrel with the ramrod in the bore, its top in the left fist.</summary>
        void PlaceModel(float w, float seconds)
        {
            if (RuleLog.Due("MusketRig.PlaceModel", 2d)) ModLog.Every("MusketRig.PlaceModel", 2d, $"MusketRig.PlaceModel(w={RuleLog.Shown(w)}, seconds={RuleLog.Shown(seconds)})");
            float[] a = MusketPoses.GripHold, s = MusketPoses.GripSide, b = MusketPoses.GripReload;
            // shouldered -> at the side (D-155) by the carry, then -> the reload grip by the reload weight
            Vector3 p = Vector3.Lerp(new Vector3(a[0], a[1], a[2]), new Vector3(s[0], s[1], s[2]), _carry);
            Quaternion q = Quaternion.Slerp(Quaternion.Euler(a[3], a[4], a[5]), Quaternion.Euler(s[3], s[4], s[5]), _carry);
            _model.localPosition = Vector3.Lerp(p, new Vector3(b[0], b[1], b[2]), w);
            _model.localRotation = Quaternion.Slerp(q, Quaternion.Euler(b[3], b[4], b[5]), w);
            if (_rod == null) { if (RuleLog.Due("MusketRig.PlaceModel:189", 2d)) ModLog.Every("MusketRig.PlaceModel:189", 2d, "MusketRig.PlaceModel: returns (line 189)"); return; }
            var rammed = new Vector3(_rodHome.x, _rodHome.y - MusketPoses.RodPipeY, _rodHome.z + MusketPoses.MuzzleZ + MusketPoseRules.RodAboveMuzzle(seconds, _stroke) - MusketPoses.RodTopZ);
            _rod.localPosition = w > 0.5f ? rammed : _rodHome;
        }

        // steadiness 0 = the animation's spine as it is, 1 = only its slow average (the breathing filtered out)
        void SteadySpine(float steadiness)
        {
            if (RuleLog.Due("MusketRig.SteadySpine", 2d)) ModLog.Every("MusketRig.SteadySpine", 2d, $"MusketRig.SteadySpine(steadiness={RuleLog.Shown(steadiness)})");
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

        /// <summary>
        /// Diagnostic switch (hook GunPowdered.musketTrace): the next N frames of every OTHER player's rig are logged one line each (skip / write, how far the
        /// right upper arm and the musket model moved since the last frame, model id and parent) - for the Bjornita vibration (user 2026-11-22). 0 = off.
        /// </summary>
        internal static int TraceFrames;

        // the game's animator bool while a player gets up after spawning or joining (Player.SetupAwake / UpdateAwake, ~1 s, every client)
        static readonly int WakeupParam = Animator.StringToHash("wakeup");
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
            if (!_wrote || _upperL == null || _upperR == null) { if (RuleLog.Due("MusketRig.AnimatorDidNotRun:213", 2d)) ModLog.Every("MusketRig.AnimatorDidNotRun:213", 2d, "MusketRig.AnimatorDidNotRun: returns false (line 213)"); return false; }
            { var returnedAt214 = _upperL.localRotation == _wroteL && _upperR.localRotation == _wroteR; if (RuleLog.Due("MusketRig.AnimatorDidNotRun:214", 2d)) ModLog.Every("MusketRig.AnimatorDidNotRun:214", 2d, $"MusketRig.AnimatorDidNotRun: returns {RuleLog.Shown(returnedAt214)} (line 214)"); return returnedAt214; }
        }

        void RememberWrite()
        {
            if (RuleLog.Due("MusketRig.RememberWrite", 2d)) ModLog.Every("MusketRig.RememberWrite", 2d, $"MusketRig.RememberWrite()");
            if (_upperL == null) _upperL = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (_upperR == null) _upperR = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (_upperL == null || _upperR == null) { if (RuleLog.Due("MusketRig.RememberWrite:222", 2d)) ModLog.Every("MusketRig.RememberWrite:222", 2d, "MusketRig.RememberWrite: returns (line 222)"); return; }
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
            { var returnedAt269 = max >= min ? ((max - min) * 1000f).ToString("0.0") + " mm" : "n/a"; if (RuleLog.Due("MusketRig.Range:269", 2d)) ModLog.Every("MusketRig.Range:269", 2d, $"MusketRig.Range: returns {RuleLog.Shown(returnedAt269)} (line 269)"); return returnedAt269; }
        }

        bool EnsureHandler()
        {
            if (_handler != null) { if (RuleLog.Due("MusketRig.EnsureHandler:274", 2d)) ModLog.Every("MusketRig.EnsureHandler:274", 2d, "MusketRig.EnsureHandler: returns true (line 274)"); return true; }
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
            if (_model != null && _model.gameObject.activeInHierarchy && _model.IsChildOf(_vis.m_rightHand)) { if (RuleLog.Due("MusketRig.FindModel:293", 2d)) ModLog.Every("MusketRig.FindModel:293", 2d, "MusketRig.FindModel: returns true (line 293)"); return true; }
            _model = null;
            Transform found = Search(_vis.m_rightHand) ?? Search(_vis.m_leftHand);
            if (found == null) { if (RuleLog.Due("MusketRig.FindModel:296", 2d)) ModLog.Every("MusketRig.FindModel:296", 2d, "MusketRig.FindModel: returns false (line 296)"); return false; }
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
            if (hand == null) { if (RuleLog.Due("MusketRig.Search:316", 2d)) ModLog.Every("MusketRig.Search:316", 2d, "MusketRig.Search: returns null (line 316)"); return null; }
            foreach (Transform attach in hand)
            {
                Transform m = attach.name.StartsWith("attach") ? attach.Find(ModelName) : null;
                if (m != null && HasChild(m, BarrelName)) { var returnedAt320 = m; if (RuleLog.Due("MusketRig.Search:320", 2d)) ModLog.Every("MusketRig.Search:320", 2d, $"MusketRig.Search: returns {RuleLog.Shown(returnedAt320)} (line 320)"); return returnedAt320; }
            }
            { if (RuleLog.Due("MusketRig.Search:322", 2d)) ModLog.Every("MusketRig.Search:322", 2d, "MusketRig.Search: returns null (line 322)"); return null; }
        }

        static bool HasChild(Transform root, string name)
        {
            if (RuleLog.Due("MusketRig.HasChild", 2d)) ModLog.Every("MusketRig.HasChild", 2d, $"MusketRig.HasChild(root={RuleLog.Shown(root)}, name={RuleLog.Shown(name)})");
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith(name)) { if (RuleLog.Due("MusketRig.HasChild:328", 2d)) ModLog.Every("MusketRig.HasChild:328", 2d, "MusketRig.HasChild: returns true (line 328)"); return true; }
            { if (RuleLog.Due("MusketRig.HasChild:329", 2d)) ModLog.Every("MusketRig.HasChild:329", 2d, "MusketRig.HasChild: returns false (line 329)"); return false; }
        }

        void OnDestroy() => _handler?.Dispose();
    }
}
