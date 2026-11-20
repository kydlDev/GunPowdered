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
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinHarness.Awake()");
            _nview = GetComponent<ZNetView>();
            _character = GetComponent<Character>();
            if (_nview != null && _nview.IsValid()) _nview.Register(RpcOn, RPC_On);
            if (_character != null) _character.m_onDeath = (Action)Delegate.Combine(_character.m_onDeath, new Action(OnDeath));
        }

        /// <summary>Asks the animal's owner to put the harness on. Expected outcome: the ZDO flag is set on the owner (the item is taken by the caller).</summary>
        internal void PutOn() => _nview.InvokeRPC(RpcOn);

        void RPC_On(long sender)
        {
            if (!_nview.IsOwner() || Harnessed) { if (RuleLog.IsOn()) ModLog.Verbose("AsksvinHarness.RPC_On: returns (line 68)"); return; }
            _nview.GetZDO().Set(KeyHarness, true);
            if (TowKey == 0L) _nview.GetZDO().Set(KeyTowKey, NewKey());
            ModLog.Info("AsksvinHarness: harness on " + name + ", tow key " + TowKey);
        }

        /// <summary>Takes the harness off and drops it as an item beside the animal (with the saddle or on death). Expected outcome: true when one was dropped.</summary>
        internal bool Drop()
        {
            if (!Harnessed) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.Drop:77", 2d, "AsksvinHarness.Drop: returns false (line 77)"); return false; }
            _nview.GetZDO().Set(KeyHarness, false);
            MobileCannonTow.AnimalGone(TowKey, _character != null && _character.IsDead() ? "the towing Asksvin died" : "the harness came off with the saddle");
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.AsksvinHarness) : null;
            if (prefab != null) Instantiate(prefab, transform.position + Vector3.up * 1.2f, Quaternion.identity);
            ModLog.Info("AsksvinHarness: harness dropped by " + name + (prefab == null ? " (no prefab, nothing spawned)" : ""));
            return true;
        }

        static long NewKey()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinHarness.NewKey()");
            long key = 0L;
            while (key == 0L) key = ((long)UnityEngine.Random.Range(int.MinValue, int.MaxValue) << 32) ^ (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            { var returnedAt91 = key; if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinHarness.NewKey: returns {RuleLog.Shown(returnedAt91)} (line 91)"); return returnedAt91; }
        }

        /// <summary>The loaded, harnessed Asksvin with this tow key. Expected outcome: it, or null when none is loaded here.</summary>
        internal static AsksvinHarness WithKey(long key)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.WithKey", 2d, $"AsksvinHarness.WithKey(key={RuleLog.Shown(key)})");
            if (key == 0L) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.WithKey:98", 2d, "AsksvinHarness.WithKey: returns null (line 98)"); return null; }
            foreach (Character c in Character.GetAllCharacters())
            {
                AsksvinHarness h = c != null ? c.GetComponent<AsksvinHarness>() : null;
                if (h != null && h.TowKey == key) { var returnedAt102 = h; if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.WithKey:102", 2d, $"AsksvinHarness.WithKey: returns {RuleLog.Shown(returnedAt102)} (line 102)"); return returnedAt102; }
            }
            { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.WithKey:104", 2d, "AsksvinHarness.WithKey: returns null (line 104)"); return null; }
        }

        void OnDeath()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"AsksvinHarness.OnDeath()");
            if (_nview != null && _nview.IsValid() && _nview.IsOwner()) Drop();
        }

        /// <summary>Called every frame by a towing cannon: the harness model shows for half a second more.</summary>
        internal void ShowHitched(Transform ring) { _shownUntil = Time.time + 0.5f; _towRing = ring; }

        internal bool ModelShown => _visual != null && _visual.activeSelf;

        void LateUpdate()
        {
            bool show = Time.time < _shownUntil;
            if (show && _visual == null) _visual = MakeVisual();
            if (_visual != null && _visual.activeSelf != show) _visual.SetActive(show);
            if (show && _visual != null) { FollowSaddle(); FollowRing(); }
            if (show) ModLog.Every("asksvin-harness-" + GetInstanceID(), 10d, "AsksvinHarness: " + name + " towing, harness model " + (_visual != null) + ", harnessed " + Harnessed);
        }

        // T-009i: the saddle is skinned to a spine bone that moves with the animation, the harness model is a rigid copy under the animal's root: its straps then
        // hung away from the saddle. The model now rides with the saddle's bone: its rest pose (under the root) is carried by the same matrix that moves the saddle
        // mesh from its rest pose to its current pose.
        SkinnedMeshRenderer _saddle;
        int _saddleBone = -1;
        bool _saddleSearched;

        // T-009i (second try): moving the whole rigid model with the saddle bone swung the parts 2-3.5 m behind the saddle (spreader, chain, hook ring) far off
        // the cannon whenever the spine bent (the idle pose). The model is now skinned at run time to two bones: the saddle's spine bone and the animal's root.
        // Everything in front of the trace anchors rides the saddle bone, everything behind the spreader stays on the root (where the cannon's tow joint pulls:
        // AsksvinHarness.HookLocal), and the traces blend between the two along their length, so they stretch instead of tearing apart.
        internal const string SaddleBone = "Bone.019";
        const float FrontFallback = -1.0f, RearFallback = -2.55f, HookRingRadius = 0.07f;
        float _front, _rear;
        // Third bone: the chain's end and the hook ring ride the cannon's tow ring itself (between the clevis plates, on the pin), so they stay connected
        // whatever the tow joint's sag or the animal's pose; the chain blends from the spreader (root) to the ring.
        Transform _hookBone, _towRing;
        float _ringLogAt = -99f;

        void FollowRing()
        {
            if (_hookBone == null) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.FollowRing:148", 2d, "AsksvinHarness.FollowRing: returns (line 148)"); return; }
            if (_towRing != null)
            {
                Transform cannon = _towRing.parent != null ? _towRing.parent : _towRing;
                _hookBone.SetPositionAndRotation(_towRing.position, cannon.rotation);
            }
            else
            {
                _hookBone.localPosition = HookLocal;
                _hookBone.localRotation = Quaternion.identity;
            }
            if (Time.time - _ringLogAt < 10f) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.FollowRing:159", 2d, "AsksvinHarness.FollowRing: returns (line 159)"); return; }
            _ringLogAt = Time.time;
            ModLog.Info("AsksvinHarness: " + name + " hook ring on the cannon's tow ring " + (_towRing != null) + ", " + (_hookBone.localPosition - HookLocal).magnitude.ToString("0.000")
                + " m from the rest hook (the joint's sag and swing, now shown by the chain), " + SkinReport());
        }

        /// <summary>Diagnostics: where the rest hook ring and the saddle-carried hook ring would be now, and how far apart (the old rigid follow error).</summary>
        internal string SkinReport()
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.SkinReport", 2d, $"AsksvinHarness.SkinReport()");
            if (_saddle == null || _saddleBone < 0) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.SkinReport:169", 2d, "AsksvinHarness.SkinReport: returns \"harness skin: no saddle bone\" (line 169)"); return "harness skin: no saddle bone"; }
            Matrix4x4 pose = _saddle.bones[_saddleBone].localToWorldMatrix * _saddle.sharedMesh.bindposes[_saddleBone] * _saddle.transform.worldToLocalMatrix;
            Vector3 hook = transform.TransformPoint(HookLocal);
            Vector3 front = transform.TransformPoint(new Vector3(0f, 1.0f, _front));
            return "harness skin: saddle bone moves the trace anchors by " + (pose.MultiplyPoint3x4(front) - front).magnitude.ToString("0.000") + " m, would have moved the hook ring by "
                + (pose.MultiplyPoint3x4(hook) - hook).magnitude.ToString("0.000") + " m (the rear now stays on the root), blend z " + _front.ToString("0.00") + " .. " + _rear.ToString("0.00");
        }

        void FollowSaddle()
        {
            if (_saddleSearched) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.FollowSaddle:179", 2d, "AsksvinHarness.FollowSaddle: returns (line 179)"); return; }
            _saddleSearched = true;
            foreach (SkinnedMeshRenderer s in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (s.name == "Sadel" && s.sharedMesh != null && s.bones != null && s.bones.Length > 0) _saddle = s;
            if (_saddle == null) { ModLog.Info("AsksvinHarness: " + name + " no saddle skin found, the harness model stays on the root"); return; }
            Transform[] bones = _saddle.bones;
            _saddleBone = 0;
            for (int i = 0; i < bones.Length; i++) if (bones[i] != null && bones[i].name == SaddleBone) _saddleBone = i;
            Transform anchor = Find(_visual.transform, "TraceAnchorL"), spreader = Find(_visual.transform, "SpreaderCentre");
            _front = anchor != null ? transform.InverseTransformPoint(anchor.position).z : FrontFallback;
            _rear = spreader != null ? transform.InverseTransformPoint(spreader.position).z : RearFallback;
            if (_front - _rear < 0.2f) { _front = FrontFallback; _rear = RearFallback; }
            var hookGo = new GameObject("gp_hook_follow");
            _hookBone = hookGo.transform;
            _hookBone.SetParent(transform, false);
            _hookBone.localPosition = HookLocal;
            _hookBone.localRotation = Quaternion.identity;
            float hookZ = HookLocal.z + HookRingRadius;

            // Bone list: every bone of the animal's skins (saddle and body), then the root, then the hook follower. The straps take the skin weights of the
            // nearest saddle / body vertex, so they bend with the animal exactly as the saddle and the hide under them do (T-009i, third try).
            var boneList = new System.Collections.Generic.List<Transform>();
            var bindList = new System.Collections.Generic.List<Matrix4x4>();     // bone bindpose * that skin's worldToLocal (world rest -> bone space)
            var donor = new SkinDonor();
            int donors = 0;
            string unreadable = "";
            foreach (SkinnedMeshRenderer s in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (s == null || s.sharedMesh == null || s.bones == null || s.bones.Length == 0 || s.transform.IsChildOf(_visual.transform)) continue;
                int[] map = new int[s.bones.Length];
                Matrix4x4[] bp = s.sharedMesh.bindposes;                     // readable even when the vertices are not
                for (int b = 0; b < s.bones.Length; b++)
                {
                    int at = boneList.IndexOf(s.bones[b]);
                    if (at < 0 && s.bones[b] != null && b < bp.Length) { boneList.Add(s.bones[b]); bindList.Add(bp[b] * s.transform.worldToLocalMatrix); at = boneList.Count - 1; }
                    map[b] = Mathf.Max(0, at);
                }
                if (!s.sharedMesh.isReadable) { unreadable += s.name + " "; continue; }
                donor.Add(s.sharedMesh.vertices, s.sharedMesh.boneWeights, map, s.transform.localToWorldMatrix);
                donors++;
            }
            if (donors == 0 && DonorFile.Load())
            {
                // the game's meshes are not readable: the same vertices and weights, exported from the ripped Asksvin (Data/AsksvinSkinDonor.txt in the bundle)
                int[] map = new int[DonorFile.Bones.Length];
                int missing = 0;
                for (int b = 0; b < map.Length; b++)
                {
                    map[b] = -1;
                    for (int k = 0; k < boneList.Count; k++) if (boneList[k] != null && boneList[k].name == DonorFile.Bones[b]) { map[b] = k; break; }
                    if (map[b] < 0) { missing++; map[b] = 0; }
                }
                donor.Add(DonorFile.Points, DonorFile.Weights, map, transform.localToWorldMatrix);
                donors = missing == 0 ? 1 : 0;
                if (missing > 0) ModLog.Warn("AsksvinHarness: " + missing + " bones of the skin donor file are not on this Asksvin - the straps follow the saddle bone only");
                else unreadable += "(donor file used) ";
            }
            int saddleIndex = boneList.IndexOf(bones[_saddleBone]);
            if (saddleIndex < 0) { boneList.Add(bones[_saddleBone]); bindList.Add(_saddle.sharedMesh.bindposes[_saddleBone] * _saddle.transform.worldToLocalMatrix); saddleIndex = boneList.Count - 1; }
            int rootIndex = boneList.Count, hookIndex = boneList.Count + 1;
            boneList.Add(transform);
            boneList.Add(_hookBone);
            Transform[] allBones = boneList.ToArray();

            int skinned = 0, kept = 0, copied = 0;
            float far = 0f;
            foreach (MeshFilter mf in _visual.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh src = mf.sharedMesh;
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (src == null || mr == null || !src.isReadable) { kept++; continue; }
                Matrix4x4 restWorld = mf.transform.localToWorldMatrix;
                Matrix4x4 toAnimal = transform.worldToLocalMatrix * restWorld;
                Mesh mesh = Instantiate(src);
                mesh.name = src.name + "_skinned";
                Vector3[] v = mesh.vertices;
                var w = new BoneWeight[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    float z = toAnimal.MultiplyPoint3x4(v[i]).z;
                    float onSaddle = GunPowdered.Features.MobileCannon.Domain.AsksvinTowRules.SaddleWeight(z, _front, _rear);
                    if (onSaddle <= 0f)
                    {
                        float onRing = 1f - GunPowdered.Features.MobileCannon.Domain.AsksvinTowRules.SaddleWeight(z, _rear, hookZ);
                        w[i] = new BoneWeight { boneIndex0 = hookIndex, weight0 = onRing, boneIndex1 = saddleIndex, weight1 = 1f - onRing };      // spreader on the spine bone, chain to the cannon's ring
                        continue;
                    }
                    float dist;
                    BoneWeight body;
                    if (donors > 0 && donor.Nearest(restWorld.MultiplyPoint3x4(v[i]), out body, out dist)) { copied++; far = Mathf.Max(far, dist); }
                    else body = new BoneWeight { boneIndex0 = saddleIndex, weight0 = 1f };
                    w[i] = SkinDonor.Blend(body, onSaddle, saddleIndex);      // straps on the hide's own weights, the traces fading to the spine bone towards the spreader
                }
                mesh.boneWeights = w;
                var bind = new Matrix4x4[allBones.Length];
                for (int b = 0; b < bindList.Count; b++) bind[b] = bindList[b] * restWorld;
                bind[rootIndex] = transform.worldToLocalMatrix * restWorld;
                bind[hookIndex] = _hookBone.worldToLocalMatrix * restWorld;
                mesh.bindposes = bind;
                Material[] mats = mr.sharedMaterials;
                GameObject go = mf.gameObject;
                DestroyImmediate(mr);
                DestroyImmediate(mf);
                SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh;
                smr.sharedMaterials = mats;
                smr.bones = allBones;
                smr.rootBone = transform;
                smr.quality = SkinQuality.Bone4;
                smr.updateWhenOffscreen = true;
                skinned++;
            }
            ModLog.Info("AsksvinHarness: " + name + " harness skinned: " + skinned + " meshes (" + kept + " kept rigid), " + allBones.Length + " bones from " + donors + " animal skins"
                + (unreadable.Length > 0 ? " (not readable: " + unreadable.Trim() + ")" : "") + ", " + copied + " strap vertices took the weights of the nearest saddle/hide vertex (farthest "
                + far.ToString("0.000") + " m), fallback bone " + bones[_saddleBone].name + "; " + SkinReport());
        }

        GameObject MakeVisual()
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.MakeVisual", 2d, $"AsksvinHarness.MakeVisual()");
            Transform model = Model();
            if (model == null) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.MakeVisual:300", 2d, "AsksvinHarness.MakeVisual: returns null (line 300)"); return null; }
            GameObject v = Instantiate(model.gameObject, transform);
            v.name = VisualName;
            v.transform.localPosition = model.localPosition;
            v.transform.localRotation = model.localRotation;
            v.transform.localScale = Vector3.one;
            foreach (Collider c in v.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (Transform t in v.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
            { var returnedAt308 = v; if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.MakeVisual:308", 2d, $"AsksvinHarness.MakeVisual: returns {RuleLog.Shown(returnedAt308)} (line 308)"); return returnedAt308; }
        }

        static Transform Model()
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.Model", 2d, $"AsksvinHarness.Model()");
            GameObject source = Plugin.Bundle != null ? Plugin.Bundle.LoadAsset<GameObject>(GunPowderedPrefabs.AsksvinHarness) : null;
            { var returnedAt315 = source != null ? source.transform.Find("model") : null; if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.Model:315", 2d, $"AsksvinHarness.Model: returns {RuleLog.Shown(returnedAt315)} (line 315)"); return returnedAt315; }
        }

        static Transform Find(Transform root, string name)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.Find", 2d, $"AsksvinHarness.Find(root={RuleLog.Shown(root)}, name={RuleLog.Shown(name)})");
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) { var returnedAt321 = t; if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.Find:321", 2d, $"AsksvinHarness.Find: returns {RuleLog.Shown(returnedAt321)} (line 321)"); return returnedAt321; }
            { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarness.Find:322", 2d, "AsksvinHarness.Find: returns null (line 322)"); return null; }
        }
    }

    /// <summary>The Asksvin's body and saddle skin (rest positions in the root's space, bone names, weights), exported from the ripped prefab: the live meshes are not readable.</summary>
    internal static class DonorFile
    {
        internal const string AssetName = "AsksvinSkinDonor";
        internal static string[] Bones;
        internal static Vector3[] Points;
        internal static BoneWeight[] Weights;
        static bool _tried;

        internal static bool Load()
        {
            if (_tried) { var returnedAt337 = Points != null; if (RuleLog.IsOn()) ModLog.Every("DonorFile.Load:337", 2d, $"DonorFile.Load: returns {RuleLog.Shown(returnedAt337)} (line 337)"); return returnedAt337; }
            _tried = true;
            TextAsset text = Plugin.Bundle != null ? Plugin.Bundle.LoadAsset<TextAsset>(AssetName) : null;
            if (text == null) { ModLog.Warn("AsksvinHarness: no " + AssetName + " in the bundle"); return false; }
            var pts = new System.Collections.Generic.List<Vector3>();
            var ws = new System.Collections.Generic.List<BoneWeight>();
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            foreach (string raw in text.text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                string[] f = line.Split(' ');
                if (f[0] == "bones") { Bones = new string[f.Length - 1]; System.Array.Copy(f, 1, Bones, 0, Bones.Length); continue; }
                if (f.Length < 11) continue;
                pts.Add(new Vector3(float.Parse(f[0], ci), float.Parse(f[1], ci), float.Parse(f[2], ci)));
                ws.Add(new BoneWeight
                {
                    boneIndex0 = int.Parse(f[3], ci), weight0 = float.Parse(f[4], ci), boneIndex1 = int.Parse(f[5], ci), weight1 = float.Parse(f[6], ci),
                    boneIndex2 = int.Parse(f[7], ci), weight2 = float.Parse(f[8], ci), boneIndex3 = int.Parse(f[9], ci), weight3 = float.Parse(f[10], ci)
                });
            }
            if (Bones == null || pts.Count == 0) { if (RuleLog.IsOn()) ModLog.Every("DonorFile.Load:358", 2d, "DonorFile.Load: returns false (line 358)"); return false; }
            Points = pts.ToArray();
            Weights = ws.ToArray();
            ModLog.Info("AsksvinHarness: skin donor file " + Points.Length + " vertices, " + Bones.Length + " bones");
            return true;
        }
    }

    /// <summary>The animal's skinned vertices at rest (world), with their bone weights re-indexed into the harness's bone list, in a coarse grid for nearest lookups.</summary>
    internal sealed class SkinDonor
    {
        const float Cell = 0.15f;
        readonly System.Collections.Generic.List<Vector3> _points = new System.Collections.Generic.List<Vector3>();
        readonly System.Collections.Generic.List<BoneWeight> _weights = new System.Collections.Generic.List<BoneWeight>();
        readonly System.Collections.Generic.Dictionary<Vector3Int, System.Collections.Generic.List<int>> _grid = new System.Collections.Generic.Dictionary<Vector3Int, System.Collections.Generic.List<int>>();

        static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.y / Cell), Mathf.FloorToInt(p.z / Cell));

        internal void Add(Vector3[] vertices, BoneWeight[] weights, int[] map, Matrix4x4 toWorld)
        {
            if (RuleLog.IsOn()) ModLog.Every("SkinDonor.Add", 2d, $"SkinDonor.Add(vertices={RuleLog.Shown(vertices)}, weights={RuleLog.Shown(weights)}, map={RuleLog.Shown(map)}, toWorld={RuleLog.Shown(toWorld)})");
            if (vertices == null || weights == null || weights.Length != vertices.Length) { if (RuleLog.IsOn()) ModLog.Every("SkinDonor.Add:379", 2d, "SkinDonor.Add: returns (line 379)"); return; }
            for (int i = 0; i < vertices.Length; i++)
            {
                BoneWeight w = weights[i];
                w.boneIndex0 = map[w.boneIndex0]; w.boneIndex1 = map[w.boneIndex1]; w.boneIndex2 = map[w.boneIndex2]; w.boneIndex3 = map[w.boneIndex3];
                Vector3 p = toWorld.MultiplyPoint3x4(vertices[i]);
                _points.Add(p);
                _weights.Add(w);
                System.Collections.Generic.List<int> list;
                Vector3Int k = Key(p);
                if (!_grid.TryGetValue(k, out list)) _grid[k] = list = new System.Collections.Generic.List<int>();
                list.Add(_points.Count - 1);
            }
        }

        /// <summary>The weights of the nearest donor vertex (searched out to 4 cells, 0.6 m). Expected outcome: true with its distance, false when none is that close.</summary>
        internal bool Nearest(Vector3 p, out BoneWeight weight, out float distance)
        {
            if (RuleLog.IsOn()) ModLog.Every("SkinDonor.Nearest", 2d, $"SkinDonor.Nearest(p={RuleLog.Shown(p)})");
            weight = default(BoneWeight);
            distance = float.MaxValue;
            int best = -1;
            Vector3Int k = Key(p);
            for (int r = 1; r <= 4 && best < 0; r++)
                for (int x = -r; x <= r; x++)
                    for (int y = -r; y <= r; y++)
                        for (int z = -r; z <= r; z++)
                        {
                            System.Collections.Generic.List<int> list;
                            if (!_grid.TryGetValue(new Vector3Int(k.x + x, k.y + y, k.z + z), out list)) continue;
                            foreach (int i in list)
                            {
                                float d = (_points[i] - p).sqrMagnitude;
                                if (d < distance) { distance = d; best = i; }
                            }
                        }
            if (best < 0) { if (RuleLog.IsOn()) ModLog.Every("SkinDonor.Nearest:415", 2d, "SkinDonor.Nearest: returns false (line 415)"); return false; }
            distance = Mathf.Sqrt(distance);
            weight = _weights[best];
            { if (RuleLog.IsOn()) ModLog.Every("SkinDonor.Nearest:418", 2d, "SkinDonor.Nearest: returns true (line 418)"); return true; }
        }

        /// <summary>The donor's weights scaled by <paramref name="share"/>, the rest on the root bone; four strongest kept and normalised.</summary>
        internal static BoneWeight Blend(BoneWeight body, float share, int root)
        {
            if (RuleLog.IsOn()) ModLog.Every("SkinDonor.Blend", 2d, $"SkinDonor.Blend(body={RuleLog.Shown(body)}, share={RuleLog.Shown(share)}, root={RuleLog.Shown(root)})");
            var idx = new[] { body.boneIndex0, body.boneIndex1, body.boneIndex2, body.boneIndex3, root };
            var wt = new[] { body.weight0 * share, body.weight1 * share, body.weight2 * share, body.weight3 * share, 1f - share };
            for (int i = 0; i < 5; i++)
                for (int j = i + 1; j < 5; j++)
                    if (idx[j] == idx[i] && wt[j] > 0f) { wt[i] += wt[j]; wt[j] = 0f; }
            System.Array.Sort(wt, idx);
            System.Array.Reverse(wt);
            System.Array.Reverse(idx);
            float sum = wt[0] + wt[1] + wt[2] + wt[3];
            if (sum <= 0f) { var returnedAt434 = new BoneWeight { boneIndex0 = root, weight0 = 1f }; if (RuleLog.IsOn()) ModLog.Every("SkinDonor.Blend:434", 2d, $"SkinDonor.Blend: returns {RuleLog.Shown(returnedAt434)} (line 434)"); return returnedAt434; }
            return new BoneWeight
            {
                boneIndex0 = idx[0], weight0 = wt[0] / sum, boneIndex1 = idx[1], weight1 = wt[1] / sum,
                boneIndex2 = idx[2], weight2 = wt[2] / sum, boneIndex3 = idx[3], weight3 = wt[3] / sum
            };
        }
    }
}
