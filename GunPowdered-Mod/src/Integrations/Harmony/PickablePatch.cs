// kydl:read-only-observer - Humanoid here is only the player who picks; no creature is moved, commanded or spawned
using System;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Guano.Domain;
using GunPowdered.Features.Sulfur.Domain;
using GunPowdered.Integrations.Valheim;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// Bat Guano deposits and Sulfurous Mud both need a pickaxe in hand (user, 2026-11-18: no special guano tool). The amount a deposit gives is the one stored in
    /// its ZDO when it was placed; a deposit without one (Bat Swamp vegetation, placed by the game) rolls it at the first pick and stores it.
    /// </summary>
    internal static class PickablePatch
    {
        internal static bool IsGuano(Pickable p) { return Utils.GetPrefabName(p.gameObject) == GunPowderedPrefabs.GuanoDeposit; }

        internal static bool IsMud(Pickable p) { return Utils.GetPrefabName(p.gameObject) == GunPowderedPrefabs.SulfurousMud; }

        [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
        internal static class RequireTool
        {
            // __args[0] is the humanoid that interacts (the parameter list is read by position)
            static bool Prefix(Pickable __instance, object[] __args, ref bool __result)
            {
                try
                {
                    bool guano = IsGuano(__instance), mud = IsMud(__instance);
                    if (!guano && !mud) { if (RuleLog.Due("RequireTool.Prefix:32", 2d)) ModLog.Every("RequireTool.Prefix:32", 2d, $"RequireTool.Prefix: returns true because !guano && !mud (line 32)"); return true; }
                    var user = __args != null && __args.Length > 0 ? __args[0] as Humanoid : null;
                    if (user == null) { if (RuleLog.Due("RequireTool.Prefix:34", 2d)) ModLog.Every("RequireTool.Prefix:34", 2d, $"RequireTool.Prefix: returns true because user == null (line 34)"); return true; }

                    ItemDrop.ItemData held = user.GetCurrentWeapon();
                    bool ok = held != null && held.m_shared.m_skillType == Skills.SkillType.Pickaxes;
                    ModLog.Decision("deposit tool check", ok ? "pick allowed" : "refused, no pickaxe", Utils.GetPrefabName(__instance.gameObject) + ", held " + (held != null ? held.m_shared.m_name + " (" + held.m_shared.m_skillType + ")" : "nothing"));
                    if (ok) return true;

                    user.Message(MessageHud.MessageType.Center, "You need a pickaxe");
                    __result = false;
                    { if (RuleLog.Due("RequireTool.Prefix:43", 2d)) ModLog.Every("RequireTool.Prefix:43", 2d, $"RequireTool.Prefix: returns false (line 43)"); return false; }
                }
                catch (System.NullReferenceException e)
                {
                    ModLog.Warn("PickablePatch.RequireTool: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                    return true;
                }
                catch (UnityEngine.MissingReferenceException e)
                {
                    ModLog.Warn("PickablePatch.RequireTool: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                    return true;
                }
                catch (Exception e)
                {
                    ModLog.Warn("PickablePatch.RequireTool: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Pickable), "RPC_Pick")]
        internal static class SetAmount
        {
            // runs on the machine that owns the deposit, just before the game drops the items
            static void Prefix(Pickable __instance)
            {
                try
                {
                    bool guano = IsGuano(__instance), mud = IsMud(__instance);
                    if (!guano && !mud) { if (RuleLog.Due("SetAmount.Prefix:72", 2d)) ModLog.Every("SetAmount.Prefix:72", 2d, $"SetAmount.Prefix: returns because !guano && !mud (line 72)"); return; }
                    ZNetView view = __instance.GetComponent<ZNetView>();
                    if (view == null || !view.IsValid()) { if (RuleLog.Due("SetAmount.Prefix:74", 2d)) ModLog.Every("SetAmount.Prefix:74", 2d, $"SetAmount.Prefix: returns because view == null || !view.IsValid() (line 74)"); return; }

                    int amount = view.GetZDO().GetInt(DepositSpawner.KeyAmount, 0);
                    if (amount < 1)
                    {
                        amount = guano ? GuanoDepositRules.GatherablesPerDeposit(CaveKind.SwampBatArea, () => UnityEngine.Random.value)
                                       : SulfurRules.Amount(SulfurSource.SulfurousMud, () => UnityEngine.Random.value);
                        view.GetZDO().Set(DepositSpawner.KeyAmount, amount);
                    }
                    bool full = !view.GetZDO().GetBool(ZDOVars.s_picked);           // a pick on a deposit that is not full gives nothing
                    __instance.m_amount = DepositRefill.Gives(full, amount);
                    if (!full) ModLog.Decision("deposit picked", "nothing, not full", Utils.GetPrefabName(__instance.gameObject));
                    ModLog.Decision("deposit picked", amount + " " + (guano ? "Bat Guano" : "Sulfur"), Utils.GetPrefabName(__instance.gameObject) + " at " + __instance.transform.position.ToString("0") + ", extra drops " + (__instance.m_extraDrops != null && __instance.m_extraDrops.m_drops != null ? __instance.m_extraDrops.m_drops.Count : 0));
                }
                catch (System.NullReferenceException e)
                {
                    ModLog.Warn("PickablePatch.SetAmount: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                }
                catch (UnityEngine.MissingReferenceException e)
                {
                    ModLog.Warn("PickablePatch.SetAmount: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                }
                catch (Exception e)
                {
                    ModLog.Warn("PickablePatch.SetAmount: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
                }
            }
        }
    }
}
