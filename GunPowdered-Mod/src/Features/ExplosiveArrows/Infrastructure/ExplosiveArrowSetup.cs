using Jotunn.Entities;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;

namespace GunPowdered.Features.ExplosiveArrows.Infrastructure
{
    /// <summary>
    /// The Explosive Arrow as real ammunition (D-100): the item is cloned from the vanilla Iron Arrow, so any bow shoots it; its flight is a copy of the
    /// iron arrow's with the arrow's own model, it is used up on the hit (no arrow to pick up again), and the hit sets off a burst - a copy of the
    /// Deep North dynamite's fiery blast without its terrain work. The burst hurts creatures (with their armour) and damages buildings, trees and rocks
    /// like any other damage; it follows the game's friendly-fire rule (not the archer, not tamed animals, players only with PvP on).
    /// </summary>
    internal static class ExplosiveArrowSetup
    {
        internal const string ProjectileId = "GP_ExplosiveArrow_projectile", ExplosionId = "GP_ExplosiveArrow_explosion";
        static readonly string[] ExplosionBases = { "bombdynamite_explosion", "bilebomb_explosion", "oozebomb_explosion" };
        static GameObject _item, _projectile, _explosion;

        /// <summary>The arrow item, its flight and its burst once built (null before / when building failed).</summary>
        internal static GameObject Item => _item;
        internal static GameObject Projectile => _projectile;
        internal static GameObject Explosion => _explosion;

        /// <summary>Called by the ItemRegistry right after the arrow item exists. Expected outcome: flight + burst registered and wired to the arrow.</summary>
        internal static bool Configure(GameObject itemPrefab)
        {
            _item = itemPrefab;
            ItemDrop drop = itemPrefab.GetComponent<ItemDrop>();
            GameObject baseFlight = drop != null ? drop.m_itemData.m_shared.m_attack.m_attackProjectile : null;
            if (baseFlight == null)
            {
                ModLog.Error("ExplosiveArrowSetup: the arrow has no vanilla flight to copy - it cannot be shot");
                return false;
            }
            if (_projectile == null) _projectile = PrefabManager.Instance.CreateClonedPrefab(ProjectileId, baseFlight);
            if (_explosion == null) _explosion = CloneFirst(ExplosionId, ExplosionBases);
            if (_projectile != null) PrefabManager.Instance.AddPrefab(new CustomPrefab(_projectile, true));
            if (_explosion != null)
            {
                StripTerrainWork(_explosion);
                PrefabManager.Instance.AddPrefab(new CustomPrefab(_explosion, true));
            }
            bool look = ArrowLook(itemPrefab, _projectile);
            bool applied = Apply();
            ModLog.Verbose("ExplosiveArrowSetup: configured - flight " + (_projectile != null) + " (from " + baseFlight.name + "), burst " + (_explosion != null) + ", arrow look " + look + ", applied " + applied);
            return _projectile != null && _explosion != null && applied;
        }

        /// <summary>Puts the Tunables on the arrow, its flight and its burst. Expected outcome: the three prefabs carry the current values.</summary>
        internal static bool Apply()
        {
            if (RuleLog.On) ModLog.Every("ExplosiveArrowSetup.Apply", 2d, $"ExplosiveArrowSetup.Apply()");
            if (_item == null || _projectile == null || _explosion == null) { if (RuleLog.On) ModLog.Every("ExplosiveArrowSetup.Apply:56", 2d, "ExplosiveArrowSetup.Apply: returns false (line 56)"); return false; }
            ItemDrop.ItemData.SharedData shared = _item.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_damages = new HitData.DamageTypes { m_pierce = ModSettings.ExplosiveArrowPierce.Value };
            shared.m_attack.m_attackProjectile = _projectile;      // a bow shoots the flight of its ammunition

            var flight = _projectile.GetComponent<Projectile>();
            if (flight != null)
            {
                flight.m_spawnOnHit = _explosion;
                flight.m_spawnOnHitChance = 1f;
                flight.m_spawnCount = 1;
                flight.m_respawnItemOnHit = false;     // used up: the burst destroys it
                flight.m_stayAfterHitStatic = false;
                flight.m_stayAfterHitDynamic = false;
                flight.m_spawnOnTtl = false;
            }

            var blast = _explosion.GetComponent<Aoe>();
            if (blast == null) { if (RuleLog.On) ModLog.Every("ExplosiveArrowSetup.Apply:74", 2d, "ExplosiveArrowSetup.Apply: returns false (line 74)"); return false; }
            blast.m_useAttackSettings = false;
            blast.m_radius = ModSettings.ExplosiveArrowRadius.Value;
            blast.m_damage = new HitData.DamageTypes { m_blunt = ModSettings.ExplosiveArrowBlunt.Value, m_fire = GunPowdered.Core.BurnRules.Total(ModSettings.ExplosiveArrowFire.Value) };
            blast.m_toolTier = 0;
            blast.m_hitProps = true;             // buildings, trees and rocks take the normal damage (D-100: only the dynamite is special)
            blast.m_hitTerrain = false;          // no holes in the ground: that is the dynamite's
            blast.m_spawnOnHitTerrain = null;
            blast.m_statusEffect = "";
            blast.m_hitOwner = false;            // the game's friendly-fire rule: never the archer,
            blast.m_hitFriendly = false;         // not tamed animals / players without PvP
            blast.m_ignorePVP = false;
            blast.m_hitAfterTtl = false;         // hit WITH the burst (the dynamite base would hit only when its 3 s life ends)
            { var returnedAt87 = flight != null && blast.m_hitProps && Mathf.Approximately(blast.m_radius, ModSettings.ExplosiveArrowRadius.Value); if (RuleLog.On) ModLog.Every("ExplosiveArrowSetup.Apply:87", 2d, $"ExplosiveArrowSetup.Apply: returns {RuleLog.Shown(returnedAt87)} (line 87)"); return returnedAt87; }
        }

        /// <summary>Is this item an Explosive Arrow (by prefab name).</summary>
        internal static bool IsExplosiveArrow(ItemDrop.ItemData item) =>
            item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == Core.GunPowderedPrefabs.ExplosiveArrow;

        // the flight shows the arrow's own model (the bundle's gp_model) instead of the iron arrow's
        static bool ArrowLook(GameObject item, GameObject flightPrefab)
        {
            if (RuleLog.On) ModLog.Verbose($"ExplosiveArrowSetup.ArrowLook(item={RuleLog.Shown(item)}, flightPrefab={RuleLog.Shown(flightPrefab)})");
            Transform model = item.transform.Find("gp_model");
            Projectile flight = flightPrefab != null ? flightPrefab.GetComponent<Projectile>() : null;
            if (model == null || flight == null) { if (RuleLog.On) ModLog.Verbose("ExplosiveArrowSetup.ArrowLook: returns false (line 100)"); return false; }
            Transform parent = flight.m_visual != null ? flight.m_visual.transform : flightPrefab.transform;
            if (parent.Find("gp_model") != null) { if (RuleLog.On) ModLog.Verbose("ExplosiveArrowSetup.ArrowLook: returns true (line 102)"); return true; }
            foreach (Renderer r in parent.GetComponentsInChildren<Renderer>(true)) if (!(r is ParticleSystemRenderer) && !(r is TrailRenderer)) r.enabled = false;
            GameObject copy = Object.Instantiate(model.gameObject, parent);
            copy.name = "gp_model";
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            foreach (Collider c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (Rigidbody rb in copy.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            { if (RuleLog.On) ModLog.Verbose("ExplosiveArrowSetup.ArrowLook: returns true (line 111)"); return true; }
        }

        static GameObject CloneFirst(string id, string[] bases)
        {
            foreach (string b in bases)
            {
                if (PrefabManager.Instance.GetPrefab(b) == null) continue;
                GameObject clone = PrefabManager.Instance.CreateClonedPrefab(id, b);
                if (clone != null) { var returnedAt120 = clone; if (RuleLog.On) ModLog.Verbose($"ExplosiveArrowSetup.CloneFirst: returns {RuleLog.Shown(returnedAt120)} (line 120)"); return returnedAt120; }
            }
            ModLog.Error("ExplosiveArrowSetup: no vanilla base for " + id);
            return null;
        }

        static void StripTerrainWork(GameObject explosion)
        {
            if (RuleLog.On) ModLog.Verbose($"ExplosiveArrowSetup.StripTerrainWork(explosion={RuleLog.Shown(explosion)})");
            foreach (TerrainOp op in explosion.GetComponentsInChildren<TerrainOp>(true)) Object.DestroyImmediate(op);
        }
    }
}
