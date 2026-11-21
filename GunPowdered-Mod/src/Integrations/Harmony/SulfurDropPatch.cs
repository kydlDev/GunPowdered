using System;
using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Sulfur.Domain;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// Blob (prefab Blob) and Oozer (prefab BlobElite) can drop 1-2 extra sulfur (D-016: 10 % chance each, a Tunable); the Writhan (prefab Writhan)
    /// 3-5 (D-146: 20 % chance, its own Tunable). The game builds the loot list in
    /// CharacterDrop.GenerateDropList on the client that owns the creature, so the roll needs no extra network code: the list is dropped as usual.
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class SulfurDropPatch
    {
        const string BlobPrefab = "Blob";
        const string OozerPrefab = "BlobElite";
        const string WrithanPrefab = "Writhan";      // D-146: Ashlands, 20 % chance of 3-5 sulfur

        static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            try
            {
                if (!ModSettings.FeatureOn(ModSettings.EnableSulfur) || __result == null || __instance == null) { if (RuleLog.Due("SulfurDropPatch.Postfix:28", 2d)) ModLog.Every("SulfurDropPatch.Postfix:28", 2d, "SulfurDropPatch.Postfix: returns (line 28)"); return; }
                SulfurSource? source = SourceOf(Utils.GetPrefabName(__instance.gameObject));
                if (source == null) { if (RuleLog.Due("SulfurDropPatch.Postfix:30", 2d)) ModLog.Every("SulfurDropPatch.Postfix:30", 2d, "SulfurDropPatch.Postfix: returns (line 30)"); return; }

                double chance = SulfurRules.KillChance(source.Value, ModSettings.KillSulfurChance.Value, ModSettings.WrithanSulfurChance.Value);
                if (!Roll.Chance(chance, UnityEngine.Random.value))
                {
                    ModLog.Decision("sulfur drop", "none", Utils.GetPrefabName(__instance.gameObject) + ", chance " + chance);
                    return;
                }
                GameObject sulfur = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(ItemCatalog.Sulfur) : null;
                if (sulfur == null) { ModLog.Warn("SulfurDropPatch: the Sulfur prefab was not found"); return; }

                int amount = SulfurRules.Amount(source.Value, () => UnityEngine.Random.value);
                __result.Add(new KeyValuePair<GameObject, int>(sulfur, amount));
                ModLog.Decision("sulfur drop", amount + " Sulfur", source.Value + " killed");
            }
            catch (Exception e)
            {
                ModLog.Warn("SulfurDropPatch: " + e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>Which sulfur source this creature prefab is, or null when it is no Blob, Oozer or Writhan.</summary>
        internal static SulfurSource? SourceOf(string prefabName)
        {
            if (RuleLog.Due("SulfurDropPatch.SourceOf", 2d)) ModLog.Every("SulfurDropPatch.SourceOf", 2d, $"SulfurDropPatch.SourceOf(prefabName={RuleLog.Shown(prefabName)})");
            if (prefabName == BlobPrefab) { var returnedAt55 = SulfurSource.Blob; if (RuleLog.Due("SulfurDropPatch.SourceOf:55", 2d)) ModLog.Every("SulfurDropPatch.SourceOf:55", 2d, $"SulfurDropPatch.SourceOf: returns {RuleLog.Shown(returnedAt55)} (line 55)"); return returnedAt55; }
            if (prefabName == OozerPrefab) { var returnedAt56 = SulfurSource.Oozer; if (RuleLog.Due("SulfurDropPatch.SourceOf:56", 2d)) ModLog.Every("SulfurDropPatch.SourceOf:56", 2d, $"SulfurDropPatch.SourceOf: returns {RuleLog.Shown(returnedAt56)} (line 56)"); return returnedAt56; }
            if (prefabName == WrithanPrefab) { var returnedAt57 = SulfurSource.Writhan; if (RuleLog.Due("SulfurDropPatch.SourceOf:57", 2d)) ModLog.Every("SulfurDropPatch.SourceOf:57", 2d, $"SulfurDropPatch.SourceOf: returns {RuleLog.Shown(returnedAt57)} (line 57)"); return returnedAt57; }
            { if (RuleLog.Due("SulfurDropPatch.SourceOf:58", 2d)) ModLog.Every("SulfurDropPatch.SourceOf:58", 2d, "SulfurDropPatch.SourceOf: returns null (line 58)"); return null; }
        }
    }
}
