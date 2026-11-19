using System;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Bootstrap;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// The harness state of one Asksvin (D-063, D-116): whether it wears the harness (ZDO, kept over relogs), putting it on (owner RPC), dropping it with
    /// the saddle or on death, and the harness model, which shows only while a Mobile Cannon is hitched (the towing cannon calls ShowHitched every frame).
    /// Added to every Asksvin by AsksvinHarnessPatch.
    /// </summary>
    internal sealed class AsksvinHarness : MonoBehaviour
    {
        internal const string AnimalPrefab = "Asksvin", KeyHarness = "gp_harness";
        const string RpcOn = "GP_HarnessOn", VisualName = "gp_harness_model", HookName = "CannonHook";
        static readonly Vector3 HookFallback = new Vector3(0f, 0.395f, -3.5f);
        static Vector3? _hook;

        ZNetView _nview;
        Character _character;
        GameObject _visual;
        float _shownUntil;

        internal bool Harnessed => _nview != null && _nview.IsValid() && _nview.GetZDO().GetBool(KeyHarness, false);

        internal Character Animal => _character;

        internal ZNetView View => _nview;

        /// <summary>Where the tow chain ends behind the tail, in the Asksvin's own space (the empty CannonHook of Harness.fbx; D-068 fallback).</summary>
        internal static Vector3 HookLocal
        {
            get
            {
                if (_hook == null)
                {
                    Transform model = Model();
                    Transform hook = model != null ? Find(model, HookName) : null;
                    _hook = hook != null ? model.InverseTransformPoint(hook.position) : HookFallback;
                    ModLog.Verbose("AsksvinHarness: tow hook at " + _hook.Value.ToString("0.000") + (hook != null ? " (CannonHook of the bundle model)" : " (fallback)"));
                }
                return _hook.Value;
            }
        }

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _character = GetComponent<Character>();
            if (_nview != null && _nview.IsValid()) _nview.Register(RpcOn, RPC_On);
            if (_character != null) _character.m_onDeath = (Action)Delegate.Combine(_character.m_onDeath, new Action(OnDeath));
        }

        /// <summary>Asks the animal's owner to put the harness on. Expected outcome: the ZDO flag is set on the owner (the item is taken by the caller).</summary>
        internal void PutOn() => _nview.InvokeRPC(RpcOn);

        void RPC_On(long sender)
        {
            if (!_nview.IsOwner() || Harnessed) return;
            _nview.GetZDO().Set(KeyHarness, true);
            ModLog.Info("AsksvinHarness: harness on " + name);
        }

        /// <summary>Takes the harness off and drops it as an item beside the animal (with the saddle or on death). Expected outcome: true when one was dropped.</summary>
        internal bool Drop()
        {
            if (!Harnessed) return false;
            _nview.GetZDO().Set(KeyHarness, false);
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.AsksvinHarness) : null;
            if (prefab != null) Instantiate(prefab, transform.position + Vector3.up * 1.2f, Quaternion.identity);
            ModLog.Info("AsksvinHarness: harness dropped by " + name + (prefab == null ? " (no prefab, nothing spawned)" : ""));
            return true;
        }

        void OnDeath()
        {
            if (_nview != null && _nview.IsValid() && _nview.IsOwner()) Drop();
        }

        /// <summary>Called every frame by a towing cannon: the harness model shows for half a second more.</summary>
        internal void ShowHitched() => _shownUntil = Time.time + 0.5f;

        internal bool ModelShown => _visual != null && _visual.activeSelf;

        void LateUpdate()
        {
            bool show = Time.time < _shownUntil;
            if (show && _visual == null) _visual = MakeVisual();
            if (_visual != null && _visual.activeSelf != show) _visual.SetActive(show);
            if (show) ModLog.Every("asksvin-harness-" + GetInstanceID(), 10d, "AsksvinHarness: " + name + " towing, harness model " + (_visual != null) + ", harnessed " + Harnessed);
        }

        GameObject MakeVisual()
        {
            Transform model = Model();
            if (model == null) return null;
            GameObject v = Instantiate(model.gameObject, transform);
            v.name = VisualName;
            v.transform.localPosition = model.localPosition;
            v.transform.localRotation = model.localRotation;
            v.transform.localScale = Vector3.one;
            foreach (Collider c in v.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (Transform t in v.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
            return v;
        }

        static Transform Model()
        {
            GameObject source = Plugin.Bundle != null ? Plugin.Bundle.LoadAsset<GameObject>(GunPowderedPrefabs.AsksvinHarness) : null;
            return source != null ? source.transform.Find("model") : null;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
