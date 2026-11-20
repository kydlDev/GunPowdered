using Jotunn.Entities;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Dynamite.Domain;

namespace GunPowdered.Features.Dynamite.Infrastructure
{
    /// <summary>
    /// Dynamite as the user approved it (D-098 proposal, D-100). The item is held in one hand (cloned from the Club, its model under "attach"); the attack
    /// button sets one stick down on the ground in front of the player (DynamiteAttackPatch). The stick (GP_DynamiteStick, <see cref="DynamiteCharge"/>) is
    /// lit with a Torch and blows after 4 s: the blast (a copy of the Deep North dynamite's, its vanilla look and sound) wrecks buildings, rocks, trees and
    /// ore deposits with blunt + chop + pickaxe damage up to the iron tier; a second, creature-only part of the same blast does 20 % of that damage
    /// ("Creatures take 20% of building damage"); and a round hole is dug where it stood (a copy of the game's own bomb dig, digg_bombdynamite: about 2.5 m
    /// wide and 1.5 m deep, the ground painted dirty around it).
    /// </summary>
    internal static class DynamiteSetup
    {
        internal const string StickId = "GP_DynamiteStick", ExplosionId = "GP_Dynamite_explosion", CraterId = "GP_Dynamite_crater";
        const string ExplosionBase = "bombdynamite_explosion", CraterBase = "digg_bombdynamite", CreatureChild = "gp_creature_blast";
        /// <summary>
        /// The hand model under the item's "attach" (= RightHand_Attach when held). The vanilla Club's handle runs along attach +Z through the origin (radius 0.017),
        /// so the fist closes round that axis. The Dynamite model stands along its own +Y with its origin at the bottom of the tube (bounds centre (-0.016, 0.152, 0)):
        /// turn +Y onto +Z (90 deg about X) and move the tube's middle to (0, 0, 0.02), fuse forward. Fitted in Valheim-Unity-AssetRipper (scene _GunPowderedPreview/DynamiteGrip,
        /// vanilla Player with "Standing Torch Idle right" sampled, Club attach as reference).
        /// </summary>
        static readonly Vector3 HandGripPosition = new Vector3(0.016f, 0f, -0.132f), HandGripEuler = new Vector3(90f, 0f, 0f);
        static GameObject _item, _stick, _explosion, _crater;
        static float _lastPlaced = -99f;

        internal static GameObject Item => _item;
        internal static GameObject Stick => _stick;
        internal static GameObject Explosion => _explosion;
        internal static GameObject Crater => _crater;

        /// <summary>Called by the ItemRegistry right after the dynamite item exists. Expected outcome: the stick, the blast and the crater exist.</summary>
        internal static bool Configure(GameObject itemPrefab)
        {
            _item = itemPrefab;
            ItemDrop.ItemData.SharedData shared = itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_damages = new HitData.DamageTypes();   // held, it is not a club: the attack button sets it down
            shared.m_attack.m_attackStamina = 0f;
            bool hand = AttachHandModel(itemPrefab);
            if (_stick == null) _stick = MakeStick(itemPrefab);
            if (_explosion == null) _explosion = MakeExplosion();
            if (_crater == null) _crater = MakeCrater();
            bool listed = ListCrater(ObjectDB.instance);   // later ObjectDB rebuilds: DynamiteTerrainPatch
            ModLog.Verbose("DynamiteSetup: crater listed as a terrain operation now " + listed + " (ObjectDB " + (ObjectDB.instance != null) + ")");
            bool applied = Apply();
            ModLog.Verbose("DynamiteSetup: configured - hand model " + hand + ", stick " + (_stick != null) + ", blast " + (_explosion != null) + ", crater " + (_crater != null) + ", applied " + applied);
            return hand && _stick != null && applied;
        }

        /// <summary>Puts the Tunables on the blast (both parts) and the crater. Expected outcome: true when both exist and carry the values.</summary>
        internal static bool Apply()
        {
            if (_explosion == null || _crater == null) return false;
            float d = ModSettings.DynamiteDamage.Value, c = DynamiteRules.CreatureDamage(d), r = ModSettings.DynamiteRadius.Value;
            Aoe props = _explosion.GetComponent<Aoe>();
            props.m_useAttackSettings = false;
            props.m_radius = r;
            props.m_damage = new HitData.DamageTypes { m_blunt = d, m_chop = d, m_pickaxe = d };
            props.m_toolTier = ModSettings.DynamiteToolTier.Value;
            props.m_hitCharacters = false;       // creatures: the child part below, at a fifth of the damage
            props.m_hitProps = true;
            props.m_hitTerrain = false;          // the hole is the crater's work
            props.m_spawnOnHitTerrain = null;
            props.m_hitAfterTtl = false;         // with the bang (the base waits for its 3 s life to end)
            props.m_statusEffect = "";

            Transform child = _explosion.transform.Find(CreatureChild);
            Aoe creatures = child != null ? child.GetComponent<Aoe>() : null;
            if (creatures == null) return false;
            creatures.m_useAttackSettings = false;
            creatures.m_radius = r;
            creatures.m_damage = new HitData.DamageTypes { m_blunt = c, m_chop = c, m_pickaxe = c, m_fire = GunPowdered.Core.BurnRules.Total(ModSettings.DynamiteCreatureFire.Value) };     // D-107: burns on top of the 20 %
            creatures.m_hitCharacters = true;
            creatures.m_hitProps = false;
            creatures.m_hitTerrain = false;
            creatures.m_hitOwner = true;         // whoever lit it should run
            creatures.m_hitFriendly = false;     // the game's friendly rule: no tamed animals, players only with PvP on
            creatures.m_hitAfterTtl = false;
            creatures.m_ttl = 0f;                // the parent's life ends it
            creatures.m_dodgeable = false;
            creatures.m_blockable = false;

            TerrainOp dig = _crater.GetComponent<TerrainOp>();
            if (dig == null) return false;
            TerrainOp.Settings s = dig.m_settings;
            s.m_level = false;
            s.m_raise = true;
            s.m_raiseRadius = ModSettings.DynamiteCraterRadius.Value;
            s.m_raiseDelta = -ModSettings.DynamiteCraterDepth.Value;
            s.m_square = false;                  // round
            s.m_smooth = true;
            s.m_smoothRadius = ModSettings.DynamiteCraterRadius.Value * 1.4f;
            s.m_paintCleared = true;
            s.m_paintType = TerrainModifier.PaintType.Dirt;    // the burnt, bare ground around the hole
            s.m_paintRadius = ModSettings.DynamiteCraterRadius.Value * 1.5f;
            dig.m_settings = s;                  // the same object (Settings is a class); kept explicit
            return Mathf.Approximately(props.m_radius, r) && Mathf.Approximately(creatures.m_damage.m_blunt, c);
        }

        /// <summary>
        /// Adds the crater prefab to the ObjectDB's terrain operations (list and hash table) once: the game digs only with terrain operations it finds by prefab
        /// hash (D-103). Expected outcome: true when the hash finds the crater.
        /// </summary>
        internal static bool ListCrater(ObjectDB db)
        {
            TerrainOp op = _crater != null ? _crater.GetComponent<TerrainOp>() : null;
            if (db == null || op == null) return false;
            if (!db.m_terrainOps.Contains(op)) db.m_terrainOps.Add(op);
            int hash = Utils.GetPrefabName(_crater.name).GetStableHashCode();
            db.m_terrainOpsByHash[hash] = op;
            return db.TryGetTerrainOp(hash, out TerrainOp found) && found == op;
        }

        /// <summary>True when the player holds dynamite.</summary>
        internal static ItemDrop.ItemData Held(Player player)
        {
            ItemDrop.ItemData item = player != null ? player.GetCurrentWeapon() : null;
            return item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == GunPowderedPrefabs.Dynamite ? item : null;
        }

        /// <summary>
        /// The attack button with dynamite in hand: one stick is set down <paramref name="metres"/> in front of the player (the usual 1.2 m; tests use
        /// more), on the ground or whatever stands there. Expected outcome: "placed at (x, y, z)", or why not.
        /// </summary>
        internal static string Place(Player player, float metres = DynamiteRules.PlaceDistance)
        {
            ItemDrop.ItemData held = Held(player);
            if (held == null) return "no dynamite in hand";
            if (_stick == null) return "no stick prefab";
            if (Time.time - _lastPlaced < 0.5f) return "too soon";
            Vector3 probe = player.transform.position + player.transform.forward * metres + Vector3.up * 2f;
            RaycastHit hit;
            int mask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece", "Default_small");
            if (!Physics.Raycast(probe, Vector3.down, out hit, 6f, mask))
            {
                player.Message(MessageHud.MessageType.Center, DynamiteRules.NoGround);
                return DynamiteRules.NoGround;
            }
            Object.Instantiate(_stick, hit.point, Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f));
            player.GetInventory().RemoveItem(held, 1);
            _lastPlaced = Time.time;
            ModLog.Info("DynamiteSetup: " + player.GetPlayerName() + " set dynamite down at " + hit.point.ToString("0.00"));
            return "placed at " + hit.point.ToString("0.00");
        }

        /// <summary>The blast of one stick at <paramref name="at"/> (made by the stick's owner). Expected outcome: the number of objects made (blast + crater).</summary>
        internal static int Blast(Vector3 at, Character owner)
        {
            int made = 0;
            if (_explosion != null)
            {
                GameObject blast = Object.Instantiate(_explosion, at, Quaternion.identity);
                foreach (Aoe a in blast.GetComponentsInChildren<Aoe>(true)) a.Setup(owner, Vector3.zero, -1f, null, null, null);
                made++;
            }
            if (_crater != null)
            {
                Object.Instantiate(_crater, at - Vector3.up * 0.15f, Quaternion.identity);
                made++;
            }
            return made;
        }

        static GameObject MakeStick(GameObject item)
        {
            Transform model = item.transform.Find("gp_model");
            GameObject stick = PrefabManager.Instance.CreateEmptyPrefab(StickId, true);
            if (stick == null) return null;
            ZNetView nv = stick.GetComponent<ZNetView>();
            if (nv != null) nv.m_persistent = true;
            foreach (Collider c in stick.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (Renderer r in stick.GetComponentsInChildren<Renderer>(true)) Object.DestroyImmediate(r);
            foreach (MeshFilter f in stick.GetComponentsInChildren<MeshFilter>(true)) Object.DestroyImmediate(f);
            Bounds b = new Bounds(Vector3.up * 0.15f, new Vector3(0.1f, 0.3f, 0.1f));
            if (model != null)
            {
                GameObject look = Object.Instantiate(model.gameObject, stick.transform);
                look.name = "gp_model";
                look.transform.localPosition = Vector3.zero;
                look.transform.localRotation = Quaternion.identity;
                foreach (Collider c in look.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                foreach (Rigidbody rb in look.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
                bool any = false;
                foreach (Renderer r in look.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                    Bounds rb = r.bounds;
                    rb.center = stick.transform.InverseTransformPoint(rb.center);
                    if (!any) { b = rb; any = true; } else b.Encapsulate(rb);
                }
            }
            var box = stick.AddComponent<BoxCollider>();      // for the hover and the use key
            box.center = b.center;
            box.size = Vector3.Max(b.size, new Vector3(0.12f, 0.12f, 0.12f));
            int layer = LayerMask.NameToLayer("piece_nonsolid");
            if (layer >= 0) foreach (Transform t in stick.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            stick.AddComponent<DynamiteCharge>();
            PrefabManager.Instance.AddPrefab(new CustomPrefab(stick, true));
            return stick;
        }

        static GameObject MakeExplosion()
        {
            if (PrefabManager.Instance.GetPrefab(ExplosionBase) == null) { ModLog.Error("DynamiteSetup: no vanilla " + ExplosionBase + " - the dynamite has no blast"); return null; }
            GameObject clone = PrefabManager.Instance.CreateClonedPrefab(ExplosionId, ExplosionBase);
            if (clone == null) return null;
            foreach (TerrainOp op in clone.GetComponentsInChildren<TerrainOp>(true)) Object.DestroyImmediate(op);
            var child = new GameObject(CreatureChild);
            child.transform.SetParent(clone.transform, false);
            child.layer = clone.layer;
            child.AddComponent<Aoe>();
            PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
            return clone;
        }

        static GameObject MakeCrater()
        {
            if (PrefabManager.Instance.GetPrefab(CraterBase) == null) { ModLog.Error("DynamiteSetup: no vanilla " + CraterBase + " - the dynamite digs no hole"); return null; }
            GameObject clone = PrefabManager.Instance.CreateClonedPrefab(CraterId, CraterBase);
            if (clone != null) PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
            return clone;
        }

        static bool AttachHandModel(GameObject item)
        {
            Transform attach = item.transform.Find("attach");
            Transform model = item.transform.Find("gp_model");
            if (attach == null || model == null)
            {
                ModLog.Warn("DynamiteSetup: no hand model - attach " + (attach != null) + ", bundle model " + (model != null));
                return false;
            }
            if (attach.Find("gp_model") != null) return true;
            foreach (Renderer old in attach.GetComponentsInChildren<Renderer>(true)) old.enabled = false;
            GameObject copy = Object.Instantiate(model.gameObject, attach);
            copy.name = "gp_model";
            copy.transform.localPosition = HandGripPosition;      // values fitted on the vanilla Player + Club attach in the AssetRipper project (T-009j)
            copy.transform.localRotation = Quaternion.Euler(HandGripEuler);
            copy.transform.localScale = Vector3.one;
            foreach (Collider c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (Transform t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = attach.gameObject.layer;
            Bounds b = default;
            bool any = false;
            foreach (Renderer r in copy.GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = true;
                if (any) b.Encapsulate(r.bounds); else { b = r.bounds; any = true; }
            }
            ModLog.Info("DynamiteSetup: hand model on the attach point, local position " + copy.transform.localPosition.ToString("0.000") + " rot " + HandGripEuler.ToString("0") + " (expected: centre near (0, 0, 0.02), long axis along +Z"
                + "), bounds centre in attach space " + (any ? attach.InverseTransformPoint(b.center).ToString("0.000") : "none") + ", size " + (any ? b.size.ToString("0.000") : "none"));
            return true;
        }
    }
}
