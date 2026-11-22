using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Features.Skilling.Domain;
using GunPowdered.Features.Skilling.Infrastructure;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The musket's reload (D-087, D-095): the reload key ([Musket] Reload Key, MusketInput; D-100) starts it; one Paper Cartridge goes from the
    /// backpack into the musket at the end of each 1.2 s ramrod stroke, until it holds 5, the backpack has none left, the player fires, or the musket is put
    /// away. What is loaded stays. The arms play the reload strokes meanwhile. Sits on the local player only.
    /// </summary>
    internal sealed class MusketReloader : MonoBehaviour
    {
        Player _player;
        float _started;
        float _stroke = MusketRules.ReloadSecondsPerCartridge;
        int _done;
        bool _active;

        /// <summary>Why the last reload ended (for test hooks).</summary>
        internal static string LastEnd { get; private set; } = "no reload yet";

        /// <summary>True while this player reloads.</summary>
        internal static bool IsReloading(Player player)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MusketReloader.IsReloading(player={RuleLog.Shown(player)})");
            MusketReloader r = player != null ? player.GetComponent<MusketReloader>() : null;
            { var returnedAt28 = r != null && r._active; if (RuleLog.IsOn()) ModLog.Verbose($"MusketReloader.IsReloading: returns {RuleLog.Shown(returnedAt28)} (line 28)"); return returnedAt28; }
        }

        /// <summary>The reload key. Expected outcome: "reloading", "already reloading", or why not (full, no cartridges - also told on screen).</summary>
        internal static string Begin(Player player)
        {
            if (RuleLog.Due("MusketReloader.Begin", 2d)) ModLog.Every("MusketReloader.Begin", 2d, $"MusketReloader.Begin(player={RuleLog.Shown(player)})");
            LastBegin = BeginCore(player);
            { var returnedAt36 = LastBegin; if (RuleLog.Due("MusketReloader.Begin:36", 2d)) ModLog.Every("MusketReloader.Begin:36", 2d, $"MusketReloader.Begin: returns {RuleLog.Shown(returnedAt36)} (line 36)"); return returnedAt36; }
        }

        /// <summary>What the last press of the reload key did (for test hooks).</summary>
        internal static string LastBegin { get; private set; } = "not pressed yet";

        static string BeginCore(Player player)
        {
            ItemDrop.ItemData musket = MusketGun.Held(player);
            if (musket == null)
            {
                ModLog.Verbose("MusketReloader: Begin refused - no musket in hand (" + (player != null ? player.GetPlayerName() : "no player") + ")");
                return "no musket in hand";
            }
            MusketReloader r = player.GetComponent<MusketReloader>();
            if (r != null && r._active)
            {
                ModLog.Verbose("MusketReloader: Begin ignored - already reloading, " + r._done + " cartridge(s) in so far");
                return "already reloading";
            }
            int loadedNow = MusketGun.Loaded(musket), carriedNow = Carried(player);
            // D-155: reloading while running is fine, not while swimming or in the air
            string why = MusketRules.WhyNotReloadMoving(player.IsOnGround(), player.IsSwimming()) ?? MusketRules.WhyNotReload(loadedNow, carriedNow);
            ModLog.Decision("musket reload", why == null ? "start" : "refused", "loaded " + loadedNow + "/" + MusketRules.Capacity + ", carries " + carriedNow + " cartridge(s)" + (why != null ? " - " + why : ""));
            if (why != null)
            {
                player.Message(MessageHud.MessageType.Center, why);
                { var returnedAt62 = why; if (RuleLog.Due("MusketReloader.BeginCore:62", 2d)) ModLog.Every("MusketReloader.BeginCore:62", 2d, $"MusketReloader.BeginCore: returns {RuleLog.Shown(returnedAt62)} (line 62)"); return returnedAt62; }
            }
            if (r == null) r = player.gameObject.AddComponent<MusketReloader>();
            r._player = player;
            r._started = Time.time;
            r._done = 0;
            r._active = true;
            // D-157: the Muskets skill sets the time per cartridge (1.8 s at level 0 .. 1.2 s at level 100), fixed for this reload
            r._stroke = SkillRules.MusketReloadSeconds(GunSkills.Level(player, GunSkills.Muskets));
            MusketRig rig = player.GetComponent<MusketRig>();
            if (rig != null) rig.Play(MusketAction.Reload, r._stroke);
            ModLog.Verbose("MusketReloader: " + player.GetPlayerName() + " reloads from " + MusketGun.Loaded(musket) + ", carries " + Carried(player) + ", " + r._stroke.ToString("0.00") + " s per cartridge (Muskets skill)");
            return "reloading";
        }

        /// <summary>Stops a running reload (a shot, the musket put away). Expected outcome: true when one was running.</summary>
        internal static bool Stop(Player player, string why)
        {
            MusketReloader r = player != null ? player.GetComponent<MusketReloader>() : null;
            if (r == null || !r._active)
            {
                ModLog.Verbose("MusketReloader: Stop (" + why + ") - no reload running");
                return false;
            }
            r.End("stopped by " + why);
            { if (RuleLog.Due("MusketReloader.Stop:85", 2d)) ModLog.Every("MusketReloader.Stop:85", 2d, "MusketReloader.Stop: returns true (line 85)"); return true; }
        }

        void Update()
        {
            if (!_active) { if (RuleLog.Due("MusketReloader.Update:90", 2d)) ModLog.Every("MusketReloader.Update:90", 2d, "MusketReloader.Update: returns (line 90)"); return; }
            ItemDrop.ItemData musket = MusketGun.Held(_player);
            if (musket == null)
            {
                End("the musket was put away");
                { if (RuleLog.Due("MusketReloader.Update:95", 2d)) ModLog.Every("MusketReloader.Update:95", 2d, "MusketReloader.Update: returns (line 95)"); return; }
            }
            string moving = MusketRules.WhyNotReloadMoving(_player.IsOnGround(), _player.IsSwimming());
            if (moving != null)
            {
                // D-155: a jump or a swim stops the reload (running does not); what is in stays in
                _player.Message(MessageHud.MessageType.Center, moving);
                End(moving);
                { if (RuleLog.Due("MusketReloader.Update:moving", 2d)) ModLog.Every("MusketReloader.Update:moving", 2d, "MusketReloader.Update: stopped - " + moving); return; }
            }
            int due = MusketRules.CartridgesDue(_started, Time.time, _stroke);
            ModLog.Every("musket-reload", 1f, "MusketReloader: " + (Time.time - _started).ToString("0.0") + " s in, " + _done + " of " + due + " due cartridge(s) in, loaded " + MusketGun.Loaded(musket));
            while (_done < due)
            {
                int loaded = MusketGun.Loaded(musket), carried = Carried(_player);
                if (MusketRules.WhyNotReload(loaded, carried) != null) break;
                _player.GetInventory().RemoveItem(CartridgeName(), 1);
                MusketGun.SetLoaded(musket, loaded + 1);
                _done++;
                GunSkills.Raise(_player, GunSkills.Muskets, SkillRules.GainCartridge);     // D-157: each cartridge rammed home trains Muskets a little
                ModLog.Verbose("MusketReloader: cartridge " + _done + " in, loaded " + (loaded + 1) + ", " + (carried - 1) + " left in the backpack");
            }
            string why = MusketRules.WhyNotReload(MusketGun.Loaded(musket), Carried(_player));
            if (why != null) End(why);
        }

        void End(string why)
        {
            _active = false;
            LastEnd = why + " after " + _done + " cartridge(s), loaded " + MusketGun.Loaded(MusketGun.Held(_player));
            MusketRig rig = _player != null ? _player.GetComponent<MusketRig>() : null;
            if (rig != null && rig.CurrentAction == MusketAction.Reload) rig.Play(MusketAction.Hold);
            ModLog.Verbose("MusketReloader: " + LastEnd);
        }

        /// <summary>How many Paper Cartridges the player carries.</summary>
        internal static int Carried(Player player)
        {
            if (RuleLog.Due("MusketReloader.Carried", 2d)) ModLog.Every("MusketReloader.Carried", 2d, $"MusketReloader.Carried(player={RuleLog.Shown(player)})");
            string name = CartridgeName();
            { var returnedAt126 = player != null && name != null ? player.GetInventory().CountItems(name) : 0; if (RuleLog.Due("MusketReloader.Carried:126", 2d)) ModLog.Every("MusketReloader.Carried:126", 2d, $"MusketReloader.Carried: returns {RuleLog.Shown(returnedAt126)} (line 126)"); return returnedAt126; }
        }

        static string CartridgeName()
        {
            if (RuleLog.Due("MusketReloader.CartridgeName", 2d)) ModLog.Every("MusketReloader.CartridgeName", 2d, $"MusketReloader.CartridgeName()");
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(GunPowderedPrefabs.PaperCartridge) : null;
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            { var returnedAt134 = drop != null ? drop.m_itemData.m_shared.m_name : null; if (RuleLog.Due("MusketReloader.CartridgeName:134", 2d)) ModLog.Every("MusketReloader.CartridgeName:134", 2d, $"MusketReloader.CartridgeName: returns {RuleLog.Shown(returnedAt134)} (line 134)"); return returnedAt134; }
        }
    }
}
