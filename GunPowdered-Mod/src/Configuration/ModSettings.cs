using UnityEngine;
using BepInEx.Configuration;
using KydlCore.Common;
using GunPowdered.Core;

namespace GunPowdered.Configuration
{
    /// <summary>
    /// All settings in one place (user rule, every mod):
    ///  * PLAYER CONFIG (BepInEx entries below, bound once from Plugin.Awake): only what a player should change. Clear descriptions, ranges,
    ///    drop-downs (enums). Descriptions never mention older versions, old names or old values.
    ///  * DEV-ONLY values (Tunable fields below): fixed values chosen by the user, NOT in the player's file. The user's KydlTools mod (F7) finds
    ///    every static Tunable here by itself and lets the user change it in game - nothing to register.
    /// When the user settles a value in game, it becomes the new fixed value / default here.
    /// </summary>
    internal static partial class ModSettings
    {
        // [General]
        public static ConfigEntry<bool> EnableMod;
        // One functionality = one [General] switch "Enable <Category>" (PROJECT_RULES 11): on only when EnableMod AND its own switch are on
        public static ConfigEntry<bool> EnableGuano, EnableWater, EnableSaltpeter, EnableSulfur, EnableGunpowder, EnableGrenades, EnableCannon, EnableMusket, EnableExplosiveArrows, EnableDynamite;   // EnableCannon = "Enable Cannons": both cannon types (user 2026-11-19 12:00)
        // [Musket] the reload key (user 2026-11-19, D-100)
        public static ConfigEntry<KeyCode> MusketReloadKey;
        /// <summary>D-155: [Musket] Aim Key (default the mouse wheel button): raises the musket from the side.</summary>
        public static ConfigEntry<KeyCode> MusketAimKey;
        // no MenuKey: the mod's menu is a set of tabs in the KY hub (F6, KydlCore), see src/Presentation/Hub/Registration/HubTabs.cs
        // One functionality = one [General] switch "Enable <Category>" (PROJECT_RULES 11), only when the mod has several of them, e.g.:
        //   public static ConfigEntry<bool> ColoursEnable;   // bound in General as "Enable Colours" with the value of the old Colours Enable entry;  on only when On && ColoursEnable.Value
        // [DebugLog]
        public static ConfigEntry<bool> DebugLog;

        /// Is the mod switched on ([General] Enable Mod)? Off = inert: no effects; nothing built is removed.
        public static bool On { get { return EnableMod == null || EnableMod.Value; } }

        /// The gate of one feature (IFeature.IsEnabled): Enable Mod AND its own [General] Enable <Feature> (null = a mod with one feature: Enable Mod only).
        /// Read at call time, never cached, so a config change applies at once.
        public static bool FeatureOn(ConfigEntry<bool> ownEnable) { return On && (ownEnable == null || ownEnable.Value); }

        // --- dev-only fixed values (Tunables): not in the config file, KydlTools changes them for testing -----------------------------------
        public static readonly Tunable<float> ExampleSpeed = new Tunable<float>("Example", "ExampleSpeed", 1f, 0f, 10f,
            "Example dev-only value (0-10, with a range as PROJECT_RULES 11 asks): replace or remove. Read it with ModSettings.ExampleSpeed.Value.");

    /// The value the player had in an entry that moved (still in the file as an orphan before DropRemoved deletes it): the first of the
    /// given (section, key, section, key, ...) pairs that holds a bool; true when there is none.
    private static bool Old(ConfigFile c, params string[] sectionAndKey)
    {
        if (RuleLog.IsOn()) ModLog.Verbose($"ModSettings.Old(c={RuleLog.Shown(c)})");
        // OrphanedEntries is not a public member of every BepInEx 5 build (ConfigTools.DropRemoved reaches it the same way)
        System.Reflection.PropertyInfo orphans = typeof(ConfigFile).GetProperty("OrphanedEntries", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        var dict = orphans != null ? orphans.GetValue(c, null) as System.Collections.IDictionary : null;
        for (int i = 0; dict != null && i + 1 < sectionAndKey.Length; i += 2)
        {
            var def = new ConfigDefinition(sectionAndKey[i], sectionAndKey[i + 1]);
            bool b;
            if (dict.Contains(def) && bool.TryParse((dict[def] as string ?? "").Trim(), out b))
            {
                { var returnedAt55 = b; if (RuleLog.IsOn()) ModLog.Verbose($"ModSettings.Old: returns {RuleLog.Shown(returnedAt55)} (line 55)"); return returnedAt55; }
            }
        }

        { if (RuleLog.IsOn()) ModLog.Verbose("ModSettings.Old: returns true (line 59)"); return true; }
    }

        /// Expected outcome: every player entry is bound (returns that check).
        public static bool Bind(ConfigFile c)
        {
            var savedLines = ConfigTools.Capture(c);     // before the first Bind: every Bind saves the file with what BepInEx parsed

            EnableMod = c.Bind("General", "Enable Mod", Old(c, "General", "Enable mod", "General", "EnableAll"),
                ConfigOrder.First("Switches the whole mod on / off while playing. Off: no effects from this mod; things already built stay in the world."));
            EnableGuano = c.Bind("General", "Enable Guano", true,
                "Bat Guano deposits in caves and swamps. Off: no new deposits; guano already collected stays in your inventory.");
            EnableWater = c.Bind("General", "Enable Water", true,
                "The Bucket, Water and the Rain Barrel. Off: buckets cannot be filled and barrels stop collecting rain; what you already carry stays.");
            EnableSaltpeter = c.Bind("General", "Enable Saltpeter", true,
                "The Leaching Vat that turns Bat Guano and Water into Saltpeter. Off: vats do not work; Saltpeter you already have stays.");
            EnableSulfur = c.Bind("General", "Enable Sulfur", true,
                "Sulfurous Mud deposits in the swamp and the extra Sulfur from Blobs and Oozers. Off: no new deposits and no extra drops.");
            EnableGunpowder = c.Bind("General", "Enable Gunpowder", true,
                "The Powder Mill and Gunpowder. Off: the mill does nothing; Gunpowder you already have stays.");
            EnableGrenades = c.Bind("General", "Enable Grenades", true,
                "The Spiked Grenade and its throwing arc. Off: grenades cannot be crafted and no arc is drawn; grenades you already have stay.");
            EnableCannon = c.Bind("General", "Enable Cannons", Old(c, "General", "Enable Cannon"),
                "Both cannon types: the Hand Cannon with its wooden Cannon Stock and the Cannon Ball, and the Mobile Cannon with the Asksvin Harness. Off: none of them can be crafted or built and mounted cannons do not fire; what you already own or built stays.");
            EnableMusket = c.Bind("General", "Enable Musket", true,
                "The Iron Musket. Off: muskets cannot be crafted; muskets you already own stay.");
            EnableExplosiveArrows = c.Bind("General", "Enable Explosive Arrows", true,
                "The Explosive Arrow. Off: explosive arrows cannot be crafted; arrows you already own stay.");
            EnableDynamite = c.Bind("General", "Enable Dynamite", true,
                "Dynamite. Off: dynamite cannot be crafted; dynamite you already own stays.");
            MusketReloadKey = c.Bind("Musket", "Reload Key", KeyCode.R,
                "The key that reloads the Iron Musket in your hands: one Cartridge from your backpack goes in every 1.2 s until it holds 5. Pick any key from the list.");
            MusketAimKey = c.Bind("Musket", "Aim Key", KeyCode.Mouse2,
                "The key that raises the Iron Musket to the shoulder after running (running lowers it to your side); it stays raised until you run again, and it only fires when raised. Default: the mouse wheel button. Pick any key from the list.");
            DebugLog = c.Bind("DebugLog", "Enable", false,
                "Writes detailed lines about everything the mod does to the BepInEx log (for bug reports). Leave off while playing normally.");

            ConfigTools.ApplyCommaFix(c, savedLines);    // after every Bind: "3,5" typed by hand means 3.5, not 35
            ConfigTools.DropRemoved(c);

            EnableMod.SettingChanged += (s, e) => ModLog.Info("Config: Enable Mod = " + EnableMod.Value + (EnableMod.Value ? ", the mod is on" : ", the mod is inert (no effects)"));
            ModLog.Verbose("Config: bound (Enable Mod " + EnableMod.Value + ", debug log " + DebugLog.Value + ")");
            return EnableMod != null && EnableGuano != null && EnableWater != null && EnableSaltpeter != null && EnableSulfur != null && EnableGunpowder != null && EnableGrenades != null && EnableCannon != null
                && EnableMusket != null && EnableExplosiveArrows != null && EnableDynamite != null && MusketReloadKey != null && MusketAimKey != null && DebugLog != null;
        }
    }
}
