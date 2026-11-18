// kydl:read-only-observer - no creature is moved, commanded or spawned here; the patch only extends the vegetation list of one alt biome
using System;
using HarmonyLib;
using Jotunn.Managers;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// The swamp alternative areas where Bat_Swamp can spawn are the alt biome entry named "Bat Swamp" (D-018). The game places an alt biome's own
    /// vegetation list by itself, so the Bat Guano deposits are added to that list once, when the world system starts. The user's chance is per area; the
    /// game places per zone, so the amount per zone is a Tunable (SwampGuanoPerZone) to tune in game (D-016).
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), "Start")]
    internal static class BatSwampPatch
    {
        const string AltName = "Bat Swamp";
        static bool _done;

        static void Postfix(ZoneSystem __instance)
        {
            try
            {
                if (_done) return;
                GameObject deposit = PrefabManager.Instance.GetPrefab(GunPowderedPrefabs.GuanoDeposit);
                if (deposit == null) { ModLog.Warn("BatSwampPatch: the guano deposit prefab is not there yet"); return; }

                int added = 0;
                foreach (AltBiomeList list in __instance.m_altBiomeLists)
                    foreach (AltBiome alt in list.m_alts)
                    {
                        if (alt.m_name != AltName) continue;
                        alt.m_addVegetation.Add(new ZoneSystem.ZoneVegetation
                        {
                            m_name = GunPowderedPrefabs.GuanoDeposit,
                            m_prefab = deposit,
                            m_enable = true,
                            m_min = 0f,
                            m_max = ModSettings.SwampGuanoPerZone.Value,
                            m_scaleMin = 1f,
                            m_scaleMax = 1f,
                            m_biome = alt.m_biome,
                            m_biomeArea = Heightmap.BiomeArea.Everything,
                            m_blockCheck = true,
                            m_minAltitude = 0.5f,
                            m_maxAltitude = 1000f,
                            m_maxTilt = 35f,
                            m_groupSizeMin = 1,
                            m_groupSizeMax = 1
                        });
                        added++;
                    }
                _done = added > 0;
                ModLog.Decision("bat swamp guano", added + " vegetation entr" + (added == 1 ? "y" : "ies") + " added", "per zone " + ModSettings.SwampGuanoPerZone.Value);
            }
            catch (Exception e)
            {
                ModLog.Warn("BatSwampPatch: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
