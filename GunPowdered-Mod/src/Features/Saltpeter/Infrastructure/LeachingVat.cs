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
    /// The Leaching Station piece (D-004, D-024, D-129): a stone-filled barrel that drips into a cauldron over a hearth. It holds Bat Guano, Water and Wood up
    /// to its full batch, which grows with the extension pieces built near it (LeachingExtension, LeachingUpgradeRules: step 0..5, batch 10/10/4 x (step + 1),
    /// run 30 minutes minus 5 per step). Use (E) puts in half a full batch from the inventory, or what the player has; holding Wood and using the station
    /// puts in wood only. A run starts by itself when the station holds the whole full batch, or when the player starts it (Shift + E, or E when nothing more
    /// can go in) with at least one base unit inside. A run leaches only whole base units; the rest stays in for the next run. After the run the Saltpeter
    /// sits in the cauldron until a player collects it.
    /// Like the game's Smelter (D-035) every item is one request named by its prefab; the owner counts it only when the station takes that item and has room.
    /// Server-authoritative (D-005): only the owner of the station's network object changes its state. State lives in the ZDO ("gp_vat_state" 0 idle /
    /// 1 burning / 2 done, "gp_vat_guano", "gp_vat_water", "gp_vat_fuel", "gp_vat_start" world time ms, "gp_vat_minutes", "gp_vat_units", "gp_vat_out"), so a
    /// run also finishes while nobody is near and survives save / load. A run started before D-129 has no "gp_vat_minutes" and takes the base 30 minutes,
    /// the same as its old one in-game day.
    /// The model's state parts (cauldron_fire, cauldron_liquid, leach_flow, cauldron_salt) are switched on and off from that state on every client.
    /// </summary>
    internal sealed class LeachingVat : MonoBehaviour, Hoverable, Interactable
    {
        const string KeyState = "gp_vat_state", KeyFuel = "gp_vat_fuel", KeyStart = "gp_vat_start", KeyOut = "gp_vat_out";
        const string KeyGuano = "gp_vat_guano", KeyWater = "gp_vat_water", KeyMinutes = "gp_vat_minutes", KeyUnits = "gp_vat_units";
        // D-035 (user: "lock it down ... the same way the game is doing fill up for Kiln"): one item per request, named by its prefab, checked by the owner
        const string RpcAdd = "GP_VatAdd", RpcCollect = "GP_VatCollect", RpcCollected = "GP_VatCollected", RpcStart = "GP_VatStart";
        const string WoodSharedName = "$item_wood", WoodPrefab = "Wood";
        const string UseKey = "[<color=yellow><b>$KEY_Use</b></color>] ", StartKey = "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] ";
        const float TickSeconds = 2f;
        ZNetView _nview;
        GameObject _fire, _liquid, _flow, _salt;

        void Awake()
        {
            if (RuleLog.On) ModLog.Verbose($"LeachingVat.Awake()");
            _nview = GetComponent<ZNetView>();
            if (_nview == null || !_nview.IsValid()) { if (RuleLog.On) ModLog.Verbose("LeachingVat.Awake: no valid network view - returns"); return; }
            _nview.Register<string>(RpcAdd, OnAdd);
            _nview.Register(RpcCollect, OnCollect);
            _nview.Register<int>(RpcCollected, OnCollected);
            _nview.Register(RpcStart, OnStart);
            _fire = Part("cauldron_fire"); _liquid = Part("cauldron_liquid"); _flow = Part("leach_flow"); _salt = Part("cauldron_salt");
            InvokeRepeating("Tick", UnityEngine.Random.Range(0.5f, TickSeconds), TickSeconds);
        }

        GameObject Part(string name)
        {
            Transform t = FindDeep(transform, name);
            if (RuleLog.On) ModLog.Verbose($"LeachingVat.Part(name={name}): returns {(t != null ? "found" : "null")}");
            return t != null ? t.gameObject : null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (RuleLog.On) ModLog.Every("LeachingVat.FindDeep", 2d, $"LeachingVat.FindDeep(root={RuleLog.Shown(root)}, name={name})");
            foreach (Transform c in root)
            {
                if (c.name == name) { if (RuleLog.On) ModLog.Every("LeachingVat.FindDeep:hit", 2d, $"LeachingVat.FindDeep: returns {name}"); return c; }
                Transform f = FindDeep(c, name);
                if (f != null) { if (RuleLog.On) ModLog.Every("LeachingVat.FindDeep:child", 2d, $"LeachingVat.FindDeep: returns {name} (deeper)"); return f; }
            }
            if (RuleLog.On) ModLog.Every("LeachingVat.FindDeep:none", 2d, $"LeachingVat.FindDeep: returns null for {name}");
            return null;
        }

        // ---------- what the station holds and can do now ----------

        int State { get { return _nview.GetZDO().GetInt(KeyState, LeachingVatRules.StateIdle); } }

        static LeachingVatRules.VatAmounts Unit
        {
            get { return new LeachingVatRules.VatAmounts(ModSettings.VatGuano.Value, ModSettings.VatWater.Value, ModSettings.VatFuel.Value); }
        }

        LeachingVatRules.VatAmounts Held
        {
            get { ZDO z = _nview.GetZDO(); return new LeachingVatRules.VatAmounts(z.GetInt(KeyGuano, 0), z.GetInt(KeyWater, 0), z.GetInt(KeyFuel, 0)); }
        }

        /// <summary>The upgrade step from the extension pieces near the station (D-129).</summary>
        int Step
        {
            get { return LeachingUpgradeRules.Step(LeachingExtension.KindsNear(transform.position)); }
        }

        static double MinutesAt(int step)
        {
            double minutes = LeachingUpgradeRules.Minutes(step, ModSettings.VatBaseMinutes.Value, ModSettings.VatMinutesPerStep.Value);
            if (RuleLog.On) ModLog.Every("LeachingVat.MinutesAt", 2d, $"LeachingVat.MinutesAt(step={step}): returns {minutes}");
            return minutes;
        }

        // ---------- owner: start, finish ----------

        void Tick()
        {
            try
            {
                if (!_nview.IsValid() || ZNet.instance == null) { if (RuleLog.On) ModLog.Every("LeachingVat.Tick:invalid", 2d, "LeachingVat.Tick: no network view or no ZNet - returns"); return; }
                if (_nview.IsOwner() && ModSettings.FeatureOn(ModSettings.EnableSaltpeter)) Advance(_nview.GetZDO(), ZNet.instance.GetTimeSeconds());
                ShowState(State);
                ModLog.Every("leach-station-" + gameObject.GetInstanceID(), 30d, $"leaching station state {State}, step {Step}, holds {LeachingUpgradeRules.Text(Held)} of {LeachingUpgradeRules.Text(LeachingUpgradeRules.FullBatch(Unit, Step))}");
            }
            catch (Exception e)
            {
                ModLog.Warn("LeachingVat.Tick: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // owner: idle + the whole full batch inside -> burning by itself; burning + time up -> done (the fire goes out, the salt is there)
        void Advance(ZDO zdo, double now)
        {
            int state = zdo.GetInt(KeyState, LeachingVatRules.StateIdle);
            if (state == LeachingVatRules.StateIdle)
            {
                int step = Step;
                if (!LeachingUpgradeRules.IsFull(Held, LeachingUpgradeRules.FullBatch(Unit, step))) { if (RuleLog.On) ModLog.Every("LeachingVat.Advance:notfull", 2d, "LeachingVat.Advance: idle, not full - returns"); return; }
                Begin(zdo, now, step, "full batch");
                if (RuleLog.On) ModLog.Every("LeachingVat.Advance:started", 2d, "LeachingVat.Advance: full batch started - returns"); return;
            }
            if (state != LeachingVatRules.StateBurning) { if (RuleLog.On) ModLog.Every("LeachingVat.Advance:done", 2d, "LeachingVat.Advance: done, waiting for a player - returns"); return; }
            double minutes = zdo.GetFloat(KeyMinutes, (float)MinutesAt(0));
            if (!LeachingVatRules.IsReady(zdo.GetLong(KeyStart, 0L) / 1000d, now, 60d, minutes)) { if (RuleLog.On) ModLog.Every("LeachingVat.Advance:burning", 2d, "LeachingVat.Advance: still leaching - returns"); return; }
            zdo.Set(KeyState, LeachingVatRules.StateDone);
            ModLog.State("leaching station", "burning", "done", zdo.GetInt(KeyOut, 0) + " Saltpeter in the cauldron after " + minutes + " min");
        }

        // owner: one run of as many whole base units as the station holds (up to the step's batch); the rest stays in (D-129)
        void Begin(ZDO zdo, double now, int step, string why)
        {
            LeachingVatRules.VatAmounts held = Held, unit = Unit;
            int units = LeachingUpgradeRules.Units(held, unit, step);
            if (units < 1) { ModLog.Decision("leaching station start", "refused", LeachingUpgradeRules.Missing(held, unit) + ", holds " + LeachingUpgradeRules.Text(held)); return; }
            LeachingVatRules.VatAmounts used = LeachingUpgradeRules.Used(unit, units);
            double minutes = MinutesAt(step);
            zdo.Set(KeyGuano, held.Guano - used.Guano);
            zdo.Set(KeyWater, held.Water - used.Water);
            zdo.Set(KeyFuel, held.Fuel - used.Fuel);
            zdo.Set(KeyUnits, units);
            zdo.Set(KeyMinutes, (float)minutes);
            zdo.Set(KeyStart, (long)(now * 1000d));
            zdo.Set(KeyOut, LeachingVatRules.Saltpeter(units, ModSettings.VatSaltpeter.Value));
            zdo.Set(KeyState, LeachingVatRules.StateBurning);
            ModLog.State("leaching station", "idle", "burning", $"{why}: step {step}, {units} unit(s) = {LeachingUpgradeRules.Text(used)} -> {zdo.GetInt(KeyOut, 0)} Saltpeter in {minutes} min, left in the station {LeachingUpgradeRules.Text(Held)}");
        }

        // owner: a player asked to start now
        void OnStart(long sender)
        {
            if (!_nview.IsOwner()) { if (RuleLog.On) ModLog.Verbose("LeachingVat.OnStart: not the owner - returns"); return; }
            if (State != LeachingVatRules.StateIdle) { ModLog.Decision("leaching station start", "refused", "busy (state " + State + "), asked by " + sender); return; }
            Begin(_nview.GetZDO(), ZNet.instance.GetTimeSeconds(), Step, "started by " + sender);
        }

        void ShowState(int state)
        {
            if (RuleLog.On) ModLog.Every("LeachingVat.ShowState", 2d, $"LeachingVat.ShowState(state={state})");
            LeachingVatRules.VatAmounts held = Held;
            bool burning = state == LeachingVatRules.StateBurning;
            bool wet = burning || (state == LeachingVatRules.StateIdle && (held.Guano > 0 || held.Water > 0));
            Set(_fire, burning);
            Set(_liquid, wet);
            Set(_flow, wet);
            Set(_salt, state == LeachingVatRules.StateDone);
        }

        static void Set(GameObject go, bool on)
        {
            if (RuleLog.On) ModLog.Every("LeachingVat.Set", 2d, $"LeachingVat.Set(go={RuleLog.Shown(go)}, on={on})");
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        // ---------- hover ----------

        public string GetHoverName() { return ItemCatalog.LeachingVat.Name; }

        public float GetHoverOffset() { return 0f; }

        public string GetHoverText()
        {
            if (!_nview.IsValid()) { if (RuleLog.On) ModLog.Every("LeachingVat.GetHoverText:invalid", 2d, "LeachingVat.GetHoverText: no network view - returns the name"); return GetHoverName(); }
            int state = State, step = Step;
            string body;
            if (state == LeachingVatRules.StateBurning)
            {
                ZDO z = _nview.GetZDO();
                double left = LeachingVatRules.SecondsLeft(z.GetLong(KeyStart, 0L) / 1000d, ZNet.instance.GetTimeSeconds(), 60d, z.GetFloat(KeyMinutes, (float)MinutesAt(0)));
                body = $"Leaching {z.GetInt(KeyOut, 0)} Saltpeter ({Mathf.CeilToInt((float)(left / 60d))} min left)\nHolds {Holds(Held, LeachingUpgradeRules.FullBatch(Unit, step))} for the next run";
            }
            else if (state == LeachingVatRules.StateDone) body = UseKey + "Collect " + _nview.GetZDO().GetInt(KeyOut, 0) + " Saltpeter";
            else body = IdleText(step);
            string text = Localization.instance.Localize(GetHoverName() + "\n" + UpgradeLine(step) + "\n" + body);
            if (RuleLog.On) ModLog.Every("LeachingVat.GetHoverText", 2d, $"LeachingVat.GetHoverText: returns {RuleLog.Shown(text)}");
            return text;
        }

        static string UpgradeLine(int step)
        {
            LeachingVatRules.VatAmounts full = LeachingUpgradeRules.FullBatch(Unit, step);
            string line = $"Upgrades {step}/{LeachingUpgradeRules.MaxStep}: up to {full.Guano} Bat Guano + {full.Water} Water + {full.Fuel} Wood"
                + $" -> {LeachingVatRules.Saltpeter(LeachingUpgradeRules.UnitsPerBatch(step), ModSettings.VatSaltpeter.Value)} Saltpeter, {MinutesAt(step)} min";
            if (RuleLog.On) ModLog.Every("LeachingVat.UpgradeLine", 2d, $"LeachingVat.UpgradeLine: returns {line}");
            return line;
        }

        static string Holds(LeachingVatRules.VatAmounts held, LeachingVatRules.VatAmounts full)
        {
            string text = $"Bat Guano {held.Guano}/{full.Guano}, Water {held.Water}/{full.Water}, Wood {held.Fuel}/{full.Fuel}";
            if (RuleLog.On) ModLog.Every("LeachingVat.Holds", 2d, $"LeachingVat.Holds: returns {text}");
            return text;
        }

        string IdleText(int step)
        {
            LeachingVatRules.VatAmounts held = Held, unit = Unit, full = LeachingUpgradeRules.FullBatch(unit, step);
            int units = LeachingUpgradeRules.Units(held, unit, step);
            string start = units >= 1
                ? StartKey + $"Start: {units} batch{(units == 1 ? "" : "es")} -> {LeachingVatRules.Saltpeter(units, ModSettings.VatSaltpeter.Value)} Saltpeter"
                : "Cannot start yet: " + LeachingUpgradeRules.Missing(held, unit);
            string text = Holds(held, full) + "\n" + UseKey + "Fill\n" + start;
            if (RuleLog.On) ModLog.Every("LeachingVat.IdleText", 2d, $"LeachingVat.IdleText(step={step}): returns {RuleLog.Shown(text)}");
            return text;
        }

        // ---------- use ----------

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (RuleLog.On) ModLog.Every("LeachingVat.Interact", 2d, $"LeachingVat.Interact(user={RuleLog.Shown(user)}, hold={hold}, alt={alt})");
            if (hold || user == null || !_nview.IsValid()) { if (RuleLog.On) ModLog.Every("LeachingVat.Interact:no", 2d, "LeachingVat.Interact: held, no user or no network view - returns false"); return false; }
            int state = State;
            if (state == LeachingVatRules.StateBurning) { user.Message(MessageHud.MessageType.Center, "The saltpeter is still forming"); if (RuleLog.On) ModLog.Every("LeachingVat.Interact:busy", 2d, "LeachingVat.Interact: burning - returns true"); return true; }
            if (state == LeachingVatRules.StateDone) { Collect(user); if (RuleLog.On) ModLog.Every("LeachingVat.Interact:collect", 2d, "LeachingVat.Interact: collect - returns true"); return true; }
            if (alt || Fill(user, true) == 0) AskStart(user);
            if (RuleLog.On) ModLog.Every("LeachingVat.Interact:end", 2d, $"LeachingVat.Interact: idle, alt {alt} - returns true");
            return true;
        }

        // holding Wood and using the station puts in wood only
        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (user == null || item == null || item.m_shared.m_name != WoodSharedName || !_nview.IsValid()) { if (RuleLog.On) ModLog.Verbose("LeachingVat.UseItem: not wood or no station - returns false"); return false; }
            if (State != LeachingVatRules.StateIdle) { user.Message(MessageHud.MessageType.Center, "The hearth is busy"); if (RuleLog.On) ModLog.Verbose("LeachingVat.UseItem: busy - returns true"); return true; }
            Fill(user, false);
            if (RuleLog.On) ModLog.Verbose("LeachingVat.UseItem: wood put in - returns true");
            return true;
        }

        // client: half a full batch per use, or what the player has (D-129); returns how many items went in
        int Fill(Humanoid user, bool everything)
        {
            int step = Step;
            Inventory inv = user.GetInventory();
            LeachingVatRules.VatAmounts unit = Unit, held = Held;
            var player = new LeachingVatRules.VatAmounts(everything ? inv.CountItems(ItemCatalog.BatGuano.Name) : 0, everything ? inv.CountItems(ItemCatalog.Water.Name) : 0, inv.CountItems(WoodSharedName));
            LeachingVatRules.VatAmounts take = LeachingUpgradeRules.Take(held, LeachingUpgradeRules.FullBatch(unit, step), LeachingUpgradeRules.FillPerUse(unit, step), player);
            int guano = SendItems(user, ItemCatalog.BatGuano.Name, GunPowderedPrefabs.BatGuano, take.Guano);
            int water = SendItems(user, ItemCatalog.Water.Name, GunPowderedPrefabs.Water, take.Water);
            int wood = SendItems(user, WoodSharedName, WoodPrefab, take.Fuel);
            int sent = guano + water + wood;
            ModLog.Decision("leaching station fill", $"{guano} Bat Guano + {water} Water + {wood} Wood sent", $"step {step}, station held {LeachingUpgradeRules.Text(held)}, player had {LeachingUpgradeRules.Text(player)}");
            if (sent > 0) user.Message(MessageHud.MessageType.Center, $"Added {guano} Bat Guano, {water} Water and {wood} Wood");
            if (RuleLog.On) ModLog.Verbose($"LeachingVat.Fill: returns {sent}");
            return sent;
        }

        // client: start now if a base unit is inside, else say exactly what is missing
        void AskStart(Humanoid user)
        {
            LeachingVatRules.VatAmounts held = Held;
            string missing = LeachingUpgradeRules.Missing(held, Unit);
            ModLog.Decision("leaching station start asked", missing.Length == 0 ? "sent to the owner" : "not sent", "holds " + LeachingUpgradeRules.Text(held) + (missing.Length == 0 ? "" : ", " + missing));
            if (missing.Length > 0) { user.Message(MessageHud.MessageType.Center, missing); if (RuleLog.On) ModLog.Verbose("LeachingVat.AskStart: too little inside - returns"); return; }
            _nview.InvokeRPC(RpcStart);
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
                        + ", inventory count now " + inv.CountItems(sharedName) + ", user " + user.GetType().Name + " is the local player " + (user == Player.m_localPlayer));
                if (!removed) break;
                _nview.InvokeRPC(RpcAdd, prefab);
                sent++;
            }
            int after = inv.CountItems(sharedName);
            ModLog.Check("leaching station: " + prefab + " left in the inventory of " + user.GetHoverName(), after, before - sent, 0f, "items");
            if (RuleLog.On) ModLog.Verbose($"LeachingVat.SendItems({prefab}, wanted {wanted}): returns {sent}");
            return sent;
        }

        static ItemDrop.ItemData FirstStack(Inventory inv, string sharedName)
        {
            foreach (ItemDrop.ItemData item in inv.GetAllItems())
            {
                if (item.m_shared.m_name == sharedName && item.m_stack > 0) { if (RuleLog.On) ModLog.Every("LeachingVat.FirstStack", 2d, $"LeachingVat.FirstStack({sharedName}): returns a stack of {item.m_stack}"); return item; }
            }
            if (RuleLog.On) ModLog.Every("LeachingVat.FirstStack:none", 2d, $"LeachingVat.FirstStack({sharedName}): returns null");
            return null;
        }

        void Collect(Humanoid user)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.Saltpeter) : null;
            int amount = _nview.GetZDO().GetInt(KeyOut, 0);
            if (prefab == null || amount < 1) { if (RuleLog.On) ModLog.Every("LeachingVat.Collect:none", 2d, "LeachingVat.Collect: no prefab or nothing to collect - returns"); return; }
            if (!user.GetInventory().CanAddItem(prefab, amount)) { user.Message(MessageHud.MessageType.Center, "Your inventory is full"); if (RuleLog.On) ModLog.Every("LeachingVat.Collect:full", 2d, "LeachingVat.Collect: inventory full - returns"); return; }
            _nview.InvokeRPC(RpcCollect);
            if (RuleLog.On) ModLog.Every("LeachingVat.Collect", 2d, $"LeachingVat.Collect: asked the owner for {amount} Saltpeter");
        }

        // owner: one item arrived; it counts only if it is one the station takes and its slot has room up to the full batch (anything else is refused and logged)
        void OnAdd(long sender, string prefab)
        {
            if (!_nview.IsOwner()) { if (RuleLog.On) ModLog.Verbose("LeachingVat.OnAdd: not the owner - returns"); return; }
            var zdo = _nview.GetZDO();
            int step = Step;
            LeachingVatRules.VatAmounts have = Held, full = LeachingUpgradeRules.FullBatch(Unit, step);
            LeachingVatRules.Feed feed = LeachingVatRules.Accept(prefab, State == LeachingVatRules.StateIdle, have, full);
            if (feed == LeachingVatRules.Feed.Guano) zdo.Set(KeyGuano, have.Guano + 1);
            else if (feed == LeachingVatRules.Feed.Water) zdo.Set(KeyWater, have.Water + 1);
            else if (feed == LeachingVatRules.Feed.Wood) zdo.Set(KeyFuel, have.Fuel + 1);
            ModLog.Decision("leaching station item", feed == LeachingVatRules.Feed.Refused ? "refused" : "took " + feed, $"item {prefab} from {sender}, state {State}, step {step}, now {LeachingUpgradeRules.Text(Held)} of {LeachingUpgradeRules.Text(full)}");
        }

        // owner: hand the saltpeter over; whatever the run did not use stays in the station
        void OnCollect(long sender)
        {
            if (!_nview.IsOwner() || State != LeachingVatRules.StateDone) { if (RuleLog.On) ModLog.Verbose("LeachingVat.OnCollect: not the owner or nothing done - returns"); return; }
            var zdo = _nview.GetZDO();
            int amount = zdo.GetInt(KeyOut, 0);
            zdo.Set(KeyOut, 0);
            zdo.Set(KeyStart, 0L);
            zdo.Set(KeyUnits, 0);
            zdo.Set(KeyState, LeachingVatRules.StateIdle);
            ModLog.State("leaching station", "done", "idle", amount + " Saltpeter collected, " + LeachingUpgradeRules.Text(Held) + " stay in for the next run");
            _nview.InvokeRPC(sender, RpcCollected, amount);
        }

        // the collecting player's client: add the saltpeter to the inventory
        void OnCollected(long sender, int amount)
        {
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.Saltpeter) : null;
            if (p == null || prefab == null || amount <= 0) { if (RuleLog.On) ModLog.Verbose($"LeachingVat.OnCollected({amount}): no player, prefab or amount - returns"); return; }
            p.GetInventory().AddItem(prefab, amount);
            p.Message(MessageHud.MessageType.TopLeft, "Collected " + amount + " Saltpeter");
            if (RuleLog.On) ModLog.Verbose($"LeachingVat.OnCollected: {amount} Saltpeter added");
        }
    }
}
