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

        /// <summary>How many cartridges one reload moves from the backpack into the musket: as many as fit and as many as the player carries. Never below 0.</summary>
        internal static int CartridgesToLoad(int loaded, int carried)
        {
            if (RuleLog.On) RuleLog.Verbose($"MusketRules.CartridgesToLoad(loaded={loaded}, carried={carried})");
            int free = Capacity - ClampLoaded(loaded);
            if (free <= 0 || carried <= 0) { if (RuleLog.On) RuleLog.Verbose("MusketRules.CartridgesToLoad: returns 0 (line 26)"); return 0; }
            { var returnedAt27 = carried < free ? carried : free; if (RuleLog.On) RuleLog.Verbose($"MusketRules.CartridgesToLoad: returns {returnedAt27} (line 27)"); return returnedAt27; }
        }

        /// <summary>Why a reload is refused (null = allowed): the musket is full, or the player carries no cartridge.</summary>
        internal static string WhyNotReload(int loaded, int carried)
        {
            if (RuleLog.On) RuleLog.Every("MusketRules.WhyNotReload", 2d, $"MusketRules.WhyNotReload(loaded={loaded}, carried={carried})");
            if (ClampLoaded(loaded) >= Capacity) { var returnedAt34 = Full; if (RuleLog.On) RuleLog.Every("MusketRules.WhyNotReload:34", 2d, $"MusketRules.WhyNotReload: returns {returnedAt34} (line 34)"); return returnedAt34; }
            { var returnedAt35 = carried >= 1 ? null : NoCartridges; if (RuleLog.On) RuleLog.Every("MusketRules.WhyNotReload:35", 2d, $"MusketRules.WhyNotReload: returns {returnedAt35} (line 35)"); return returnedAt35; }
        }

        /// <summary>Why a shot is refused (null = allowed): nothing is loaded.</summary>
        internal static string WhyNotFire(int loaded)
        {
            if (RuleLog.On) RuleLog.Every("MusketRules.WhyNotFire", 2d, $"MusketRules.WhyNotFire(loaded={loaded})");
            { var returnedAt42 = ClampLoaded(loaded) >= 1 ? null : Empty; if (RuleLog.On) RuleLog.Every("MusketRules.WhyNotFire:42", 2d, $"MusketRules.WhyNotFire: returns {returnedAt42} (line 42)"); return returnedAt42; }
        }

        /// <summary>The load after one shot: one cartridge less, never below 0.</summary>
        internal static int AfterShot(int loaded)
        {
            if (RuleLog.On) RuleLog.Every("MusketRules.AfterShot", 2d, $"MusketRules.AfterShot(loaded={loaded})");
            int l = ClampLoaded(loaded);
            { var returnedAt50 = l > 0 ? l - 1 : 0; if (RuleLog.On) RuleLog.Every("MusketRules.AfterShot:50", 2d, $"MusketRules.AfterShot: returns {returnedAt50} (line 50)"); return returnedAt50; }
        }

        /// <summary>The load after a reload from <paramref name="carried"/> cartridges: it grows by <see cref="CartridgesToLoad"/> and never passes <see cref="Capacity"/>.</summary>
        internal static int AfterReload(int loaded, int carried)
        {
            if (RuleLog.On) RuleLog.Verbose($"MusketRules.AfterReload(loaded={loaded}, carried={carried})");
            { var returnedAt57 = ClampLoaded(loaded) + CartridgesToLoad(loaded, carried); if (RuleLog.On) RuleLog.Verbose($"MusketRules.AfterReload: returns {returnedAt57} (line 57)"); return returnedAt57; }
        }

        /// <summary>The shortest time between two shots (s): holding the attack button fires again only after it (D-095).</summary>
        internal const float ShotIntervalSeconds = 0.8f;

        /// <summary>One cartridge goes in per ramrod stroke (D-087: "one cartridge per 1.2 s").</summary>
        internal const float ReloadSecondsPerCartridge = 1.2f;

        /// <summary>The bullet (D-087 proposal, D-095): pierce damage, muzzle speed (m/s), spread (degrees each way), gravity (m/s2), life (s).</summary>
        internal const float BulletFirePerSecond = 5f;          // D-107: burns 5/s for 5 s
        internal const float BulletPierce = 50f, BulletSpeed = 110f, BulletSpreadDegrees = 0.6f, BulletGravity = 4f, BulletSeconds = 3f;

        /// <summary>How far the recoil shoves the shooter back (m; D-087 "recoil pushback", proposal).</summary>
        internal const float RecoilShoveMetres = 0.25f;

        /// <summary>
        /// The push force to hand Character.ApplyPushback so the shooter slides back about <paramref name="metres"/>. The game (1.0.21) turns a push
        /// force F into a push of F x clamp01(1 + equipment movement modifier) / mass x 2.5, which drives the body at up to 20 m/s while it fades at
        /// 100 per second: the slide is about push / 5 metres. Expected outcome: 0 for no shove, a heavy body or a modifier of -1 or less.
        /// </summary>
        internal static float RecoilPushForce(float metres, float mass, float equipmentModifier)
        {
            if (RuleLog.On) RuleLog.Every("MusketRules.RecoilPushForce", 2d, $"MusketRules.RecoilPushForce(metres={metres}, mass={mass}, equipmentModifier={equipmentModifier})");
            float factor = 1f + equipmentModifier;
            if (factor > 1f) factor = 1f;
            if (metres <= 0f || mass <= 0f || factor <= 0f) { if (RuleLog.On) RuleLog.Every("MusketRules.RecoilPushForce:83", 2d, "MusketRules.RecoilPushForce: returns 0f (line 83)"); return 0f; }
            { var returnedAt84 = 5f * metres * mass / (2.5f * factor); if (RuleLog.On) RuleLog.Every("MusketRules.RecoilPushForce:84", 2d, $"MusketRules.RecoilPushForce: returns {returnedAt84} (line 84)"); return returnedAt84; }
        }

        /// <summary>Whether enough time has passed since the last shot (<paramref name="lastShot"/>, s) to fire again at <paramref name="now"/>.</summary>
        internal static bool ShotIntervalOver(float now, float lastShot)
        {
            if (RuleLog.On) RuleLog.Every("MusketRules.ShotIntervalOver", 2d, $"MusketRules.ShotIntervalOver(now={now}, lastShot={lastShot})");
            { var returnedAt91 = now - lastShot >= ShotIntervalSeconds; if (RuleLog.On) RuleLog.Every("MusketRules.ShotIntervalOver:91", 2d, $"MusketRules.ShotIntervalOver: returns {returnedAt91} (line 91)"); return returnedAt91; }
        }

        /// <summary>How many cartridges a reload that began at <paramref name="started"/> should have moved in by <paramref name="now"/>: one at the end of each 1.2 s stroke.</summary>
        internal static int CartridgesDue(float started, float now)
        {
            if (RuleLog.On) RuleLog.Every("MusketRules.CartridgesDue", 2d, $"MusketRules.CartridgesDue(started={started}, now={now})");
            if (now <= started) { if (RuleLog.On) RuleLog.Every("MusketRules.CartridgesDue:98", 2d, "MusketRules.CartridgesDue: returns 0 (line 98)"); return 0; }
            { var returnedAt99 = (int)Math.Floor((now - started) / ReloadSecondsPerCartridge + 1e-4); if (RuleLog.On) RuleLog.Every("MusketRules.CartridgesDue:99", 2d, $"MusketRules.CartridgesDue: returns {returnedAt99} (line 99)"); return returnedAt99; }
        }

        /// <summary>Reads a stored load ("0".."5"); anything unreadable counts as empty, anything outside 0..Capacity as the nearest valid load.</summary>
        internal static int ParseLoaded(string stored)
        {
            if (RuleLog.On) RuleLog.Every("MusketRules.ParseLoaded", 2d, $"MusketRules.ParseLoaded(stored={stored})");
            int v;
            { var returnedAt107 = int.TryParse(stored, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out v) ? ClampLoaded(v) : 0; if (RuleLog.On) RuleLog.Every("MusketRules.ParseLoaded:107", 2d, $"MusketRules.ParseLoaded: returns {returnedAt107} (line 107)"); return returnedAt107; }
        }

        /// <summary>The load counter next to the crosshair: "3/5" (user 2026-11-19 12:32).</summary>
        internal static string LoadText(int loaded)
        {
            if (RuleLog.On) RuleLog.Every("MusketRules.LoadText", 2d, $"MusketRules.LoadText(loaded={loaded})");
            { var returnedAt114 = ClampLoaded(loaded).ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + Capacity.ToString(System.Globalization.CultureInfo.InvariantCulture); if (RuleLog.On) RuleLog.Every("MusketRules.LoadText:114", 2d, $"MusketRules.LoadText: returns {returnedAt114} (line 114)"); return returnedAt114; }
        }

        /// <summary>A stored load outside 0..Capacity (a damaged save, a changed constant) counts as the nearest valid load.</summary>
        internal static int ClampLoaded(int loaded)
        {
            if (RuleLog.On) RuleLog.Every("MusketRules.ClampLoaded", 2d, $"MusketRules.ClampLoaded(loaded={loaded})");
            if (loaded < 0) { if (RuleLog.On) RuleLog.Every("MusketRules.ClampLoaded:121", 2d, "MusketRules.ClampLoaded: returns 0 (line 121)"); return 0; }
            { var returnedAt122 = loaded > Capacity ? Capacity : loaded; if (RuleLog.On) RuleLog.Every("MusketRules.ClampLoaded:122", 2d, $"MusketRules.ClampLoaded: returns {returnedAt122} (line 122)"); return returnedAt122; }
        }
    }
}
