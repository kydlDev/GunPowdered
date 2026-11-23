// kydl:read-only-observer - no creature is moved, commanded or spawned here; the patch only lists the dynamite crater as a terrain operation
using System;
using HarmonyLib;
using KydlCore.Common;
using GunPowdered.Features.Dynamite.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// The game digs only with terrain operations it knows: a TerrainOp sends its prefab's hash, and the receiver looks the settings up in
    /// ObjectDB.m_terrainOpsByHash ("Failed to deserialize TerrainOp settings ... cancelling TerrainOp" when it is missing - lab a7dyn4, D-103).
    /// After every ObjectDB register rebuild the dynamite crater is added to that list and that table.
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), "UpdateRegisters")]
    internal static class DynamiteTerrainPatch
    {
        static void Postfix(ObjectDB __instance)
        {
            try
            {
                bool listed = DynamiteSetup.ListCrater(__instance);
                ModLog.Verbose("DynamiteTerrainPatch: crater listed as a terrain operation " + listed);
            }
            catch (NullReferenceException e)
            {
                ModLog.Warn("DynamiteTerrainPatch: " + e.Message + " - dynamite will not dig a crater");
            }
            catch (Exception e)
            {
                ModLog.Warn("DynamiteTerrainPatch: " + e.GetType().FullName + ": " + e.Message + " - dynamite will not dig a crater" + "\n" + e.StackTrace);
            }
        }
    }
}
