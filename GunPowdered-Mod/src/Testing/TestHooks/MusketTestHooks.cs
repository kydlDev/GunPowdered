using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Musket.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>TestLab hooks for the Iron Musket's hand look (D-088 step 0): set the hand pose and say what the player holds.</summary>
    internal static partial class MusketTestHooks
    {
        /// <summary>Puts the musket's hand model at a position (metres) and rotation (degrees) in the item's attach space; the next equip shows it. Returns the pose set.</summary>
        [TestHook("GunPowdered.musketHandPose")]
        static string HandPose(double x, double y, double z, double rx, double ry, double rz)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.HandPose(x={x}, y={y}, z={z}, rx={rx}, ry={ry}, rz={rz})");
            var pos = new Vector3((float)x, (float)y, (float)z);
            var rot = new Vector3((float)rx, (float)ry, (float)rz);
            bool ok = MusketSetup.SetHandPose(pos, rot);
            { var returnedAt19 = (ok ? "pose " : "no model ") + pos.ToString("0.00") + " " + rot.ToString("0"); if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.HandPose: returns {returnedAt19} (line 19)"); return returnedAt19; }
        }

        /// <summary>What the local player holds right now: the item name, its animation state, whether the weapon counts as loaded, and the hand model's renderers.</summary>
        [TestHook("GunPowdered.musketHeld")]
        static string Held()
        {
            if (RuleLog.Due("MusketTestHooks.Held", 2d)) ModLog.Every("MusketTestHooks.Held", 2d, $"MusketTestHooks.Held()");
            Player p = Player.m_localPlayer;
            ItemDrop.ItemData w = p != null ? p.GetCurrentWeapon() : null;
            if (w == null) { if (RuleLog.Due("MusketTestHooks.Held:29", 2d)) ModLog.Every("MusketTestHooks.Held:29", 2d, $"MusketTestHooks.Held: returns \"nothing in hand\" because w == null (line 29)"); return "nothing in hand"; }
            int renderers = 0;
            foreach (Renderer r in p.GetComponentsInChildren<Renderer>(false)) if (r.enabled && r.transform.name.StartsWith("musket_")) renderers++;
            { var returnedAt32 = w.m_shared.m_name + ", animation " + w.m_shared.m_animationState + ", type " + w.m_shared.m_itemType + ", loaded " + p.IsWeaponLoaded() + ", musket renderers " + renderers; if (RuleLog.Due("MusketTestHooks.Held:32", 2d)) ModLog.Every("MusketTestHooks.Held:32", 2d, $"MusketTestHooks.Held: returns {returnedAt32} (line 32)"); return returnedAt32; }
        }

        /// <summary>
        /// Where the right hand joint points while the player holds the musket: the hand's local X, Y and Z axes and the hand position, all in the player's local
        /// space (forward = +Z, up = +Y, right = +X), plus the shoulder height, so the musket can be turned to point forward and put at the shoulder (D-088).
        /// </summary>
        [TestHook("GunPowdered.musketHandAxes")]
        static string HandAxes()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.HandAxes()");
            Player p = Player.m_localPlayer;
            VisEquipment vis = p != null ? p.GetComponent<VisEquipment>() : null;
            if (vis == null || vis.m_rightHand == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.HandAxes: returns \"no hand\" because vis == null || vis.m_rightHand == null (line 45)"); return "no hand"; }
            Transform hand = vis.m_rightHand, body = p.transform;
            Vector3 x = body.InverseTransformDirection(hand.right), y = body.InverseTransformDirection(hand.up), z = body.InverseTransformDirection(hand.forward);
            Vector3 pos = body.InverseTransformPoint(hand.position);
            Transform head = vis.m_helmet != null ? vis.m_helmet : null;
            { var returnedAt50 = "hand X " + x.ToString("0.00") + " Y " + y.ToString("0.00") + " Z " + z.ToString("0.00") + " at " + pos.ToString("0.00") + " head " + (head != null ? body.InverseTransformPoint(head.position).ToString("0.00") : "n/a"); if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.HandAxes: returns {returnedAt50} (line 50)"); return returnedAt50; }
        }

        /// <summary>Poses the musket model that is in the player's hand RIGHT NOW (position in metres, rotation in degrees, in the item's attach space), so poses can be tried one after the other without re-equipping.</summary>
        [TestHook("GunPowdered.musketLivePose")]
        static string LivePose(double x, double y, double z, double rx, double ry, double rz)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.LivePose(x={x}, y={y}, z={z}, rx={rx}, ry={ry}, rz={rz})");
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.LivePose: returns \"no player\" because p == null (line 59)"); return "no player"; }
            int n = 0;
            foreach (Transform t in p.GetComponentsInChildren<Transform>(false))
            {
                if (t.name != "gp_model" || t.parent == null || !t.parent.name.StartsWith("attach")) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.LivePose: skips to the next item (continue) because t.name != \"gp_model\" || t.parent == null || !t.parent.name.StartsWith(\"attach\") (line 63)"); continue; }
                t.localPosition = new Vector3((float)x, (float)y, (float)z);
                t.localEulerAngles = new Vector3((float)rx, (float)ry, (float)rz);
                n++;
            }
            { var returnedAt68 = "live pose on " + n + " model(s) " + x.ToString("0.00") + "," + y.ToString("0.00") + "," + z.ToString("0.00") + " rot " + rx.ToString("0") + "," + ry.ToString("0") + "," + rz.ToString("0"); if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.LivePose: returns {returnedAt68} (line 68)"); return returnedAt68; }
        }

        const string FreeCamName = "gp_freecam";

        /// <summary>
        /// A free camera that orbits the local player: azimuth 0 = behind, 90 = right side, 180 = front (degrees), elevation above the chest height (degrees), distance in metres.
        /// It is a second camera drawn over the game camera, so screenshots show it; freeCamOff removes it. Returns the camera position.
        /// </summary>
        [TestHook("GunPowdered.freeCam")]
        static string FreeCam(double azimuth, double elevation, double metres)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.FreeCam(azimuth={azimuth}, elevation={elevation}, metres={metres})");
            Player p = Player.m_localPlayer;
            Camera main = Camera.main;
            if (p == null || main == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.FreeCam: returns \"no player or main camera\" because p == null || main == null (line 83)"); return "no player or main camera"; }
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
            { var returnedAt98 = "free camera at " + pos.ToString("0.0"); if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.FreeCam: returns {returnedAt98} (line 98)"); return returnedAt98; }
        }

        /// <summary>Removes the free camera again.</summary>
        [TestHook("GunPowdered.freeCamOff")]
        static string FreeCamOff()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.FreeCamOff()");
            GameObject go = GameObject.Find(FreeCamName);
            if (go == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.FreeCamOff: returns \"no free camera\" because go == null (line 107)"); return "no free camera"; }
            Object.Destroy(go);
            { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.FreeCamOff: returns \"free camera removed\" (line 109)"); return "free camera removed"; }
        }

        /// <summary>
        /// Writes what the custom musket clips need to know about the player rig to the file (a lab output): the Animator's controller, avatar, parameters, the clip names
        /// of its controller, the current state hash of every layer, and the transform path of every humanoid bone. Returns a one-line count.
        /// </summary>
        [TestHook("GunPowdered.musketAnimatorDump")]
        static string AnimatorDump(string file)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.AnimatorDump(file={file})");
            Player p = Player.m_localPlayer;
            Animator anim = p != null ? p.GetComponentInChildren<Animator>() : null;
            if (anim == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.AnimatorDump: returns \"no animator\" because anim == null (line 122)"); return "no animator"; }
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
                    if (b == HumanBodyBones.LastBone) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.AnimatorDump: skips to the next item (continue) because b == HumanBodyBones.LastBone (line 136)"); continue; }
                    Transform bt = anim.GetBoneTransform(b);
                    if (bt == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.AnimatorDump: skips to the next item (continue) because bt == null (line 138)"); continue; }
                    string path = bt.name;
                    for (Transform up = bt.parent; up != null && up != anim.transform; up = up.parent) path = up.name + "/" + path;
                    sb.AppendLine("  " + b + " = " + path + " local " + bt.localPosition.ToString("0.000") + " " + bt.localEulerAngles.ToString("0"));
                    bones++;
                }
            System.IO.File.WriteAllText(file, sb.ToString());
            { var returnedAt145 = "animator dump written, " + anim.layerCount + " layers, " + bones + " bones"; if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.AnimatorDump: returns {returnedAt145} (line 145)"); return returnedAt145; }
        }

        /// <summary>Plays a musket action on the local player's arms (hold / fire / reload) and returns what the rig reports.</summary>
        [TestHook("GunPowdered.musketAction")]
        static string Action(string action)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.Action(action={action})");
            Player p = Player.m_localPlayer;
            var rig = p != null ? p.GetComponent<GunPowdered.Features.Musket.Infrastructure.MusketRig>() : null;
            if (rig == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.Action: returns \"no musket rig\" because rig == null (line 155)"); return "no musket rig"; }
            if (!System.Enum.TryParse(action, true, out GunPowdered.Features.Musket.Domain.MusketAction a)) return "unknown action " + action;
            rig.Play(a);
            { var returnedAt158 = "playing " + rig.CurrentAction; if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.Action: returns {returnedAt158} (line 158)"); return returnedAt158; }
        }

        /// <summary>
        /// Presses the attack button (secondary = false: fire, the way the game does: Humanoid.StartAttack, so MusketAttackPatch runs) or the reload key
        /// (secondary = true: the same code the [Musket] Reload Key runs, D-100). Returns what the press did, the load and whether a reload runs:
        /// "&lt;result&gt;; loaded N; reloading B" (D-095).
        /// </summary>
        [TestHook("GunPowdered.musketPress")]
        static string Press(bool secondary)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.Press(secondary={secondary})");
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.Press: returns \"no player\" because p == null (line 171)"); return "no player"; }
            if (secondary) MusketInput.ReloadPressed(p);
            else p.StartAttack(null, false);
            string result = secondary ? MusketReloader.LastBegin : MusketGun.LastAttempt;
            string pressed = result + "; loaded " + MusketGun.Loaded(MusketGun.Held(p)) + "; reloading " + MusketReloader.IsReloading(p);
            ModLog.Verbose("MusketTestHooks.Press: returns " + pressed);
            return pressed;
        }

        /// <summary>Presses the game's secondary attack (middle mouse) with the musket in hand: it must do nothing now (D-100). Returns "loaded N; reloading B".</summary>
        [TestHook("GunPowdered.musketSecondaryAttack")]
        static string SecondaryAttack()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.SecondaryAttack()");
            Player p = Player.m_localPlayer;
            if (p == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.SecondaryAttack: returns \"no player\" because p == null (line 186)"); return "no player"; }
            p.StartAttack(null, true);
            string state = "loaded " + MusketGun.Loaded(MusketGun.Held(p)) + "; reloading " + MusketReloader.IsReloading(p);
            ModLog.Verbose("MusketTestHooks.SecondaryAttack: returns " + state);
            return state;
        }

        /// <summary>The key bound to the musket reload ([Musket] Reload Key).</summary>
        [TestHook("GunPowdered.musketReloadKey")]
        static string ReloadKey() => GunPowdered.Configuration.ModSettings.MusketReloadKey != null ? GunPowdered.Configuration.ModSettings.MusketReloadKey.Value.ToString() : "none";

        /// <summary>What the last reload start did (MusketReloader.Begin): "reloading", "already reloading", the refusal, or "not pressed yet".</summary>
        [TestHook("GunPowdered.musketLastBegin")]
        static string LastBegin() => MusketReloader.LastBegin;

        /// <summary>How many cartridges the musket in hand holds (-1 without a musket in hand).</summary>
        [TestHook("GunPowdered.musketLoaded")]
        static int Loaded()
        {
            if (RuleLog.Due("MusketTestHooks.Loaded", 2d)) ModLog.Every("MusketTestHooks.Loaded", 2d, $"MusketTestHooks.Loaded()");
            ItemDrop.ItemData m = MusketGun.Held(Player.m_localPlayer);
            { var returnedAt207 = m != null ? MusketGun.Loaded(m) : -1; if (RuleLog.Due("MusketTestHooks.Loaded:207", 2d)) ModLog.Every("MusketTestHooks.Loaded:207", 2d, $"MusketTestHooks.Loaded: returns {returnedAt207} (line 207)"); return returnedAt207; }
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
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.BulletFlew(metres={metres})");
            GameObject b = MusketGun.LastBullet;
            if (b == null) { var returnedAt223 = MusketGun.LastBulletReach >= metres; if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.BulletFlew: returns {returnedAt223} because b == null (line 223)"); return returnedAt223; }
            { var returnedAt224 = Vector3.Distance(b.transform.position, MusketGun.LastMuzzle) >= metres || MusketGun.LastBulletReach >= metres; if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.BulletFlew: returns {returnedAt224} (line 224)"); return returnedAt224; }
        }

        static int _apiShots, _apiLastLeft = -1;
        static bool _apiListening;
        // Open for the whole test session: the failing listener below makes ShotFired warn ON PURPOSE; the warning is written as Info and counted here (T-023).
        static KydlCore.Common.ExpectedWarning _apiExpectedFailure;

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
                _apiExpectedFailure = KydlCore.Common.ModLog.ExpectWarning("a listener failed");
                _apiListening = true;
            }
            KydlCore.Common.ModLog.Verbose("GunPowdered.apiShotListen: listening, the failing listener's warning is expected");
            return "listening";
        }

        /// <summary>The shots the API listener saw since apiShotListen: "N shot(s), last left L".</summary>
        [TestHook("GunPowdered.apiShots")]
        static string ApiShots() => _apiShots + " shot(s), last left " + _apiLastLeft + ", " + (_apiExpectedFailure == null ? 0 : _apiExpectedFailure.Count) + " expected listener warning(s)";

        /// <summary>Sets the held musket's load through the public API and reads it back: "set S, read R" (-1 = not a musket).</summary>
        [TestHook("GunPowdered.apiSetLoaded")]
        static string ApiSetLoaded(int loaded)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.ApiSetLoaded(loaded={loaded})");
            ItemDrop.ItemData m = GunPowderedApi.Musket.HeldBy(Player.m_localPlayer);
            int set = GunPowderedApi.Musket.SetLoaded(m, loaded);
            { var returnedAt263 = "set " + set + ", read " + GunPowderedApi.Musket.GetLoaded(m); if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.ApiSetLoaded: returns {returnedAt263} (line 263)"); return returnedAt263; }
        }

        /// <summary>What the load counter next to the crosshair shows ("3/5"), or "hidden".</summary>
        [TestHook("GunPowdered.musketLoadText")]
        static string LoadText() => MusketAmmoHud.Current;

        /// <summary>The key hint texts shown at the bottom right right now (active ones only), joined with " | ".</summary>
        [TestHook("GunPowdered.keyHintTexts")]
        static string KeyHintTexts()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.KeyHintTexts()");
            if (KeyHints.instance == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.KeyHintTexts: returns \"no key hints\" because KeyHints.instance == null (line 275)"); return "no key hints"; }
            var texts = new System.Collections.Generic.List<string>();
            foreach (TMPro.TMP_Text t in KeyHints.instance.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if (!string.IsNullOrWhiteSpace(t.text) && !texts.Contains(t.text.Trim())) texts.Add(t.text.Trim());
            { var returnedAt279 = string.Join(" | ", texts); if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.KeyHintTexts: returns {returnedAt279} (line 279)"); return returnedAt279; }
        }

        /// <summary>Starts a new shake measurement on the local player's musket rig (D-099).</summary>
        [TestHook("GunPowdered.musketShakeReset")]
        static string ShakeReset()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.ShakeReset()");
            var rig = Player.m_localPlayer != null ? Player.m_localPlayer.GetComponent<MusketRig>() : null;
            if (rig == null) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.ShakeReset: returns \"no musket rig\" because rig == null (line 288)"); return "no musket rig"; }
            rig.ResetMeasure();
            { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.ShakeReset: returns \"measuring\" (line 290)"); return "measuring"; }
        }

        /// <summary>How much the musket moved up and down since musketShakeReset: "frames N, skipped S, range R mm, max step M mm".</summary>
        [TestHook("GunPowdered.musketShake")]
        static string Shake()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.Shake()");
            var rig = Player.m_localPlayer != null ? Player.m_localPlayer.GetComponent<MusketRig>() : null;
            { var returnedAt299 = rig == null ? "no musket rig" : rig.ShakeReport(); if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.Shake: returns {returnedAt299} (line 299)"); return returnedAt299; }
        }

        /// <summary>Test switch: off = the old behaviour (the pose is read back and written every frame) to compare. Returns the state.</summary>
        [TestHook("GunPowdered.musketSkipGuard")]
        static string SkipGuard(bool on)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.SkipGuard(on={on})");
            MusketRig.NoSkipGuard = !on;
            { var returnedAt308 = "skip guard " + (on ? "on" : "off"); if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.SkipGuard: returns {returnedAt308} (line 308)"); return returnedAt308; }
        }

        /// <summary>What the last shot did (muzzle, direction, flight, effects, load left).</summary>
        [TestHook("GunPowdered.musketLastShot")]
        static string LastShot() => MusketGun.LastShot;

        /// <summary>What the local player's musket rig did in the last frame.</summary>
        [TestHook("GunPowdered.musketRig")]
        static string Rig()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.Rig()");
            Player p = Player.m_localPlayer;
            var rig = p != null ? p.GetComponent<GunPowdered.Features.Musket.Infrastructure.MusketRig>() : null;
            { var returnedAt322 = rig == null ? "no musket rig" : rig.LastReport; if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.Rig: returns {returnedAt322} (line 322)"); return returnedAt322; }
        }

        // ---- D-155 aim / side carry / off-hand / reload while moving / block stats

        /// <summary>Presses the aim key the way the game does (MusketInput.AimPressed -> MusketRig.Aim): "raising", "running", "already aimed".</summary>
        [TestHook("GunPowdered.musketAim")]
        static string Aim()
        {
            ModLog.Verbose("MusketTestHooks.Aim()");
            string s = MusketInput.AimPressed(Player.m_localPlayer);
            ModLog.Info("MusketTestHooks.Aim: returns " + s);
            return s;
        }

        /// <summary>The local player's musket state: "raised R, ready F; REPORT".</summary>
        [TestHook("GunPowdered.musketRaised")]
        static string RaisedState()
        {
            ModLog.Verbose("MusketTestHooks.RaisedState()");
            var rig = Player.m_localPlayer != null ? Player.m_localPlayer.GetComponent<MusketRig>() : null;
            string s = rig == null ? "no musket rig" : "raised " + rig.Raised + ", ready " + rig.ReadyToFire + "; " + rig.LastReport;
            ModLog.Info("MusketTestHooks.RaisedState: returns " + s);
            return s;
        }

        /// <summary>The rig takes this ground speed (m/s) as the local player's (above 5 = running) instead of the real one; below 0 switches it off. Returns "speed S".</summary>
        [TestHook("GunPowdered.musketTestSpeed")]
        static string TestSpeed(double speed)
        {
            ModLog.Verbose("MusketTestHooks.TestSpeed(speed=" + speed + ")");
            MusketRig.TestSpeed = (float)speed;
            string s = "speed " + MusketRig.TestSpeed.ToString("0.0");
            ModLog.Info("MusketTestHooks.TestSpeed: returns " + s);
            return s;
        }

        /// <summary>Whether the game counts the local player as on the ground right now: "on ground G".</summary>
        [TestHook("GunPowdered.onGround")]
        static string OnGround()
        {
            ModLog.Verbose("MusketTestHooks.OnGround()");
            Player p = Player.m_localPlayer;
            string s = p == null ? "no player" : "on ground " + p.IsOnGround();
            ModLog.Verbose("MusketTestHooks.OnGround: returns " + s);
            return s;
        }

        /// <summary>The Iron Musket item's block and speed values: "block B, per level L, parry P, parry adrenaline A, block adrenaline D, movement M".</summary>
        [TestHook("GunPowdered.musketStats")]
        static string Stats()
        {
            ModLog.Verbose("MusketTestHooks.Stats()");
            GameObject item = MusketSetup.Item;
            ItemDrop drop = item != null ? item.GetComponent<ItemDrop>() : null;
            if (drop == null) { ModLog.Verbose("MusketTestHooks.Stats: no musket item - returns why"); return "no musket item"; }
            var sh = drop.m_itemData.m_shared;
            string s = $"block {sh.m_blockPower:0.##}, per level {sh.m_blockPowerPerLevel:0.##}, parry {sh.m_timedBlockBonus:0.##}, parry adrenaline {sh.m_perfectBlockAdrenaline:0.##}, block adrenaline {sh.m_blockAdrenaline:0.##}, movement {sh.m_movementModifier:0.##}";
            ModLog.Info("MusketTestHooks.Stats: returns " + s);
            return s;
        }

        /// <summary>
        /// Another player's musket rig as THIS client renders it (DrakesTestLab virtual player Bjornita, peerMirror): the action it plays and what it
        /// posed last frame - "player P: action A, holding H; REPORT". The other client's actions arrive through the player's ZDO (D-098).
        /// </summary>
        [TestHook("GunPowdered.musketRigOf")]
        static string RigOf(string name)
        {
            ModLog.Verbose("MusketTestHooks.RigOf(name=" + name + ")");
            foreach (Player p in Player.GetAllPlayers())
            {
                if (p == null || p == Player.m_localPlayer || p.GetPlayerName().IndexOf(name ?? "", System.StringComparison.OrdinalIgnoreCase) < 0) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.RigOf: skips to the next item (continue) because p == null || p == Player.m_localPlayer || p.GetPlayerName().IndexOf(name ?? \"\", System.StringComparison.OrdinalIgnoreCase) < 0 (line 394)"); continue; }
                var rig = p.GetComponent<MusketRig>();
                string s = rig == null
                    ? "player " + p.GetPlayerName() + ": no musket rig"
                    : "player " + p.GetPlayerName() + ": action " + rig.CurrentAction + ", musket model shown " + rig.TryMuzzle(out _) + ", slots " + Shown(p) + ", skipped frames " + rig.SkippedFrames + "; " + rig.LastReport;
                ModLog.Info("MusketTestHooks.RigOf: returns " + s);
                return s;
            }
            ModLog.Verbose("MusketTestHooks.RigOf: no other player named " + name + " - returns why");
            return "no player " + name;
        }

        // what the player's character shows in the right hand (a remote player's inventory is not synced, its VisEquipment is)
        static string Shown(Player p)
        {
            VisEquipment vis = p.GetComponent<VisEquipment>();
            // VisEquipment.m_rightItem is the item's prefab hash (int) in this game version, not a name (lab a17bjornita1: "Specified cast is not valid")
            // the musket is a bow-type item: the game puts it in the LEFT hand slot (MusketRig moves the model to the right hand), so both slots are read
            string s = "right " + ItemIn(vis, "m_rightItem") + ", left " + ItemIn(vis, "m_leftItem");
            ModLog.Verbose("MusketTestHooks.Shown: returns " + s);
            return s;
        }

        // the prefab name behind a VisEquipment item slot (the slot keeps the item's prefab hash)
        static string ItemIn(VisEquipment vis, string field)
        {
            int hash = vis != null ? HarmonyLib.Traverse.Create(vis).Field(field).GetValue<int>() : 0;
            GameObject prefab = hash != 0 && ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(hash) : null;
            string s = hash == 0 ? "nothing" : prefab != null ? prefab.name : "item hash " + hash;
            ModLog.Verbose("MusketTestHooks.ItemIn(" + field + "): returns " + s);
            return s;
        }
    }
}
