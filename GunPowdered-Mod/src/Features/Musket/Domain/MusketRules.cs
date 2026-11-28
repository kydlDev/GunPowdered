using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Musket.Domain
{
    /// <summary>
    /// The Iron Musket's magazine and firing rules (D-080, D-081, user 2026-11-19), pure so the unit tests check them. The player loads PAPER CARTRIDGES (never loose
    /// bullets), at most <see cref="Capacity"/> at a time. The player may reload whenever fewer than <see cref="Capacity"/> are loaded; the load never goes above it.
    /// A shot uses one loaded cartridge: only the bullet leaves the barrel, the paper and powder burn up in the pan.
    /// </summary>
    internal static class MusketRules
    {
        /// <summary>The most cartridges the musket holds (user: "the player loads 5 cartridges at any time ... they never go above 5").</summary>
        internal const int Capacity = 5;

        /// <summary>The key hints while the musket is in hand.</summary>
        internal const string HintFire = "Fire", HintReload = "Reload", HintBlock = "Block";

        internal const string Full = "The Iron Musket is fully loaded", NoCartridges = "You need Cartridges", Empty = "The Iron Musket is empty: reload it";

        /// <summary>D-155: the aim key hint, and what a press of fire says while the musket hangs at the side or is still coming up.</summary>
        internal const string HintAim = "Both hands", NotAimed = "Raise the Iron Musket first";     // D-182: the key only brings the musket into both hands

        /// <summary>D-155: why a reload is refused or stopped while moving (running is allowed).</summary>
        internal const string ReloadSwimming = "You cannot reload while swimming", ReloadInAir = "You cannot reload while jumping";

        /// <summary>
        /// D-155 (user 2026-11-22): the musket blocks like a light weapon. Block armour 10 at Blocking skill 0 (the game raises it to x1.5 = 15 at skill 100, so
        /// no step per quality), parry bonus 1.5, parry adrenaline 10, block adrenaline 2. No movement speed reduction while it is held.
        /// </summary>
        internal const float BlockPower = 10f, BlockPowerPerLevel = 0f, ParryBonus = 1.5f, ParryAdrenaline = 10f, BlockAdrenaline = 2f, MovementModifier = 0f;

        /// <summary>D-198: the game button name that means "no gamepad button".</summary>
        internal const string NoPadButton = "None";

        /// <summary>D-198: the game binding a gamepad musket action reads ("Use" -> "JoyUse", a "Joy..." name is kept); null for "None" / empty = no gamepad button.</summary>
        internal static string PadBinding(string button)
        {
            string name = (button ?? "").Trim();
            string binding = name.Length == 0 || string.Equals(name, NoPadButton, StringComparison.OrdinalIgnoreCase) ? null
                : name.StartsWith("Joy", StringComparison.Ordinal) ? name : "Joy" + name;
            if (RuleLog.Due("MusketRules.PadBinding", 2d)) RuleLog.Every("MusketRules.PadBinding", 2d, $"MusketRules.PadBinding(button={RuleLog.Shown(button)}): returns {RuleLog.Shown(binding)}");
            return binding;
        }

        /// <summary>
        /// D-198 (user 2026-11-28): may the gamepad reload button reload now? The game's Use button also opens doors and chests, so on Use the musket
        /// reloads only while the player hovers nothing usable; any other button always reloads.
        /// </summary>
        internal static bool PadReloadAllowed(string button, bool hovering)
        {
            bool allowed = !hovering || !string.Equals((button ?? "").Trim(), "Use", StringComparison.OrdinalIgnoreCase);
            RuleLog.Verbose($"MusketRules.PadReloadAllowed(button={RuleLog.Shown(button)}, hovering={hovering}): returns {allowed}");
            return allowed;
        }

        /// <summary>D-155: why a reload cannot run now because of how the player moves (null = allowed): swimming or in the air (jumping, falling). Running is fine.</summary>
        internal static string WhyNotReloadMoving(bool onGround, bool swimming)
        {
            string why = swimming ? ReloadSwimming : (!onGround ? ReloadInAir : null);
            if (RuleLog.Due("MusketRules.WhyNotReloadMoving", 2d)) RuleLog.Every("MusketRules.WhyNotReloadMoving", 2d, $"MusketRules.WhyNotReloadMoving(onGround={onGround}, swimming={swimming}): returns {RuleLog.Shown(why)}");
            return why;
        }

        /// <summary>
        /// D-155 (user 2026-11-22: "only torches and shields should be able to go into the offhand"): which item types (the game's ItemType name) the left hand
        /// may hold next to the lowered musket. Anything else is equipped the game's own way (a club puts the musket away).
        /// </summary>
        internal static bool FitsBesideLoweredMusket(string itemType)
        {
            bool fits = itemType == "Torch" || itemType == "Shield";
            if (RuleLog.Due("MusketRules.FitsBesideLoweredMusket", 2d)) RuleLog.Every("MusketRules.FitsBesideLoweredMusket", 2d, $"MusketRules.FitsBesideLoweredMusket({itemType}): returns {fits}");
            return fits;
        }

        /// <summary>D-155: the message when fire is pressed before the musket is raised, naming the aim key.</summary>
        internal static string AimFirst(string key)
        {
            string s = string.IsNullOrEmpty(key) ? NotAimed : NotAimed + " (" + key + ")";
            if (RuleLog.Due("MusketRules.AimFirst", 2d)) RuleLog.Every("MusketRules.AimFirst", 2d, $"MusketRules.AimFirst(key={key}): returns {s}");
            return s;
        }

        /// <summary>How many cartridges one reload moves from the backpack into the musket: as many as fit and as many as the player carries. Never below 0.</summary>
        internal static int CartridgesToLoad(int loaded, int carried)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"MusketRules.CartridgesToLoad(loaded={RuleLog.Shown(loaded)}, carried={RuleLog.Shown(carried)})");
            int free = Capacity - ClampLoaded(loaded);
            if (free <= 0 || carried <= 0) { if (RuleLog.IsOn()) RuleLog.Verbose($"MusketRules.CartridgesToLoad: returns 0 because free <= 0 || carried <= 0 (line 65)"); return 0; }
            { var returnedAt66 = carried < free ? carried : free; if (RuleLog.IsOn()) RuleLog.Verbose($"MusketRules.CartridgesToLoad: returns {returnedAt66} (line 66)"); return returnedAt66; }
        }

        /// <summary>Why a reload is refused (null = allowed): the musket is full, or the player carries no cartridge.</summary>
        /// <summary>
        /// D-182 (user 2026-11-24: "make musket auto-reload when reaching 0"): after a shot that left the musket empty, the reload starts by itself when
        /// the player carries a cartridge. Without cartridges nothing happens (no "no cartridges" message after every last shot).
        /// </summary>
        internal static bool AutoReloads(int leftAfterShot, int carried)
        {
            bool auto = leftAfterShot <= 0 && carried >= 1;
            if (RuleLog.Due("MusketRules.AutoReloads", 2d)) RuleLog.Every("MusketRules.AutoReloads", 2d, $"MusketRules.AutoReloads(leftAfterShot={leftAfterShot}, carried={carried}): returns {auto}");
            return auto;
        }

        /// <summary>D-182: seconds after the shot before the automatic reload starts (the fire pose ends first).</summary>
        internal const float AutoReloadDelay = MusketPoseRules.FireSeconds + 0.15f;

        internal static string WhyNotReload(int loaded, int carried)
        {
            if (RuleLog.Due("MusketRules.WhyNotReload", 2d)) RuleLog.Every("MusketRules.WhyNotReload", 2d, $"MusketRules.WhyNotReload(loaded={RuleLog.Shown(loaded)}, carried={RuleLog.Shown(carried)})");
            if (ClampLoaded(loaded) >= Capacity) return Full;
            { var returnedAt74 = carried >= 1 ? null : NoCartridges; if (RuleLog.Due("MusketRules.WhyNotReload:74", 2d)) RuleLog.Every("MusketRules.WhyNotReload:74", 2d, $"MusketRules.WhyNotReload: returns {returnedAt74} (line 74)"); return returnedAt74; }
        }

        /// <summary>Why a shot is refused (null = allowed): nothing is loaded.</summary>
        internal static string WhyNotFire(int loaded)
        {
            if (RuleLog.Due("MusketRules.WhyNotFire", 2d)) RuleLog.Every("MusketRules.WhyNotFire", 2d, $"MusketRules.WhyNotFire(loaded={RuleLog.Shown(loaded)})");
            return ClampLoaded(loaded) >= 1 ? null : Empty;
        }

        /// <summary>The load after one shot: one cartridge less, never below 0.</summary>
        internal static int AfterShot(int loaded)
        {
            if (RuleLog.Due("MusketRules.AfterShot", 2d)) RuleLog.Every("MusketRules.AfterShot", 2d, $"MusketRules.AfterShot(loaded={RuleLog.Shown(loaded)})");
            int l = ClampLoaded(loaded);
            { var returnedAt89 = l > 0 ? l - 1 : 0; if (RuleLog.Due("MusketRules.AfterShot:89", 2d)) RuleLog.Every("MusketRules.AfterShot:89", 2d, $"MusketRules.AfterShot: returns {returnedAt89} (line 89)"); return returnedAt89; }
        }

        /// <summary>The load after a reload from <paramref name="carried"/> cartridges: it grows by <see cref="CartridgesToLoad"/> and never passes <see cref="Capacity"/>.</summary>
        internal static int AfterReload(int loaded, int carried)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"MusketRules.AfterReload(loaded={RuleLog.Shown(loaded)}, carried={RuleLog.Shown(carried)})");
            return ClampLoaded(loaded) + CartridgesToLoad(loaded, carried);
        }

        /// <summary>The shortest time between two shots (s): holding the attack button fires again only after it (D-095).</summary>
        internal const float ShotIntervalSeconds = 0.8f;

        /// <summary>One cartridge goes in per ramrod stroke (D-087: "one cartridge per 1.2 s").</summary>
        internal const float ReloadSecondsPerCartridge = 1.2f;

        /// <summary>The bullet (D-087 proposal, D-095): pierce damage, muzzle speed (m/s), spread (degrees each way), gravity (m/s2), life (s).</summary>
        // D-145: the damage is the musket's part (whatever it loads) plus the cartridge's part; BulletPierce is the shot's sum with the Iron Cartridge.
        internal const float BulletFirePerSecond = 5f;          // D-107 / D-145: the musket's part, burns 5/s for 5 s
        internal const float MusketPierce = 20f, IronCartridgePierce = 30f;
        internal const float BulletPierce = MusketPierce + IronCartridgePierce, BulletSpeed = 110f, BulletSpreadDegrees = 0.6f, BulletGravity = 4f, BulletSeconds = 3f;

        /// <summary>How far the recoil shoves the shooter back (m; D-087 "recoil pushback", proposal).</summary>
        internal const float RecoilShoveMetres = 0.25f;

        /// <summary>
        /// The push force to hand Character.ApplyPushback so the shooter slides back about <paramref name="metres"/>. The game (1.0.21) turns a push
        /// force F into a push of F x clamp01(1 + equipment movement modifier) / mass x 2.5, which drives the body at up to 20 m/s while it fades at
        /// 100 per second: the slide is about push / 5 metres. Expected outcome: 0 for no shove, a heavy body or a modifier of -1 or less.
        /// </summary>
        internal static float RecoilPushForce(float metres, float mass, float equipmentModifier)
        {
            if (RuleLog.Due("MusketRules.RecoilPushForce", 2d)) RuleLog.Every("MusketRules.RecoilPushForce", 2d, $"MusketRules.RecoilPushForce(metres={RuleLog.Shown(metres)}, mass={RuleLog.Shown(mass)}, equipmentModifier={RuleLog.Shown(equipmentModifier)})");
            float factor = 1f + equipmentModifier;
            if (factor > 1f) factor = 1f;
            if (metres <= 0f || mass <= 0f || factor <= 0f) { if (RuleLog.Due("MusketRules.RecoilPushForce:124", 2d)) RuleLog.Every("MusketRules.RecoilPushForce:124", 2d, $"MusketRules.RecoilPushForce: returns 0f because metres <= 0f || mass <= 0f || factor <= 0f (line 124)"); return 0f; }
            { var returnedAt125 = 5f * metres * mass / (2.5f * factor); if (RuleLog.Due("MusketRules.RecoilPushForce:125", 2d)) RuleLog.Every("MusketRules.RecoilPushForce:125", 2d, $"MusketRules.RecoilPushForce: returns {returnedAt125} (line 125)"); return returnedAt125; }
        }

        /// <summary>Whether enough time has passed since the last shot (<paramref name="lastShot"/>, s) to fire again at <paramref name="now"/>.</summary>
        internal static bool ShotIntervalOver(float now, float lastShot)
        {
            if (RuleLog.Due("MusketRules.ShotIntervalOver", 2d)) RuleLog.Every("MusketRules.ShotIntervalOver", 2d, $"MusketRules.ShotIntervalOver(now={RuleLog.Shown(now)}, lastShot={RuleLog.Shown(lastShot)})");
            return now - lastShot >= ShotIntervalSeconds;
        }

        /// <summary>How many cartridges a reload that began at <paramref name="started"/> should have moved in by <paramref name="now"/>: one at the end of each 1.2 s stroke.</summary>
        internal static int CartridgesDue(float started, float now, float strokeSeconds = ReloadSecondsPerCartridge)
        {
            if (RuleLog.Due("MusketRules.CartridgesDue", 2d)) RuleLog.Every("MusketRules.CartridgesDue", 2d, $"MusketRules.CartridgesDue(started={RuleLog.Shown(started)}, now={RuleLog.Shown(now)})");
            if (now <= started) return 0;
            { var returnedAt140 = (int)Math.Floor((now - started) / (strokeSeconds > 0.05f ? strokeSeconds : ReloadSecondsPerCartridge) + 1e-4); if (RuleLog.Due("MusketRules.CartridgesDue:140", 2d)) RuleLog.Every("MusketRules.CartridgesDue:140", 2d, $"MusketRules.CartridgesDue: returns {returnedAt140} (line 140)"); return returnedAt140; }
        }

        /// <summary>Reads a stored load ("0".."5"); anything unreadable counts as empty, anything outside 0..Capacity as the nearest valid load.</summary>
        internal static int ParseLoaded(string stored)
        {
            if (RuleLog.Due("MusketRules.ParseLoaded", 2d)) RuleLog.Every("MusketRules.ParseLoaded", 2d, $"MusketRules.ParseLoaded(stored={RuleLog.Shown(stored)})");
            int v;
            { var returnedAt148 = int.TryParse(stored, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out v) ? ClampLoaded(v) : 0; if (RuleLog.Due("MusketRules.ParseLoaded:148", 2d)) RuleLog.Every("MusketRules.ParseLoaded:148", 2d, $"MusketRules.ParseLoaded: returns {returnedAt148} (line 148)"); return returnedAt148; }
        }

        /// <summary>The load counter next to the crosshair: "3/5" (user 2026-11-19 12:32).</summary>
        internal static string LoadText(int loaded)
        {
            if (RuleLog.Due("MusketRules.LoadText", 2d)) RuleLog.Every("MusketRules.LoadText", 2d, $"MusketRules.LoadText(loaded={RuleLog.Shown(loaded)})");
            return ClampLoaded(loaded).ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + Capacity.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>A stored load outside 0..Capacity (a damaged save, a changed constant) counts as the nearest valid load.</summary>
        internal static int ClampLoaded(int loaded)
        {
            if (RuleLog.Due("MusketRules.ClampLoaded", 2d)) RuleLog.Every("MusketRules.ClampLoaded", 2d, $"MusketRules.ClampLoaded(loaded={RuleLog.Shown(loaded)})");
            if (loaded < 0) return 0;
            { var returnedAt163 = loaded > Capacity ? Capacity : loaded; if (RuleLog.Due("MusketRules.ClampLoaded:163", 2d)) RuleLog.Every("MusketRules.ClampLoaded:163", 2d, $"MusketRules.ClampLoaded: returns {returnedAt163} (line 163)"); return returnedAt163; }
        }
    }
}
