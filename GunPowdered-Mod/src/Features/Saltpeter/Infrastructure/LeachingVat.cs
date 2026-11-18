// kydl:read-only-observer - Humanoid here is only the player who uses the station; no creature is moved, commanded or spawned
using System;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Saltpeter.Domain;

namespace GunPowdered.Features.Saltpeter.Infrastructure
{
    /// <summary>
    /// The Leaching Station piece (D-004, D-014, D-024): a stone-filled barrel that drips into a cauldron over a hearth. Two feed spots: the barrel takes
    /// Bat Guano + Water, the hearth takes Wood. With both loaded the hearth burns for the configured in-game days (1 by default); then the water is gone,
    /// the fire is out and the Saltpeter sits in the cauldron until a player collects it. There is no inventory window: the player interacts with the station
    /// (use = put in the guano / water still missing, then the wood, or collect when done; holding Wood and using the station feeds the hearth). Like the game's
    /// Smelter (D-035), every item is one request named by its prefab; the owner counts it only when the station takes that item and has room for it.
    /// Server-authoritative (D-005): only the owner of the station's network object changes its state. State lives in the ZDO ("gp_vat_state" 0 idle / 1 burning /
    /// 2 done, "gp_vat_loaded", "gp_vat_fuel", "gp_vat_start" world time ms, "gp_vat_out"), so it also finishes while nobody is near and survives save / load.
    /// The model's state parts (cauldron_fire, cauldron_liquid, leach_flow, cauldron_salt) are switched on and off from that state on every client.
    /// </summary>
    internal sealed class LeachingVat : MonoBehaviour, Hoverable, Interactable
    {
        const string KeyState = "gp_vat_state", KeyLoaded = "gp_vat_loaded", KeyFuel = "gp_vat_fuel", KeyStart = "gp_vat_start", KeyOut = "gp_vat_out";
        const string KeyGuano = "gp_vat_guano", KeyWater = "gp_vat_water";
        // D-035 (user: "lock it down ... the same way the game is doing fill up for Kiln"): one item per request, named by its prefab, checked by the owner
        const string RpcAdd = "GP_VatAdd", RpcCollect = "GP_VatCollect", RpcCollected = "GP_VatCollected";
        const string WoodSharedName = "$item_wood", WoodPrefab = "Wood";
        const float TickSeconds = 2f;
        ZNetView _nview;
        GameObject _fire, _liquid, _flow, _salt;

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
            if (_nview == null || !_nview.IsValid()) return;
            _nview.Register<string>(RpcAdd, OnAdd);
            _nview.Register(RpcCollect, OnCollect);
            _nview.Register<int>(RpcCollected, OnCollected);
            _fire = Part("cauldron_fire"); _liquid = Part("cauldron_liquid"); _flow = Part("leach_flow"); _salt = Part("cauldron_salt");
            InvokeRepeating("Tick", UnityEngine.Random.Range(0.5f, TickSeconds), TickSeconds);
        }

        GameObject Part(string name)
        {
            Transform t = FindDeep(transform, name);
            return t != null ? t.gameObject : null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform c in root)
            {
                if (c.name == name) return c;
                Transform f = FindDeep(c, name);
                if (f != null) return f;
            }
            return null;
        }

        static double DayLength
        {
            get { return EnvMan.instance != null ? EnvMan.instance.m_dayLengthSec : LeachingVatRules.DefaultDayLengthSeconds; }
        }

        int State { get { return _nview.GetZDO().GetInt(KeyState, LeachingVatRules.StateIdle); } }

        bool Loaded { get { return _nview.GetZDO().GetInt(KeyLoaded, 0) > 0; } }

        int Fuel { get { return _nview.GetZDO().GetInt(KeyFuel, 0); } }

        int Guano { get { return _nview.GetZDO().GetInt(KeyGuano, 0); } }

        int WaterIn { get { return _nview.GetZDO().GetInt(KeyWater, 0); } }

        void Tick()
        {
            try
            {
                if (!_nview.IsValid() || ZNet.instance == null) return;
                if (_nview.IsOwner() && ModSettings.FeatureOn(ModSettings.EnableSaltpeter)) Advance(_nview.GetZDO(), ZNet.instance.GetTimeSeconds());
                ShowState(State);
                ModLog.Every("leach-station-" + gameObject.GetInstanceID(), 30d, "leaching station state " + State + ", barrel loaded " + Loaded + ", hearth wood " + Fuel + "/" + ModSettings.VatFuel.Value);
            }
            catch (Exception e)
            {
                ModLog.Warn("LeachingVat.Tick: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // owner: idle + loaded + fuel -> burning; burning + time up -> done (the fire goes out, the water is gone, the salt is there)
        void Advance(ZDO zdo, double now)
        {
            int state = zdo.GetInt(KeyState, LeachingVatRules.StateIdle);
            if (state == LeachingVatRules.StateIdle)
            {
                int need = ModSettings.VatFuel.Value;
                if (!LeachingVatRules.CanStart(zdo.GetInt(KeyLoaded, 0) > 0, zdo.GetInt(KeyFuel, 0), need)) return;
                zdo.Set(KeyLoaded, 0);
                zdo.Set(KeyGuano, 0);
                zdo.Set(KeyWater, 0);
                zdo.Set(KeyFuel, zdo.GetInt(KeyFuel, 0) - need);
                zdo.Set(KeyStart, (long)(now * 1000d));
                zdo.Set(KeyOut, LeachingVatRules.Saltpeter(1, ModSettings.VatSaltpeter.Value));
                zdo.Set(KeyState, LeachingVatRules.StateBurning);
                ModLog.State("leaching station", "idle", "burning", ModSettings.VatDays.Value + " day(s), " + need + " wood burnt per batch");
            }
            else if (state == LeachingVatRules.StateBurning)
            {
                if (!LeachingVatRules.IsReady(zdo.GetLong(KeyStart, 0L) / 1000d, now, DayLength, ModSettings.VatDays.Value)) return;
                zdo.Set(KeyState, LeachingVatRules.StateDone);
                ModLog.State("leaching station", "burning", "done", zdo.GetInt(KeyOut, 0) + " Saltpeter in the cauldron");
            }
        }

        void ShowState(int state)
        {
            bool burning = state == LeachingVatRules.StateBurning;
            bool wet = burning || (state == LeachingVatRules.StateIdle && Loaded);
            Set(_fire, burning);
            Set(_liquid, wet);
            Set(_flow, wet);
            Set(_salt, state == LeachingVatRules.StateDone);
        }

        static void Set(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        public string GetHoverName() { return ItemCatalog.LeachingVat.Name; }

        public float GetHoverOffset() { return 0f; }

        public string GetHoverText()
        {
            if (!_nview.IsValid()) return GetHoverName();
            string key = "[<color=yellow><b>$KEY_Use</b></color>] ";
            string body;
            int state = State;
            if (state == LeachingVatRules.StateBurning)
            {
                double left = LeachingVatRules.SecondsLeft(_nview.GetZDO().GetLong(KeyStart, 0L) / 1000d, ZNet.instance.GetTimeSeconds(), DayLength, ModSettings.VatDays.Value);
                body = "Leaching (" + Mathf.CeilToInt((float)(left / 60d)) + " min left)";
            }
            else if (state == LeachingVatRules.StateDone) body = key + "Collect " + _nview.GetZDO().GetInt(KeyOut, 0) + " Saltpeter";
            else if (!Loaded) body = key + "Fill barrel (Bat Guano " + Guano + "/" + ModSettings.VatGuano.Value + ", Water " + WaterIn + "/" + ModSettings.VatWater.Value + ")";
            else if (Fuel < ModSettings.VatFuel.Value) body = key + "Feed hearth (" + Fuel + "/" + ModSettings.VatFuel.Value + " Wood)";
            else body = "Ready";
            return Localization.instance.Localize(GetHoverName() + "\n" + body);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || user == null || !_nview.IsValid()) return false;
            int state = State;
            if (state == LeachingVatRules.StateBurning) { user.Message(MessageHud.MessageType.Center, "The saltpeter is still forming"); return true; }
            if (state == LeachingVatRules.StateDone) { Collect(user); return true; }
            if (!Loaded) FillBarrel(user);
            else FeedHearth(user);
            return true;
        }

        // holding Wood and using the station feeds the hearth
        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (user == null || item == null || item.m_shared.m_name != WoodSharedName || !_nview.IsValid()) return false;
            if (State != LeachingVatRules.StateIdle) { user.Message(MessageHud.MessageType.Center, "The hearth is busy"); return true; }
            FeedHearth(user);
            return true;
        }

        // client: put in what is still missing, one item per request (like the game's Smelter): the item leaves the player's inventory, then the owner is told
        void FillBarrel(Humanoid user)
        {
            int guano = SendItems(user, ItemCatalog.BatGuano.Name, GunPowderedPrefabs.BatGuano, ModSettings.VatGuano.Value - Guano);
            int water = SendItems(user, ItemCatalog.Water.Name, GunPowderedPrefabs.Water, ModSettings.VatWater.Value - WaterIn);
            ModLog.Decision("leaching station fill", guano + " Bat Guano + " + water + " Water sent", "barrel had " + Guano + "/" + ModSettings.VatGuano.Value + " guano, " + WaterIn + "/" + ModSettings.VatWater.Value + " water");
            if (guano + water == 0) user.Message(MessageHud.MessageType.Center, "You need Bat Guano and Water");
            else user.Message(MessageHud.MessageType.Center, "Added " + guano + " Bat Guano and " + water + " Water");
        }

        void FeedHearth(Humanoid user)
        {
            int missing = ModSettings.VatFuel.Value - Fuel;
            if (missing <= 0) { user.Message(MessageHud.MessageType.Center, "The hearth is full"); return; }
            int sent = SendItems(user, WoodSharedName, WoodPrefab, missing);
            ModLog.Decision("leaching station hearth", sent + " Wood sent", "hearth had " + Fuel + "/" + ModSettings.VatFuel.Value);
            if (sent == 0) user.Message(MessageHud.MessageType.Center, "You need Wood for the hearth");
        }

        // up to `wanted` items of one kind: each one is removed from the inventory and announced to the owner by its prefab name
        int SendItems(Humanoid user, string sharedName, string prefab, int wanted)
        {
            Inventory inv = user.GetInventory();
            int before = inv.CountItems(sharedName);
            int give = Mathf.Min(Mathf.Max(wanted, 0), before);
            int sent = 0;
            for (int i = 0; i < give; i++)
            {
                // the item itself is removed (like the game's Smelter does for ore): removing by name left every item in the inventory in the test
                // profile (run core4, another mod may patch that overload), and an item is only announced when it really left the inventory
                ItemDrop.ItemData stack = FirstStack(inv, sharedName);
                int stackBefore = stack != null ? stack.m_stack : -1;
                bool removed = stack != null && inv.RemoveItem(stack, 1);
                if (i == 0 || !removed)
                    ModLog.Verbose("LeachingVat.SendItems: " + prefab + " #" + i + " stack " + stackBefore + " -> " + (stack != null ? stack.m_stack : -1) + ", removed " + removed
                        + ", inventory count now " + inv.CountItems(sharedName) + ", user " + user.GetType().Name + " is the local player " + (user == Player.m_localPlayer)
                        + ", same inventory as the local player " + (Player.m_localPlayer != null && ReferenceEquals(inv, Player.m_localPlayer.GetInventory())));
                if (!removed) break;
                _nview.InvokeRPC(RpcAdd, prefab);
                sent++;
            }
            give = sent;
            int after = inv.CountItems(sharedName);
            ModLog.Check("leaching station: " + prefab + " left in the inventory of " + user.GetHoverName(), after, before - give, 0f, "items");
            return give;
        }
        static ItemDrop.ItemData FirstStack(Inventory inv, string sharedName)
        {
            foreach (ItemDrop.ItemData item in inv.GetAllItems()) if (item.m_shared.m_name == sharedName && item.m_stack > 0) return item;
            return null;
        }

        void Collect(Humanoid user)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.Saltpeter) : null;
            int amount = _nview.GetZDO().GetInt(KeyOut, 0);
            if (prefab == null || amount < 1) return;
            if (!user.GetInventory().CanAddItem(prefab, amount)) { user.Message(MessageHud.MessageType.Center, "Your inventory is full"); return; }
            _nview.InvokeRPC(RpcCollect);
        }

        // owner: one item arrived; it counts only if it is one the station takes and its slot has room (anything else is refused and logged)
        void OnAdd(long sender, string prefab)
        {
            if (!_nview.IsOwner()) return;
            var zdo = _nview.GetZDO();
            var have = new LeachingVatRules.VatAmounts(Guano, WaterIn, Fuel);
            var need = new LeachingVatRules.VatAmounts(ModSettings.VatGuano.Value, ModSettings.VatWater.Value, ModSettings.VatFuel.Value);
            LeachingVatRules.Feed feed = LeachingVatRules.Accept(prefab, State == LeachingVatRules.StateIdle, have, need);
            if (feed == LeachingVatRules.Feed.Guano) zdo.Set(KeyGuano, have.Guano + 1);
            else if (feed == LeachingVatRules.Feed.Water) zdo.Set(KeyWater, have.Water + 1);
            else if (feed == LeachingVatRules.Feed.Wood) zdo.Set(KeyFuel, have.Fuel + 1);
            bool full = LeachingVatRules.BarrelFull(Guano, WaterIn, need.Guano, need.Water);
            if (full && !Loaded) zdo.Set(KeyLoaded, 1);
            ModLog.Decision("leaching station item", feed == LeachingVatRules.Feed.Refused ? "refused" : "took " + feed, "item " + prefab + " from " + sender + ", state " + State + ", now guano " + Guano + "/" + need.Guano + ", water " + WaterIn + "/" + need.Water + ", wood " + Fuel + "/" + need.Fuel + ", barrel full " + full);
        }
        // owner: hand the saltpeter over and reset
        void OnCollect(long sender)
        {
            if (!_nview.IsOwner() || State != LeachingVatRules.StateDone) return;
            var zdo = _nview.GetZDO();
            int amount = zdo.GetInt(KeyOut, 0);
            zdo.Set(KeyOut, 0);
            zdo.Set(KeyStart, 0L);
            zdo.Set(KeyState, LeachingVatRules.StateIdle);
            ModLog.State("leaching station", "done", "idle", amount + " Saltpeter collected");
            _nview.InvokeRPC(sender, RpcCollected, amount);
        }

        // the collecting player's client: add the saltpeter to the inventory
        void OnCollected(long sender, int amount)
        {
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.Saltpeter) : null;
            if (p == null || prefab == null || amount <= 0) return;
            p.GetInventory().AddItem(prefab, amount);
            p.Message(MessageHud.MessageType.TopLeft, "Collected " + amount + " Saltpeter");
        }
    }
}
