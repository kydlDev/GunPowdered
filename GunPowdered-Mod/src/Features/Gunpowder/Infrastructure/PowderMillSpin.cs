using System;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Gunpowder.Domain;

namespace GunPowdered.Features.Gunpowder.Infrastructure
{
    /// <summary>
    /// The Powder Mill turns by itself (D-024, D-026): nobody cranks it. Since D-137 the mill grinds automatically, so the sweep arm and its two polished
    /// stones (the model parts mill_arm and mill_runners, pivot at the model origin) turn on every client while the mill grinds (PowderMill.IsGrinding,
    /// read from the ZDO), so other players see it work too.
    /// </summary>
    internal sealed class PowderMillSpin : MonoBehaviour
    {
        const string ArmName = "mill_arm", RunnersName = "mill_runners";
        PowderMill _mill;
        Transform _arm, _runners;

        void Awake()
        {
            _mill = GetComponent<PowderMill>();
            _arm = FindDeep(transform, ArmName);
            _runners = FindDeep(transform, RunnersName);
            if (RuleLog.IsOn()) ModLog.Verbose($"PowderMillSpin.Awake: mill {_mill != null}, arm {_arm != null}, stones {_runners != null}");
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform c in root)
            {
                if (c.name == name) { if (RuleLog.IsOn()) ModLog.Verbose($"PowderMillSpin.FindDeep: returns {name}"); return c; }
                Transform f = FindDeep(c, name);
                if (f != null) { if (RuleLog.IsOn()) ModLog.Verbose($"PowderMillSpin.FindDeep: returns {name} (deeper)"); return f; }
            }
            if (RuleLog.IsOn()) ModLog.Verbose($"PowderMillSpin.FindDeep: returns null for {name}");
            return null;
        }

        void Update()
        {
            try
            {
                if (_mill == null || !_mill.IsGrinding) { if (RuleLog.Due("PowderMillSpin.Update:idle", 2d)) ModLog.Every("PowderMillSpin.Update:idle", 2d, "PowderMillSpin.Update: not grinding - returns"); return; }
                float step = GunpowderRules.TurnDegreesPerSecond * Time.deltaTime;
                if (_arm != null) _arm.Rotate(0f, step, 0f, Space.Self);
                if (_runners != null) _runners.Rotate(0f, step, 0f, Space.Self);
                ModLog.Every("powder-mill-" + gameObject.GetInstanceID(), 30d, "powder mill turning at " + GunpowderRules.TurnDegreesPerSecond + " deg/s, arm " + (_arm != null) + ", stones " + (_runners != null));
            }
            catch (Exception e)
            {
                ModLog.Warn($"PowderMillSpin.Update: {e.GetType().FullName}: {e.Message} - the arm does not turn this frame");
            }
        }
    }
}
