// kydl:read-only-observer - test-only, drives no creature: keeps the free camera beside the cannon's tow ring for GameProbe pictures
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// D-143: keeps the free camera (gp_freecam) at a fixed offset beside the cannon's tow ring and looking at it, every frame: the hitch moves 1-2 m between
    /// placing a still camera and the screenshot (a14slope2), which left the hitch at the edge of the picture. Removed with the free camera (freeCamOff).
    /// </summary>
    internal sealed class HitchCamFollow : MonoBehaviour
    {
        internal Transform Ring;
        internal Vector3 Offset;
        float _logged;

        void LateUpdate()
        {
            if (Ring == null) { ModLog.Verbose("HitchCamFollow.LateUpdate: no ring - returns"); return; }
            transform.position = Ring.position + Offset;
            transform.LookAt(Ring.position);
            if (Time.time - _logged > 2f) { _logged = Time.time; ModLog.Verbose($"HitchCamFollow.LateUpdate: camera at {transform.position:0.00}, ring {Ring.position:0.00}"); }
        }
    }
}
