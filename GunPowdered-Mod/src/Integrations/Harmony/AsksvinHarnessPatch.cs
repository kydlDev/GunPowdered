// kydl:read-only-observer - adds the harness state to Asksvins and handles the harness item; vanilla saddle, taming and cart behaviour are unchanged
using System;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.MobileCannon.Domain;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>D-116: every Asksvin gets the harness state component (ZDO flag, RPC, model).</summary>
    [HarmonyPatch(typeof(Tameable), "Awake")]
    internal static class AsksvinHarnessAwakePatch
    {
        static void Postfix(Tameable __instance)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinHarnessAwakePatch.Postfix", 2d, $"AsksvinHarnessAwakePatch.Postfix(__instance={RuleLog.Shown(__instance)})");
            if (__instance == null || Utils.GetPrefabName(__instance.gameObject) != AsksvinHarness.AnimalPrefab) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarnessAwakePatch.Postfix:19", 2d, "AsksvinHarnessAwakePatch.Postfix: returns (line 19)"); return; }
            if (__instance.GetComponent<AsksvinHarness>() == null) __instance.gameObject.AddComponent<AsksvinHarness>();
        }
    }

    /// <summary>D-116: using the Asksvin Harness item on a saddled tame Asksvin puts it on (like the saddle); on any other animal it is refused.</summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.UseItem))]
    internal static class AsksvinHarnessUsePatch
    {
        static bool Prefix(Tameable __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            if (item == null || item.m_dropPrefab == null || item.m_dropPrefab.name != GunPowderedPrefabs.AsksvinHarness) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarnessUsePatch.Prefix:30", 2d, "AsksvinHarnessUsePatch.Prefix: returns true (line 30)"); return true; }
            __result = true;
            try
            {
                AsksvinHarness h = __instance.GetComponent<AsksvinHarness>();
                ZNetView nview = __instance.GetComponent<ZNetView>();
                if (h == null || nview == null || !nview.IsValid())
                {
                    user.Message(MessageHud.MessageType.Center, "Only an Asksvin can wear the harness");
                    { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarnessUsePatch.Prefix:39", 2d, "AsksvinHarnessUsePatch.Prefix: returns false (line 39)"); return false; }
                }
                string why = AsksvinTowRules.WhyNotHarness(__instance.IsTamed(), nview.GetZDO().GetBool(ZDOVars.s_haveSaddleHash), h.Harnessed);
                ModLog.Decision("asksvin harness", why == null ? "put on" : "refused", why ?? __instance.GetHoverName());
                if (why != null)
                {
                    user.Message(MessageHud.MessageType.Center, why);
                    { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarnessUsePatch.Prefix:46", 2d, "AsksvinHarnessUsePatch.Prefix: returns false (line 46)"); return false; }
                }
                h.PutOn();
                user.GetInventory().RemoveOneItem(item);
                user.Message(MessageHud.MessageType.Center, __instance.GetHoverName() + " wears the cannon harness");
            }
            catch (NullReferenceException e)
            {
                ModLog.Warn("AsksvinHarnessUsePatch: " + e.Message + " - harness not put on");
            }
            { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarnessUsePatch.Prefix:56", 2d, "AsksvinHarnessUsePatch.Prefix: returns false (line 56)"); return false; }
        }
    }

    /// <summary>D-116: taking the saddle off drops the harness too.</summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.DropSaddle))]
    internal static class AsksvinHarnessDropPatch
    {
        static void Postfix(Tameable __instance, bool __result)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinHarnessDropPatch.Postfix", 2d, $"AsksvinHarnessDropPatch.Postfix(__instance={RuleLog.Shown(__instance)}, __result={RuleLog.Shown(__result)})");
            if (!__result || __instance == null) { if (RuleLog.IsOn()) ModLog.Every("AsksvinHarnessDropPatch.Postfix:67", 2d, "AsksvinHarnessDropPatch.Postfix: returns (line 67)"); return; }
            AsksvinHarness h = __instance.GetComponent<AsksvinHarness>();
            if (h != null) h.Drop();
        }
    }

    /// <summary>The tame Asksvin's hover text says when it wears the harness.</summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    internal static class AsksvinHarnessHoverPatch
    {
        static void Postfix(Tameable __instance, ref string __result)
        {
            if (RuleLog.IsOn()) ModLog.Every("AsksvinHarnessHoverPatch.Postfix", 2d, $"AsksvinHarnessHoverPatch.Postfix(__instance={RuleLog.Shown(__instance)}, __result={RuleLog.Shown(__result)})");
            AsksvinHarness h = __instance != null ? __instance.GetComponent<AsksvinHarness>() : null;
            if (h != null && h.Harnessed && !string.IsNullOrEmpty(__result)) __result += "\nWears a cannon harness";
        }
    }

    /// <summary>D-116: a Mobile Cannon hitched to an Asksvin cannot be grabbed by hand (the cart's own E) until it is unhitched at the tow ring.</summary>
    [HarmonyPatch(typeof(Vagon), nameof(Vagon.Interact))]
    internal static class MobileCannonTowedGrabPatch
    {
        static bool Prefix(Vagon __instance, Humanoid character, ref bool __result)
        {
            if (RuleLog.IsOn()) ModLog.Every("MobileCannonTowedGrabPatch.Prefix", 2d, $"MobileCannonTowedGrabPatch.Prefix(__instance={RuleLog.Shown(__instance)}, character={RuleLog.Shown(character)}, __result={RuleLog.Shown(__result)})");
            MobileCannonTow tow = __instance != null ? __instance.GetComponent<MobileCannonTow>() : null;
            if (tow == null || !tow.Towed) { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTowedGrabPatch.Prefix:93", 2d, "MobileCannonTowedGrabPatch.Prefix: returns true (line 93)"); return true; }
            if (character != null) character.Message(MessageHud.MessageType.Center, "The cannon is hitched to an Asksvin: unhitch it at the tow ring");
            __result = false;
            { if (RuleLog.IsOn()) ModLog.Every("MobileCannonTowedGrabPatch.Prefix:96", 2d, "MobileCannonTowedGrabPatch.Prefix: returns false (line 96)"); return false; }
        }
    }
}
