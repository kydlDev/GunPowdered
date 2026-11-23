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
    internal static class DynamiteSetup
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
            foreach (ChargeKind k in Kinds) if (k.ChargeId == chargeId) { var returnedAt62 = k; if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.KindOfCharge: returns {RuleLog.Shown(returnedAt62)} (line 62)"); return returnedAt62; }
            { var returnedAt63 = StickKind; if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.KindOfCharge: returns {RuleLog.Shown(returnedAt63)} (line 63)"); return returnedAt63; }
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
            if (kind.Explosion == null || kind.Crater == null) { if (RuleLog.IsOn()) ModLog.Verbose("ChargeKind.ApplyKind: returns false (line 110)"); return false; }
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
            if (creatures == null) { if (RuleLog.IsOn()) ModLog.Verbose("ChargeKind.ApplyKind: returns false (line 127)"); return false; }
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
            if (dig == null) { if (RuleLog.IsOn()) ModLog.Verbose("ChargeKind.ApplyKind: returns false (line 143)"); return false; }
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
            { var returnedAt156 = Mathf.Approximately(props.m_radius, r) && Mathf.Approximately(creatures.m_damage.m_blunt, c); if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.ApplyKind: returns {RuleLog.Shown(returnedAt156)} (line 156)"); return returnedAt156; }
        }

        /// <summary>
        /// Adds the crater prefab to the ObjectDB's terrain operations (list and hash table) once: the game digs only with terrain operations it finds by prefab
        /// hash (D-103). Expected outcome: true when the hash finds the crater.
        /// </summary>
        internal static bool ListCrater(ObjectDB db)
        {
            if (RuleLog.Due("ChargeKind.ListCrater", 2d)) ModLog.Every("ChargeKind.ListCrater", 2d, $"ChargeKind.ListCrater(db={RuleLog.Shown(db)})");
            if (db == null || StickKind.Crater == null) { if (RuleLog.Due("ChargeKind.ListCrater:166", 2d)) ModLog.Every("ChargeKind.ListCrater:166", 2d, "ChargeKind.ListCrater: returns false (line 166)"); return false; }
            bool all = true;
            foreach (ChargeKind k in Kinds) if (k.Crater != null) all &= ListOne(db, k.Crater);
            { var returnedAt169 = all; if (RuleLog.Due("ChargeKind.ListCrater:169", 2d)) ModLog.Every("ChargeKind.ListCrater:169", 2d, $"ChargeKind.ListCrater: returns {RuleLog.Shown(returnedAt169)} (line 169)"); return returnedAt169; }
        }

        static bool ListOne(ObjectDB db, GameObject crater)
        {
            if (RuleLog.Due("ChargeKind.ListOne", 2d)) ModLog.Every("ChargeKind.ListOne", 2d, $"ChargeKind.ListOne(db={RuleLog.Shown(db)}, crater={RuleLog.Shown(crater)})");
            TerrainOp op = crater.GetComponent<TerrainOp>();
            if (op == null) { if (RuleLog.Due("ChargeKind.ListOne:176", 2d)) ModLog.Every("ChargeKind.ListOne:176", 2d, "ChargeKind.ListOne: returns false (line 176)"); return false; }
            if (!db.m_terrainOps.Contains(op)) db.m_terrainOps.Add(op);
            int hash = Utils.GetPrefabName(crater.name).GetStableHashCode();
            db.m_terrainOpsByHash[hash] = op;
            { var returnedAt180 = db.TryGetTerrainOp(hash, out TerrainOp found) && found == op; if (RuleLog.Due("ChargeKind.ListOne:180", 2d)) ModLog.Every("ChargeKind.ListOne:180", 2d, $"ChargeKind.ListOne: returns {RuleLog.Shown(returnedAt180)} (line 180)"); return returnedAt180; }
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
            if (item == null || item.m_dropPrefab == null) { if (RuleLog.Due("ChargeKind.HeldKind:192", 2d)) ModLog.Every("ChargeKind.HeldKind:192", 2d, "ChargeKind.HeldKind: returns null (line 192)"); return null; }
            foreach (ChargeKind k in Kinds) if (item.m_dropPrefab.name == k.ItemId) { kind = k; { var returnedAt193 = item; if (RuleLog.Due("ChargeKind.HeldKind:193", 2d)) ModLog.Every("ChargeKind.HeldKind:193", 2d, $"ChargeKind.HeldKind: returns {RuleLog.Shown(returnedAt193)} (line 193)"); return returnedAt193; } }
            { if (RuleLog.Due("ChargeKind.HeldKind:194", 2d)) ModLog.Every("ChargeKind.HeldKind:194", 2d, "ChargeKind.HeldKind: returns null (line 194)"); return null; }
        }

        /// <summary>
        /// The attack button with dynamite in hand: one stick is set down <paramref name="metres"/> in front of the player (the usual 1.2 m; tests use
        /// more), on the ground or whatever stands there. Expected outcome: "placed at (x, y, z)", or why not.
        /// </summary>
        internal static string Place(Player player, float metres = DynamiteRules.PlaceDistance)
        {
            ItemDrop.ItemData held = HeldKind(player, out ChargeKind kind);
            if (held == null) { if (RuleLog.Due("ChargeKind.Place:204", 2d)) ModLog.Every("ChargeKind.Place:204", 2d, "ChargeKind.Place: returns \"no dynamite in hand\" (line 204)"); return "no dynamite in hand"; }
            if (kind.Charge == null) { var returnedAt205 = "no charge prefab for " + kind.Name; if (RuleLog.Due("ChargeKind.Place:205", 2d)) ModLog.Every("ChargeKind.Place:205", 2d, $"ChargeKind.Place: returns {RuleLog.Shown(returnedAt205)} (line 205)"); return returnedAt205; }
            if (Time.time - _lastPlaced < 0.5f) { if (RuleLog.Due("ChargeKind.Place:206", 2d)) ModLog.Every("ChargeKind.Place:206", 2d, "ChargeKind.Place: returns \"too soon\" (line 206)"); return "too soon"; }
            if (!FindSpot(player, metres, out Vector3 spot))
            {
                player.Message(MessageHud.MessageType.Center, DynamiteRules.NoGround);
                { var returnedAt213 = DynamiteRules.NoGround; if (RuleLog.Due("ChargeKind.Place:213", 2d)) ModLog.Every("ChargeKind.Place:213", 2d, $"ChargeKind.Place: returns {RuleLog.Shown(returnedAt213)} (line 213)"); return returnedAt213; }
            }
            player.StartEmote("kneel");                                // D-148: the character bends down to set it on the ground (the game's own kneel)
            if (!TestNoStandUp) player.StartCoroutine(StandUp(player));      // D-158: the game's kneel stays until the player moves; stand up again after it
            Object.Instantiate(kind.Charge, spot, Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f));
            if (held.m_stack <= 1) player.UnequipItem(held, false);     // the last one: unequip first like the game's Attack.ConsumeItem, or its model stays in the hand (Agent-10, lab a10packvis1)
            player.GetInventory().RemoveItem(held, 1);
            _lastPlaced = Time.time;
            ModLog.Info("DynamiteSetup: " + player.GetPlayerName() + " set " + kind.Name + " down at " + spot.ToString("0.00"));
            return "placed at " + spot.ToString("0.00");
        }

        /// <summary>Test switch (hook GunPowdered.dynamiteNoStandUp, A/B for dynamite-alerts-mobs): true = the old D-148 kneel that stays until the player moves.</summary>
        internal static bool TestNoStandUp;

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
            for (int tries = 0; tries < 4; tries++, d = Mathf.Max(DynamiteRules.NearestPlace, d - 0.2f))
            {
                Vector3 from = chest + fwd * d;
                if (!Physics.Raycast(from, Vector3.down, out RaycastHit ground, DynamiteRules.ChestHeight + DynamiteRules.SurfaceBelow + 0.5f, PlaceMask, QueryTriggerInteraction.Ignore)) continue;
                if (!DynamiteRules.SurfaceOk(feet.y, ground.point.y)) continue;
                if (Physics.CheckSphere(ground.point + Vector3.up * 0.18f, 0.06f, PlaceMask, QueryTriggerInteraction.Ignore)) continue;   // something sticks into the stick's space
                spot = ground.point;
                ModLog.Verbose($"ChargeKind.FindSpot: wanted {metres:0.00} m, blocked at {blockedAt:0.00} m ({(blockedAt >= 0f ? block.collider.name : "nothing")}), set {d:0.00} m ahead on {ground.collider.name} at {spot:0.00}, {ground.point.y - feet.y:+0.00;-0.00} m from the feet - returns true");
                return true;
            }
            ModLog.Verbose($"ChargeKind.FindSpot: no free ground within reach (blocked at {blockedAt:0.00} m) - returns false");
            return false;
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
            { var returnedAt241 = made; if (RuleLog.Due("ChargeKind.Blast:241", 2d)) ModLog.Every("ChargeKind.Blast:241", 2d, $"ChargeKind.Blast: returns {RuleLog.Shown(returnedAt241)} (line 241)"); return returnedAt241; }
        }

        static GameObject MakeCharge(ChargeKind kind, GameObject item)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.MakeCharge(kind={RuleLog.Shown(kind)}, item={RuleLog.Shown(item)})");
            Transform model = item.transform.Find("gp_model");
            GameObject stick = PrefabManager.Instance.CreateEmptyPrefab(kind.ChargeId, true);
            if (stick == null) { if (RuleLog.IsOn()) ModLog.Verbose("ChargeKind.MakeCharge: returns null (line 249)"); return null; }
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
            AddSparkle(stick, b);
            var box = stick.AddComponent<BoxCollider>();      // for the hover and the use key
            box.center = b.center;
            box.size = Vector3.Max(b.size, new Vector3(0.12f, 0.12f, 0.12f));
            int layer = LayerMask.NameToLayer("piece_nonsolid");
            if (layer >= 0) foreach (Transform t in stick.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            stick.AddComponent<DynamiteCharge>().m_kindId = kind.ChargeId;
            PrefabManager.Instance.AddPrefab(new CustomPrefab(stick, true));
            { var returnedAt280 = stick; if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.MakeCharge: returns {RuleLog.Shown(returnedAt280)} (line 280)"); return returnedAt280; }
        }

        /// <summary>D-158: the child that holds the placed stick's sparkle (DynamiteCharge hides it once the fuse burns).</summary>
        internal const string SparkleName = "gp_sparkle";

        /// <summary>D-158: the sparkle's colour, light red (user 2026-11-23: "if you can you could change its color to a light red").</summary>
        internal static readonly Color SparkleColor = new Color(1f, 0.4f, 0.4f, 1f);

        /// <summary>
        /// D-158 (user 2026-11-23: "add the sparkling effect, that dropped items have, to the placed dynamite ... so the player can better identify where the
        /// dynamite has been placed"): the game's dropped-item sparkle is a particle system on every item prefab's root (Wood.prefab, Coins.prefab ...). A copy
        /// of the Wood one, without anything else of the item, goes on the placed stick, light red. Expected outcome: true when the sparkle was added.
        /// </summary>
        static bool AddSparkle(GameObject stick, Bounds bounds)
        {
            GameObject wood = PrefabManager.Cache.GetPrefab<GameObject>("Wood");
            if (wood == null || wood.GetComponent<ParticleSystem>() == null) { ModLog.Warn("DynamiteSetup: no Wood item sparkle found - the placed dynamite shows none"); return false; }
            // the charge prefab lies under Jotunn's inactive prefab root, so the copy's game scripts (ItemDrop, ZNetView ...) never wake up before they go
            GameObject sparkle = Object.Instantiate(wood, stick.transform);
            sparkle.name = SparkleName;
            foreach (Transform child in sparkle.transform.Cast<Transform>().ToList()) Object.DestroyImmediate(child.gameObject);
            Component[] parts = sparkle.GetComponents<Component>();
            // scripts first (they need the rest), then physics, then everything but the transform and the particles
            foreach (Component c in parts) if (c is MonoBehaviour) Object.DestroyImmediate(c);
            foreach (Component c in sparkle.GetComponents<Component>()) if (c is Collider || c is Rigidbody) Object.DestroyImmediate(c);
            foreach (Component c in sparkle.GetComponents<Component>()) if (!(c is Transform) && !(c is ParticleSystem) && !(c is ParticleSystemRenderer)) Object.DestroyImmediate(c);
            sparkle.transform.localPosition = bounds.center;
            sparkle.transform.localRotation = Quaternion.identity;
            sparkle.transform.localScale = Vector3.one;
            ParticleSystem ps = sparkle.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = SparkleColor;
            ModLog.Info("DynamiteSetup: light red sparkle added to " + stick.name + " (copied from the Wood item, " + sparkle.GetComponents<Component>().Length + " components)");
            return true;
        }

        static GameObject MakeExplosion(ChargeKind kind)
        {
            if (PrefabManager.Instance.GetPrefab(ExplosionBase) == null) { ModLog.Error("DynamiteSetup: no vanilla " + ExplosionBase + " - the " + kind.Name + " has no blast"); return null; }
            GameObject clone = PrefabManager.Instance.CreateClonedPrefab(kind.ExplosionId, ExplosionBase);
            if (clone == null) { if (RuleLog.IsOn()) ModLog.Verbose("ChargeKind.MakeExplosion: returns null (line 287)"); return null; }
            foreach (TerrainOp op in clone.GetComponentsInChildren<TerrainOp>(true)) Object.DestroyImmediate(op);
            var child = new GameObject(CreatureChild);
            child.transform.SetParent(clone.transform, false);
            child.layer = clone.layer;
            child.AddComponent<Aoe>();
            if (!Mathf.Approximately(kind.ExplosionScale, 1f))        // the pack's bang looks bigger; the Aoe reach is its radius, not the scale
                foreach (Transform t in clone.transform) if (t.name != CreatureChild) t.localScale *= kind.ExplosionScale;
            PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
            { var returnedAt296 = clone; if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.MakeExplosion: returns {RuleLog.Shown(returnedAt296)} (line 296)"); return returnedAt296; }
        }

        static GameObject MakeCrater(string id)
        {
            if (PrefabManager.Instance.GetPrefab(CraterBase) == null) { ModLog.Error("DynamiteSetup: no vanilla " + CraterBase + " - " + id + " digs no hole"); return null; }
            GameObject clone = PrefabManager.Instance.CreateClonedPrefab(id, CraterBase);
            if (clone != null) PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
            { var returnedAt304 = clone; if (RuleLog.IsOn()) ModLog.Verbose($"ChargeKind.MakeCrater: returns {RuleLog.Shown(returnedAt304)} (line 304)"); return returnedAt304; }
        }

        static bool AttachHandModel(GameObject item, bool stickGrip)
        {
            Transform attach = item.transform.Find("attach");
            Transform model = item.transform.Find("gp_model");
            if (attach == null || model == null)
            {
                ModLog.Warn("DynamiteSetup: no hand model - attach " + (attach != null) + ", bundle model " + (model != null));
                return false;
            }
            if (attach.Find("gp_model") != null) { if (RuleLog.IsOn()) ModLog.Verbose("ChargeKind.AttachHandModel: returns true (line 316)"); return true; }
            foreach (Renderer old in attach.GetComponentsInChildren<Renderer>(true)) old.enabled = false;
            GameObject copy = Object.Instantiate(model.gameObject, attach);
            copy.name = "gp_model";
            copy.transform.localPosition = stickGrip ? HandGripPosition : PackGripPosition;      // values fitted on the vanilla Player + Club attach in the AssetRipper project (T-009j)
            copy.transform.localRotation = Quaternion.Euler(stickGrip ? HandGripEuler : PackGripEuler);
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
            { if (RuleLog.IsOn()) ModLog.Verbose("ChargeKind.AttachHandModel: returns true (line 334)"); return true; }
        }
    }
}
