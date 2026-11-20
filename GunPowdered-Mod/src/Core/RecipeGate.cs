using System;

namespace GunPowdered.Core
{
    /// <summary>
    /// Whether a recipe of the mod is on: its feature switch, and the world key it waits for (D-108: the Deep North Dynamite Pack after Fader). Pure, so the
    /// unit tests check it; ItemRegistry puts the answer on the recipe before the game lists or discovers recipes (RecipeGatePatch).
    /// </summary>
    public static class RecipeGate
    {
        /// <summary>True when the feature is on and the key is either not needed or set. A missing key check (null) counts as "not set".</summary>
        public static bool IsOn(bool featureOn, string requiredKey, Func<string, bool> hasKey)
        {
            if (RuleLog.On) RuleLog.Every("RecipeGate.IsOn", 2d, $"RecipeGate.IsOn(featureOn={RuleLog.Shown(featureOn)}, requiredKey={RuleLog.Shown(requiredKey)}, hasKey={RuleLog.Shown(hasKey)})");
            if (!featureOn) { if (RuleLog.On) RuleLog.Every("RecipeGate.IsOn:15", 2d, "RecipeGate.IsOn: returns false (line 15)"); return false; }
            if (string.IsNullOrEmpty(requiredKey)) { if (RuleLog.On) RuleLog.Every("RecipeGate.IsOn:16", 2d, "RecipeGate.IsOn: returns true (line 16)"); return true; }
            { var returnedAt17 = hasKey != null && hasKey(requiredKey); if (RuleLog.On) RuleLog.Every("RecipeGate.IsOn:17", 2d, $"RecipeGate.IsOn: returns {RuleLog.Shown(returnedAt17)} (line 17)"); return returnedAt17; }
        }
    }
}