using System;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Features.Guano.Infrastructure
{
    /// <summary>
    /// Decides WHO fills a cave with guano (D-036). Sits on the game's LocationProxy (the networked object of every location). A player who goes in asks the
    /// proxy's owner; the owner answers the first asker "yes" and sets the cave's flag, every later asker "no". The owner handles the requests one after the
    /// other, so two players entering the same cave at the same moment cannot both place (the flag was set by the entering client before: a race).
    /// The client that got "yes" places the guano itself, because only it has the cave's rooms loaded.
    /// </summary>
    internal sealed class CaveClaim : MonoBehaviour
    {
        internal const string KeyPlaced = "gp_guano_placed";
        const string RpcClaim = "GP_CaveClaim", RpcClaimed = "GP_CaveClaimed";
        ZNetView _nview;
        Action<bool> _onAnswer;

        void Awake()
        {
            if (RuleLog.On) ModLog.Verbose($"CaveClaim.Awake()");
            _nview = GetComponent<ZNetView>();
            if (_nview == null || !_nview.IsValid()) { if (RuleLog.On) ModLog.Verbose("CaveClaim.Awake: returns (line 25)"); return; }
            _nview.Register(RpcClaim, OnClaim);
            _nview.Register<bool>(RpcClaimed, OnClaimed);
        }

        /// <summary>True when the cave was filled (or claimed) already, as this client sees it.</summary>
        internal bool Placed => _nview != null && _nview.IsValid() && _nview.GetZDO().GetBool(KeyPlaced);

        /// <summary>Asks the owner whether this client may fill the cave; the answer arrives in <paramref name="onAnswer"/>. Expected outcome: the request was sent.</summary>
        internal bool Request(Action<bool> onAnswer)
        {
            if (_nview == null || !_nview.IsValid()) { var returnedAt36 = _nview != null && _nview.IsValid(); if (RuleLog.On) ModLog.Every("CaveClaim.Request:36", 2d, $"CaveClaim.Request: returns {returnedAt36} (line 36)"); return returnedAt36; }
            _onAnswer = onAnswer;
            _nview.InvokeRPC(RpcClaim);
            ModLog.Verbose("CaveClaim: asked the owner of " + name + " (owner is me " + _nview.IsOwner() + ")");
            return _nview.IsValid();
        }

        // owner: first asker wins
        void OnClaim(long sender)
        {
            if (!_nview.IsOwner()) { if (RuleLog.On) ModLog.Verbose("CaveClaim.OnClaim: returns (line 46)"); return; }
            ZDO zdo = _nview.GetZDO();
            bool granted = !zdo.GetBool(KeyPlaced);
            if (granted) zdo.Set(KeyPlaced, true);
            ModLog.Decision("cave claim", granted ? "granted" : "refused, already filled", "asked by " + sender + ", cave proxy at " + transform.position.ToString("0"));
            _nview.InvokeRPC(sender, RpcClaimed, granted);
        }

        // asker: the owner's answer
        void OnClaimed(long sender, bool granted)
        {
            ModLog.Verbose("CaveClaim: answer from " + sender + ": " + (granted ? "granted" : "refused"));
            Action<bool> answer = _onAnswer;
            _onAnswer = null;
            answer?.Invoke(granted);
        }
    }
}
