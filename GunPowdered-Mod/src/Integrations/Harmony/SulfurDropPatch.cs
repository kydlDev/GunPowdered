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
    /// Blob (prefab Blob) and Oozer (prefab BlobElite) can drop 1-2 extra sulfur (D-016: 10 % chance each, a Tunable). The game builds the loot list in
    /// CharacterDrop.GenerateDropList on the client that owns the creature, so the roll needs no extra network code: the list is dropped as usual.
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class SulfurDropPatch
    {
        const string BlobPrefab = "Blob";
        const string OozerPrefab = "BlobElite";

        static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            try
            {
                if (!ModSettings.FeatureOn(ModSettings.EnableSulfur) || __result == null || __instance == null) { if (RuleLog.On) ModLog.Every("SulfurDropPatch.Postfix:26", 2d, "SulfurDropPatch.Postfix: returns (line 26)"); return; }
                SulfurSource? source = SourceOf(Utils.GetPrefabName(__instance.gameObject));
                if (source == null) { if (RuleLog.On) ModLog.Every("SulfurDropPatch.Postfix:28", 2d, "SulfurDropPatch.Postfix: returns (line 28)"); return; }

                if (!Roll.Chance(ModSettings.KillSulfurChance.Value, UnityEngine.Random.value))
                {
                    ModLog.Decision("sulfur drop", "none", Utils.GetPrefabName(__instance.gameObject) + ", chance " + ModSettings.KillSulfurChance.Value);
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

        /// <summary>Which sulfur source this creature prefab is, or null when it is neither a Blob nor an Oozer.</summary>
        internal static SulfurSource? SourceOf(string prefabName)
        {
            if (RuleLog.On) ModLog.Every("SulfurDropPatch.SourceOf", 2d, $"SulfurDropPatch.SourceOf(prefabName={prefabName})");
            if (prefabName == BlobPrefab) { var returnedAt52 = SulfurSource.Blob; if (RuleLog.On) ModLog.Every("SulfurDropPatch.SourceOf:52", 2d, $"SulfurDropPatch.SourceOf: returns {returnedAt52} (line 52)"); return returnedAt52; }
            if (prefabName == OozerPrefab) { var returnedAt53 = SulfurSource.Oozer; if (RuleLog.On) ModLog.Every("SulfurDropPatch.SourceOf:53", 2d, $"SulfurDropPatch.SourceOf: returns {returnedAt53} (line 53)"); return returnedAt53; }
            { if (RuleLog.On) ModLog.Every("SulfurDropPatch.SourceOf:54", 2d, "SulfurDropPatch.SourceOf: returns null (line 54)"); return null; }
        }
    }
}
