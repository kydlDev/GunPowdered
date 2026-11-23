using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using KydlCore.Features.Diagnostics.Contracts;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// D-144 (T-014): backing an Asksvin up, so it can be put in front of the Mobile Cannon to hitch it. Ridden: S walks it backwards at half its walk speed,
    /// A / D turn it slowly (the rider's input reaches the animal's owner by RPC, like the game's own saddle controls). On foot: E on the saddle's chest rope
    /// (<see cref="AsksvinLeadRope"/>) takes it by the rope (toggle); walking into it backs it up, walking off leads it forward, it turns to face the player.
    /// The animal's owner drives it from <c>MonsterAI.UpdateAI</c> (AsksvinReversePatch) while either is on. Added to every Asksvin with the harness state.
    /// </summary>
    internal sealed class AsksvinReverse : MonoBehaviour
    {
        const string RpcRide = "gp_rev_ride", RpcLead = "gp_rev_lead", KeyLeader = "gp_rev_leader";

        /// <summary>The Asksvins that walk backwards this frame: their turn ignores the move direction (Character.UpdateRotation would turn them round).</summary>
        internal static readonly HashSet<Character> Backing = new HashSet<Character>();

        static readonly Dictionary<MonsterAI, AsksvinReverse> ByAi = new Dictionary<MonsterAI, AsksvinReverse>();

        ZNetView _nview;
        Character _character;
        MonsterAI _ai;
        Tameable _tame;
        bool _rideBack, _sentBack;
        int _rideTurn, _sentTurn;

        /// <summary>Whether this Asksvin walks backwards now (owner side): a hitched cannon is kept straight behind it.</summary>
        internal bool IsBacking => _character != null && Backing.Contains(_character);

        /// <summary>The player leading it by the chest rope (ZDO, so every client's hover text agrees). Expected outcome: ZDOID.None when nobody leads it.</summary>
        internal ZDOID Leader => _nview != null && _nview.IsValid() ? _nview.GetZDO().GetZDOID(KeyLeader) : ZDOID.None;

        /// <summary>Whether it wears a saddle (only then has it the chest rope).</summary>
        internal bool Saddled => _nview != null && _nview.IsValid() && _nview.GetZDO().GetBool(ZDOVars.s_haveSaddleHash);

        void Awake()
        {
            if (RuleLog.IsOn()) ModLog.Verbose("AsksvinReverse.Awake()");
            _nview = GetComponent<ZNetView>();
            _character = GetComponent<Character>();
            _ai = GetComponent<MonsterAI>();
            _tame = GetComponent<Tameable>();
            if (_nview != null && _nview.IsValid())
            {
                _nview.Register<bool, int>(RpcRide, RPC_Ride);
                _nview.Register<ZDOID>(RpcLead, RPC_Lead);
            }
            if (_ai != null) ByAi[_ai] = this;
            AsksvinLeadRope.AddTo(this);
        }

        void OnDestroy()
        {
            if (RuleLog.IsOn()) ModLog.Verbose("AsksvinReverse.OnDestroy()");
            if (_ai != null) ByAi.Remove(_ai);
            if (_character != null) Backing.Remove(_character);
        }

        /// <summary>The Asksvin a MonsterAI belongs to, if it has backing. Expected outcome: null for every other creature.</summary>
        internal static AsksvinReverse Of(MonsterAI ai)
        {
            if (RuleLog.Due("AsksvinReverse.Of", 2d)) ModLog.Every("AsksvinReverse.Of", 2d, "AsksvinReverse.Of(ai=" + RuleLog.Shown(ai) + ")");
            AsksvinReverse r;
            { var returnedAt70 = ai != null && ByAi.Count > 0 && ByAi.TryGetValue(ai, out r) ? r : null; if (RuleLog.Due("AsksvinReverse.Of:70", 2d)) ModLog.Every("AsksvinReverse.Of:70", 2d, $"AsksvinReverse.Of: returns {returnedAt70} (line 70)"); return returnedAt70; }
        }

        /// <summary>The rider's controls (local player, every frame from Sadle.ApplyControlls): sends back / turn to the owner when they change.</summary>
        internal void RiderInput(Vector3 moveDir, bool run)
        {
            bool back = AsksvinReverseRules.WantsBack(moveDir.z, run);
            int turn = back ? AsksvinReverseRules.TurnInput(moveDir.x) : 0;
            if (back == _sentBack && turn == _sentTurn) { if (RuleLog.Due("AsksvinReverse.RiderInput:same", 2d)) ModLog.Every("AsksvinReverse.RiderInput:same", 2d, "AsksvinReverse.RiderInput: unchanged - returns"); return; }
            _sentBack = back;
            _sentTurn = turn;
            _nview.InvokeRPC(RpcRide, back, turn);
            ModLog.Info($"AsksvinReverse: {name} rider back {back}, turn {turn}");
        }

        void RPC_Ride(long sender, bool back, int turn)
        {
            if (!_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Verbose("AsksvinReverse.RPC_Ride: not the owner - returns"); return; }
            _rideBack = back;
            _rideTurn = turn;
            ModLog.Verbose($"AsksvinReverse.RPC_Ride: back {back}, turn {turn} - returns");
        }

        /// <summary>E on the chest rope: takes it by the rope, or lets go when this player already leads it. Expected outcome: true when the request was sent.</summary>
        internal bool ToggleLead(Player player)
        {
            if (player == null || !Saddled) { ModLog.Verbose("AsksvinReverse.ToggleLead: no player or no saddle - returns false"); return false; }
            ZDOID me = player.GetZDOID();
            ZDOID next = Leader == me ? ZDOID.None : me;
            _nview.InvokeRPC(RpcLead, next);
            ModLog.Info($"AsksvinReverse: {name} {(next == ZDOID.None ? "let go of" : "led by the chest rope by")} {player.GetPlayerName()}");
            return true;
        }

        void RPC_Lead(long sender, ZDOID leader)
        {
            if (!_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Verbose("AsksvinReverse.RPC_Lead: not the owner - returns"); return; }
            _nview.GetZDO().Set(KeyLeader, leader);
            ModLog.Verbose("AsksvinReverse.RPC_Lead: leader " + leader + " - returns");
        }

        /// <summary>
        /// Owner side, every AI step: drives the animal while it is backed by its rider or led by the rope. Expected outcome: true when this took over the
        /// step (the game's AI is skipped), false when the game's AI runs as usual.
        /// </summary>
        internal bool Drive(float dt)
        {
            if (_character == null || _ai == null || !_nview.IsValid() || !_nview.IsOwner() || _character.IsDead()) { StopBacking(); { if (RuleLog.Due("AsksvinReverse.Drive:117", 2d)) ModLog.Every("AsksvinReverse.Drive:117", 2d, $"AsksvinReverse.Drive: returns false because _character == null || _ai == null || !_nview.IsValid() || !_nview.IsOwner() || _character.IsDead() (line 117)"); return false; } }
            if (_rideBack && Ridden()) { Back(dt, _rideTurn); { if (RuleLog.Due("AsksvinReverse.Drive:118", 2d)) ModLog.Every("AsksvinReverse.Drive:118", 2d, $"AsksvinReverse.Drive: returns true because _rideBack && Ridden() (line 118)"); return true; } }
            _rideBack = _rideBack && Ridden();
            ZDOID leader = Leader;
            if (leader == ZDOID.None || !Saddled || Ridden()) { _ledBy = ZDOID.None; EndLeadWalk(); StopBacking(); if (RuleLog.Due("AsksvinReverse.Drive:ai", 2d)) ModLog.Every("AsksvinReverse.Drive:ai", 2d, "AsksvinReverse.Drive: not backed nor led - returns false"); return false; }
            GameObject go = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(leader) : null;
            if (go == null) { LetGo("the leader is not loaded"); { if (RuleLog.Due("AsksvinReverse.Drive:123", 2d)) ModLog.Every("AsksvinReverse.Drive:123", 2d, $"AsksvinReverse.Drive: returns false because go == null (line 123)"); return false; } }
            bool led = Lead(leader, go.transform.position, dt);
            if (RuleLog.Due("AsksvinReverse.Drive:lead", 2d)) ModLog.Every("AsksvinReverse.Drive:lead", 2d, "AsksvinReverse.Drive: led - returns " + led);
            return led;
        }

        // user 21:46: holding the rope, the Asksvin moves WITH the player along its own axis (towards its back -> it walks backwards, forward -> forward),
        // measured from where the player stood when taking the rope; it keeps its heading (no turning towards the player: leading, not following)
        ZDOID _ledBy = ZDOID.None;
        float _grabAhead;

        bool Lead(ZDOID leader, Vector3 player, float dt)
        {
            Vector3 rope = AsksvinLeadRope.WorldPoint(this);
            Vector3 fwd = Flat(transform.forward);
            Vector3 toPlayer = player - rope;
            toPlayer.y = 0f;
            float ahead = Vector3.Dot(toPlayer, fwd);
            if (leader != _ledBy) { _ledBy = leader; _grabAhead = ahead; ModLog.Info($"AsksvinReverse: {name} rope taken, player {ahead:0.00} m ahead of the rope"); }
            string what = AsksvinReverseRules.Lead(ahead, _grabAhead, toPlayer.magnitude);
            if (ModLog.IsDue("asksvin-lead-" + GetInstanceID(), 2d)) ModLog.Every("asksvin-lead-" + GetInstanceID(), 2d, $"AsksvinReverse: {name} led, player moved {ahead - _grabAhead:0.00} m along it since taking the rope - {what}");
            if (what == "let go") { LetGo("the player walked off"); { if (RuleLog.Due("AsksvinReverse.Lead:letgo", 2d)) ModLog.Every("AsksvinReverse.Lead:letgo", 2d, "AsksvinReverse.Lead: returns false (let go)"); return false; } }
            if (what == "back") { EndLeadWalk(); Back(dt, 0f); { if (RuleLog.Due("AsksvinReverse.Lead:back", 2d)) ModLog.Every("AsksvinReverse.Lead:back", 2d, "AsksvinReverse.Lead: returns true (back)"); return true; } }
            StopBacking();
            if (what == "forward") { _character.SetWalk(true); _leadWalk = true; _ai.MoveTowards(fwd, false); { if (RuleLog.Due("AsksvinReverse.Lead:forward", 2d)) ModLog.Every("AsksvinReverse.Lead:forward", 2d, "AsksvinReverse.Lead: returns true (forward)"); return true; } }
            EndLeadWalk();
            _ai.StopMoving();
            { if (RuleLog.Due("AsksvinReverse.Lead:stay", 2d)) ModLog.Every("AsksvinReverse.Lead:stay", 2d, "AsksvinReverse.Lead: returns true (stay)"); return true; }
        }

        bool _leadWalk;

        // a led Asksvin walks (not jogs) after the player; the game's AI gets its own pace back afterwards
        void EndLeadWalk()
        {
            if (!_leadWalk) { if (RuleLog.Due("AsksvinReverse.EndLeadWalk:none", 2d)) ModLog.Every("AsksvinReverse.EndLeadWalk:none", 2d, "AsksvinReverse.EndLeadWalk: not walking - returns"); return; }
            _leadWalk = false;
            _character.SetWalk(false);
            ModLog.Verbose("AsksvinReverse.EndLeadWalk: walk off - returns");
        }

        void LetGo(string why)
        {
            EndLeadWalk();
            StopBacking();
            if (_nview.IsOwner()) _nview.GetZDO().Set(KeyLeader, ZDOID.None);
            ModLog.Info("AsksvinReverse: " + name + " let go of the chest rope - " + why);
        }

        // walks backwards at half the walk speed, facing ahead (turned by turn x the backing turn rate)
        void Back(float dt, float turn)
        {
            Vector3 f = Quaternion.Euler(0f, AsksvinReverseRules.TurnStep(turn, dt), 0f) * Flat(transform.forward);
            _character.SetLookDir(f);
            _character.SetRun(false);
            _character.SetWalk(true);
            _character.SetMoveDir(-f * AsksvinReverseRules.BackSpeedFactor);
            if (!Backing.Add(_character)) { if (RuleLog.Due("AsksvinReverse.Back:180", 2d)) ModLog.Every("AsksvinReverse.Back:180", 2d, $"AsksvinReverse.Back: returns because !Backing.Add(_character) (line 180)"); return; }
            string by = Ridden() ? "rider" : "chest rope";
            FlightRecorder.Track(_character, "Asksvin backing up (" + by + ")", () => "walks backwards at half its walk speed, backing " + IsBacking + ", leader " + Leader);
            ModLog.Info($"AsksvinReverse: {name} walks backwards ({by})");
        }

        void StopBacking()
        {
            if (_character == null || !Backing.Remove(_character)) { if (RuleLog.Due("AsksvinReverse.StopBacking:none", 2d)) ModLog.Every("AsksvinReverse.StopBacking:none", 2d, "AsksvinReverse.StopBacking: was not backing - returns"); return; }
            _character.SetWalk(false);
            _character.SetMoveDir(Vector3.zero);
            ModLog.Info("AsksvinReverse: " + name + " stops walking backwards");
        }

        bool Ridden()
        {
            if (RuleLog.Due("AsksvinReverse.Ridden", 2d)) ModLog.Every("AsksvinReverse.Ridden", 2d, "AsksvinReverse.Ridden()");
            Sadle s = _tame != null ? _tame.m_saddle : null;
            { var returnedAt198 = s != null && s.HaveValidUser(); if (RuleLog.Due("AsksvinReverse.Ridden:198", 2d)) ModLog.Every("AsksvinReverse.Ridden:198", 2d, $"AsksvinReverse.Ridden: returns {returnedAt198} (line 198)"); return returnedAt198; }
        }

        static Vector3 Flat(Vector3 v)
        {
            if (RuleLog.Due("AsksvinReverse.Flat", 2d)) ModLog.Every("AsksvinReverse.Flat", 2d, "AsksvinReverse.Flat(v=" + RuleLog.Shown(v) + ")");
            v.y = 0f;
            { var returnedAt205 = v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward; if (RuleLog.Due("AsksvinReverse.Flat:205", 2d)) ModLog.Every("AsksvinReverse.Flat:205", 2d, $"AsksvinReverse.Flat: returns {returnedAt205} (line 205)"); return returnedAt205; }
        }

        /// <summary>The move direction a backing Asksvin's turn must ignore (Character.m_moveDir).</summary>
        internal static readonly AccessTools.FieldRef<Character, Vector3> MoveDir = AccessTools.FieldRefAccess<Character, Vector3>("m_moveDir");
    }
}
