// kydl:read-only-observer - Humanoid here is only the player who uses the item; no creature is moved, commanded or spawned
using System;
using KydlCore.Common;
using KydlCore.Core;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Water.Domain;

namespace GunPowdered.Features.Water.Infrastructure
{
    /// <summary>
    /// The Rain Barrel piece (D-009, D-010, D-012): collects water by itself while it rains and nothing covers it, and the player takes the water by
    /// interacting. Server-authoritative (D-005): only the client that owns the barrel's network object fills it (so one barrel is filled once);
    /// taking goes through the owner, which answers with the amount the asking player may add. State lives in the barrel's ZDO: "gp_water" (units) and
    /// "gp_time" (world time of the last fill step, milliseconds).
    /// </summary>
    internal sealed class RainBarrel : MonoBehaviour, Hoverable, Interactable
    {
        const string KeyWater = "gp_water", KeyTime = "gp_time", RpcTake = "GP_RainTake", RpcTaken = "GP_RainTaken";
        const float TickSeconds = 2f;
        const int MaxTake = 1000;                   // sanity cap; the player takes ALL the water that fits in the inventory in one go (user, 2026-11-18)
        ZNetView _nview;

        void Awake()
        {
            if (RuleLog.On) ModLog.Verbose($"RainBarrel.Awake()");
            _nview = GetComponent<ZNetView>();
            if (_nview == null || !_nview.IsValid()) { if (RuleLog.On) ModLog.Verbose("RainBarrel.Awake: returns (line 29)"); return; }
            _nview.Register<int>(RpcTake, OnTake);
            _nview.Register<int>(RpcTaken, OnTaken);
            InvokeRepeating("Tick", UnityEngine.Random.Range(0.5f, TickSeconds), TickSeconds);
        }

        float Water { get { return _nview.GetZDO().GetFloat(KeyWater, 0f); } }

        void Tick()
        {
            try
            {
                if (!_nview.IsValid() || !_nview.IsOwner() || ZNet.instance == null) { if (RuleLog.On) ModLog.Every("RainBarrel.Tick:41", 2d, "RainBarrel.Tick: returns (line 41)"); return; }
                double now = ZNet.instance.GetTimeSeconds();
                var zdo = _nview.GetZDO();
                long last = zdo.GetLong(KeyTime, 0L);
                zdo.Set(KeyTime, (long)(now * 1000d));
                if (last == 0L) { if (RuleLog.On) ModLog.Every("RainBarrel.Tick:46", 2d, "RainBarrel.Tick: returns (line 46)"); return; }                                  // first step only starts the clock

                bool wet = EnvMan.instance != null && EnvMan.IsWet();
                var r = RainBarrelRules.Fill(Water, ModSettings.RainBarrelCapacity.Value, ModSettings.RainBarrelRatePerMinute.Value,
                    (float)(now - last / 1000d), wet && ModSettings.FeatureOn(ModSettings.EnableWater), IsCovered());
                if (r.Status == ResponseStatus.Success && Math.Abs(r.Value - Water) > 0.001f)
                {
                    zdo.Set(KeyWater, r.Value);
                    ModLog.Every("rain-barrel-" + gameObject.GetInstanceID(), 30d, "rain barrel " + r.Value.ToString("0.#") + " water");
                }
            }
            catch (Exception e)
            {
                ModLog.Warn("RainBarrel.Tick: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // A roof, a floor or the terrain within 20 m straight above blocks the rain (D-010). The barrel's own colliders do not count.
        bool IsCovered()
        {
            if (RuleLog.On) ModLog.Every("RainBarrel.IsCovered", 2d, $"RainBarrel.IsCovered()");
            Vector3 from = transform.position + Vector3.up * 1.2f;
            RaycastHit[] hits = Physics.RaycastAll(from, Vector3.up, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in hits) if (h.collider != null && !h.collider.transform.IsChildOf(transform)) { if (RuleLog.On) ModLog.Every("RainBarrel.IsCovered:69", 2d, "RainBarrel.IsCovered: returns true (line 69)"); return true; }
            { if (RuleLog.On) ModLog.Every("RainBarrel.IsCovered:70", 2d, "RainBarrel.IsCovered: returns false (line 70)"); return false; }
        }

        public string GetHoverText()
        {
            if (RuleLog.On) ModLog.Verbose($"RainBarrel.GetHoverText()");
            string line = Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] Take all water");
            { var returnedAt77 = ItemCatalog.RainBarrel.Name + " (" + Mathf.FloorToInt(Water) + "/" + Mathf.FloorToInt(ModSettings.RainBarrelCapacity.Value) + ")\n" + line; if (RuleLog.On) ModLog.Verbose($"RainBarrel.GetHoverText: returns {returnedAt77} (line 77)"); return returnedAt77; }
        }

        public string GetHoverName() { return ItemCatalog.RainBarrel.Name; }

        public float GetHoverOffset() { return 0f; }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (RuleLog.On) ModLog.Every("RainBarrel.Interact", 2d, $"RainBarrel.Interact(user={user}, hold={hold}, alt={alt})");
            if (hold || user == null || !_nview.IsValid()) { if (RuleLog.On) ModLog.Every("RainBarrel.Interact:87", 2d, "RainBarrel.Interact: returns false (line 87)"); return false; }
            int fits = FreeRoom(user.GetInventory());
            if (fits <= 0) { user.Message(MessageHud.MessageType.Center, "Your inventory is full"); { if (RuleLog.On) ModLog.Every("RainBarrel.Interact:89", 2d, "RainBarrel.Interact: returns true (line 89)"); return true; } }
            _nview.InvokeRPC(RpcTake, fits);
            { if (RuleLog.On) ModLog.Every("RainBarrel.Interact:91", 2d, "RainBarrel.Interact: returns true (line 91)"); return true; }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) { return false; }

        // owner: take the water out of the barrel and tell the asking player how much arrived
        void OnTake(long sender, int requested)
        {
            if (RuleLog.On) ModLog.Verbose($"RainBarrel.OnTake(sender={sender}, requested={requested})");
            if (!_nview.IsOwner()) { if (RuleLog.On) ModLog.Verbose("RainBarrel.OnTake: returns (line 100)"); return; }
            var r = RainBarrelRules.Take(Water, Mathf.Clamp(requested, 0, MaxTake));
            if (r.Status != ResponseStatus.Success) { if (RuleLog.On) ModLog.Verbose("RainBarrel.OnTake: returns (line 102)"); return; }
            _nview.GetZDO().Set(KeyWater, Water - r.Value);
            _nview.InvokeRPC(sender, RpcTaken, r.Value);
        }

        // the asking player's client: add the water to the inventory
        void OnTaken(long sender, int amount)
        {
            if (RuleLog.On) ModLog.Verbose($"RainBarrel.OnTaken(sender={sender}, amount={amount})");
            Player p = Player.m_localPlayer;
            GameObject water = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.Water) : null;
            if (p == null || water == null || amount <= 0) { if (RuleLog.On) ModLog.Verbose("RainBarrel.OnTaken: returns (line 113)"); return; }
            for (int left = amount; left > 0; left -= ItemCatalog.Water.Stack) p.GetInventory().AddItem(water, Mathf.Min(left, ItemCatalog.Water.Stack));
            p.Message(MessageHud.MessageType.TopLeft, "Took " + amount + " Water");
        }

        static int FreeRoom(Inventory inv)
        {
            if (RuleLog.On) ModLog.Every("RainBarrel.FreeRoom", 2d, $"RainBarrel.FreeRoom(inv={inv})");
            GameObject water = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(GunPowderedPrefabs.Water) : null;
            if (inv == null || water == null) { if (RuleLog.On) ModLog.Every("RainBarrel.FreeRoom:122", 2d, "RainBarrel.FreeRoom: returns 0 (line 122)"); return 0; }
            for (int n = Mathf.Min(MaxTake, Mathf.FloorToInt(ModSettings.RainBarrelCapacity.Value)); n > 0; n--) if (inv.CanAddItem(water, n)) { var returnedAt123 = n; if (RuleLog.On) ModLog.Every("RainBarrel.FreeRoom:123", 2d, $"RainBarrel.FreeRoom: returns {returnedAt123} (line 123)"); return returnedAt123; }
            { if (RuleLog.On) ModLog.Every("RainBarrel.FreeRoom:124", 2d, "RainBarrel.FreeRoom: returns 0 (line 124)"); return 0; }
        }
    }
}
