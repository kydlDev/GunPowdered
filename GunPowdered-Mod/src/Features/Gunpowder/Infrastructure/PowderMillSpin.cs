// kydl:read-only-observer - Player here is only the local player whose open crafting window keeps the mill turning; no creature is moved, commanded or spawned
using System;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Gunpowder.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Gunpowder.Infrastructure
{
    /// <summary>
    /// The Powder Mill turns by itself (D-024, D-026): nobody cranks it. While a player has the mill's crafting window open, that client stamps the
    /// station's ZDO ("gp_mill_use", world time in milliseconds) once a second; every client that sees the mill turns the sweep arm and its two polished
    /// stones (the model parts mill_arm and mill_runners, pivot at the model origin) while the stamp is fresh, so other players see it work too.
    /// </summary>
    internal sealed class PowderMillSpin : MonoBehaviour
    {
        const string KeyUse = "gp_mill_use", ArmName = "mill_arm", RunnersName = "mill_runners";
        const float StampSeconds = 1f;
        ZNetView _nview;
        CraftingStation _station;
        Transform _arm, _runners;
        float _nextStamp;

        void Awake()
        {
            if (RuleLog.On) ModLog.Verbose($"PowderMillSpin.Awake()");
            _nview = GetComponent<ZNetView>();
            _station = GetComponent<CraftingStation>();
            _arm = FindDeep(transform, ArmName);
            _runners = FindDeep(transform, RunnersName);
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (RuleLog.On) ModLog.Verbose($"PowderMillSpin.FindDeep(root={root}, name={name})");
            foreach (Transform c in root)
            {
                if (c.name == name) { var returnedAt38 = c; if (RuleLog.On) ModLog.Verbose($"PowderMillSpin.FindDeep: returns {returnedAt38} (line 38)"); return returnedAt38; }
                Transform f = FindDeep(c, name);
                if (f != null) { var returnedAt40 = f; if (RuleLog.On) ModLog.Verbose($"PowderMillSpin.FindDeep: returns {returnedAt40} (line 40)"); return returnedAt40; }
            }
            { if (RuleLog.On) ModLog.Verbose("PowderMillSpin.FindDeep: returns null (line 42)"); return null; }
        }

        void Update()
        {
            try
            {
                if (_nview == null || !_nview.IsValid() || ZNet.instance == null) { if (RuleLog.On) ModLog.Every("PowderMillSpin.Update:49", 2d, "PowderMillSpin.Update: returns (line 49)"); return; }
                double now = ZNet.instance.GetTimeSeconds();
                Stamp(now);
                if (!GunpowderRules.IsTurning(_nview.GetZDO().GetLong(KeyUse, 0L) / 1000d, now, GunpowderRules.TurnGraceSeconds)) { if (RuleLog.On) ModLog.Every("PowderMillSpin.Update:52", 2d, "PowderMillSpin.Update: returns (line 52)"); return; }

                float step = GunpowderRules.TurnDegreesPerSecond * Time.deltaTime;
                if (_arm != null) _arm.Rotate(0f, step, 0f, Space.Self);
                if (_runners != null) _runners.Rotate(0f, step, 0f, Space.Self);
                ModLog.Every("powder-mill-" + gameObject.GetInstanceID(), 30d, "powder mill turning at " + GunpowderRules.TurnDegreesPerSecond + " deg/s, arm " + (_arm != null) + ", stones " + (_runners != null));
            }
            catch (Exception e)
            {
                ModLog.Warn("PowderMillSpin.Update: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // the local player has this mill's window open: keep the stamp fresh (the owner of the network object is not needed, any client may write it)
        void Stamp(double now)
        {
            if (RuleLog.On) ModLog.Every("PowderMillSpin.Stamp", 2d, $"PowderMillSpin.Stamp(now={now})");
            if (Time.time < _nextStamp || Player.m_localPlayer == null || _station == null) { if (RuleLog.On) ModLog.Every("PowderMillSpin.Stamp:69", 2d, "PowderMillSpin.Stamp: returns (line 69)"); return; }
            if (Player.m_localPlayer.GetCurrentCraftingStation() != _station) { if (RuleLog.On) ModLog.Every("PowderMillSpin.Stamp:70", 2d, "PowderMillSpin.Stamp: returns (line 70)"); return; }
            _nextStamp = Time.time + StampSeconds;
            if (!_nview.IsOwner()) _nview.ClaimOwnership();
            _nview.GetZDO().Set(KeyUse, (long)(now * 1000d));
        }
    }
}
