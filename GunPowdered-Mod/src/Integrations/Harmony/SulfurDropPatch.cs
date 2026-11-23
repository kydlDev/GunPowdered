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
                if (!ModSettings.FeatureOn(ModSettings.EnableSulfur) || __result == null || __instance == null) { if (RuleLog.Due("SulfurDropPatch.Postfix:28", 2d)) ModLog.Every("SulfurDropPatch.Postfix:28", 2d, $"SulfurDropPatch.Postfix: returns because !ModSettings.FeatureOn(ModSettings.EnableSulfur) || __result == null || __instance == null (line 28)"); return; }
                SulfurSource? source = SourceOf(Utils.GetPrefabName(__instance.gameObject));
                if (source == null) { if (RuleLog.Due("SulfurDropPatch.Postfix:30", 2d)) ModLog.Every("SulfurDropPatch.Postfix:30", 2d, $"SulfurDropPatch.Postfix: returns because source == null (line 30)"); return; }

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
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("SulfurDropPatch: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("SulfurDropPatch: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("SulfurDropPatch: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>Which sulfur source this creature prefab is, or null when it is no Blob, Oozer or Writhan.</summary>
        internal static SulfurSource? SourceOf(string prefabName)
        {
            if (RuleLog.Due("SulfurDropPatch.SourceOf", 2d)) ModLog.Every("SulfurDropPatch.SourceOf", 2d, $"SulfurDropPatch.SourceOf(prefabName={RuleLog.Shown(prefabName)})");
            if (prefabName == BlobPrefab) return SulfurSource.Blob;
            if (prefabName == OozerPrefab) { var returnedAt64 = SulfurSource.Oozer; if (RuleLog.Due("SulfurDropPatch.SourceOf:64", 2d)) ModLog.Every("SulfurDropPatch.SourceOf:64", 2d, $"SulfurDropPatch.SourceOf: returns {returnedAt64} because prefabName == OozerPrefab (line 64)"); return returnedAt64; }
            if (prefabName == WrithanPrefab) { var returnedAt65 = SulfurSource.Writhan; if (RuleLog.Due("SulfurDropPatch.SourceOf:65", 2d)) ModLog.Every("SulfurDropPatch.SourceOf:65", 2d, $"SulfurDropPatch.SourceOf: returns {returnedAt65} because prefabName == WrithanPrefab (line 65)"); return returnedAt65; }
            { if (RuleLog.Due("SulfurDropPatch.SourceOf:66", 2d)) ModLog.Every("SulfurDropPatch.SourceOf:66", 2d, $"SulfurDropPatch.SourceOf: returns null (line 66)"); return null; }
        }
    }
}
