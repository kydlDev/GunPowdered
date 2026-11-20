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
            if (!featureOn) return false;
            if (string.IsNullOrEmpty(requiredKey)) return true;
            return hasKey != null && hasKey(requiredKey);
        }
    }
}