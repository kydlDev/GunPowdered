using System;
using Jotunn.Entities;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;

namespace GunPowdered.Features.Grenades.Infrastructure
{
    /// <summary>
    /// Builds the Spiked Grenade's own flight and blast prefabs (D-046: own prefabs, cloned from vanilla bases) and keeps the item, the flight and
    /// the blast on the approved numbers (D-047, Tunables). The item itself is cloned from the vanilla Ooze Bomb by the ItemRegistry (its throw_bomb attack).
    /// </summary>
    internal static class GrenadeSetup
    {
        internal const string ProjectileId = "GP_SpikedGrenade_projectile";
        internal const string ExplosionId = "GP_SpikedGrenade_explosion";
        // the vanilla bases: the Ooze Bomb's flight (gravity 10, no bounce) and the Deep North dynamite's fiery blast (not the ooze's green poison cloud)
        static readonly string[] ProjectileBases = { "oozebomb_projectile", "bilebomb_projectile" };
        static readonly string[] ExplosionBases = { "bombdynamite_explosion", "bilebomb_explosion", "oozebomb_explosion" };

        static GameObject _projectile, _explosion, _item, _landed;

        /// <summary>D-157: the landed grenade whose fuse burns before the blast.</summary>
        internal const string LandedId = "GP_SpikedGrenade_landed";
        internal static GameObject Landed => _landed;

        /// <summary>
        /// D-157 (user 2026-11-23: "Add a fuse after landing"): the object a grenade's flight leaves where it ends - the grenade model on a small physics body
        /// (it drops to the ground when it hit a creature) with the fuse (GrenadeFuse). Synced position, not saved with the world. Expected outcome: the prefab.
        /// </summary>
        static GameObject MakeLanded(GameObject item)
        {
            GameObject landed = PrefabManager.Instance.CreateEmptyPrefab(LandedId, true);
            if (landed == null) { ModLog.Warn("GrenadeSetup: the landed grenade could not be made - it bursts on impact as before"); return null; }
            ZNetView nv = landed.GetComponent<ZNetView>();
            if (nv != null) nv.m_persistent = false;
            foreach (Collider c in landed.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            foreach (Renderer r in landed.GetComponentsInChildren<Renderer>(true)) UnityEngine.Object.DestroyImmediate(r);
            foreach (MeshFilter f in landed.GetComponentsInChildren<MeshFilter>(true)) UnityEngine.Object.DestroyImmediate(f);
            Transform model = item != null ? item.transform.Find("gp_model") : null;
            if (model != null)
            {
                GameObject look = UnityEngine.Object.Instantiate(model.gameObject, landed.transform);
                look.name = "gp_model";
                look.transform.localPosition = Vector3.up * PotCentreHeight;
                look.transform.localRotation = Quaternion.identity;
                foreach (Collider c in look.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
                foreach (Renderer r in look.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            }
            var ball = landed.AddComponent<SphereCollider>();
            ball.radius = 0.12f;
            ball.center = Vector3.up * 0.12f;
            var body = landed.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.linearDamping = 1f;
            body.angularDamping = 5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation;      // it drops and lies where it landed, it does not roll off down a slope
            var sync = landed.AddComponent<ZSyncTransform>();
            sync.m_syncPosition = true;
            sync.m_syncRotation = true;
            int layer = LayerMask.NameToLayer("item");
            if (layer >= 0) foreach (Transform t in landed.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            landed.AddComponent<GrenadeFuse>();
            PrefabManager.Instance.AddPrefab(new CustomPrefab(landed, true));
            ModLog.Info("GrenadeSetup: landed grenade " + LandedId + " made (model " + (model != null) + ")");
            return landed;
        }
        // the values last put on the prefabs and the ones now asked for (filled in place: no allocation in the per-frame check)
        static readonly float[] Applied = new float[9], Wanted = new float[9];
        static bool _everApplied;

        /// <summary>
        /// Test only (TestLab negative test): a drag the flight gets but the drawn arc does not model, so the check that the throw follows the arc can be
        /// proven to fail. 0 = off (always 0 outside that one test, which resets it).
        /// </summary>
        internal static float TestFlightDrag { get; set; }

        /// <summary>The grenade item prefab, its flight and its blast, once built (null before / when building failed).</summary>
        internal static GameObject Item => _item;
        internal static GameObject Projectile => _projectile;
        internal static GameObject Explosion => _explosion;

        /// <summary>Called by the ItemRegistry right after the grenade item exists. Expected outcome: flight + blast registered and the item wired to them.</summary>
        internal static bool Configure(GameObject itemPrefab)
        {
            _item = itemPrefab;
            _explosion = CloneFirst(ExplosionId, ExplosionBases);
            _projectile = CloneFirst(ProjectileId, ProjectileBases);
            if (_explosion != null) StripTerrainWork(_explosion);
            bool hand = AttachHandModel(itemPrefab);
            if (_landed == null) _landed = MakeLanded(itemPrefab);
            bool applied = Apply();
            ModLog.Verbose("GrenadeSetup: configured - item " + (_item != null) + ", projectile " + (_projectile != null) + ", explosion " + (_explosion != null) + ", values applied " + applied + ", hand model " + hand);
            return _item != null && _projectile != null && _explosion != null && applied;
        }

        /// <summary>
        /// Applies the values when any of them changed since the last time (one string compare; called every frame while a grenade is held). A throw that
        /// left with older values than the arc showed landed 6 m short (run gren2: gravity reset 1 s before the throw, applied only every 5 s).
        /// Expected outcome: the prefabs carry the current values (true), also when nothing had to change.
        /// </summary>
        internal static bool ApplyIfChanged()
        {
            if (RuleLog.Due("GrenadeSetup.ApplyIfChanged", 2d)) ModLog.Every("GrenadeSetup.ApplyIfChanged", 2d, $"GrenadeSetup.ApplyIfChanged()");
            Read(Wanted);
            bool same = _everApplied;
            for (int i = 0; same && i < Wanted.Length; i++) same = Wanted[i].CompareTo(Applied[i]) == 0;
            { var returnedAt112 = same ? _item != null : Apply(); if (RuleLog.Due("GrenadeSetup.ApplyIfChanged:112", 2d)) ModLog.Every("GrenadeSetup.ApplyIfChanged:112", 2d, $"GrenadeSetup.ApplyIfChanged: returns {returnedAt112} (line 112)"); return returnedAt112; }
        }

        static void Read(float[] into)
        {
            if (RuleLog.Due("GrenadeSetup.Read", 2d)) ModLog.Every("GrenadeSetup.Read", 2d, $"GrenadeSetup.Read(into={RuleLog.Shown(into)})");
            into[0] = ModSettings.GrenadeSpeed.Value;
            into[1] = ModSettings.GrenadeGravity.Value;
            into[2] = ModSettings.GrenadeFuseSeconds.Value;
            into[3] = ModSettings.GrenadeRadius.Value;
            into[4] = ModSettings.GrenadeBlunt.Value;
            into[5] = ModSettings.GrenadePierce.Value;
            into[6] = ModSettings.GrenadeFire.Value;
            into[7] = ModSettings.GrenadeRecoil.Value;
            into[8] = TestFlightDrag;
        }

        /// <summary>
        /// Puts the Tunables (D-047) on the item's attack, the flight and the blast (PROJECT_RULES 17). Expected outcome: the three prefabs carry the current values.
        /// </summary>
        internal static bool Apply()
        {
            if (_item == null || _projectile == null || _explosion == null) { if (RuleLog.Due("GrenadeSetup.Apply:134", 2d)) ModLog.Every("GrenadeSetup.Apply:134", 2d, $"GrenadeSetup.Apply: returns false because _item == null || _projectile == null || _explosion == null (line 134)"); return false; }
            float speed = ModSettings.GrenadeSpeed.Value;

            ItemDrop.ItemData.SharedData shared = _item.GetComponent<ItemDrop>().m_itemData.m_shared;
            Attack attack = shared.m_attack;
            attack.m_attackProjectile = _projectile;
            attack.m_projectileVel = speed;
            attack.m_projectileVelMin = speed;
            attack.m_projectileAccuracy = 0f;        // no random spread: the throw follows the drawn arc exactly
            attack.m_projectileAccuracyMin = 0f;
            attack.m_randomVelocity = false;
            attack.m_launchAngle = 0f;
            attack.m_recoilPushback = ModSettings.GrenadeRecoil.Value;
            shared.m_damages = new HitData.DamageTypes();   // the damage is the blast's; the Ooze Bomb's own poison must not ride along
            shared.m_attackStatusEffect = null;

            var flight = _projectile.GetComponent<global::Projectile>();
            flight.m_gravity = ModSettings.GrenadeGravity.Value;
            flight.m_drag = TestFlightDrag;           // 0 in play; the arc assumes no drag
            flight.m_bounce = false;
            flight.m_ttl = ModSettings.GrenadeFuseSeconds.Value;
            flight.m_spawnOnTtl = true;              // a grenade that hits nothing still bursts at the end of its fuse
            flight.m_spawnOnHit = _landed != null ? _landed : _explosion;      // D-157: it lands and its fuse burns (GrenadeFuse), then bursts
            flight.m_spawnOffset = Vector3.zero;     // the Ooze Bomb bursts 1 m back along its flight (0, 0, -1): 0.76 m short of the drawn ring (run gren1)
            flight.m_statusEffect = "";

            var blast = _explosion.GetComponent<Aoe>();
            blast.m_useAttackSettings = false;
            blast.m_radius = ModSettings.GrenadeRadius.Value;
            blast.m_damage = new HitData.DamageTypes
            {
                m_blunt = ModSettings.GrenadeBlunt.Value,
                m_pierce = ModSettings.GrenadePierce.Value,
                m_fire = GunPowdered.Core.BurnRules.Total(ModSettings.GrenadeFire.Value)
            };
            blast.m_toolTier = 0;
            blast.m_hitProps = true;                 // buildings, trees and rocks take the normal damage (user D-100: only the dynamite is special)
            blast.m_hitTerrain = false;
            blast.m_spawnOnHitTerrain = null;
            blast.m_statusEffect = "";
            blast.m_hitOwner = true;                 // the red ring warns of it
            blast.m_hitAfterTtl = false;             // hit WITH the burst: the dynamite base hits only when its 3 s life ends (bombdynamite_explosion m_hitAfterTtl 1, D-100)

            Read(Applied);
            _everApplied = true;
            ModLog.Verbose("GrenadeSetup: applied - speed " + speed + " m/s, gravity " + flight.m_gravity + ", fuse " + flight.m_ttl + " s, burst offset " + flight.m_spawnOffset + ", radius " + blast.m_radius
                + " m, damage blunt " + blast.m_damage.m_blunt + " pierce " + blast.m_damage.m_pierce + " fire " + blast.m_damage.m_fire + ", recoil " + attack.m_recoilPushback
                + (TestFlightDrag > 0f ? ", TEST flight drag " + TestFlightDrag : ""));
            { var returnedAt182 = Mathf.Approximately(attack.m_projectileVel, speed) && Mathf.Approximately(blast.m_radius, ModSettings.GrenadeRadius.Value) && blast.m_hitProps; if (RuleLog.Due("GrenadeSetup.Apply:182", 2d)) ModLog.Every("GrenadeSetup.Apply:182", 2d, $"GrenadeSetup.Apply: returns {returnedAt182} (line 182)"); return returnedAt182; }
        }

        static GameObject CloneFirst(string id, string[] bases)
        {
            foreach (string b in bases)
            {
                if (PrefabManager.Instance.GetPrefab(b) == null)
                {
                    ModLog.Warn("GrenadeSetup: vanilla base " + b + " for " + id + " not found, trying the next one");
                    continue;
                }
                GameObject clone = PrefabManager.Instance.CreateClonedPrefab(id, b);
                if (clone == null) { ModLog.Warn("GrenadeSetup: cloning " + b + " into " + id + " failed"); continue; }
                BundleVisuals.Apply(id, clone);
                PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
                ModLog.Verbose("GrenadeSetup: " + id + " cloned from " + b);
                return clone;
            }
            ModLog.Error("GrenadeSetup: no vanilla base found for " + id + " (" + string.Join(", ", bases) + ")");
            return null;
        }

        /// The pot's centre above its bottom (the model's origin), so the hand copy sits where the vanilla bomb's ball was.
        const float PotCentreHeight = 0.065f;

        /// <summary>
        /// The game shows only the item's "attach" child in the hand (VisEquipment); BundleVisuals put the bundle model at the item's root and hid the
        /// Ooze Bomb's own ball, so the hand would be empty. A copy of the model goes under "attach", centred where the ball was.
        /// Expected outcome: true when "attach" holds the copy (false when the item has no "attach" or no bundle model; then the hand shows nothing new).
        /// </summary>
        static bool AttachHandModel(GameObject item)
        {
            Transform attach = item.transform.Find("attach");
            Transform model = item.transform.Find("gp_model");
            if (attach == null || model == null)
            {
                ModLog.Warn("GrenadeSetup: no hand model - attach " + (attach != null) + ", bundle model " + (model != null));
                return false;
            }
            if (attach.Find("gp_model") != null) { if (RuleLog.IsOn()) ModLog.Verbose($"GrenadeSetup.AttachHandModel: returns true because attach.Find(\"gp_model\") != null (line 222)"); return true; }
            Renderer old = attach.GetComponentInChildren<Renderer>(true);
            Vector3 centre = old != null ? attach.InverseTransformPoint(old.transform.position) : Vector3.zero;
            GameObject copy = UnityEngine.Object.Instantiate(model.gameObject, attach);
            copy.name = "gp_model";
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;
            copy.transform.localPosition = centre - (Vector3.up * PotCentreHeight);
            foreach (Transform t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = attach.gameObject.layer;
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            ModLog.Verbose("GrenadeSetup: hand model under attach at " + copy.transform.localPosition.ToString("0.000") + " (the vanilla ball was " + (old != null ? old.name : "not found") + ")");
            return attach.Find("gp_model") != null;
        }

        // The dynamite's blast digs the ground (a TerrainOp it spawns or carries); the grenade must not.
        static void StripTerrainWork(GameObject explosion)
        {
            int removed = 0;
            foreach (TerrainOp op in explosion.GetComponentsInChildren<TerrainOp>(true))
            {
                UnityEngine.Object.DestroyImmediate(op);
                removed++;
            }
            ModLog.Verbose("GrenadeSetup: " + removed + " terrain op(s) removed from " + explosion.name);
        }

        /// <summary>Is this item a Spiked Grenade (by prefab name).</summary>
        internal static bool IsGrenade(ItemDrop.ItemData item) =>
            item != null && item.m_dropPrefab != null && string.Equals(item.m_dropPrefab.name, GunPowderedPrefabs.SpikedGrenade, StringComparison.Ordinal);
    }
}
