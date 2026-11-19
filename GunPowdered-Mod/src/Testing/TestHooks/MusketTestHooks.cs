using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Musket.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>TestLab hooks for the Iron Musket's hand look (D-088 step 0): set the hand pose and say what the player holds.</summary>
    internal static class MusketTestHooks
    {
        /// <summary>Puts the musket's hand model at a position (metres) and rotation (degrees) in the item's attach space; the next equip shows it. Returns the pose set.</summary>
        [TestHook("GunPowdered.musketHandPose")]
        static string HandPose(double x, double y, double z, double rx, double ry, double rz)
        {
            var pos = new Vector3((float)x, (float)y, (float)z);
            var rot = new Vector3((float)rx, (float)ry, (float)rz);
            bool ok = MusketSetup.SetHandPose(pos, rot);
            return (ok ? "pose " : "no model ") + pos.ToString("0.00") + " " + rot.ToString("0");
        }

        /// <summary>What the local player holds right now: the item name, its animation state, whether the weapon counts as loaded, and the hand model's renderers.</summary>
        [TestHook("GunPowdered.musketHeld")]
        static string Held()
        {
            Player p = Player.m_localPlayer;
            ItemDrop.ItemData w = p != null ? p.GetCurrentWeapon() : null;
            if (w == null) return "nothing in hand";
            int renderers = 0;
            foreach (Renderer r in p.GetComponentsInChildren<Renderer>(false)) if (r.enabled && r.transform.name.StartsWith("musket_")) renderers++;
            return w.m_shared.m_name + ", animation " + w.m_shared.m_animationState + ", type " + w.m_shared.m_itemType + ", loaded " + p.IsWeaponLoaded() + ", musket renderers " + renderers;
        }

        /// <summary>
        /// Where the right hand joint points while the player holds the musket: the hand's local X, Y and Z axes and the hand position, all in the player's local
        /// space (forward = +Z, up = +Y, right = +X), plus the shoulder height, so the musket can be turned to point forward and put at the shoulder (D-088).
        /// </summary>
        [TestHook("GunPowdered.musketHandAxes")]
        static string HandAxes()
        {
            Player p = Player.m_localPlayer;
            VisEquipment vis = p != null ? p.GetComponent<VisEquipment>() : null;
            if (vis == null || vis.m_rightHand == null) return "no hand";
            Transform hand = vis.m_rightHand, body = p.transform;
            Vector3 x = body.InverseTransformDirection(hand.right), y = body.InverseTransformDirection(hand.up), z = body.InverseTransformDirection(hand.forward);
            Vector3 pos = body.InverseTransformPoint(hand.position);
            Transform head = vis.m_helmet != null ? vis.m_helmet : null;
            return "hand X " + x.ToString("0.00") + " Y " + y.ToString("0.00") + " Z " + z.ToString("0.00") + " at " + pos.ToString("0.00") + " head " + (head != null ? body.InverseTransformPoint(head.position).ToString("0.00") : "n/a");
        }

        /// <summary>Poses the musket model that is in the player's hand RIGHT NOW (position in metres, rotation in degrees, in the item's attach space), so poses can be tried one after the other without re-equipping.</summary>
        [TestHook("GunPowdered.musketLivePose")]
        static string LivePose(double x, double y, double z, double rx, double ry, double rz)
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            int n = 0;
            foreach (Transform t in p.GetComponentsInChildren<Transform>(false))
            {
                if (t.name != "gp_model" || t.parent == null || !t.parent.name.StartsWith("attach")) continue;
                t.localPosition = new Vector3((float)x, (float)y, (float)z);
                t.localEulerAngles = new Vector3((float)rx, (float)ry, (float)rz);
                n++;
            }
            return "live pose on " + n + " model(s) " + x.ToString("0.00") + "," + y.ToString("0.00") + "," + z.ToString("0.00") + " rot " + rx.ToString("0") + "," + ry.ToString("0") + "," + rz.ToString("0");
        }

        const string FreeCamName = "gp_freecam";

        /// <summary>
        /// A free camera that orbits the local player: azimuth 0 = behind, 90 = right side, 180 = front (degrees), elevation above the chest height (degrees), distance in metres.
        /// It is a second camera drawn over the game camera, so screenshots show it; freeCamOff removes it. Returns the camera position.
        /// </summary>
        [TestHook("GunPowdered.freeCam")]
        static string FreeCam(double azimuth, double elevation, double metres)
        {
            Player p = Player.m_localPlayer;
            Camera main = Camera.main;
            if (p == null || main == null) return "no player or main camera";
            GameObject go = GameObject.Find(FreeCamName);
            if (go == null)
            {
                go = new GameObject(FreeCamName);
                go.AddComponent<Camera>();
            }
            Camera cam = go.GetComponent<Camera>();
            cam.CopyFrom(main);
            cam.depth = main.depth + 1f;
            Vector3 centre = p.transform.position + Vector3.up * 1.3f;
            Vector3 behind = -p.transform.forward;
            Vector3 pos = centre + Quaternion.AngleAxis(-(float)azimuth, Vector3.up) * (Quaternion.AngleAxis((float)elevation, p.transform.right) * behind) * (float)metres;
            go.transform.position = pos;
            go.transform.LookAt(centre);
            return "free camera at " + pos.ToString("0.0");
        }

        /// <summary>Removes the free camera again.</summary>
        [TestHook("GunPowdered.freeCamOff")]
        static string FreeCamOff()
        {
            GameObject go = GameObject.Find(FreeCamName);
            if (go == null) return "no free camera";
            Object.Destroy(go);
            return "free camera removed";
        }

        /// <summary>
        /// Writes what the custom musket clips need to know about the player rig to the file (a lab output): the Animator's controller, avatar, parameters, the clip names
        /// of its controller, the current state hash of every layer, and the transform path of every humanoid bone. Returns a one-line count.
        /// </summary>
        [TestHook("GunPowdered.musketAnimatorDump")]
        static string AnimatorDump(string file)
        {
            Player p = Player.m_localPlayer;
            Animator anim = p != null ? p.GetComponentInChildren<Animator>() : null;
            if (anim == null) return "no animator";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("controller " + (anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "none") + ", avatar " + (anim.avatar != null ? anim.avatar.name : "none") + ", humanoid " + anim.isHuman + ", root " + anim.transform.name);
            sb.AppendLine("layers " + anim.layerCount);
            for (int i = 0; i < anim.layerCount; i++) sb.AppendLine("  layer " + i + " " + anim.GetLayerName(i) + " weight " + anim.GetLayerWeight(i) + " state " + anim.GetCurrentAnimatorStateInfo(i).shortNameHash + " length " + anim.GetCurrentAnimatorStateInfo(i).length.ToString("0.00"));
            sb.AppendLine("parameters");
            foreach (AnimatorControllerParameter prm in anim.parameters) sb.AppendLine("  " + prm.type + " " + prm.name);
            sb.AppendLine("clips");
            if (anim.runtimeAnimatorController != null) foreach (AnimationClip c in anim.runtimeAnimatorController.animationClips) sb.AppendLine("  " + c.name + " " + c.length.ToString("0.00") + " s, loop " + c.isLooping + ", humanMotion " + c.humanMotion);
            sb.AppendLine("bones (HumanBodyBones -> path from the animator root)");
            int bones = 0;
            if (anim.isHuman)
                foreach (HumanBodyBones b in System.Enum.GetValues(typeof(HumanBodyBones)))
                {
                    if (b == HumanBodyBones.LastBone) continue;
                    Transform bt = anim.GetBoneTransform(b);
                    if (bt == null) continue;
                    string path = bt.name;
                    for (Transform up = bt.parent; up != null && up != anim.transform; up = up.parent) path = up.name + "/" + path;
                    sb.AppendLine("  " + b + " = " + path + " local " + bt.localPosition.ToString("0.000") + " " + bt.localEulerAngles.ToString("0"));
                    bones++;
                }
            System.IO.File.WriteAllText(file, sb.ToString());
            return "animator dump written, " + anim.layerCount + " layers, " + bones + " bones";
        }

        /// <summary>Plays a musket action on the local player's arms (hold / fire / reload) and returns what the rig reports.</summary>
        [TestHook("GunPowdered.musketAction")]
        static string Action(string action)
        {
            Player p = Player.m_localPlayer;
            var rig = p != null ? p.GetComponent<GunPowdered.Features.Musket.Infrastructure.MusketRig>() : null;
            if (rig == null) return "no musket rig";
            if (!System.Enum.TryParse(action, true, out GunPowdered.Features.Musket.Domain.MusketAction a)) return "unknown action " + action;
            rig.Play(a);
            return "playing " + rig.CurrentAction;
        }

        /// <summary>
        /// Presses the attack button (secondary = false: fire) or the reload key (secondary = true) the way the game does (Humanoid.StartAttack, so
        /// MusketAttackPatch runs). Returns what the press did, the load and whether a reload runs: "&lt;result&gt;; loaded N; reloading B" (D-095).
        /// </summary>
        [TestHook("GunPowdered.musketPress")]
        static string Press(bool secondary)
        {
            Player p = Player.m_localPlayer;
            if (p == null) return "no player";
            p.StartAttack(null, secondary);
            string result = secondary ? MusketReloader.LastBegin : MusketGun.LastAttempt;
            return result + "; loaded " + MusketGun.Loaded(MusketGun.Held(p)) + "; reloading " + MusketReloader.IsReloading(p);
        }

        /// <summary>How many cartridges the musket in hand holds (-1 without a musket in hand).</summary>
        [TestHook("GunPowdered.musketLoaded")]
        static int Loaded()
        {
            ItemDrop.ItemData m = MusketGun.Held(Player.m_localPlayer);
            return m != null ? MusketGun.Loaded(m) : -1;
        }

        /// <summary>Why the last reload ended and how many cartridges it moved in.</summary>
        [TestHook("GunPowdered.musketReloadEnd")]
        static string ReloadEnd() => MusketReloader.LastEnd;

        /// <summary>
        /// Whether the last bullet flew at least <paramref name="metres"/> from where it was fired (it may have hit something since: then false unless it got
        /// that far before). Returns true / false.
        /// </summary>
        [TestHook("GunPowdered.musketBulletFlew")]
        static bool BulletFlew(double metres)
        {
            GameObject b = MusketGun.LastBullet;
            if (b == null) return MusketGun.LastBulletReach >= metres;
            return Vector3.Distance(b.transform.position, MusketGun.LastMuzzle) >= metres || MusketGun.LastBulletReach >= metres;
        }

        static int _apiShots, _apiLastLeft = -1;
        static bool _apiListening;

        /// <summary>
        /// Listens to the public GunPowderedApi.Musket.ShotFired like another mod would (D-097), plus one listener that always throws (it must not stop
        /// the shot). Returns "listening".
        /// </summary>
        [TestHook("GunPowdered.apiShotListen")]
        static string ApiShotListen()
        {
            _apiShots = 0;
            _apiLastLeft = -1;
            if (!_apiListening)
            {
                GunPowderedApi.Musket.ShotFired += s => { _apiShots++; _apiLastLeft = s.CartridgesLeft; };
                GunPowderedApi.Musket.ShotFired += s => { throw new System.InvalidOperationException("test listener that fails on purpose"); };
                _apiListening = true;
            }
            return "listening";
        }

        /// <summary>The shots the API listener saw since apiShotListen: "N shot(s), last left L".</summary>
        [TestHook("GunPowdered.apiShots")]
        static string ApiShots() => _apiShots + " shot(s), last left " + _apiLastLeft;

        /// <summary>Sets the held musket's load through the public API and reads it back: "set S, read R" (-1 = not a musket).</summary>
        [TestHook("GunPowdered.apiSetLoaded")]
        static string ApiSetLoaded(int loaded)
        {
            ItemDrop.ItemData m = GunPowderedApi.Musket.HeldBy(Player.m_localPlayer);
            int set = GunPowderedApi.Musket.SetLoaded(m, loaded);
            return "set " + set + ", read " + GunPowderedApi.Musket.GetLoaded(m);
        }

        /// <summary>What the load counter next to the crosshair shows ("3/5"), or "hidden".</summary>
        [TestHook("GunPowdered.musketLoadText")]
        static string LoadText() => MusketAmmoHud.Current;

        /// <summary>The key hint texts shown at the bottom right right now (active ones only), joined with " | ".</summary>
        [TestHook("GunPowdered.keyHintTexts")]
        static string KeyHintTexts()
        {
            if (KeyHints.instance == null) return "no key hints";
            var texts = new System.Collections.Generic.List<string>();
            foreach (TMPro.TMP_Text t in KeyHints.instance.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if (!string.IsNullOrWhiteSpace(t.text) && !texts.Contains(t.text.Trim())) texts.Add(t.text.Trim());
            return string.Join(" | ", texts);
        }

        /// <summary>Starts a new shake measurement on the local player's musket rig (D-099).</summary>
        [TestHook("GunPowdered.musketShakeReset")]
        static string ShakeReset()
        {
            var rig = Player.m_localPlayer != null ? Player.m_localPlayer.GetComponent<MusketRig>() : null;
            if (rig == null) return "no musket rig";
            rig.ResetMeasure();
            return "measuring";
        }

        /// <summary>How much the musket moved up and down since musketShakeReset: "frames N, skipped S, range R mm, max step M mm".</summary>
        [TestHook("GunPowdered.musketShake")]
        static string Shake()
        {
            var rig = Player.m_localPlayer != null ? Player.m_localPlayer.GetComponent<MusketRig>() : null;
            return rig == null ? "no musket rig" : rig.ShakeReport();
        }

        /// <summary>Test switch: off = the old behaviour (the pose is read back and written every frame) to compare. Returns the state.</summary>
        [TestHook("GunPowdered.musketSkipGuard")]
        static string SkipGuard(bool on)
        {
            MusketRig.NoSkipGuard = !on;
            return "skip guard " + (on ? "on" : "off");
        }

        /// <summary>What the last shot did (muzzle, direction, flight, effects, load left).</summary>
        [TestHook("GunPowdered.musketLastShot")]
        static string LastShot() => MusketGun.LastShot;

        /// <summary>What the local player's musket rig did in the last frame.</summary>
        [TestHook("GunPowdered.musketRig")]
        static string Rig()
        {
            Player p = Player.m_localPlayer;
            var rig = p != null ? p.GetComponent<GunPowdered.Features.Musket.Infrastructure.MusketRig>() : null;
            return rig == null ? "no musket rig" : rig.LastReport;
        }
    }
}
