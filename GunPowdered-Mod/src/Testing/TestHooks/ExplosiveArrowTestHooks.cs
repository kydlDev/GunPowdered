using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.ExplosiveArrows.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the Explosive Arrow (D-100). The shot uses the arrow's own flight prefab and Projectile.Setup like a bow does (owner = the local
    /// player, the arrow as the ammunition), aimed straight at a target, so the flight, the burst and the damage are the real ones; only the bow's draw
    /// and aim are left out.
    /// </summary>
    internal static class ExplosiveArrowTestHooks
    {
        const float ShotSpeed = 60f;
        static float _shotAt = -99f;

        /// <summary>What the arrow is: "ammo &lt;type&gt;, item &lt;type&gt;, pierce P, flight F, burst B, radius R, props H, after life A".</summary>
        [TestHook("GunPowdered.explosiveArrowInfo")]
        static string Info()
        {
            GameObject item = ExplosiveArrowSetup.Item;
            if (item == null) return "no arrow";
            ItemDrop.ItemData.SharedData s = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            Aoe blast = ExplosiveArrowSetup.Explosion != null ? ExplosiveArrowSetup.Explosion.GetComponent<Aoe>() : null;
            Projectile flight = ExplosiveArrowSetup.Projectile != null ? ExplosiveArrowSetup.Projectile.GetComponent<Projectile>() : null;
            return "ammo " + s.m_ammoType + ", item " + s.m_itemType + ", pierce " + s.m_damages.m_pierce
                + ", flight " + (s.m_attack.m_attackProjectile != null ? s.m_attack.m_attackProjectile.name : "none")
                + ", burst " + (flight != null && flight.m_spawnOnHit != null ? flight.m_spawnOnHit.name : "none")
                + ", radius " + (blast != null ? blast.m_radius.ToString("0.#") : "-") + ", props " + (blast != null && blast.m_hitProps)
                + ", after life " + (blast != null && blast.m_hitAfterTtl) + ", respawn item " + (flight != null && flight.m_respawnItemOnHit);
        }

        /// <summary>
        /// Shoots one Explosive Arrow from the local player's eyes straight at the nearest object of this prefab within 40 m (a creature's centre or a piece's
        /// middle). Returns "shot at D m" or why not.
        /// </summary>
        [TestHook("GunPowdered.shootExplosiveArrowAt")]
        static string ShootAt(string prefab)
        {
            Player p = Player.m_localPlayer;
            GameObject flightPrefab = ExplosiveArrowSetup.Projectile;
            if (p == null || flightPrefab == null || ExplosiveArrowSetup.Item == null) return "no player or no arrow";
            Vector3 from = p.GetEyePoint() + p.transform.forward * 0.6f;
            Vector3? target = Nearest(prefab, from);
            if (!target.HasValue) return "no " + prefab + " within 40 m";
            Vector3 dir = (target.Value - from).normalized;
            GameObject arrow = Object.Instantiate(flightPrefab, from, Quaternion.LookRotation(dir));
            var flight = arrow.GetComponent<Projectile>();
            ItemDrop.ItemData ammo = ExplosiveArrowSetup.Item.GetComponent<ItemDrop>().m_itemData.Clone();
            var hit = new HitData { m_damage = ammo.m_shared.m_damages, m_skill = Skills.SkillType.Bows };
            flight.Setup(p, dir * ShotSpeed, -1f, hit, null, ammo);
            _shotAt = Time.time;
            float d = Vector3.Distance(from, target.Value);
            ModLog.Info("ExplosiveArrowTestHooks: shot at " + prefab + " " + d.ToString("0.0") + " m away");
            return "shot at " + d.ToString("0") + " m";
        }

        /// <summary>True when an Explosive Arrow burst exists that was made after the last test shot.</summary>
        [TestHook("GunPowdered.explosiveArrowBurstSeen")]
        static bool BurstSeen()
        {
            foreach (Aoe a in Object.FindObjectsByType<Aoe>(FindObjectsSortMode.None))
                if (a.name.StartsWith(ExplosiveArrowSetup.ExplosionId) && Time.time - _shotAt < 6f) return true;
            return false;
        }

        static readonly System.Collections.Generic.List<GameObject> Placed = new System.Collections.Generic.List<GameObject>();

        /// <summary>Places a piece prefab on the ground <paramref name="metres"/> in front of the local player, facing them. Returns "placed &lt;prefab&gt;".</summary>
        [TestHook("GunPowdered.placePieceAhead")]
        static string PlacePieceAhead(string prefab, double metres)
        {
            Player p = Player.m_localPlayer;
            GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            if (p == null || source == null) return "no player or no prefab " + prefab;
            Vector3 at = p.transform.position + p.transform.forward * (float)metres;
            float ground;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out ground)) at.y = ground;
            GameObject go = Object.Instantiate(source, at, Quaternion.LookRotation(-p.transform.forward));
            if (go != null) Placed.Add(go);
            return go != null ? "placed " + prefab : "could not place " + prefab;
        }

        /// <summary>Removes what placePieceAhead placed. Returns "removed N".</summary>
        [TestHook("GunPowdered.removePiecesAhead")]
        static string RemovePiecesAhead(string prefab)
        {
            int n = 0;
            foreach (GameObject go in Placed)
            {
                if (go == null) continue;
                ZNetView nv = go.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid()) { nv.ClaimOwnership(); ZNetScene.instance.Destroy(go); }
                else Object.Destroy(go);
                n++;
            }
            Placed.Clear();
            return "removed " + n;
        }

        static Vector3? Nearest(string prefab, Vector3 from)
        {
            Vector3? best = null;
            float bestDist = 40f;
            foreach (ZNetView v in Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
            {
                if (Utils.GetPrefabName(v.gameObject) != prefab) continue;
                Character c = v.GetComponent<Character>();
                Vector3 at = c != null ? c.GetCenterPoint() : v.transform.position + Vector3.up;
                float d = Vector3.Distance(from, at);
                if (d < bestDist) { best = at; bestDist = d; }
            }
            return best;
        }
    }
}
