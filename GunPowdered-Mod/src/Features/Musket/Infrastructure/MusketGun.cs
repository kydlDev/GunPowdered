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
            ItemDrop.ItemData weapon = player != null ? player.GetCurrentWeapon() : null;
            return IsMusket(weapon) ? weapon : null;
        }

        /// <summary>How many cartridges this musket holds (0 when nothing is stored).</summary>
        internal static int Loaded(ItemDrop.ItemData musket)
        {
            if (musket == null || musket.m_customData == null) return 0;
            string stored;
            return musket.m_customData.TryGetValue(LoadedKey, out stored) ? MusketRules.ParseLoaded(stored) : 0;
        }

        /// <summary>Stores a load on the musket (kept inside 0..5). Expected outcome: the load stored.</summary>
        internal static int SetLoaded(ItemDrop.ItemData musket, int loaded)
        {
            if (musket == null) return 0;
            int v = MusketRules.ClampLoaded(loaded);
            if (musket.m_customData == null) musket.m_customData = new System.Collections.Generic.Dictionary<string, string>();
            musket.m_customData[LoadedKey] = v.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return v;
        }

        /// <summary>
        /// The attack button with the musket in hand. Expected outcome: "fired, N left", or why not ("too soon", the empty message, no musket / bullet) - a
        /// refused shot tells the player in the middle of the screen. A running reload stops first (what is loaded stays).
        /// </summary>
        internal static string Fire(Player player)
        {
            LastAttempt = FireCore(player);
            return LastAttempt;
        }

        /// <summary>What the last press of the attack button with the musket did (for test hooks).</summary>
        internal static string LastAttempt { get; private set; } = "not pressed yet";

        static string FireCore(Player player)
        {
            ItemDrop.ItemData musket = Held(player);
            if (musket == null) return "no musket in hand";
            MusketReloader.Stop(player, "a shot");
            if (!MusketRules.ShotIntervalOver(Time.time, _lastShot)) return "too soon";
            int loaded = Loaded(musket);
            string why = MusketRules.WhyNotFire(loaded);
            if (why != null)
            {
                player.Message(MessageHud.MessageType.Center, why);
                return why;
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
            var flight = bullet.GetComponent<Projectile>();
            var hit = new HitData();
            hit.m_damage.m_pierce = MusketRules.BulletPierce;
            hit.m_damage.m_fire = GunPowdered.Core.BurnRules.Total(MusketRules.BulletFirePerSecond);
            hit.m_pushForce = musket.m_shared.m_attackForce;
            hit.m_skill = musket.m_shared.m_skillType;
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
            return "fired, " + left + " left";
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
            Rigidbody body = player.GetComponent<Rigidbody>();
            float push = MusketRules.RecoilPushForce(MusketRules.RecoilShoveMetres, body != null ? body.mass : 0f, player.GetEquipmentMovementModifier());
            if (push > 0f) player.ApplyPushback(-dir, push);
            return push;
        }

        static void RaiseFired(Player player, ItemDrop.ItemData musket, Vector3 muzzle, Vector3 dir, int left, GameObject bullet)
        {
            System.Action<Player, ItemDrop.ItemData, Vector3, Vector3, int, GameObject> listeners = Fired;
            if (listeners == null) return;
            foreach (System.Delegate d in listeners.GetInvocationList())
            {
                try { ((System.Action<Player, ItemDrop.ItemData, Vector3, Vector3, int, GameObject>)d)(player, musket, muzzle, dir, left, bullet); }
                catch (System.Exception e) { ModLog.Warn("MusketGun: a shot listener failed (" + d.Method.DeclaringType + "." + d.Method.Name + "): " + e.GetType().Name + ": " + e.Message); }
            }
        }

        static Vector3 Spread(Vector3 aim, float degrees)
        {
            Vector3 side = Vector3.Cross(aim, Vector3.up);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            Vector3 d = Quaternion.AngleAxis(Random.Range(-degrees, degrees), side) * aim;
            return (Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.up) * d).normalized;
        }

        static int PlayMuzzle(Vector3 at, Vector3 forward)
        {
            if (ZNetScene.instance == null) return 0;
            int made = 0;
            foreach (string n in MuzzleEffects)
            {
                GameObject fx = ZNetScene.instance.GetPrefab(n);
                if (fx == null) continue;
                Object.Instantiate(fx, at, Quaternion.LookRotation(forward));
                made++;
            }
            return made;
        }
    }
}
