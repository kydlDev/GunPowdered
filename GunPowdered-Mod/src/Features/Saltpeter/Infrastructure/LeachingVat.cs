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
    /// run 30 minutes minus 5 per step). The station holds at most that full batch; use (E) puts in half of it from the inventory, or what the player has,
    /// at any time (also during a run); holding Wood and using the station puts in wood only. D-132 (user 2026-11-20): whenever the station is idle and holds
    /// at least one base unit it starts by itself (no button) and leaches the whole base units inside, up to the step's batch; the rest stays in. The
    /// Saltpeter of a finished run piles up in the cauldron ("gp_vat_ready") and the next run starts at once while a base unit is inside; below that the
    /// station stops. E also takes the whole pile.
    /// Like the game's Smelter (D-035) every item is one request named by its prefab; the owner counts it only when the station takes that item and has room.
    /// Server-authoritative (D-005): only the owner of the station's network object changes its state. State lives in the ZDO ("gp_vat_state" 0 idle /
    /// 1 burning, "gp_vat_guano", "gp_vat_water", "gp_vat_fuel", "gp_vat_start" world time ms, "gp_vat_minutes", "gp_vat_units", "gp_vat_out" = the running
    /// run's Saltpeter, "gp_vat_ready" = the pile), so a run also finishes while nobody is near and survives save / load. A run started before D-129 has no
    /// "gp_vat_minutes" and takes the base 30 minutes; a station saved in the old state 2 (done, before D-132) moves its Saltpeter onto the pile.
    /// The model's state parts (cauldron_fire, cauldron_liquid, leach_flow, cauldron_salt) are switched on and off from that state on every client.
    /// </summary>
    internal sealed class LeachingVat : MonoBehaviour, Hoverable, Interactable
    {
        const string KeyState = "gp_vat_state", KeyFuel = "gp_vat_fuel", KeyStart = "gp_vat_start", KeyOut = "gp_vat_out";
        const string KeyGuano = "gp_vat_guano", KeyWater = "gp_vat_water", KeyMinutes = "gp_vat_minutes", KeyUnits = "gp_vat_units", KeyReady = "gp_vat_ready";
        // D-035 (user: "lock it down ... the same way the game is doing fill up for Kiln"): one item per request, named by its prefab, checked by the owner
        const string RpcAdd = "GP_VatAdd", RpcCollect = "GP_VatCollect", RpcCollected = "GP_VatCollected";
        const string WoodSharedName = "$item_wood", WoodPrefab = "Wood";
        const string UseKey = "[<color=yellow><b>$KEY_Use</b></color>] ";
        const float TickSeconds = 2f;
        ZNetView _nview;
        GameObject _fire, _liquid, _flow, _salt;

        void Awake()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"LeachingVat.Awake()");
            _nview = GetComponent<ZNetView>();
            if (_nview == null || !_nview.IsValid()) { if (RuleLog.IsOn()) ModLog.Verbose("LeachingVat.Awake: no valid network view - returns"); return; }
            _nview.Register<string>(RpcAdd, OnAdd);
            _nview.Register(RpcCollect, OnCollect);
            _nview.Register<int>(RpcCollected, OnCollected);
            _fire = Part("cauldron_fire"); _liquid = Part("cauldron_liquid"); _flow = Part("leach_flow"); _salt = Part("cauldron_salt");
            InvokeRepeating("Tick", UnityEngine.Random.Range(0.5f, TickSeconds), TickSeconds);
        }

        GameObject Part(string name)
        {
            Transform t = FindDeep(transform, name);
            if (RuleLog.IsOn()) ModLog.Verbose($"LeachingVat.Part(name={name}): returns {(t != null ? "found" : "null")}");
            return t != null ? t.gameObject : null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.FindDeep", 2d, $"LeachingVat.FindDeep(root={RuleLog.Shown(root)}, name={name})");
            foreach (Transform c in root)
            {
                if (c.name == name) { if (RuleLog.IsOn()) ModLog.Every("LeachingVat.FindDeep:hit", 2d, $"LeachingVat.FindDeep: returns {name}"); return c; }
                Transform f = FindDeep(c, name);
                if (f != null) { if (RuleLog.IsOn()) ModLog.Every("LeachingVat.FindDeep:child", 2d, $"LeachingVat.FindDeep: returns {name} (deeper)"); return f; }
            }
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.FindDeep:none", 2d, $"LeachingVat.FindDeep: returns null for {name}");
            return null;
        }

        // ---------- what the station holds and can do now ----------

        int State { get { return _nview.GetZDO().GetInt(KeyState, LeachingVatRules.StateIdle); } }

        /// <summary>The Saltpeter piled up in the cauldron, waiting for a player (D-132).</summary>
        int Ready { get { return _nview.GetZDO().GetInt(KeyReady, 0); } }

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
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.MinutesAt", 2d, $"LeachingVat.MinutesAt(step={step}): returns {minutes}");
            return minutes;
        }

        // ---------- owner: start, finish ----------

        void Tick()
        {
            try
            {
                if (!_nview.IsValid() || ZNet.instance == null) { if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Tick:invalid", 2d, "LeachingVat.Tick: no network view or no ZNet - returns"); return; }
                if (_nview.IsOwner() && ModSettings.FeatureOn(ModSettings.EnableSaltpeter)) Advance(_nview.GetZDO(), ZNet.instance.GetTimeSeconds());
                ShowState(State);
                ModLog.Every("leach-station-" + gameObject.GetInstanceID(), 30d, $"leaching station state {State}, step {Step}, holds {LeachingUpgradeRules.Text(Held)} of {LeachingUpgradeRules.Text(LeachingUpgradeRules.FullBatch(Unit, Step))}, pile {Ready} Saltpeter");
            }
            catch (Exception e)
            {
                ModLog.Warn("LeachingVat.Tick: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // owner (D-132): a run whose time is up puts its Saltpeter on the pile; then an idle station with at least one base unit inside starts the next run
        void Advance(ZDO zdo, double now)
        {
            int state = zdo.GetInt(KeyState, LeachingVatRules.StateIdle);
            string why = "base batch inside";
            if (state == LeachingVatRules.StateDone)
            {
                Finish(zdo, "a station saved before D-132 in the old 'done' state");
                state = LeachingVatRules.StateIdle;
            }
            else if (state == LeachingVatRules.StateBurning)
            {
                double minutes = zdo.GetFloat(KeyMinutes, (float)MinutesAt(0));
                if (!LeachingVatRules.IsReady(zdo.GetLong(KeyStart, 0L) / 1000d, now, 60d, minutes)) { if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Advance:burning", 2d, "LeachingVat.Advance: still leaching - returns"); return; }
                Finish(zdo, minutes + " min up");
                state = LeachingVatRules.StateIdle;
                why = "next run";
            }
            int step = Step;
            if (!LeachingUpgradeRules.StartsNow(state, Held, Unit, step))
            {
                if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Advance:idle", 2d, $"LeachingVat.Advance: idle, {LeachingUpgradeRules.Missing(Held, Unit)} - returns");
                return;
            }
            Begin(zdo, now, step, why);
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Advance:started", 2d, $"LeachingVat.Advance: started ({why}) - returns");
        }

        // owner: the run's Saltpeter goes onto the pile, the station is idle again (it may start the next run in the same tick)
        void Finish(ZDO zdo, string why)
        {
            int run = zdo.GetInt(KeyOut, 0);
            int pile = LeachingUpgradeRules.Pile(zdo.GetInt(KeyReady, 0), run);
            zdo.Set(KeyReady, pile);
            zdo.Set(KeyOut, 0);
            zdo.Set(KeyUnits, 0);
            zdo.Set(KeyStart, 0L);
            zdo.Set(KeyState, LeachingVatRules.StateIdle);
            ModLog.State("leaching station", "burning", "idle", $"{why}: {run} Saltpeter onto the pile, pile now {pile}, left inside {LeachingUpgradeRules.Text(Held)}");
        }

        // owner: one run of as many whole base units as the station holds (up to the step's batch); the rest stays in (D-129)
        void Begin(ZDO zdo, double now, int step, string why)
        {
            LeachingVatRules.VatAmounts held = Held, unit = Unit;
            int units = LeachingUpgradeRules.Units(held, unit, step);
            if (units < 1) { ModLog.Decision("leaching station start", "not started", LeachingUpgradeRules.Missing(held, unit) + ", holds " + LeachingUpgradeRules.Text(held)); return; }
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

        void ShowState(int state)
        {
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.ShowState", 2d, $"LeachingVat.ShowState(state={state})");
            LeachingVatRules.VatAmounts held = Held;
            bool burning = state == LeachingVatRules.StateBurning;
            bool wet = burning || (state == LeachingVatRules.StateIdle && (held.Guano > 0 || held.Water > 0));
            Set(_fire, burning);
            Set(_liquid, wet);
            Set(_flow, wet);
            Set(_salt, Ready > 0);
        }

        static void Set(GameObject go, bool on)
        {
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Set", 2d, $"LeachingVat.Set(go={RuleLog.Shown(go)}, on={on})");
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        // ---------- hover ----------

        public string GetHoverName() { return ItemCatalog.LeachingVat.Name; }

        public float GetHoverOffset() { return 0f; }

        public string GetHoverText()
        {
            if (!_nview.IsValid()) { if (RuleLog.IsOn()) ModLog.Every("LeachingVat.GetHoverText:invalid", 2d, "LeachingVat.GetHoverText: no network view - returns the name"); return GetHoverName(); }
            int state = State, step = Step, ready = Ready;
            LeachingVatRules.VatAmounts held = Held, unit = Unit, full = LeachingUpgradeRules.FullBatch(unit, step);
            string run;
            if (state == LeachingVatRules.StateBurning)
            {
                ZDO z = _nview.GetZDO();
                double left = LeachingVatRules.SecondsLeft(z.GetLong(KeyStart, 0L) / 1000d, ZNet.instance.GetTimeSeconds(), 60d, z.GetFloat(KeyMinutes, (float)MinutesAt(0)));
                run = $"Leaching {z.GetInt(KeyOut, 0)} Saltpeter ({Mathf.CeilToInt((float)(left / 60d))} min left)";
            }
            else
            {
                string missing = LeachingUpgradeRules.Missing(held, unit);
                run = missing.Length > 0 ? "Cannot start yet: " + missing : "Starting";
            }
            string text = GetHoverName() + "\n" + UpgradeLine(step) + "\n" + run + "\nHolds " + Holds(held, full) + "\n" + UseKey + "Fill";
            if (ready > 0) text += "\n" + UseKey + "Collect " + ready + " Saltpeter";
            text = Localization.instance.Localize(text);
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.GetHoverText", 2d, $"LeachingVat.GetHoverText: returns {RuleLog.Shown(text)}");
            return text;
        }

        static string UpgradeLine(int step)
        {
            LeachingVatRules.VatAmounts full = LeachingUpgradeRules.FullBatch(Unit, step);
            string line = $"Upgrades {step}/{LeachingUpgradeRules.MaxStep}: up to {full.Guano} Bat Guano + {full.Water} Water + {full.Fuel} Wood"
                + $" -> {LeachingVatRules.Saltpeter(LeachingUpgradeRules.UnitsPerBatch(step), ModSettings.VatSaltpeter.Value)} Saltpeter, {MinutesAt(step)} min";
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.UpgradeLine", 2d, $"LeachingVat.UpgradeLine: returns {line}");
            return line;
        }

        static string Holds(LeachingVatRules.VatAmounts held, LeachingVatRules.VatAmounts full)
        {
            string text = $"Bat Guano {held.Guano}/{full.Guano}, Water {held.Water}/{full.Water}, Wood {held.Fuel}/{full.Fuel}";
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Holds", 2d, $"LeachingVat.Holds: returns {text}");
            return text;
        }

        // ---------- use ----------

        // D-132: E takes the whole pile and puts in half a batch (up to what the station holds), at any time; nothing to take and nothing to add says why
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Interact", 2d, $"LeachingVat.Interact(user={RuleLog.Shown(user)}, hold={hold}, alt={alt})");
            if (hold || user == null || !_nview.IsValid()) { if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Interact:no", 2d, "LeachingVat.Interact: held, no user or no network view - returns false"); return false; }
            int collected = Collect(user);
            int sent = Fill(user, true);
            if (collected == 0 && sent == 0) TellWhyNothing(user);
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Interact:end", 2d, $"LeachingVat.Interact: collected {collected}, put in {sent} - returns true");
            return true;
        }

        // nothing collected and nothing put in: say why (what is missing for a base batch, the station is full, or the run is going)
        void TellWhyNothing(Humanoid user)
        {
            LeachingVatRules.VatAmounts held = Held, unit = Unit;
            string missing = LeachingUpgradeRules.Missing(held, unit);
            string text;
            if (missing.Length > 0) text = missing;
            else if (State == LeachingVatRules.StateBurning) text = "The saltpeter is forming";
            else text = "The leaching starts";
            user.Message(MessageHud.MessageType.Center, text);
            ModLog.Decision("leaching station use", "nothing to take or add", $"told '{text}', state {State}, holds {LeachingUpgradeRules.Text(held)}");
        }

        // holding Wood and using the station puts in wood only
        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (user == null || item == null || item.m_shared.m_name != WoodSharedName || !_nview.IsValid()) { if (RuleLog.IsOn()) ModLog.Verbose("LeachingVat.UseItem: not wood or no station - returns false"); return false; }
            Fill(user, false);
            if (RuleLog.IsOn()) ModLog.Verbose("LeachingVat.UseItem: wood put in - returns true");
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
            if (RuleLog.IsOn()) ModLog.Verbose($"LeachingVat.Fill: returns {sent}");
            return sent;
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
            if (RuleLog.IsOn()) ModLog.Verbose($"LeachingVat.SendItems({prefab}, wanted {wanted}): returns {sent}");
            return sent;
        }

        static ItemDrop.ItemData FirstStack(Inventory inv, string sharedName)
        {
            foreach (ItemDrop.ItemData item in inv.GetAllItems())
            {
                if (item.m_shared.m_name == sharedName && item.m_stack > 0) { if (RuleLog.IsOn()) ModLog.Every("LeachingVat.FirstStack", 2d, $"LeachingVat.FirstStack({sharedName}): returns a stack of {item.m_stack}"); return item; }
            }
            if (RuleLog.IsOn()) ModLog.Every("LeachingVat.FirstStack:none", 2d, $"LeachingVat.FirstStack({sharedName}): returns null");
            return null;
        }

        // client: ask the owner for the whole pile; returns how much was asked for (0 = nothing waiting or no room)
        int Collect(Humanoid user)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.Saltpeter) : null;
            int amount = Ready;
            if (prefab == null || amount < 1) { if (RuleLog.IsOn()) ModLog.Every("LeachingVat.Collect:none", 2d, $"LeachingVat.Collect: nothing waiting (pile {amount}) - returns 0"); return 0; }
            if (!user.GetInventory().CanAddItem(prefab, amount)) { user.Message(MessageHud.MessageType.Center, "Your inventory is full"); ModLog.Decision("leaching station collect", "not asked", $"inventory has no room for {amount} Saltpeter"); return 0; }
            _nview.InvokeRPC(RpcCollect);
            ModLog.Decision("leaching station collect", "asked the owner", $"{amount} Saltpeter waiting");
            return amount;
        }

        // owner: one item arrived; it counts only if it is one the station takes and its slot has room up to the full batch (anything else is refused and logged)
        void OnAdd(long sender, string prefab)
        {
            if (!_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Verbose("LeachingVat.OnAdd: not the owner - returns"); return; }
            var zdo = _nview.GetZDO();
            int step = Step;
            LeachingVatRules.VatAmounts have = Held, full = LeachingUpgradeRules.FullBatch(Unit, step);
            LeachingVatRules.Feed feed = LeachingVatRules.Accept(prefab, have, full);
            if (feed == LeachingVatRules.Feed.Guano) zdo.Set(KeyGuano, have.Guano + 1);
            else if (feed == LeachingVatRules.Feed.Water) zdo.Set(KeyWater, have.Water + 1);
            else if (feed == LeachingVatRules.Feed.Wood) zdo.Set(KeyFuel, have.Fuel + 1);
            ModLog.Decision("leaching station item", feed == LeachingVatRules.Feed.Refused ? "refused" : "took " + feed, $"item {prefab} from {sender}, state {State}, step {step}, now {LeachingUpgradeRules.Text(Held)} of {LeachingUpgradeRules.Text(full)}");
        }

        // owner: hand the whole pile over (D-132: collect always takes all); a running run and the materials inside are not touched
        void OnCollect(long sender)
        {
            if (!_nview.IsOwner()) { if (RuleLog.IsOn()) ModLog.Verbose("LeachingVat.OnCollect: not the owner - returns"); return; }
            var zdo = _nview.GetZDO();
            int amount = zdo.GetInt(KeyReady, 0);
            if (amount < 1) { ModLog.Decision("leaching station collect", "nothing sent", "the pile is empty, asked by " + sender); return; }
            zdo.Set(KeyReady, 0);
            ModLog.Decision("leaching station collect", "sent " + amount + " Saltpeter", $"to {sender}, state {State}, holds {LeachingUpgradeRules.Text(Held)}");
            _nview.InvokeRPC(sender, RpcCollected, amount);
        }

        // the collecting player's client: add the saltpeter to the inventory
        void OnCollected(long sender, int amount)
        {
            Player p = Player.m_localPlayer;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.Saltpeter) : null;
            if (p == null || prefab == null || amount <= 0) { if (RuleLog.IsOn()) ModLog.Verbose($"LeachingVat.OnCollected({amount}): no player, prefab or amount - returns"); return; }
            p.GetInventory().AddItem(prefab, amount);
            p.Message(MessageHud.MessageType.TopLeft, "Collected " + amount + " Saltpeter");
            if (RuleLog.IsOn()) ModLog.Verbose($"LeachingVat.OnCollected: {amount} Saltpeter added");
        }
    }
}
