using Jotunn.Configs;
using Jotunn.Managers;
using KydlCore.Common;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The musket's own reload key (user 2026-11-19 13:14, D-100: "a separate reload key (R) instead of middle mouse"): [Musket] Reload Key, default R,
    /// registered with Jotunn's input manager so the key hint shows whatever key the player bound. Read once per frame for the local player while the
    /// musket is in hand and no chat, console, menu, inventory, text box or map is open.
    /// </summary>
    internal static class MusketInput
    {
        internal const string ReloadButtonName = "GunPowdered_MusketReload";
        static ButtonConfig _reload;
        static bool _registered;

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
            if (_registered) return true;
            ModLog.Verbose("MusketInput: Register - key setting " + (ModSettings.MusketReloadKey != null ? ModSettings.MusketReloadKey.Value.ToString() : "missing"));
            if (ReloadButton == null)
            {
                ModLog.Warn("MusketInput: no [Musket] Reload Key setting - the musket has no reload key");
                return false;
            }
            InputManager.Instance.AddButton(Plugin.ModGuid, ReloadButton);
            _registered = true;
            ModLog.Verbose("MusketInput: reload key " + ModSettings.MusketReloadKey.Value + " registered");
            return true;
        }

        /// <summary>Once per frame for the local player: the reload key starts a reload. Expected outcome: what the press did, or null without a press.</summary>
        internal static string Poll(Player local)
        {
            UnityEngine.KeyCode key = ModSettings.MusketReloadKey != null ? ModSettings.MusketReloadKey.Value : UnityEngine.KeyCode.None;
            // the game's own key read (ZInput.GetKeyDown, like KydlCore's hub key) is what starts the reload. The Jotunn button alone never fired in game
            // (user 2026-11-20 09:08, lab a10rkey1: key down, button False); it stays registered for the key hint and is logged next to the key read.
            bool gameKey = key != UnityEngine.KeyCode.None && ZInput.GetKeyDown(key, true);
            bool button = _registered && ZInput.GetButtonDown(ReloadButtonName);
            bool raw = key != UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyDown(key);
            if (!gameKey && !button && !raw) return null;                       // nothing pressed this frame: nothing to log
            bool held = MusketGun.Held(local) != null;
            string blocked = Blocker();
            ModLog.Verbose("MusketInput: reload key " + key + " down - game key " + gameKey + ", Jotunn button " + button + ", raw key " + raw
                + ", musket in hand " + held + ", blocked by " + (blocked ?? "nothing"));
            if (!held || blocked != null) return null;
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
            if (Chat.instance != null && Chat.instance.HasFocus()) return "chat";
            if (Console.IsVisible()) return "console";
            if (Menu.IsVisible()) return "menu";
            if (InventoryGui.IsVisible()) return "inventory";
            if (TextInput.IsVisible()) return "text input";
            if (Minimap.IsOpen()) return "map";
            return null;
        }
    }
}
