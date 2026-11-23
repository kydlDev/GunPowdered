using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.Musket.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>Musket test hooks, second half (G04 split).</summary>
    internal static partial class MusketTestHooks
    {
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
            { if (RuleLog.IsOn()) ModLog.Verbose($"MusketTestHooks.ApiShotListen: returns \"listening\" (line 249)"); return "listening"; }
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
            if (KeyHints.instance == null) return "no key hints";
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
