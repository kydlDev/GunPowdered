// kydl:read-only-observer - only the player's own animator state (the game's synced "statei"/"statef") changes while the musket hangs at the side; no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Musket.Infrastructure;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// D-155 (user 2026-11-22: "I don't want the left hand to hold the musket while the player is running (regardless if the player has or not a torch/weapon
    /// on the other hand)"): the musket is a crossbow-type item, so the game animates the left arm reaching for the barrel. While it hangs at the side the
    /// game's animation state is switched like a one-handed weapon (the left arm hangs and swings free) or, with a torch next to it, the game's own left-torch
    /// state (the torch held up like the game does it with a club). The right arm stays MusketRig's side carry. Raised again = the game's own state. The
    /// state is the game's synced animator value, so every client sees it.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), "SetupAnimationState")]
    internal static class MusketAnimationStatePatch
    {
        static readonly AccessTools.FieldRef<Humanoid, ItemDrop.ItemData> RightItem = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>("m_rightItem");
        static readonly Action<Humanoid, ItemDrop.ItemData.AnimationState> SetAnimationState = AccessTools.MethodDelegate<Action<Humanoid, ItemDrop.ItemData.AnimationState>>(AccessTools.Method(typeof(Humanoid), "SetAnimationState"));
        static readonly Action<Humanoid> SetupAnimationState = AccessTools.MethodDelegate<Action<Humanoid>>(AccessTools.Method(typeof(Humanoid), "SetupAnimationState"));

        static void Postfix(Humanoid __instance)
        {
            try
            {
                Player player = __instance as Player;
                ItemDrop.ItemData musket = player != null ? MusketGun.Held(player) : null;
                MusketRig rig = musket != null ? player.GetComponent<MusketRig>() : null;
                if (rig == null || rig.Raised) { if (RuleLog.Due("MusketAnimationStatePatch.Postfix:own", 2d)) ModLog.Every("MusketAnimationStatePatch.Postfix:own", 2d, "MusketAnimationStatePatch.Postfix: no lowered musket - the game's own state"); return; }
                ItemDrop.ItemData other = RightItem(player);
                bool torch = other != null && other != musket && other.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch;
                ItemDrop.ItemData.AnimationState state = torch ? ItemDrop.ItemData.AnimationState.LeftTorch : ItemDrop.ItemData.AnimationState.OneHanded;
                SetAnimationState(player, state);
                ModLog.Verbose("MusketAnimationStatePatch: " + player.GetPlayerName() + " musket at the side - animation state " + state);
            }
            catch (Exception e)
            {
                ModLog.Warn("MusketAnimationStatePatch: " + e.GetType().FullName + ": " + e.Message + " - the game's own animation state stays");
            }
        }

        /// <summary>Applies the animation state again (the musket was raised or lowered). Expected outcome: true when it ran.</summary>
        internal static bool Refresh(Humanoid human)
        {
            if (human == null) { ModLog.Verbose("MusketAnimationStatePatch.Refresh: no character - returns false"); return false; }
            try
            {
                SetupAnimationState(human);
                ModLog.Verbose("MusketAnimationStatePatch.Refresh: applied for " + human.name);
                return true;
            }
            catch (Exception e)
            {
                ModLog.Warn("MusketAnimationStatePatch.Refresh: " + e.GetType().FullName + ": " + e.Message);
                return false;
            }
        }
    }
}
