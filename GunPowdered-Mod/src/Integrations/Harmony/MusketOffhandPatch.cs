// kydl:read-only-observer - only the player's own equipment slots change (a torch next to the lowered musket); no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Musket.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// D-155 (user 2026-11-22: "left hand should be free when running, to be able to equip the torch"; "when going into aim mode, if there is anything in left
    /// hand, that item gets unequipped"): while the Iron Musket hangs at the side, equipping a Torch keeps the musket equipped. The game normally puts a
    /// bow-type weapon away for a torch; here the torch goes into the other hand slot and the game's own SetupEquipment shows both (MusketRig moves the torch
    /// model to the left hand). The aim key takes the torch off again (MusketRig.Aim). While the musket is raised the game's own rule applies.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    internal static class MusketOffhandPatch
    {
        static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> RightItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_rightItem");
        static readonly Action<Humanoid> SetupEquipment = AccessTools.MethodDelegate<Action<Humanoid>>(AccessTools.Method(typeof(Humanoid), "SetupEquipment"));

        static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, ref bool __result)
        {
            try
            {
                Player player = __instance as Player;
                if (player == null || item == null || item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch) { if (RuleLog.Due("MusketOffhandPatch.Prefix:other", 2d)) ModLog.Every("MusketOffhandPatch.Prefix:other", 2d, "MusketOffhandPatch.Prefix: not a torch for a player - the game equips it"); return true; }
                ItemDrop.ItemData musket = MusketGun.Held(player);
                MusketRig rig = musket != null ? player.GetComponent<MusketRig>() : null;
                if (rig == null || rig.Raised) { ModLog.Verbose("MusketOffhandPatch: torch while the musket is " + (musket == null ? "not held" : "raised") + " - the game's own rule"); return true; }
                if (player.IsItemEquiped(item) || !player.GetInventory().ContainsItem(item)) { ModLog.Verbose("MusketOffhandPatch: torch already equipped or not carried - the game decides"); return true; }
                ItemDrop.ItemData other = RightItem(player);
                if (other != null && other != musket) player.UnequipItem(other, triggerEquipEffects);
                RightItem(player) = item;
                item.m_equipped = true;
                SetupEquipment(player);
                __result = true;
                ModLog.Info("MusketOffhandPatch: " + player.GetPlayerName() + " holds " + item.m_shared.m_name + " next to the lowered musket");
                return false;
            }
            catch (Exception e)
            {
                ModLog.Warn("MusketOffhandPatch: " + e.GetType().FullName + ": " + e.Message + " - the game equips the torch its own way");
                return true;
            }
        }
    }
}
