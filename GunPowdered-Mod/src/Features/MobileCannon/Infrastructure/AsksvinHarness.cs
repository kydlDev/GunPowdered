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
        internal const string AnimalPrefab = "Asksvin", KeyHarness = "gp_harness", KeyTowKey = "gp_tow_key";
        const string RpcOn = "GP_HarnessOn", VisualName = "gp_harness_model", HookName = "CannonHook";
        static readonly Vector3 HookFallback = new Vector3(0f, 0.395f, -3.5f);
        static Vector3? _hook;

        ZNetView _nview;
        Character _character;
        GameObject _visual;
        float _shownUntil;

        internal bool Harnessed => _nview != null && _nview.IsValid() && _nview.GetZDO().GetBool(KeyHarness, false);

        /// <summary>
        /// The harness's own random key (ZDO, set when it is put on). A towing cannon stores this key, not the ZDOID: the game gives every ZDO a new id
        /// when a world loads (lab a8relog1: 68212842:33848 became 1:28668), so only the key finds the same Asksvin after a relog.
        /// </summary>
        internal long TowKey => _nview != null && _nview.IsValid() ? _nview.GetZDO().GetLong(KeyTowKey, 0L) : 0L;

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
            if (TowKey == 0L) _nview.GetZDO().Set(KeyTowKey, NewKey());
            ModLog.Info("AsksvinHarness: harness on " + name + ", tow key " + TowKey);
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

        static long NewKey()
        {
            long key = 0L;
            while (key == 0L) key = ((long)UnityEngine.Random.Range(int.MinValue, int.MaxValue) << 32) ^ (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            return key;
        }

        /// <summary>The loaded, harnessed Asksvin with this tow key. Expected outcome: it, or null when none is loaded here.</summary>
        internal static AsksvinHarness WithKey(long key)
        {
            if (key == 0L) return null;
            foreach (Character c in Character.GetAllCharacters())
            {
                AsksvinHarness h = c != null ? c.GetComponent<AsksvinHarness>() : null;
                if (h != null && h.TowKey == key) return h;
            }
            return null;
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
            if (show && _visual != null) FollowSaddle();
            if (show) ModLog.Every("asksvin-harness-" + GetInstanceID(), 10d, "AsksvinHarness: " + name + " towing, harness model " + (_visual != null) + ", harnessed " + Harnessed);
        }

        // T-009i: the saddle is skinned to a spine bone that moves with the animation, the harness model is a rigid copy under the animal's root: its straps then
        // hung away from the saddle. The model now rides with the saddle's bone: its rest pose (under the root) is carried by the same matrix that moves the saddle
        // mesh from its rest pose to its current pose.
        SkinnedMeshRenderer _saddle;
        int _saddleBone = -1;
        Vector3 _restPos;
        Quaternion _restRot;
        bool _saddleSearched;

        void FollowSaddle()
        {
            if (!_saddleSearched)
            {
                _saddleSearched = true;
                foreach (SkinnedMeshRenderer s in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (s.name == "Sadel" && s.sharedMesh != null && s.bones != null && s.bones.Length > 0) _saddle = s;
                if (_saddle != null)
                {
                    Transform[] bones = _saddle.bones;
                    _saddleBone = 0;
                    for (int i = 0; i < bones.Length; i++) if (bones[i] != null && bones[i].name == "Bone.019") _saddleBone = i;
                    _restPos = _visual.transform.localPosition;
                    _restRot = _visual.transform.localRotation;
                }
                ModLog.Info("AsksvinHarness: " + name + " harness follows the saddle bone " + (_saddle != null ? _saddle.bones[_saddleBone].name : "(no saddle skin found, model stays on the root)"));
            }
            if (_saddle == null || _saddle.bones[_saddleBone] == null) return;
            Matrix4x4 pose = _saddle.bones[_saddleBone].localToWorldMatrix * _saddle.sharedMesh.bindposes[_saddleBone] * _saddle.transform.worldToLocalMatrix;
            Matrix4x4 restWorld = transform.localToWorldMatrix * Matrix4x4.TRS(_restPos, _restRot, Vector3.one);
            Matrix4x4 now = pose * restWorld;
            _visual.transform.SetPositionAndRotation(now.GetColumn(3), now.rotation);
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
