// kydl:read-only-observer - no creature is moved or commanded; the player's teleport is only watched, the placer spawns guano deposits
using System;
using System.Collections;
using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Features.Guano.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Guano.Infrastructure
{
    /// <summary>
    /// Places a cave's Bat Guano the first time a player goes in through its entrance (D-033, user 2026-11-18: "locate the on-ground location of the
    /// entrance ... do the placement while the player is in the teleport screen"). A cave's rooms sit ~5000 m up and have no floors while nobody is
    /// there, so placing when the world generates the cave missed (Troll caves 0 of 6, mountain caves never ran). The location proxy's ZDO keeps a
    /// flag so a cave is filled once, and its seed keeps the roll the same for every player.
    /// </summary>
    internal static class CaveEntryPlacer
    {
        const float ArriveDistance = 25f, MaxWaitSeconds = 20f, SettleSeconds = 0.5f;

        /// <summary>Totals since the game started (read by the TestLab hook; nothing in the mod depends on them).</summary>
        internal static class Stats
        {
            internal static int Filled, Planned, Placed;
            internal static void Record(int planned, int placed) { Filled++; Planned += planned; Placed += placed; }
            internal static string Text() => "filled " + Filled + ", planned " + Planned + ", placed " + Placed;
        }

        /// <summary>Called after a successful dungeon teleport. Expected outcome: a placement run started when the target is a guano cave not yet filled (logged as a decision).</summary>
        internal static void OnEntered(Player player, Location entranceLocation, Vector3 target, bool goingIn)
        {
            if (player == null || player != Player.m_localPlayer || !ModSettings.FeatureOn(ModSettings.EnableGuano)) { if (RuleLog.On) ModLog.Every("Stats.OnEntered:35", 2d, "Stats.OnEntered: returns (line 35)"); return; }
            // the entrance's own location: looking it up by the target point picked an older cave that overlapped it (run entry4)
            Location loc = entranceLocation != null ? entranceLocation : Location.GetLocation(target, true);
            ZNetView proxy = loc != null ? loc.GetComponentInParent<ZNetView>() : null;
            string locName = loc != null ? Utils.GetPrefabName(loc.gameObject) : "none";
            DungeonGenerator gen = Generator(loc);
            string genName = gen != null ? Utils.GetPrefabName(gen.gameObject) : "";
            CaveKind? kind = CaveEntryRules.KindOf(locName, genName);
            CaveClaim claim = proxy != null ? proxy.GetComponent<CaveClaim>() : null;
            bool placed = claim != null && claim.Placed;
            bool ask = proxy != null && proxy.IsValid() && claim != null && CaveEntryRules.ShouldPlace(goingIn, kind, placed);
            ModLog.Decision("cave entry", ask ? "ask the owner to fill" : "nothing to place", "location " + locName + ", generator " + genName + ", kind " + (kind.HasValue ? kind.Value.ToString() : "none") + ", going in " + goingIn + ", placed before " + placed + ", proxy " + (proxy != null) + ", claim part " + (claim != null));
            if (!ask) return;
            // D-036: the proxy's owner decides who fills the cave (first asker wins), so two players entering at once cannot both place
            CaveKind k = kind.Value;
            claim.Request(granted =>
            {
                ModLog.Decision("cave fill", granted ? "this client fills the cave" : "another player filled it first", locName);
                if (granted && Plugin.Instance != null) Plugin.Instance.StartCoroutine(PlaceWhenArrived(player, target, loc, proxy, k));
            });
        }

        static IEnumerator PlaceWhenArrived(Player player, Vector3 target, Location loc, ZNetView proxy, CaveKind kind)
        {
            float waited = 0f;
            while (waited < MaxWaitSeconds && player != null && (player.IsTeleporting() || Vector3.Distance(player.transform.position, target) > ArriveDistance))
            {
                waited += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(SettleSeconds);
            Physics.SyncTransforms();
            try
            {
                if (loc == null || proxy == null || !proxy.IsValid()) { ModLog.Warn("CaveEntryPlacer: the cave unloaded before placing (waited " + waited.ToString("0.0") + " s)"); yield break; }
                int seed = proxy.GetZDO().GetInt(ZDOVars.s_seed);
                List<Vector3> spots = Spots(loc, target);
                int count = GuanoPlacer.Place(kind, spots, seed, out int planned);
                Stats.Record(planned, count);
                ModLog.Check("cave guano placed of planned (" + Utils.GetPrefabName(loc.gameObject) + ")", count, planned, 0f, "deposits");
                ModLog.Decision("cave guano on entry", count + " deposit(s)", Utils.GetPrefabName(loc.gameObject) + " " + kind + ", seed " + seed + ", " + spots.Count + " spots, waited " + waited.ToString("0.0") + " s, player at " + (player != null ? player.transform.position.ToString("0") : "-"));
            }
            catch (NullReferenceException e)
            {
                ModLog.Warn("CaveEntryPlacer: " + e.GetType().FullName + " while placing in a cave that went away: " + e.Message + " - skipped (the cave stays marked as filled)");
            }
            catch (Exception e)
            {
                ModLog.Warn("CaveEntryPlacer: " + e.GetType().FullName + " while placing guano: " + e.Message + " - skipped (the cave stays marked as filled)");
            }
        }

        // m_generator is empty on a spawned copy of a crypt (seen in run entry4), so the generator is also looked for among the children.
        static DungeonGenerator Generator(Location loc)
        {
            if (RuleLog.On) ModLog.Every("Stats.Generator", 2d, $"Stats.Generator(loc={loc})");
            if (loc == null) { if (RuleLog.On) ModLog.Every("Stats.Generator:91", 2d, "Stats.Generator: returns null (line 91)"); return null; }
            { var returnedAt92 = loc.m_generator != null ? loc.m_generator : loc.GetComponentInChildren<DungeonGenerator>(true); if (RuleLog.On) ModLog.Every("Stats.Generator:92", 2d, $"Stats.Generator: returns {returnedAt92} (line 92)"); return returnedAt92; }
        }

        // Rooms of a generated dungeon, the fixed room pieces under a Troll cave's Interior, or else every Room within the interior radius of the
        // arrival point (crypt rooms are not always children of the location: 0 spots in run entry4).
        static List<Vector3> Spots(Location loc, Vector3 target)
        {
            var spots = new List<Vector3>();
            DungeonGenerator gen = Generator(loc);
            if (gen != null)
            {
                foreach (Room room in gen.GetComponentsInChildren<Room>()) spots.Add(room.transform.position);
                ModLog.Verbose("CaveEntryPlacer: " + spots.Count + " room(s) under the generator " + gen.name);
                if (spots.Count > 0) return spots;
            }
            Transform interior = loc.m_interiorTransform != null ? loc.m_interiorTransform : loc.transform.Find("Interior");
            if (interior != null)
            {
                foreach (Transform t in interior.GetComponentsInChildren<Transform>())
                    if (t.name.StartsWith("room", StringComparison.Ordinal) || (t.parent != null && t.parent.name.StartsWith("room", StringComparison.Ordinal))) spots.Add(t.position);
                ModLog.Verbose("CaveEntryPlacer: " + spots.Count + " room piece(s) under " + interior.name);
                if (spots.Count > 0) return spots;
            }
            float radius = Mathf.Max(loc.m_interiorRadius, 32f) + 16f;
            foreach (Room room in UnityEngine.Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
                if (Vector3.Distance(room.transform.position, target) <= radius) spots.Add(room.transform.position);
            ModLog.Verbose("CaveEntryPlacer: " + spots.Count + " room(s) within " + radius.ToString("0") + " m of the arrival point");
            return spots;
        }
    }
}
