using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The musket's reload (D-087, D-095): the reload key (the game's secondary attack, see MusketAttackPatch) starts it; one Paper Cartridge goes from the
    /// backpack into the musket at the end of each 1.2 s ramrod stroke, until it holds 5, the backpack has none left, the player fires, or the musket is put
    /// away. What is loaded stays. The arms play the reload strokes meanwhile. Sits on the local player only.
    /// </summary>
    internal sealed class MusketReloader : MonoBehaviour
    {
        Player _player;
        float _started;
        int _done;
        bool _active;

        /// <summary>Why the last reload ended (for test hooks).</summary>
        internal static string LastEnd { get; private set; } = "no reload yet";

        /// <summary>True while this player reloads.</summary>
        internal static bool IsReloading(Player player)
        {
            MusketReloader r = player != null ? player.GetComponent<MusketReloader>() : null;
            return r != null && r._active;
        }

        /// <summary>The reload key. Expected outcome: "reloading", "already reloading", or why not (full, no cartridges - also told on screen).</summary>
        internal static string Begin(Player player)
        {
            LastBegin = BeginCore(player);
            return LastBegin;
        }

        /// <summary>What the last press of the reload key did (for test hooks).</summary>
        internal static string LastBegin { get; private set; } = "not pressed yet";

        static string BeginCore(Player player)
        {
            ItemDrop.ItemData musket = MusketGun.Held(player);
            if (musket == null) return "no musket in hand";
            MusketReloader r = player.GetComponent<MusketReloader>();
            if (r != null && r._active) return "already reloading";
            string why = MusketRules.WhyNotReload(MusketGun.Loaded(musket), Carried(player));
            if (why != null)
            {
                player.Message(MessageHud.MessageType.Center, why);
                return why;
            }
            if (r == null) r = player.gameObject.AddComponent<MusketReloader>();
            r._player = player;
            r._started = Time.time;
            r._done = 0;
            r._active = true;
            MusketRig rig = player.GetComponent<MusketRig>();
            if (rig != null) rig.Play(MusketAction.Reload);
            ModLog.Verbose("MusketReloader: " + player.GetPlayerName() + " reloads from " + MusketGun.Loaded(musket) + ", carries " + Carried(player));
            return "reloading";
        }

        /// <summary>Stops a running reload (a shot, the musket put away). Expected outcome: true when one was running.</summary>
        internal static bool Stop(Player player, string why)
        {
            MusketReloader r = player != null ? player.GetComponent<MusketReloader>() : null;
            if (r == null || !r._active) return false;
            r.End("stopped by " + why);
            return true;
        }

        void Update()
        {
            if (!_active) return;
            ItemDrop.ItemData musket = MusketGun.Held(_player);
            if (musket == null)
            {
                End("the musket was put away");
                return;
            }
            int due = MusketRules.CartridgesDue(_started, Time.time);
            ModLog.Every("musket-reload", 1f, "MusketReloader: " + (Time.time - _started).ToString("0.0") + " s in, " + _done + " of " + due + " due cartridge(s) in, loaded " + MusketGun.Loaded(musket));
            while (_done < due)
            {
                int loaded = MusketGun.Loaded(musket), carried = Carried(_player);
                if (MusketRules.WhyNotReload(loaded, carried) != null) break;
                _player.GetInventory().RemoveItem(CartridgeName(), 1);
                MusketGun.SetLoaded(musket, loaded + 1);
                _done++;
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
            string name = CartridgeName();
            return player != null && name != null ? player.GetInventory().CountItems(name) : 0;
        }

        static string CartridgeName()
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(GunPowderedPrefabs.PaperCartridge) : null;
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return drop != null ? drop.m_itemData.m_shared.m_name : null;
        }
    }
}
