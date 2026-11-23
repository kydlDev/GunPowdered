using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The Iron Musket's shot and magazine (D-087, D-095). The load (0..5 paper cartridges) is stored on the musket item itself (custom data "gp_loaded"),
    /// so it stays with the item through saves, chests and trades. A shot (the attack button, see MusketAttackPatch) uses one loaded cartridge: the bullet
    /// flies from the muzzle along the player's aim as a projectile with the game's own flight and hit rules (friendly fire, blocking, skills), the
    /// muzzle blast plays and the arms kick back. Only the local player fires; the bullet and the effects reach the others through the network.
    /// </summary>
    internal static class MusketGun
    {
        internal const string LoadedKey = "gp_loaded";
        static readonly string[] MuzzleEffects = { "fx_turret_fire", "vfx_arbalest_fire", "sfx_arbalest_fire" };
        static float _lastShot = -99f;

        /// <summary>How many shots were fired since the start, and what the last one did (for test hooks).</summary>
        internal static int Shots { get; private set; }
        internal static string LastShot { get; private set; } = "no shot yet";
        internal static GameObject LastBullet { get; private set; }

        /// <summary>Where the last bullet left the muzzle, and the farthest it got from there (m) - kept after it hit something (test hooks).</summary>
        internal static Vector3 LastMuzzle { get; private set; }
        internal static float LastBulletReach { get; private set; }

        internal static void NoteReach(float metres)
        {
            if (RuleLog.Due("MusketGun.NoteReach", 2d)) ModLog.Every("MusketGun.NoteReach", 2d, $"MusketGun.NoteReach(metres={RuleLog.Shown(metres)})");
            if (metres > LastBulletReach) LastBulletReach = metres;
        }

        /// <summary>
        /// Raised on the shooter's client after every shot: shooter, musket, muzzle, direction, cartridges left, bullet. The public API passes it on
        /// (GunPowderedApi.Musket.ShotFired, D-097); a failing listener is logged and does not stop the shot.
        /// </summary>
        internal static event System.Action<Player, ItemDrop.ItemData, Vector3, Vector3, int, GameObject> Fired;

        /// <summary>True for the Iron Musket item.</summary>
        internal static bool IsMusket(ItemDrop.ItemData item) => item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == GunPowderedPrefabs.Musket;

        /// <summary>The musket the player has in hand, or null.</summary>
        internal static ItemDrop.ItemData Held(Player player)
        {
            if (RuleLog.Due("MusketGun.Held", 2d)) ModLog.Every("MusketGun.Held", 2d, $"MusketGun.Held(player={RuleLog.Shown(player)})");
            ItemDrop.ItemData weapon = player != null ? player.GetCurrentWeapon() : null;
            // D-155: with a torch next to the lowered musket the current weapon is the torch; the musket is still in the hand (the bow-type left slot)
            if (!IsMusket(weapon) && player != null && IsMusket(player.LeftItem)) weapon = player.LeftItem;
            { var returnedAt51 = IsMusket(weapon) ? weapon : null; if (RuleLog.Due("MusketGun.Held:51", 2d)) ModLog.Every("MusketGun.Held:51", 2d, $"MusketGun.Held: returns {returnedAt51} (line 51)"); return returnedAt51; }
        }

        /// <summary>How many cartridges this musket holds (0 when nothing is stored).</summary>
        internal static int Loaded(ItemDrop.ItemData musket)
        {
            if (RuleLog.Due("MusketGun.Loaded", 2d)) ModLog.Every("MusketGun.Loaded", 2d, $"MusketGun.Loaded(musket={RuleLog.Shown(musket)})");
            if (musket == null || musket.m_customData == null) return 0;
            string stored;
            { var returnedAt60 = musket.m_customData.TryGetValue(LoadedKey, out stored) ? MusketRules.ParseLoaded(stored) : 0; if (RuleLog.Due("MusketGun.Loaded:60", 2d)) ModLog.Every("MusketGun.Loaded:60", 2d, $"MusketGun.Loaded: returns {returnedAt60} (line 60)"); return returnedAt60; }
        }

        /// <summary>Stores a load on the musket (kept inside 0..5). Expected outcome: the load stored.</summary>
        internal static int SetLoaded(ItemDrop.ItemData musket, int loaded)
        {
            if (RuleLog.Due("MusketGun.SetLoaded", 2d)) ModLog.Every("MusketGun.SetLoaded", 2d, $"MusketGun.SetLoaded(musket={RuleLog.Shown(musket)}, loaded={RuleLog.Shown(loaded)})");
            if (musket == null) return 0;
            int v = MusketRules.ClampLoaded(loaded);
            if (musket.m_customData == null) musket.m_customData = new System.Collections.Generic.Dictionary<string, string>();
            musket.m_customData[LoadedKey] = v.ToString(System.Globalization.CultureInfo.InvariantCulture);
            { var returnedAt71 = v; if (RuleLog.Due("MusketGun.SetLoaded:71", 2d)) ModLog.Every("MusketGun.SetLoaded:71", 2d, $"MusketGun.SetLoaded: returns {returnedAt71} (line 71)"); return returnedAt71; }
        }

        /// <summary>
        /// The attack button with the musket in hand. Expected outcome: "fired, N left", or why not ("too soon", the empty message, no musket / bullet) - a
        /// refused shot tells the player in the middle of the screen. A running reload stops first (what is loaded stays).
        /// </summary>
        internal static string Fire(Player player)
        {
            if (RuleLog.Due("MusketGun.Fire", 2d)) ModLog.Every("MusketGun.Fire", 2d, $"MusketGun.Fire(player={RuleLog.Shown(player)})");
            LastAttempt = FireCore(player);
            { var returnedAt82 = LastAttempt; if (RuleLog.Due("MusketGun.Fire:82", 2d)) ModLog.Every("MusketGun.Fire:82", 2d, $"MusketGun.Fire: returns {returnedAt82} (line 82)"); return returnedAt82; }
        }

        /// <summary>What the last press of the attack button with the musket did (for test hooks).</summary>
        internal static string LastAttempt { get; private set; } = "not pressed yet";

        static string FireCore(Player player)
        {
            ItemDrop.ItemData musket = Held(player);
            if (musket == null) { if (RuleLog.Due("MusketGun.FireCore:89", 2d)) ModLog.Every("MusketGun.FireCore:91", 2d, "MusketGun.FireCore: returns \"no musket in hand\" (line 91)"); return "no musket in hand"; }
            MusketRig aimRig = player.GetComponent<MusketRig>();
            if (aimRig != null && !aimRig.ReadyToFire)
            {
                // D-155: the musket fires only when raised (the aim key) and the raise has finished
                string notAimed = MusketRules.AimFirst(MusketInput.AimKeyText);
                player.Message(MessageHud.MessageType.Center, notAimed);
                ModLog.Verbose("MusketGun: fire refused - the musket is not raised (" + (aimRig.Raised ? "still coming up" : "at the side") + ")");
                return notAimed;
            }
            MusketReloader.Stop(player, "a shot");
            if (!MusketRules.ShotIntervalOver(Time.time, _lastShot)) { if (RuleLog.Due("MusketGun.FireCore:91", 2d)) ModLog.Every("MusketGun.FireCore:102", 2d, "MusketGun.FireCore: returns \"too soon\" (line 102)"); return "too soon"; }
            int loaded = Loaded(musket);
            string why = MusketRules.WhyNotFire(loaded);
            if (why != null)
            {
                player.Message(MessageHud.MessageType.Center, why);
                { var returnedAt108 = why; if (RuleLog.Due("MusketGun.FireCore:108", 2d)) ModLog.Every("MusketGun.FireCore:108", 2d, $"MusketGun.FireCore: returns {returnedAt108} (line 108)"); return returnedAt108; }
            }
            GameObject prefab = MusketSetup.Bullet;
            if (prefab == null)
            {
                ModLog.Warn("MusketGun: no bullet prefab - the musket cannot fire");
                return "no bullet prefab";
            }
            MusketRig rig = player.GetComponent<MusketRig>();
            Vector3 muzzle;
            if (rig == null || !rig.TryMuzzle(out muzzle)) muzzle = player.GetEyePoint() + player.GetLookDir() * 0.8f;
            Vector3 aim = player.GetAimDir(muzzle);
            Vector3 dir = Spread(aim, MusketRules.BulletSpreadDegrees);

            GameObject bullet = Object.Instantiate(prefab, muzzle, Quaternion.LookRotation(dir));
            GunPowdered.Integrations.Valheim.GunNoise.Alert(muzzle, NoiseRules.MusketShotRadius, player, "musket shot");      // D-149: mobs hear it
            var flight = bullet.GetComponent<Projectile>();
            var hit = new HitData();
            hit.m_damage.m_pierce = MusketRules.BulletPierce;
            hit.m_damage.m_fire = GunPowdered.Core.BurnRules.Total(MusketRules.BulletFirePerSecond);
            hit.m_pushForce = musket.m_shared.m_attackForce;
            hit.m_skill = GunPowdered.Features.Skilling.Infrastructure.GunSkills.Muskets;      // D-157: a hit trains Muskets (the game raises it on a creature hit)
            hit.m_blockable = true;
            hit.m_dodgeable = true;
            if (flight != null) flight.Setup(player, dir * MusketRules.BulletSpeed, -1f, hit, musket, null);

            int left = SetLoaded(musket, MusketRules.AfterShot(loaded));
            _lastShot = Time.time;
            Shots++;
            LastBullet = bullet;
            LastMuzzle = muzzle;
            LastBulletReach = 0f;
            bullet.AddComponent<MusketBulletTrace>().From = muzzle;
            int fx = PlayMuzzle(muzzle, dir);
            if (rig != null) rig.Play(MusketAction.Fire);
            float push = Shove(player, dir);
            LastShot = "fired from " + muzzle.ToString("0.00") + " along " + dir.ToString("0.00") + ", flight " + (flight != null) + ", effects " + fx + ", shove " + push.ToString("0.0") + ", " + left + " left";
            ModLog.Info("MusketGun: " + player.GetPlayerName() + " " + LastShot);
            RaiseFired(player, musket, muzzle, dir, left, bullet);
            { var returnedAt147 = "fired, " + left + " left"; if (RuleLog.Due("MusketGun.FireCore:147", 2d)) ModLog.Every("MusketGun.FireCore:147", 2d, $"MusketGun.FireCore: returns {returnedAt147} (line 147)"); return returnedAt147; }
        }

        /// <summary>On the bullet this client fired: notes how far it got (MusketGun.LastBulletReach).</summary>
        internal sealed class MusketBulletTrace : MonoBehaviour
        {
            internal Vector3 From;

            void LateUpdate()
            {
                NoteReach(Vector3.Distance(transform.position, From));
                ModLog.Every("musket-bullet", 0.5f, "MusketGun: bullet " + LastBulletReach.ToString("0.0") + " m from the muzzle");
            }
        }

        // the recoil shoves the shooter back along the flat shot direction (D-087 "recoil pushback"), through the game's own pushback
        static float Shove(Player player, Vector3 dir)
        {
            if (RuleLog.Due("MusketBulletTrace.Shove", 2d)) ModLog.Every("MusketBulletTrace.Shove", 2d, $"MusketBulletTrace.Shove(player={RuleLog.Shown(player)}, dir={RuleLog.Shown(dir)})");
            Rigidbody body = player.GetComponent<Rigidbody>();
            float push = MusketRules.RecoilPushForce(MusketRules.RecoilShoveMetres, body != null ? body.mass : 0f, player.GetEquipmentMovementModifier());
            if (push > 0f) player.ApplyPushback(-dir, push);
            { var returnedAt169 = push; if (RuleLog.Due("MusketBulletTrace.Shove:169", 2d)) ModLog.Every("MusketBulletTrace.Shove:169", 2d, $"MusketBulletTrace.Shove: returns {returnedAt169} (line 169)"); return returnedAt169; }
        }

        static void RaiseFired(Player player, ItemDrop.ItemData musket, Vector3 muzzle, Vector3 dir, int left, GameObject bullet)
        {
            System.Action<Player, ItemDrop.ItemData, Vector3, Vector3, int, GameObject> listeners = Fired;
            if (listeners == null) { if (RuleLog.Due("MusketBulletTrace.RaiseFired:175", 2d)) ModLog.Every("MusketBulletTrace.RaiseFired:175", 2d, $"MusketBulletTrace.RaiseFired: returns because listeners == null (line 175)"); return; }
            foreach (System.Delegate d in listeners.GetInvocationList())
            {
                try { ((System.Action<Player, ItemDrop.ItemData, Vector3, Vector3, int, GameObject>)d)(player, musket, muzzle, dir, left, bullet); }
                catch (System.Exception e) { ModLog.Warn("MusketGun: a shot listener failed (" + d.Method.DeclaringType + "." + d.Method.Name + "): " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }      // foreign code: another mod's listener / Jotunn ran here - its exception types are not ours to know, so the catch-all is the only catch
            }
        }

        static Vector3 Spread(Vector3 aim, float degrees)
        {
            if (RuleLog.Due("MusketBulletTrace.Spread", 2d)) ModLog.Every("MusketBulletTrace.Spread", 2d, $"MusketBulletTrace.Spread(aim={RuleLog.Shown(aim)}, degrees={RuleLog.Shown(degrees)})");
            Vector3 side = Vector3.Cross(aim, Vector3.up);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            Vector3 d = Quaternion.AngleAxis(Random.Range(-degrees, degrees), side) * aim;
            { var returnedAt189 = (Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.up) * d).normalized; if (RuleLog.Due("MusketBulletTrace.Spread:189", 2d)) ModLog.Every("MusketBulletTrace.Spread:189", 2d, $"MusketBulletTrace.Spread: returns {returnedAt189} (line 189)"); return returnedAt189; }
        }

        static int PlayMuzzle(Vector3 at, Vector3 forward)
        {
            if (RuleLog.Due("MusketBulletTrace.PlayMuzzle", 2d)) ModLog.Every("MusketBulletTrace.PlayMuzzle", 2d, $"MusketBulletTrace.PlayMuzzle(at={RuleLog.Shown(at)}, forward={RuleLog.Shown(forward)})");
            if (ZNetScene.instance == null) return 0;
            int made = 0;
            foreach (string n in MuzzleEffects)
            {
                GameObject fx = ZNetScene.instance.GetPrefab(n);
                if (fx == null) { if (RuleLog.Due("MusketBulletTrace.PlayMuzzle:200", 2d)) ModLog.Every("MusketBulletTrace.PlayMuzzle:200", 2d, $"MusketBulletTrace.PlayMuzzle: skips to the next item (continue) because fx == null (line 200)"); continue; }
                Object.Instantiate(fx, at, Quaternion.LookRotation(forward));
                made++;
            }
            { var returnedAt204 = made; if (RuleLog.Due("MusketBulletTrace.PlayMuzzle:204", 2d)) ModLog.Every("MusketBulletTrace.PlayMuzzle:204", 2d, $"MusketBulletTrace.PlayMuzzle: returns {returnedAt204} (line 204)"); return returnedAt204; }
        }
    }
}
