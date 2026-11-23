using Jotunn.Configs;
using Jotunn.Managers;
using KydlCore.Common;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The musket's own reload key (user 2026-11-19 13:14, D-100: "a separate reload key (R) instead of middle mouse"): [Musket] Reload Key, default R,
    /// registered with Jotunn's input manager so the key hint shows whatever key the player bound. Read once per frame for the local player while the
    /// musket is in hand and no chat, console, menu, inventory, text box or map is open.
    /// </summary>
    internal static class MusketInput
    {
        internal const string ReloadButtonName = "GunPowdered_MusketReload", AimButtonName = "GunPowdered_MusketAim";
        static ButtonConfig _reload, _aim;
        static bool _registered;

        /// <summary>D-155: the aim button ([Musket] Aim Key, default the mouse wheel button), also used by the key hint.</summary>
        internal static ButtonConfig AimButton
        {
            get
            {
                if (_aim == null && ModSettings.MusketAimKey != null)
                    _aim = new ButtonConfig { Name = AimButtonName, Config = ModSettings.MusketAimKey, Hint = MusketRules.HintAim, ActiveInCustomGUI = false };
                return _aim;
            }
        }

        /// <summary>D-155: the aim key as the player bound it, for messages ("Mouse2" reads as "Mouse wheel").</summary>
        internal static string AimKeyText
        {
            get
            {
                UnityEngine.KeyCode key = ModSettings.MusketAimKey != null ? ModSettings.MusketAimKey.Value : UnityEngine.KeyCode.None;
                string s = key == UnityEngine.KeyCode.Mouse2 ? "mouse wheel button" : key == UnityEngine.KeyCode.None ? "" : key.ToString();
                if (RuleLog.Due("MusketInput.AimKeyText", 2d)) ModLog.Every("MusketInput.AimKeyText", 2d, "MusketInput.AimKeyText: returns " + s);
                return s;
            }
        }

        /// <summary>
        /// D-155: once per frame for the local player: the aim key raises the musket from the side (MusketRig.Aim). The game's own key read, like the reload
        /// key. Expected outcome: what the press did, or null without a press.
        /// </summary>
        static string PollAim(Player local)
        {
            UnityEngine.KeyCode key = ModSettings.MusketAimKey != null ? ModSettings.MusketAimKey.Value : UnityEngine.KeyCode.None;
            bool down = key != UnityEngine.KeyCode.None && (ZInput.GetKeyDown(key, true) || UnityEngine.Input.GetKeyDown(key));
            if (!down) { if (RuleLog.Due("MusketInput.PollAim:none", 2d)) ModLog.Every("MusketInput.PollAim:none", 2d, "MusketInput.PollAim: returns null (no press)"); return null; }
            string blocked = Blocker();
            if (MusketGun.Held(local) == null || blocked != null) { ModLog.Verbose("MusketInput: aim key " + key + " down - no musket in hand or blocked by " + (blocked ?? "nothing")); return null; }
            string result = AimPressed(local);
            ModLog.Info("MusketInput: aim key " + key + " down -> " + result);
            return result;
        }

        /// <summary>D-155: the aim key pressed (test hooks call this too). Expected outcome: what MusketRig.Aim answered.</summary>
        internal static string AimPressed(Player local)
        {
            MusketRig rig = local != null ? local.GetComponent<MusketRig>() : null;
            string result = rig != null ? rig.Aim() : "no musket rig";
            ModLog.Verbose("MusketInput: AimPressed for " + (local != null ? local.GetPlayerName() : "nobody") + " -> " + result);
            return result;
        }

        /// <summary>The reload button (also used by the key hint).</summary>
        internal static ButtonConfig ReloadButton
        {
            get
            {
                if (_reload == null && ModSettings.MusketReloadKey != null)
                    _reload = new ButtonConfig { Name = ReloadButtonName, Config = ModSettings.MusketReloadKey, Hint = MusketRules.HintReload, ActiveInCustomGUI = false };
                return _reload;
            }
        }

        /// <summary>Registers the button with Jotunn (once, at start). Expected outcome: true when it is registered.</summary>
        internal static bool Register()
        {
            if (_registered) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketInput.Register: returns true because _registered (line 84)"); return true; }
            ModLog.Verbose("MusketInput: Register - key setting " + (ModSettings.MusketReloadKey != null ? ModSettings.MusketReloadKey.Value.ToString() : "missing"));
            if (ReloadButton == null)
            {
                ModLog.Warn("MusketInput: no [Musket] Reload Key setting - the musket has no reload key");
                return false;
            }
            InputManager.Instance.AddButton(Plugin.ModGuid, ReloadButton);
            if (AimButton != null) InputManager.Instance.AddButton(Plugin.ModGuid, AimButton);     // D-155: for the key hint
            _registered = true;
            ModLog.Verbose("MusketInput: reload key " + ModSettings.MusketReloadKey.Value + " registered");
            return true;
        }

        /// <summary>Once per frame for the local player: the reload key starts a reload. Expected outcome: what the press did, or null without a press.</summary>
        internal static string Poll(Player local)
        {
            PollAim(local);
            UnityEngine.KeyCode key = ModSettings.MusketReloadKey != null ? ModSettings.MusketReloadKey.Value : UnityEngine.KeyCode.None;
            // the game's own key read (ZInput.GetKeyDown, like KydlCore's hub key) is what starts the reload. The Jotunn button alone never fired in game
            // (user 2026-11-20 09:08, lab a10rkey1: key down, button False); it stays registered for the key hint and is logged next to the key read.
            bool gameKey = key != UnityEngine.KeyCode.None && ZInput.GetKeyDown(key, true);
            bool button = _registered && ZInput.GetButtonDown(ReloadButtonName);
            bool raw = key != UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyDown(key);
            if (!gameKey && !button && !raw) { if (RuleLog.Due("MusketInput.Poll:108", 2d)) ModLog.Every("MusketInput.Poll:108", 2d, $"MusketInput.Poll: returns null because !gameKey && !button && !raw (line 108)"); return null; }                       // nothing pressed this frame: nothing to log
            bool held = MusketGun.Held(local) != null;
            string blocked = Blocker();
            ModLog.Verbose("MusketInput: reload key " + key + " down - game key " + gameKey + ", Jotunn button " + button + ", raw key " + raw
                + ", musket in hand " + held + ", blocked by " + (blocked ?? "nothing"));
            if (!held || blocked != null) { if (RuleLog.Due("MusketInput.Poll:113", 2d)) ModLog.Every("MusketInput.Poll:113", 2d, $"MusketInput.Poll: returns null because !held || blocked != null (line 113)"); return null; }
            if (!gameKey && !button)
            {
                ModLog.Verbose("MusketInput: only the raw key saw the press, the game's input did not - no reload");
                return null;
            }
            string result = ReloadPressed(local);
            ModLog.Verbose("MusketInput: reload key -> " + result);
            return result;
        }

        /// <summary>The reload key pressed (test hooks call this too). Expected outcome: what MusketReloader.Begin answered.</summary>
        internal static string ReloadPressed(Player local)
        {
            string result = MusketReloader.Begin(local);
            ModLog.Verbose("MusketInput: ReloadPressed for " + (local != null ? local.GetPlayerName() : "nobody") + " -> " + result);
            return result;
        }

        /// <summary>The window that takes the keys right now (chat, console, menu, inventory, text box, map), or null when none does.</summary>
        static string Blocker()
        {
            if (RuleLog.Due("MusketInput.Blocker", 2d)) ModLog.Every("MusketInput.Blocker", 2d, $"MusketInput.Blocker()");
            if (Chat.instance != null && Chat.instance.HasFocus()) { if (RuleLog.Due("MusketInput.Blocker:86", 2d)) ModLog.Every("MusketInput.Blocker:136", 2d, "MusketInput.Blocker: returns \"chat\" (line 136)"); return "chat"; }
            if (Console.IsVisible()) { if (RuleLog.Due("MusketInput.Blocker:87", 2d)) ModLog.Every("MusketInput.Blocker:137", 2d, "MusketInput.Blocker: returns \"console\" (line 137)"); return "console"; }
            if (Menu.IsVisible()) { if (RuleLog.Due("MusketInput.Blocker:88", 2d)) ModLog.Every("MusketInput.Blocker:138", 2d, "MusketInput.Blocker: returns \"menu\" (line 138)"); return "menu"; }
            if (InventoryGui.IsVisible()) { if (RuleLog.Due("MusketInput.Blocker:89", 2d)) ModLog.Every("MusketInput.Blocker:139", 2d, "MusketInput.Blocker: returns \"inventory\" (line 139)"); return "inventory"; }
            if (TextInput.IsVisible()) { if (RuleLog.Due("MusketInput.Blocker:90", 2d)) ModLog.Every("MusketInput.Blocker:140", 2d, "MusketInput.Blocker: returns \"text input\" (line 140)"); return "text input"; }
            if (Minimap.IsOpen()) { if (RuleLog.Due("MusketInput.Blocker:91", 2d)) ModLog.Every("MusketInput.Blocker:141", 2d, "MusketInput.Blocker: returns \"map\" (line 141)"); return "map"; }
            return null;
        }
    }
}
