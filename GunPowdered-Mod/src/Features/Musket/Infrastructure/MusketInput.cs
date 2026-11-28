using KydlCore.Common;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The musket's own reload key (user 2026-11-19 13:14, D-100: "a separate reload key (R) instead of middle mouse"): [Musket] Reload Key, default R,
    /// shown in the key hint (KyKeyHints) with whatever key the player bound. Read once per frame for the local player while the
    /// musket is in hand and no chat, console, menu, inventory, text box or map is open.
    /// </summary>
    internal static class MusketInput
    {
        internal const string ReloadButtonName = "GunPowdered_MusketReload", AimButtonName = "GunPowdered_MusketAim";
        static KyKeyHint _reload, _aim;
        static bool _registered;

        /// <summary>D-155: the aim button ([Musket] Aim Key, default the mouse wheel button), also used by the key hint.</summary>
        internal static KyKeyHint AimHint
        {
            get
            {
                if (_aim == null && ModSettings.MusketAimKey != null)
                    _aim = new KyKeyHint { Key = ModSettings.MusketAimKey, Hint = MusketRules.HintAim, GamepadButton = PadHintButton(ModSettings.MusketAimPad) };
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

        /// <summary>The reload key row of the key hint.</summary>
        internal static KyKeyHint ReloadHint
        {
            get
            {
                if (_reload == null && ModSettings.MusketReloadKey != null)
                    _reload = new KyKeyHint { Key = ModSettings.MusketReloadKey, Hint = MusketRules.HintReload, GamepadButton = PadHintButton(ModSettings.MusketReloadPad) };
                return _reload;
            }
        }

        /// <summary>D-198: the game button a hint row shows on the gamepad side ("" for None: the row then shows the keyboard key there).</summary>
        static string PadHintButton(BepInEx.Configuration.ConfigEntry<string> setting)
        {
            string button = setting != null && MusketRules.PadBinding(setting.Value) != null ? setting.Value.Trim() : "";
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketInput.PadHintButton({(setting != null ? setting.Definition.Key : "<no setting>")}): returns '{button}'");
            return button;
        }

        /// <summary>D-198: a gamepad button setting changed: the hint rows show the new button at once (rule 17).</summary>
        static void PadSettingChanged()
        {
            if (_aim != null) _aim.GamepadButton = PadHintButton(ModSettings.MusketAimPad);
            if (_reload != null) _reload.GamepadButton = PadHintButton(ModSettings.MusketReloadPad);
            int panels = KyKeyHints.Redraw();
            ModLog.Info($"MusketInput: gamepad buttons - aim '{ModSettings.MusketAimPad?.Value}', reload '{ModSettings.MusketReloadPad?.Value}' ({panels} key-hint panel(s) redrawn)");
        }

        /// <summary>D-198: once per frame: was the gamepad button of a musket action pressed (the game's binding of that game button, any controller layout)?</summary>
        static bool PadDown(BepInEx.Configuration.ConfigEntry<string> setting)
        {
            string binding = setting != null ? MusketRules.PadBinding(setting.Value) : null;
            bool down = binding != null && ZInput.GetButtonDown(binding);
            if (down) ModLog.Verbose($"MusketInput.PadDown: {setting.Definition.Key} '{setting.Value}' ({binding}) pressed");
            if (RuleLog.Due("MusketInput.PadDown", 2d)) ModLog.Every("MusketInput.PadDown", 2d, $"MusketInput.PadDown: {(setting != null ? setting.Definition.Key : "<no setting>")} ({binding ?? "no gamepad button"}) -> {down}");
            return down;
        }

        /// <summary>D-198: a gamepad press as the game's own key read (test hooks call it with the setting's button). Expected outcome: what the press did, or null.</summary>
        internal static string PadPressed(Player local, bool reload)
        {
            if (local == null || MusketGun.Held(local) == null) { ModLog.Verbose($"MusketInput.PadPressed(reload={reload}): no musket in hand - nothing"); return null; }
            string blocked = Blocker();
            if (blocked != null) { ModLog.Verbose($"MusketInput.PadPressed(reload={reload}): blocked by {blocked}"); return null; }
            if (!reload) { string aim = AimPressed(local); ModLog.Info($"MusketInput: aim gamepad button -> {aim}"); return aim; }
            bool hovering = local.GetHoverObject() != null;
            if (!MusketRules.PadReloadAllowed(ModSettings.MusketReloadPad?.Value, hovering))
            {
                ModLog.Verbose($"MusketInput: reload gamepad button '{ModSettings.MusketReloadPad?.Value}' pressed while hovering '{local.GetHoverObject()?.name}' - the game uses it, no reload");
                return null;
            }
            string result = ReloadPressed(local);
            ModLog.Info($"MusketInput: reload gamepad button -> {result}");
            return result;
        }

        /// <summary>Checks the key settings the hint rows follow (once, at start). Expected outcome: true when the reload key setting exists.</summary>
        internal static bool Register()
        {
            if (_registered) { if (RuleLog.IsOn()) ModLog.Verbose($"MusketInput.Register: returns true because _registered (line 84)"); return true; }
            ModLog.Verbose("MusketInput: Register - key setting " + (ModSettings.MusketReloadKey != null ? ModSettings.MusketReloadKey.Value.ToString() : "missing"));
            if (ReloadHint == null)
            {
                ModLog.Warn("MusketInput: no [Musket] Reload Key setting - the musket has no reload key");
                return false;
            }
            _ = AimHint;          // D-155: the aim row of the key hint
            if (ModSettings.MusketAimPad != null) ModSettings.MusketAimPad.SettingChanged += (s, e) => PadSettingChanged();       // D-198
            if (ModSettings.MusketReloadPad != null) ModSettings.MusketReloadPad.SettingChanged += (s, e) => PadSettingChanged();
            _registered = true;
            ModLog.Verbose("MusketInput: reload key " + ModSettings.MusketReloadKey.Value + " ready");
            return true;
        }

        /// <summary>Once per frame for the local player: the reload key starts a reload. Expected outcome: what the press did, or null without a press.</summary>
        internal static string Poll(Player local)
        {
            PollAim(local);
            // D-198: the gamepad buttons (the game's bindings of the chosen game buttons); the same paths as the keys
            if (PadDown(ModSettings.MusketAimPad)) PadPressed(local, false);
            if (PadDown(ModSettings.MusketReloadPad))
            {
                string padResult = PadPressed(local, true);
                ModLog.Verbose($"MusketInput.Poll: returns the reload gamepad button's result '{padResult ?? "nothing"}'");
                return padResult;
            }
            UnityEngine.KeyCode key = ModSettings.MusketReloadKey != null ? ModSettings.MusketReloadKey.Value : UnityEngine.KeyCode.None;
            // the game's own key read (ZInput.GetKeyDown, like KydlCore's hub key) is what starts the reload (user 2026-11-20 09:08, lab a10rkey1)
            bool gameKey = key != UnityEngine.KeyCode.None && ZInput.GetKeyDown(key, true);
            bool raw = key != UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyDown(key);
            if (!gameKey && !raw) { if (RuleLog.Due("MusketInput.Poll:108", 2d)) ModLog.Every("MusketInput.Poll:108", 2d, $"MusketInput.Poll: returns null because !gameKey && !raw (line 108)"); return null; }                       // nothing pressed this frame: nothing to log
            bool held = MusketGun.Held(local) != null;
            string blocked = Blocker();
            ModLog.Verbose("MusketInput: reload key " + key + " down - game key " + gameKey + ", raw key " + raw
                + ", musket in hand " + held + ", blocked by " + (blocked ?? "nothing"));
            if (!held || blocked != null) { if (RuleLog.Due("MusketInput.Poll:113", 2d)) ModLog.Every("MusketInput.Poll:113", 2d, $"MusketInput.Poll: returns null because !held || blocked != null (line 113)"); return null; }
            if (!gameKey)
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
            if (Chat.instance != null && Chat.instance.HasFocus()) return "chat";
            if (Console.IsVisible()) { if (RuleLog.Due("MusketInput.Blocker:137", 2d)) ModLog.Every("MusketInput.Blocker:137", 2d, $"MusketInput.Blocker: returns \"console\" because Console.IsVisible() (line 137)"); return "console"; }
            if (Menu.IsVisible()) { if (RuleLog.Due("MusketInput.Blocker:138", 2d)) ModLog.Every("MusketInput.Blocker:138", 2d, $"MusketInput.Blocker: returns \"menu\" because Menu.IsVisible() (line 138)"); return "menu"; }
            if (InventoryGui.IsVisible()) { if (RuleLog.Due("MusketInput.Blocker:139", 2d)) ModLog.Every("MusketInput.Blocker:139", 2d, $"MusketInput.Blocker: returns \"inventory\" because InventoryGui.IsVisible() (line 139)"); return "inventory"; }
            if (TextInput.IsVisible()) { if (RuleLog.Due("MusketInput.Blocker:140", 2d)) ModLog.Every("MusketInput.Blocker:140", 2d, $"MusketInput.Blocker: returns \"text input\" because TextInput.IsVisible() (line 140)"); return "text input"; }
            if (Minimap.IsOpen()) { if (RuleLog.Due("MusketInput.Blocker:141", 2d)) ModLog.Every("MusketInput.Blocker:141", 2d, $"MusketInput.Blocker: returns \"map\" because Minimap.IsOpen() (line 141)"); return "map"; }
            { if (RuleLog.Due("MusketInput.Blocker:142", 2d)) ModLog.Every("MusketInput.Blocker:142", 2d, $"MusketInput.Blocker: returns null (line 142)"); return null; }
        }
    }
}
