using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Features.MobileCannon.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// The barrel elevation of one Mobile Cannon (D-104). The pitch lives in the piece's ZDO ("gp_mc_pitch", degrees) so it stays after a relog and every
    /// client sees it; a client that turns the crank sends its notches to the owner (RPC GP_MC_Turn), who keeps the pitch inside the limits. Every
    /// client turns the barrel, the crank and the elevating screw from that pitch.
    /// </summary>
    internal sealed class MobileCannonAim : MonoBehaviour
    {
        internal const string KeyPitch = "gp_mc_pitch", RpcTurn = "GP_MC_Turn";
        const string Barrel = "cannon_barrel", Bands = "cannon_bands", Crank = "elevation_crank", Screw = "elevation_screw";

        ZNetView _nview;
        Transform _barrel, _bands, _crank, _screw;
        Quaternion _barrelRest, _bandsRest, _crankRest;
        Vector3 _screwRest;

        internal float Pitch => _nview != null && _nview.IsValid() ? _nview.GetZDO().GetFloat(KeyPitch, 0f) : 0f;

        /// <summary>The barrel part (for the shot's direction, stage 3).</summary>
        internal Transform BarrelPart => _barrel;

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
            Transform model = transform.Find(MobileCannonSetup.ModelName);
            if (model != null)
            {
                _barrel = model.Find(Barrel);
                _bands = model.Find(Bands);
                _crank = model.Find(Crank);
                _screw = model.Find(Screw);
            }
            if (_barrel != null) _barrelRest = _barrel.localRotation;
            if (_bands != null) _bandsRest = _bands.localRotation;
            if (_crank != null) _crankRest = _crank.localRotation;
            if (_screw != null) _screwRest = _screw.localPosition;
            if (_nview != null && _nview.GetZDO() != null) _nview.Register<int>(RpcTurn, RPC_Turn);
            AddCrankHandle();
            ModLog.Verbose("MobileCannonAim: " + name + " parts barrel " + (_barrel != null) + " crank " + (_crank != null) + " screw " + (_screw != null));
        }

        /// <summary>A small solid box on the crank that the player can look at (MobileCannonCrank answers the hover and takes the mouse wheel).</summary>
        void AddCrankHandle()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonAim.AddCrankHandle()");
            if (_crank == null || transform.Find(MobileCannonCrank.ObjectName) != null) return;
            var go = new GameObject(MobileCannonCrank.ObjectName);
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.transform.position = _crank.position;
            go.transform.rotation = _crank.rotation;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.45f, 0.3f, 0.3f);
            box.center = new Vector3(-0.15f, -0.05f, 0f);
            go.AddComponent<MobileCannonCrank>().Aim = this;
        }

        /// <summary>Turns the crank by wheel notches (+ = muzzle up). Any client may call it; the owner applies it. Expected outcome: true when sent.</summary>
        internal bool Turn(int notches)
        {
            if (RuleLog.Due("MobileCannonAim.Turn", 2d)) ModLog.Every("MobileCannonAim.Turn", 2d, $"MobileCannonAim.Turn(notches={RuleLog.Shown(notches)})");
            if (notches == 0 || _nview == null || !_nview.IsValid()) return false;
            _nview.InvokeRPC(ZNetView.Everybody, RpcTurn, notches);
            { if (RuleLog.Due("MobileCannonAim.Turn:71", 2d)) ModLog.Every("MobileCannonAim.Turn:71", 2d, $"MobileCannonAim.Turn: returns true (line 71)"); return true; }
        }

        void RPC_Turn(long sender, int notches)
        {
            if (!_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonAim.RPC_Turn: returns because !_nview.IsOwner() (line 76)"); return; }
            float before = Pitch;
            float after = MobileCannonAimRules.Turn(before, notches, ModSettings.MobileCannonPitchStep.Value, ModSettings.MobileCannonMinPitch.Value, ModSettings.MobileCannonMaxPitch.Value);
            _nview.GetZDO().Set(KeyPitch, after);
            ModLog.Decision("mobile cannon crank", after.ToString("0.0") + " deg", "was " + before.ToString("0.0") + ", " + notches + " notch(es) from " + sender);
        }

        void LateUpdate()
        {
            float pitch = Pitch;
            Quaternion lift = Quaternion.Euler(-pitch, 0f, 0f);
            if (_barrel != null) _barrel.localRotation = _barrelRest * lift;
            if (_bands != null) _bands.localRotation = _bandsRest * lift;
            if (_crank != null) _crank.localRotation = _crankRest * Quaternion.Euler(MobileCannonAimRules.CrankAngle(pitch), 0f, 0f);
            if (_screw != null) _screw.localPosition = _screwRest - Vector3.up * MobileCannonAimRules.ScrewDrop(pitch);
            ModLog.Every("mobilecannon-aim-" + GetInstanceID(), 10d, "MobileCannonAim: " + name + " barrel at " + pitch.ToString("0.0") + " deg, crank " + MobileCannonAimRules.CrankAngle(pitch).ToString("0") + " deg");
        }
    }
}
