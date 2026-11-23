// kydl:read-only-observer - only the player's own equipment slots change (a torch or shield next to the lowered musket); no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Musket.Domain;
using GunPowdered.Features.Musket.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// D-155 (user 2026-11-22: "left hand should be free when running, to be able to equip the torch"; "when going into aim mode, if there is anything in left
    /// hand, that item gets unequipped"; "only torches and shields should be able to go into the offhand"): while the Iron Musket hangs at the side, equipping
    /// a Torch or a Shield keeps the musket equipped. The game normally puts a bow-type weapon away for them; here the item goes into the other hand slot and
    /// the game's own SetupEquipment shows both (MusketRig moves its model to the left hand). The aim key takes it off again (MusketRig.Aim). While the musket
    /// is raised, and for every other item (a club puts the musket away), the game's own rule applies (MusketRules.FitsBesideLoweredMusket).
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
                if (player == null || item == null || !MusketRules.FitsBesideLoweredMusket(item.m_shared.m_itemType.ToString())) { if (RuleLog.Due("MusketOffhandPatch.Prefix:other", 2d)) ModLog.Every("MusketOffhandPatch.Prefix:other", 2d, "MusketOffhandPatch.Prefix: not a torch or shield for a player - the game equips it"); return true; }
                ItemDrop.ItemData musket = MusketGun.Held(player);
                MusketRig rig = musket != null ? player.GetComponent<MusketRig>() : null;
                if (rig == null || rig.Raised) { ModLog.Verbose("MusketOffhandPatch: " + item.m_shared.m_itemType + " while the musket is " + (musket == null ? "not held" : "raised") + " - the game's own rule"); return true; }
                if (player.IsItemEquiped(item) || !player.GetInventory().ContainsItem(item)) { ModLog.Verbose("MusketOffhandPatch: " + item.m_shared.m_itemType + " already equipped or not carried - the game decides"); return true; }
                ItemDrop.ItemData other = RightItem(player);
                if (other != null && other != musket) player.UnequipItem(other, triggerEquipEffects);
                RightItem(player) = item;
                item.m_equipped = true;
                SetupEquipment(player);
                __result = true;
                ModLog.Info("MusketOffhandPatch: " + player.GetPlayerName() + " holds " + item.m_shared.m_name + " (" + item.m_shared.m_itemType + ") next to the lowered musket");
                return false;
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("MusketOffhandPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().FullName + ": " + e.Message + " - the game equips the item its own way" + "\n" + e.StackTrace);
                return true;
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("MusketOffhandPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().FullName + ": " + e.Message + " - the game equips the item its own way" + "\n" + e.StackTrace);
                return true;
            }
            catch (Exception e)
            {
                ModLog.Warn("MusketOffhandPatch: " + e.GetType().FullName + ": " + e.Message + " - the game equips the item its own way" + "\n" + e.StackTrace);
                return true;
            }
        }
    }

    /// <summary>
    /// D-155: a shield held next to the lowered musket blocks (the game would block with the musket, its left-slot item). Raised = no shield (the aim key took
    /// it off) and the musket's own block (user: "keep the current blocking").
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), "GetCurrentBlocker")]
    internal static class MusketOffhandBlockerPatch
    {
        static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> RightItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_rightItem");

        static void Postfix(Humanoid __instance, ref ItemDrop.ItemData __result)
        {
            try
            {
                ItemDrop.ItemData shield = RightItem(__instance);
                if (shield == null || shield.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield || !(__instance is Player player) || MusketGun.Held(player) == null) { if (RuleLog.Due("MusketOffhandBlockerPatch:none", 2d)) ModLog.Every("MusketOffhandBlockerPatch:none", 2d, "MusketOffhandBlockerPatch: no shield beside the musket - the game's blocker"); return; }
                __result = shield;
                if (RuleLog.Due("MusketOffhandBlockerPatch:shield", 2d)) ModLog.Every("MusketOffhandBlockerPatch:shield", 2d, "MusketOffhandBlockerPatch: " + player.GetPlayerName() + " blocks with " + shield.m_shared.m_name + " (beside the lowered musket)");
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("MusketOffhandBlockerPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().FullName + ": " + e.Message + " - the game's blocker stays" + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("MusketOffhandBlockerPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().FullName + ": " + e.Message + " - the game's blocker stays" + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("MusketOffhandBlockerPatch: " + e.GetType().FullName + ": " + e.Message + " - the game's blocker stays" + "\n" + e.StackTrace);
            }
        }

        /// <summary>Test hook use: the item the character would block with now. Expected outcome: the item or null.</summary>
        internal static ItemDrop.ItemData Blocker(Humanoid human)
        {
            if (human == null) { ModLog.Verbose("MusketOffhandBlockerPatch.Blocker: no character - returns null"); return null; }
            ItemDrop.ItemData b = Traverse.Create(human).Method("GetCurrentBlocker").GetValue<ItemDrop.ItemData>();
            ModLog.Verbose("MusketOffhandBlockerPatch.Blocker: returns " + (b != null ? b.m_shared.m_name : "null"));
            return b;
        }
    }
}
