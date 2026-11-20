using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// Turns the cannon model's two wheels with the cart's physics wheels (D-104): each visible wheel copies the turn of the physics wheel on its side,
    /// measured from how both stood when the cannon was built or loaded.
    /// </summary>
    internal sealed class MobileCannonWheels : MonoBehaviour
    {
        const string WheelPrefix = "wheel_";

        Transform[] _shown = new Transform[0];
        Rigidbody[] _physics = new Rigidbody[0];
        Quaternion[] _offset = new Quaternion[0];

        /// <summary>How far the first wheel has turned in total since it was linked, in degrees, summed frame by frame (for the test hooks).</summary>
        internal float TurnedDegrees { get; private set; }

        Quaternion[] _start = new Quaternion[0];

        void Start()
        {
            Vagon vagon = GetComponent<Vagon>();
            Transform model = transform.Find(MobileCannonSetup.ModelName);
            if (vagon == null || model == null || vagon.m_wheels == null) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonWheels.Start:28", 2d, "MobileCannonWheels.Start: returns (line 28)"); return; }
            var shown = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in model)
                if (t.name.StartsWith(WheelPrefix) && !t.name.EndsWith("_tyre")) shown.Add(t);
            int n = Mathf.Min(shown.Count, vagon.m_wheels.Length);
            _shown = new Transform[n];
            _physics = new Rigidbody[n];
            _offset = new Quaternion[n];
            _start = new Quaternion[n];
            for (int i = 0; i < n; i++)
            {
                Rigidbody body = vagon.m_wheels[i];
                Transform nearest = shown[0];
                foreach (Transform s in shown)
                    if ((s.position - body.position).sqrMagnitude < (nearest.position - body.position).sqrMagnitude) nearest = s;
                shown.Remove(nearest);
                _shown[i] = nearest;
                _physics[i] = body;
                _offset[i] = Quaternion.Inverse(body.transform.rotation) * nearest.rotation;
                _start[i] = body.transform.localRotation;
            }
            ModLog.Verbose("MobileCannonWheels: " + n + " wheel(s) linked on " + name);
        }

        void LateUpdate()
        {
            for (int i = 0; i < _shown.Length; i++)
            {
                if (_shown[i] == null || _physics[i] == null) continue;
                _shown[i].rotation = _physics[i].transform.rotation * _offset[i];
                Quaternion now = _physics[i].transform.localRotation;
                if (i == 0) TurnedDegrees += Quaternion.Angle(_start[i], now);
                _start[i] = now;                                  // the last frame's turn: summed, so full turns are counted too
            }
            float turned = TurnedDegrees;
            if (_shown.Length > 0) ModLog.Every("mobilecannon-wheels-" + GetInstanceID(), 5d, "MobileCannonWheels: " + name + " wheels turned up to " + turned.ToString("0") + " deg since linked");
        }
    }
}
