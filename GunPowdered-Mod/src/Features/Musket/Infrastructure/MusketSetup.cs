using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Musket.Domain;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The Iron Musket as an equippable weapon (D-087 step 1, D-088 step 0): the item is cloned from the vanilla Arbalest (two-handed, shoulder-aimed, with the
    /// game's reload) and its hand model is the bundle's musket put under the clone's "attach" child, where the game takes what the player holds. The arms get
    /// the musket's own poses from MusketRig (D-092); the model sits in the right hand at the pose authored with them. The game's one-bolt reload is switched off: the
    /// musket keeps its own 5-cartridge magazine and fires its own bullet (MusketGun, MusketReloader, D-095).
    /// </summary>
    internal static class MusketSetup
    {
        static GameObject _item;
        static Vector3 _handPosition = new Vector3(MusketPoses.GripHold[0], MusketPoses.GripHold[1], MusketPoses.GripHold[2]);           // authored with the arm poses (D-092)
        static Vector3 _handEuler = new Vector3(MusketPoses.GripHold[3], MusketPoses.GripHold[4], MusketPoses.GripHold[5]);

        /// <summary>The musket item prefab once configured (null before).</summary>
        internal static GameObject Item => _item;

        /// <summary>Called by the ItemRegistry right after the musket item exists. Expected outcome: true when the hand model sits under "attach".</summary>
        internal static bool Configure(GameObject itemPrefab)
        {
            _item = itemPrefab;
            bool hand = AttachHandModel(itemPrefab);
            bool gun = MakeGun(itemPrefab);
            if (_bullet == null) _bullet = MakeBullet();
            AddKeyHints();
            ModLog.Verbose("MusketSetup: configured - hand model " + hand + ", own magazine " + gun + ", bullet " + (_bullet != null));
            return hand && gun && _bullet != null;
        }

        internal const string BulletId = "GP_MusketBullet_projectile";
        const string BoltItem = "BoltIron";
        static GameObject _bullet;
        static bool _bulletLook;

        /// <summary>
        /// The flying bullet prefab (null before Configure or when no vanilla bolt projectile was found). Its look (the Iron Bullet model) is put on at the
        /// first use, because the Iron Bullet item is registered after the musket.
        /// </summary>
        internal static GameObject Bullet
        {
            get
            {
                if (_bullet != null && !_bulletLook)
                {
                    _bulletLook = BulletLook(_bullet, _bullet.GetComponent<Projectile>());
                    ModLog.Verbose("MusketSetup: bullet look " + _bulletLook);
                }
                return _bullet;
            }
        }

        static bool _hints;

        // the key hints at the bottom right while the musket is in hand (user 2026-11-19 12:32: "there is no UI item that tells you how to reload"):
        // the game's own buttons, so they show whatever the player bound
        static void AddKeyHints()
        {
            if (_hints) return;
            _hints = KeyHintManager.Instance.AddKeyHint(new KeyHintConfig
            {
                Item = GunPowderedPrefabs.Musket,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", Hint = MusketRules.HintFire },
                    new ButtonConfig { Name = "SecondaryAttack", Hint = MusketRules.HintReload },
                    new ButtonConfig { Name = "Block", Hint = MusketRules.HintBlock }
                }
            });
            ModLog.Verbose("MusketSetup: key hints " + _hints);
        }

        // D-095: the musket keeps its own magazine (MusketGun); the Arbalest's one-bolt reload and its bolt ammo are switched off
        static bool MakeGun(GameObject item)
        {
            ItemDrop drop = item.GetComponent<ItemDrop>();
            if (drop == null) return false;
            ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
            shared.m_attack.m_requiresReload = false;
            shared.m_ammoType = "";
            return !shared.m_attack.m_requiresReload;
        }

        // the bullet: a clone of the vanilla iron bolt's projectile (the game's flight, hit, friendly fire and skill rules) with the Iron Bullet's model
        static GameObject MakeBullet()
        {
            GameObject bolt = PrefabManager.Instance.GetPrefab(BoltItem);
            ItemDrop boltDrop = bolt != null ? bolt.GetComponent<ItemDrop>() : null;
            GameObject baseFlight = boltDrop != null ? boltDrop.m_itemData.m_shared.m_attack.m_attackProjectile : null;
            if (baseFlight == null)
            {
                ModLog.Error("MusketSetup: no vanilla bolt projectile (" + BoltItem + ") - the musket cannot fire");
                return null;
            }
            GameObject clone = PrefabManager.Instance.CreateClonedPrefab(BulletId, baseFlight);
            if (clone == null) return null;
            Projectile flight = clone.GetComponent<Projectile>();
            if (flight != null)
            {
                flight.m_gravity = MusketRules.BulletGravity;
                flight.m_ttl = MusketRules.BulletSeconds;
                flight.m_respawnItemOnHit = false;      // a bullet is not picked up again like a bolt
                flight.m_spawnOnHit = null;
                flight.m_stayAfterHitStatic = false;
                flight.m_stayAfterHitDynamic = false;
            }
            PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
            ModLog.Verbose("MusketSetup: " + BulletId + " cloned from " + baseFlight.name + ", projectile " + (flight != null));
            return clone;
        }

        static bool BulletLook(GameObject clone, Projectile flight)
        {
            GameObject bulletItem = PrefabManager.Instance.GetPrefab(GunPowderedPrefabs.IronBullet);
            Transform model = bulletItem != null ? bulletItem.transform.Find("gp_model") : null;
            if (model == null) return false;
            Transform parent = flight != null && flight.m_visual != null ? flight.m_visual.transform : clone.transform;
            foreach (Renderer r in parent.GetComponentsInChildren<Renderer>(true)) if (!(r is ParticleSystemRenderer)) r.enabled = false;
            GameObject copy = Object.Instantiate(model.gameObject, parent);
            copy.name = "gp_model";
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            foreach (Collider c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (Rigidbody rb in copy.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            return true;
        }

        /// <summary>
        /// Moves and turns the hand model on the item prefab (the next equip shows it; pictures decide the numbers). Expected outcome: true when the model exists and
        /// now sits at the asked pose.
        /// </summary>
        internal static bool SetHandPose(Vector3 position, Vector3 euler)
        {
            _handPosition = position;
            _handEuler = euler;
            Transform copy = _item != null ? _item.transform.Find("attach/gp_model") : null;
            if (copy == null)
            {
                ModLog.Warn("MusketSetup: no hand model to pose");
                return false;
            }
            copy.localPosition = position;
            copy.localEulerAngles = euler;
            ModLog.Verbose("MusketSetup: hand pose " + position.ToString("0.000") + " rotation " + euler.ToString("0.0"));
            return copy.localPosition == position;
        }

        static bool AttachHandModel(GameObject item)
        {
            Transform attach = item.transform.Find("attach");
            Transform model = item.transform.Find("gp_model");
            if (attach == null || model == null)
            {
                ModLog.Warn("MusketSetup: no hand model - attach " + (attach != null) + ", bundle model " + (model != null));
                return false;
            }
            if (attach.Find("gp_model") != null) return true;
            foreach (Renderer old in attach.GetComponentsInChildren<Renderer>(true)) old.enabled = false;
            GameObject copy = Object.Instantiate(model.gameObject, attach);
            copy.name = "gp_model";
            copy.transform.localPosition = _handPosition;
            copy.transform.localEulerAngles = _handEuler;
            copy.transform.localScale = Vector3.one;
            foreach (Transform t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = attach.gameObject.layer;
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            ModLog.Verbose("MusketSetup: hand model under attach (" + attach.childCount + " children), the vanilla look hidden");
            return attach.Find("gp_model") != null;
        }
    }
}
