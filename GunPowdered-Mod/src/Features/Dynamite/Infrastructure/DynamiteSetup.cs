using System.Linq;
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
    internal static partial class DynamiteSetup
    {
        internal const string StickId = "GP_DynamiteStick", ExplosionId = "GP_Dynamite_explosion", CraterId = "GP_Dynamite_crater";
        internal const string PackChargeId = "GP_DeepNorthPackCharge", PackExplosionId = "GP_DeepNorthPack_explosion", PackCraterId = "GP_DeepNorthPack_crater";
        const string ExplosionBase = "bombdynamite_explosion", CraterBase = "digg_bombdynamite", CreatureChild = "gp_creature_blast";

        /// <summary>
        /// One kind of charge: the stick (D-100) or the Deep North Dynamite Pack (D-108). Each has its own item, the object set down, the blast and the crater;
        /// the numbers come from its Tunables at <see cref="Apply"/>.
        /// </summary>
        internal sealed class ChargeKind
        {
            internal string ItemId, ChargeId, ExplosionId, CraterId, Name;
            internal bool ShowsTimer;          // D-108: a burning pack shows its countdown on screen
            internal float ExplosionScale = 1f;
            internal GameObject Item, Charge, Explosion, Crater;
            internal System.Func<float> Radius, Blunt, Chop, Pickaxe, CreatureFirePerSecond, CraterRadius, CraterDepth;
            internal System.Func<int> ToolTier;
        }

        internal static readonly ChargeKind StickKind = new ChargeKind
        {
            ItemId = GunPowderedPrefabs.Dynamite, ChargeId = StickId, ExplosionId = ExplosionId, CraterId = CraterId, Name = "Dynamite",
            Radius = () => ModSettings.DynamiteRadius.Value, Blunt = () => ModSettings.DynamiteDamage.Value, Chop = () => ModSettings.DynamiteDamage.Value,
            Pickaxe = () => ModSettings.DynamiteDamage.Value, CreatureFirePerSecond = () => ModSettings.DynamiteCreatureFire.Value,
            CraterRadius = () => ModSettings.DynamiteCraterRadius.Value, CraterDepth = () => ModSettings.DynamiteCraterDepth.Value, ToolTier = () => ModSettings.DynamiteToolTier.Value
        };

        internal static readonly ChargeKind PackKind = new ChargeKind
        {
            ItemId = GunPowderedPrefabs.DeepNorthPack, ChargeId = PackChargeId, ExplosionId = PackExplosionId, CraterId = PackCraterId, Name = "Deep North Dynamite Pack",
            ShowsTimer = true, ExplosionScale = 3f,
            Radius = () => ModSettings.PackRadius.Value, Blunt = () => ModSettings.PackDamage.Value, Chop = () => ModSettings.PackDamage.Value,
            Pickaxe = () => ModSettings.PackPickaxe.Value, CreatureFirePerSecond = () => ModSettings.PackCreatureFire.Value,
            CraterRadius = () => ModSettings.PackCraterRadius.Value, CraterDepth = () => ModSettings.PackCraterDepth.Value, ToolTier = () => ModSettings.PackToolTier.Value
        };

        static readonly ChargeKind[] Kinds = { StickKind, PackKind };

        /// <summary>The kind whose charge prefab is <paramref name="chargeId"/> (the stick when unknown).</summary>
        internal static ChargeKind KindOfCharge(string chargeId)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.KindOfCharge(chargeId={RuleLog.Shown(chargeId)})");
            foreach (ChargeKind k in Kinds) if (k.ChargeId == chargeId) return k;
            { var returnedAt64 = StickKind; if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.KindOfCharge: returns {returnedAt64} (line 64)"); return returnedAt64; }
        }
        /// <summary>
        /// The hand model under the item's "attach" (= RightHand_Attach when held). The vanilla Club's handle runs along attach +Z through the origin (radius 0.017),
        /// so the fist closes round that axis. The Dynamite model stands along its own +Y with its origin at the bottom of the tube (bounds centre (-0.016, 0.152, 0)):
        /// turn +Y onto +Z (90 deg about X) and move the tube's middle to (0, 0, 0.02), fuse forward. Fitted in Valheim-Unity-AssetRipper (scene _GunPowderedPreview/DynamiteGrip,
        /// vanilla Player with "Standing Torch Idle right" sampled, Club attach as reference).
        /// </summary>
        static readonly Vector3 HandGripPosition = new Vector3(0.016f, 0f, -0.132f), HandGripEuler = new Vector3(90f, 0f, 0f);
        /// <summary>D-108: the pack hangs below the fist, upright, held by its top strap (model origin = bottom centre, about 0.3 m tall). First fit, to be checked in game.</summary>
        static readonly Vector3 PackGripPosition = new Vector3(0f, -0.27f, 0.04f), PackGripEuler = new Vector3(0f, 0f, 0f);
        static float _lastPlaced = -99f;

        internal static GameObject Item => StickKind.Item;
        internal static GameObject Stick => StickKind.Charge;
        internal static GameObject Explosion => StickKind.Explosion;
        internal static GameObject Crater => StickKind.Crater;

        /// <summary>Called by the ItemRegistry right after the dynamite item exists. Expected outcome: the stick, the blast and the crater exist.</summary>
        internal static bool Configure(GameObject itemPrefab) => ConfigureKind(StickKind, itemPrefab);

        /// <summary>D-108: the Deep North Dynamite Pack, set down and lit like a stick. Expected outcome: its charge, blast and crater exist.</summary>
        internal static bool ConfigurePack(GameObject itemPrefab) => ConfigureKind(PackKind, itemPrefab);

        static bool ConfigureKind(ChargeKind kind, GameObject itemPrefab)
        {
            kind.Item = itemPrefab;
            MakeQuietFuseSound();
            ItemDrop.ItemData.SharedData shared = itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_damages = new HitData.DamageTypes();   // held, it is not a club: the attack button sets it down
            shared.m_attack.m_attackStamina = 0f;
            bool hand = AttachHandModel(itemPrefab, kind == StickKind);
            if (kind.Charge == null) kind.Charge = MakeCharge(kind, itemPrefab);
            if (kind.Explosion == null) kind.Explosion = MakeExplosion(kind);
            if (kind.Crater == null) kind.Crater = MakeCrater(kind.CraterId);
            bool listed = ListCrater(ObjectDB.instance);   // later ObjectDB rebuilds: DynamiteTerrainPatch
            ModLog.Verbose("DynamiteSetup: " + kind.Name + " crater listed as a terrain operation now " + listed + " (ObjectDB " + (ObjectDB.instance != null) + ")");
            bool applied = ApplyKind(kind);
            ModLog.Verbose("DynamiteSetup: " + kind.Name + " configured - hand model " + hand + ", charge " + (kind.Charge != null) + ", blast " + (kind.Explosion != null) + ", crater " + (kind.Crater != null) + ", applied " + applied);
            return hand && kind.Charge != null && applied;
        }

        /// <summary>Puts the Tunables on the blast (both parts) and the crater. Expected outcome: true when both exist and carry the values.</summary>
        internal static bool Apply()
        {
            if (RuleLog.IsOn()) ModLog.Verbose("DynamiteSetup.Apply()");
            // both kinds are set up even when the stick fails, so one broken charge does not hide the other (Sonar S2178: two named results instead of '&')
            bool stickReady = ApplyKind(StickKind);
            bool packReady = PackKind.Explosion == null || ApplyKind(PackKind);
            { var returnedAt112 = stickReady && packReady; if (RuleLog.IsOn()) ModLog.Verbose($"DynamiteSetup.Apply: stick {stickReady}, pack {packReady} - returns {RuleLog.Shown(returnedAt112)} (line 112)"); return returnedAt112; }
        }

        static bool ApplyKind(ChargeKind kind)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.ApplyKind(kind={RuleLog.Shown(kind)})");
            if (kind.Explosion == null || kind.Crater == null) return false;
            float r = kind.Radius(), blunt = kind.Blunt(), chop = kind.Chop(), pick = kind.Pickaxe();
            float c = DynamiteRules.CreatureDamage(blunt);
            Aoe props = kind.Explosion.GetComponent<Aoe>();
            props.m_useAttackSettings = false;
            props.m_radius = r;
            props.m_damage = new HitData.DamageTypes { m_blunt = blunt, m_chop = chop, m_pickaxe = pick };
            props.m_toolTier = kind.ToolTier();
            props.m_hitCharacters = false;       // creatures: the child part below, at a fifth of the damage
            props.m_hitProps = true;
            props.m_hitTerrain = false;          // the hole is the crater's work
            props.m_spawnOnHitTerrain = null;
            props.m_hitAfterTtl = false;         // with the bang (the base waits for its 3 s life to end)
            props.m_statusEffect = "";

            Transform child = kind.Explosion.transform.Find(CreatureChild);
            Aoe creatures = child != null ? child.GetComponent<Aoe>() : null;
            if (creatures == null) { if (RuleLog.Due("ChargeKind.ApplyKind:136", 2d)) ModLog.Every("ChargeKind.ApplyKind:136", 2d, $"ChargeKind.ApplyKind: returns false because creatures == null (line 136)"); return false; }
            creatures.m_useAttackSettings = false;
            creatures.m_radius = r;
            creatures.m_damage = new HitData.DamageTypes { m_blunt = c, m_chop = DynamiteRules.CreatureDamage(chop), m_pickaxe = DynamiteRules.CreatureDamage(pick), m_fire = GunPowdered.Core.BurnRules.Total(kind.CreatureFirePerSecond()) };     // D-107: burns on top of the 20 %
            creatures.m_hitCharacters = true;
            creatures.m_hitProps = false;
            creatures.m_hitTerrain = false;
            creatures.m_hitOwner = true;         // whoever lit it should run
            creatures.m_hitFriendly = false;     // the game's friendly rule: no tamed animals, players only with PvP on
            creatures.m_hitAfterTtl = false;
            creatures.m_hitInterval = 0f;        // ONE hit, like the vanilla blast; the default 1 s re-hit every second of the 3 s life (T-009l: 3 hits = 2.44x, lab a10watch1)
            creatures.m_ttl = 0f;                // the parent's life ends it
            creatures.m_dodgeable = false;
            creatures.m_blockable = false;

            TerrainOp dig = kind.Crater.GetComponent<TerrainOp>();
            if (dig == null) { if (RuleLog.Due("ChargeKind.ApplyKind:152", 2d)) ModLog.Every("ChargeKind.ApplyKind:152", 2d, $"ChargeKind.ApplyKind: returns false because dig == null (line 152)"); return false; }
            TerrainOp.Settings s = dig.m_settings;
            s.m_level = false;
            s.m_raise = true;
            s.m_raiseRadius = kind.CraterRadius();
            s.m_raiseDelta = -kind.CraterDepth();
            s.m_square = false;                  // round
            s.m_smooth = true;
            s.m_smoothRadius = kind.CraterRadius() * 1.4f;
            s.m_paintCleared = true;
            s.m_paintType = TerrainModifier.PaintType.Dirt;    // the burnt, bare ground around the hole
            s.m_paintRadius = kind.CraterRadius() * 1.5f;
            dig.m_settings = s;                  // the same object (Settings is a class); kept explicit
            { var returnedAt165 = Mathf.Approximately(props.m_radius, r) && Mathf.Approximately(creatures.m_damage.m_blunt, c); if (RuleLog.Due("ChargeKind.ApplyKind:165", 2d)) ModLog.Every("ChargeKind.ApplyKind:165", 2d, $"ChargeKind.ApplyKind: returns {returnedAt165} (line 165)"); return returnedAt165; }
        }

        /// <summary>
        /// Adds the crater prefab to the ObjectDB's terrain operations (list and hash table) once: the game digs only with terrain operations it finds by prefab
        /// hash (D-103). Expected outcome: true when the hash finds the crater.
        /// </summary>
        internal static bool ListCrater(ObjectDB db)
        {
            if (RuleLog.Due("ChargeKind.ListCrater", 2d)) ModLog.Every("ChargeKind.ListCrater", 2d, $"ChargeKind.ListCrater(db={RuleLog.Shown(db)})");
            if (db == null || StickKind.Crater == null) return false;
            bool all = true;
            foreach (ChargeKind k in Kinds) if (k.Crater != null) all &= ListOne(db, k.Crater);
            { var returnedAt178 = all; if (RuleLog.Due("ChargeKind.ListCrater:178", 2d)) ModLog.Every("ChargeKind.ListCrater:178", 2d, $"ChargeKind.ListCrater: returns {returnedAt178} (line 178)"); return returnedAt178; }
        }

        static bool ListOne(ObjectDB db, GameObject crater)
        {
            if (RuleLog.Due("ChargeKind.ListOne", 2d)) ModLog.Every("ChargeKind.ListOne", 2d, $"ChargeKind.ListOne(db={RuleLog.Shown(db)}, crater={RuleLog.Shown(crater)})");
            TerrainOp op = crater.GetComponent<TerrainOp>();
            if (op == null) { if (RuleLog.Due("ChargeKind.ListOne:185", 2d)) ModLog.Every("ChargeKind.ListOne:185", 2d, $"ChargeKind.ListOne: returns false because op == null (line 185)"); return false; }
            if (!db.m_terrainOps.Contains(op)) db.m_terrainOps.Add(op);
            int hash = Utils.GetPrefabName(crater.name).GetStableHashCode();
            db.m_terrainOpsByHash[hash] = op;
            { var returnedAt189 = db.TryGetTerrainOp(hash, out TerrainOp found) && found == op; if (RuleLog.Due("ChargeKind.ListOne:189", 2d)) ModLog.Every("ChargeKind.ListOne:189", 2d, $"ChargeKind.ListOne: returns {returnedAt189} (line 189)"); return returnedAt189; }
        }

        /// <summary>D-149: the burning fuse's crackle - the game's sfx_FireAddFuel at <see cref="FuseVolume"/> of its volume (user: "too loud"), its own network prefab.</summary>
        internal const string QuietFuseSound = "sfx_gp_fuse_quiet";
        const float FuseVolume = 0.35f;

        static void MakeQuietFuseSound()
        {
            if (PrefabManager.Instance.GetPrefab(QuietFuseSound) != null) { ModLog.Verbose("DynamiteSetup.MakeQuietFuseSound: already made - returns"); return; }
            GameObject clone = PrefabManager.Instance.CreateClonedPrefab(QuietFuseSound, "sfx_FireAddFuel");
            if (clone == null) { ModLog.Warn("DynamiteSetup: no vanilla sfx_FireAddFuel - the fuse burns silently"); return; }
            int scaled = 0;
            foreach (ZSFX z in clone.GetComponentsInChildren<ZSFX>(true)) { z.m_minVol *= FuseVolume; z.m_maxVol *= FuseVolume; scaled++; }
            PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
            ModLog.Verbose("DynamiteSetup.MakeQuietFuseSound: " + QuietFuseSound + " at " + FuseVolume + " x the volume (" + scaled + " sound(s)) - returns");
        }

        /// <summary>True when the player holds dynamite.</summary>
        internal static ItemDrop.ItemData Held(Player player) => HeldKind(player, out _);

        /// <summary>The dynamite or pack the player holds, and its kind (null when neither).</summary>
        internal static ItemDrop.ItemData HeldKind(Player player, out ChargeKind kind)
        {
            if (RuleLog.Due("ChargeKind.HeldKind", 2d)) ModLog.Every("ChargeKind.HeldKind", 2d, $"ChargeKind.HeldKind(player={RuleLog.Shown(player)})");
            kind = null;
            ItemDrop.ItemData item = player != null ? player.GetCurrentWeapon() : null;
            if (item == null || item.m_dropPrefab == null) { if (RuleLog.Due("ChargeKind.HeldKind:216", 2d)) ModLog.Every("ChargeKind.HeldKind:216", 2d, $"ChargeKind.HeldKind: returns null because item == null || item.m_dropPrefab == null (line 216)"); return null; }
            foreach (ChargeKind k in Kinds) if (item.m_dropPrefab.name == k.ItemId) { kind = k; { var returnedAt217 = item; if (RuleLog.Due("ChargeKind.HeldKind:217", 2d)) ModLog.Every("ChargeKind.HeldKind:217", 2d, $"ChargeKind.HeldKind: returns {returnedAt217} because item.m_dropPrefab.name == k.ItemId (line 217)"); return returnedAt217; } }
            { if (RuleLog.Due("ChargeKind.HeldKind:218", 2d)) ModLog.Every("ChargeKind.HeldKind:218", 2d, $"ChargeKind.HeldKind: returns null (line 218)"); return null; }
        }

        /// <summary>
        /// The attack button with dynamite in hand: one stick is set down <paramref name="metres"/> in front of the player (the usual 1.2 m; tests use
        /// more), on the ground or whatever stands there. Expected outcome: "placed at (x, y, z)", or why not.
        /// </summary>
        internal static string Place(Player player, float metres = DynamiteRules.PlaceDistance)
        {
            ItemDrop.ItemData held = HeldKind(player, out ChargeKind kind);
            if (held == null) { if (RuleLog.Due("ChargeKind.Place:204", 2d)) ModLog.Every("ChargeKind.Place:228", 2d, "ChargeKind.Place: returns \"no dynamite in hand\" (line 228)"); return "no dynamite in hand"; }
            if (kind.Charge == null) return "no charge prefab for " + kind.Name;
            if (Time.time - _lastPlaced < 0.5f) { if (RuleLog.Due("ChargeKind.Place:206", 2d)) ModLog.Every("ChargeKind.Place:230", 2d, "ChargeKind.Place: returns \"too soon\" (line 230)"); return "too soon"; }
            if (!FindSpot(player, metres, out Vector3 spot))
            {
                player.Message(MessageHud.MessageType.Center, DynamiteRules.NoGround);
                { var returnedAt234 = DynamiteRules.NoGround; if (RuleLog.Due("ChargeKind.Place:234", 2d)) ModLog.Every("ChargeKind.Place:234", 2d, $"ChargeKind.Place: returns {returnedAt234} (line 234)"); return returnedAt234; }
            }
            player.StartEmote("kneel");                                // D-148: the character bends down to set it on the ground (the game's own kneel)
            player.StartCoroutine(StandUp(player));      // D-158: the game's kneel stays until the player moves; stand up again after it
            Object.Instantiate(kind.Charge, spot, Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f));
            if (held.m_stack <= 1) player.UnequipItem(held, false);     // the last one: unequip first like the game's Attack.ConsumeItem, or its model stays in the hand (Agent-10, lab a10packvis1)
            player.GetInventory().RemoveItem(held, 1);
            _lastPlaced = Time.time;
            ModLog.Info("DynamiteSetup: " + player.GetPlayerName() + " set " + kind.Name + " down at " + spot.ToString("0.00"));
            return "placed at " + spot.ToString("0.00");
        }

        static readonly System.Action<Player> StopEmote = HarmonyLib.AccessTools.MethodDelegate<System.Action<Player>>(HarmonyLib.AccessTools.Method(typeof(Player), "StopEmote"));

        /// <summary>
        /// D-158 (user 2026-11-23: "have it get back up and not remain kneeled"): after <see cref="DynamiteRules.KneelSeconds"/> the kneel is stopped, if the
        /// player is still in it (moving already ended it). The game syncs the emote to every client.
        /// </summary>
        static System.Collections.IEnumerator StandUp(Player player)
        {
            ModLog.Verbose("DynamiteSetup.StandUp: in " + DynamiteRules.KneelSeconds + " s");
            yield return new WaitForSeconds(DynamiteRules.KneelSeconds);
            ZNetView nview = player != null ? player.GetComponent<ZNetView>() : null;
            if (nview == null || !nview.IsValid() || nview.GetZDO().GetString(ZDOVars.s_emote) != "kneel") { ModLog.Verbose("DynamiteSetup.StandUp: no longer kneeling - nothing to do"); yield break; }
            StopEmote(player);
            ModLog.Info("DynamiteSetup: " + player.GetPlayerName() + " stands up after setting the dynamite down");
        }

        static readonly int PlaceMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece", "Default_small", "vehicle");

        /// <summary>
        /// D-148 (user 2026-11-21 12:25: placed from the outer side of a half-mined copper deposit, the stick ended on its inner wall): where a stick goes -
        /// at chest height towards the wanted spot, stopping in front of anything in the way; then straight down onto the first surface, which must be the
        /// ground the player stands on (a step up or down at most); the stick's own space must be free, else it comes a little nearer. Expected outcome:
        /// true with a visible spot on top of the surface, false when there is none.
        /// </summary>
        static bool FindSpot(Player player, float metres, out Vector3 spot)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.FindSpot(player={RuleLog.Shown(player)}, metres={metres})");
            spot = Vector3.zero;
            Vector3 feet = player.transform.position;
            Vector3 fwd = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            Vector3 chest = feet + Vector3.up * DynamiteRules.ChestHeight;
            float blockedAt = Physics.SphereCast(chest, 0.1f, fwd, out RaycastHit block, metres + DynamiteRules.BlockMargin, PlaceMask, QueryTriggerInteraction.Ignore) ? block.distance : -1f;
            float d = DynamiteRules.ClearDistance(metres, blockedAt);
            System.Text.StringBuilder why = new System.Text.StringBuilder();
            for (int tries = 0; tries < 4; tries++, d = Mathf.Max(DynamiteRules.NearestPlace, d - 0.2f))
            {
                Vector3 from = chest + fwd * d;
                if (!Physics.Raycast(from, Vector3.down, out RaycastHit ground, DynamiteRules.ChestHeight + DynamiteRules.SurfaceBelow + 0.5f, PlaceMask, QueryTriggerInteraction.Ignore)) { why.Append($"; {d:0.00} m: no surface below {from:0.00}{DeepBelow(from)}"); { if (RuleLog.Due("ChargeKind.FindSpot:283", 2d)) ModLog.Every("ChargeKind.FindSpot:283", 2d, $"ChargeKind.FindSpot: skips to the next item (continue) because !Physics.Raycast(from, Vector3.down, out RaycastHit ground, DynamiteRules.ChestHeight + DynamiteRules.SurfaceBelow + 0.5f, PlaceMask, QueryTriggerInteraction.Ignore) (line 283)"); continue; } }
                if (!DynamiteRules.SurfaceOk(feet.y, ground.point.y)) { why.Append($"; {d:0.00} m: {ground.collider.name} at {ground.point.y - feet.y:+0.00;-0.00} m from the feet"); { if (RuleLog.Due("ChargeKind.FindSpot:284", 2d)) ModLog.Every("ChargeKind.FindSpot:284", 2d, $"ChargeKind.FindSpot: skips to the next item (continue) because !DynamiteRules.SurfaceOk(feet.y, ground.point.y) (line 284)"); continue; } }
                if (Physics.CheckSphere(ground.point + Vector3.up * 0.18f, 0.06f, PlaceMask, QueryTriggerInteraction.Ignore)) { why.Append($"; {d:0.00} m: space taken at {ground.point:0.00}"); { if (RuleLog.Due("ChargeKind.FindSpot:285", 2d)) ModLog.Every("ChargeKind.FindSpot:285", 2d, $"ChargeKind.FindSpot: skips to the next item (continue) because Physics.CheckSphere(ground.point + Vector3.up * 0.18f, 0.06f, PlaceMask, QueryTriggerInteraction.Ignore) (line 285)"); continue; } }   // something sticks into the stick's space
                spot = ground.point;
                ModLog.Verbose($"ChargeKind.FindSpot: wanted {metres:0.00} m, blocked at {blockedAt:0.00} m ({(blockedAt >= 0f ? block.collider.name : "nothing")}), set {d:0.00} m ahead on {ground.collider.name} at {spot:0.00}, {ground.point.y - feet.y:+0.00;-0.00} m from the feet - returns true");
                return true;
            }
            ModLog.Verbose($"ChargeKind.FindSpot: no free ground within reach (blocked at {blockedAt:0.00} m, feet {feet:0.00}, facing {fwd:0.00}{why}) - returns false");
            return false;
        }

        // Diagnostics for a failed spot (a18g16b / a18g16d-2 visual-dynamite-kneel: "no surface below" on flat arena ground): what a long ray finds and
        // the terrain height the heightmap reports there, so a missing ground collider and a real pit can be told apart. Only built when placing failed.
        static string DeepBelow(Vector3 from)
        {
            if (RuleLog.Due("ChargeKind.DeepBelow", 2d)) ModLog.Every("ChargeKind.DeepBelow", 2d, $"ChargeKind.DeepBelow(from={from})");
            string hit = Physics.Raycast(from, Vector3.down, out RaycastHit far, 60f, PlaceMask, QueryTriggerInteraction.Ignore)
                ? $"first surface {far.collider.name} at y {far.point.y:0.00}" : "nothing within 60 m";
            float h = 0f;
            string terrain = Heightmap.GetHeight(from, out h) ? $"heightmap ground y {h:0.00}" : "no heightmap here";
            Heightmap hm = Heightmap.FindHeightmap(from);
            Collider col = hm != null ? hm.GetComponent<Collider>() : null;
            { var returnedAt305 = $" ({hit}, {terrain}, heightmap collider {(col == null ? "none" : col.enabled ? "on" : "off")})"; if (RuleLog.Due("ChargeKind.DeepBelow:305", 2d)) ModLog.Every("ChargeKind.DeepBelow:305", 2d, $"ChargeKind.DeepBelow: returns {returnedAt305} (line 305)"); return returnedAt305; }
        }

        /// <summary>The blast of one stick at <paramref name="at"/> (made by the stick's owner). Expected outcome: the number of objects made (blast + crater).</summary>
        internal static int Blast(Vector3 at, Character owner) => Blast(StickKind, at, owner);

        internal static int Blast(ChargeKind kind, Vector3 at, Character owner)
        {
            if (RuleLog.Due("ChargeKind.Blast", 2d)) ModLog.Every("ChargeKind.Blast", 2d, $"ChargeKind.Blast(kind={RuleLog.Shown(kind)}, at={RuleLog.Shown(at)}, owner={RuleLog.Shown(owner)})");
            int made = 0;
            if (kind.Explosion != null)
            {
                GameObject blast = Object.Instantiate(kind.Explosion, at, Quaternion.identity);
                foreach (Aoe a in blast.GetComponentsInChildren<Aoe>(true)) a.Setup(owner, Vector3.zero, -1f, null, null, null);
                made++;
            }
            if (kind.Crater != null)
            {
                Object.Instantiate(kind.Crater, at - Vector3.up * 0.15f, Quaternion.identity);
                made++;
            }
            { var returnedAt326 = made; if (RuleLog.Due("ChargeKind.Blast:326", 2d)) ModLog.Every("ChargeKind.Blast:326", 2d, $"ChargeKind.Blast: returns {returnedAt326} (line 326)"); return returnedAt326; }
        }

    }
}
