using System.Collections.Generic;
using GunPowdered.Core;
using GunPowdered.Features.Dynamite.Domain;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// D-182: a blast (HitData with a radius) on a mine rock (copper, tin, big rocks) hits EVERY rock piece within its reach. The game searches with a
    /// 128-collider buffer (<see cref="BlastRockRules"/>); when that buffer would be full this prefix does the same search with a big buffer and damages
    /// each piece once, exactly like the game does below the limit. Below the limit the game's own code runs.
    /// </summary>
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
    internal static class MineRockBlastPatch
    {
        static readonly Collider[] Found = new Collider[2048];
        static readonly HashSet<Collider> Pieces = new HashSet<Collider>();
        static readonly AccessTools.FieldRef<MineRock5, ZNetView> View = AccessTools.FieldRefAccess<MineRock5, ZNetView>("m_nview");
        static readonly AccessTools.FieldRef<MineRock5, bool> AllDestroyed = AccessTools.FieldRefAccess<MineRock5, bool>("m_allDestroyed");
        static readonly System.Reflection.FieldInfo Areas = AccessTools.Field(typeof(MineRock5), "m_hitAreas");      // List<MineRock5.HitArea>, a private type
        static readonly System.Func<MineRock5, Collider, int> AreaIndex = AccessTools.MethodDelegate<System.Func<MineRock5, Collider, int>>(AccessTools.Method(typeof(MineRock5), "GetAreaIndex"));
        static int _mask;

        static bool Prefix(MineRock5 __instance, HitData hit)
        {
            if (hit == null || hit.m_radius <= 0f || __instance == null) { if (RuleLog.Due("MineRockBlastPatch.Prefix:swing", 2d)) ModLog.Every("MineRockBlastPatch.Prefix:swing", 2d, "MineRockBlastPatch.Prefix: returns true (the game's code) because hit == null || hit.m_radius <= 0f || __instance == null (line 28)"); return true; }
            ZNetView view = View(__instance);
            if (view == null || !view.IsValid() || Areas == null || Areas.GetValue(__instance) == null) { if (RuleLog.Due("MineRockBlastPatch.Prefix:view", 2d)) ModLog.Every("MineRockBlastPatch.Prefix:view", 2d, "MineRockBlastPatch.Prefix: returns true (the game's code) because view == null || !view.IsValid() || Areas == null || Areas.GetValue(__instance) == null (line 30)"); return true; }
            if (_mask == 0) _mask = LayerMask.GetMask("piece", "Default", "static_solid", "Default_small", "terrain");     // the game's own MineRock5 mask
            int n = Physics.OverlapSphereNonAlloc(hit.m_point, hit.m_radius, Found, _mask);
            if (n == Found.Length && ModLog.IsDue("MineRockBlastPatch.full", 5d)) ModLog.Warn($"MineRockBlastPatch: {n} colliders fill the big buffer too - pieces of {__instance.name} may still be missed");
            if (!BlastRockRules.GameMissesPieces(n)) { if (RuleLog.Due("MineRockBlastPatch.Prefix:game", 2d)) ModLog.Every("MineRockBlastPatch.Prefix:game", 2d, $"MineRockBlastPatch.Prefix: returns true (the game's search) because {n} colliders in the blast on {__instance.name} fit its buffer (line 34)"); return true; }
            Pieces.Clear();
            Transform rock = __instance.transform;
            for (int i = 0; i < n; i++)
            {
                Transform parent = Found[i].transform.parent;
                if (parent == rock || (parent != null && parent.parent == rock)) Pieces.Add(Found[i]);
            }
            int hitPieces = 0;
            foreach (Collider piece in Pieces)
            {
                int area = AreaIndex(__instance, piece);
                if (area < 0) { if (RuleLog.Due("MineRockBlastPatch.Prefix:area", 2d)) ModLog.Every("MineRockBlastPatch.Prefix:area", 2d, "MineRockBlastPatch.Prefix: skips to the next item (continue) because area < 0 (line 46)"); continue; }
                hitPieces++;
                view.InvokeRPC("RPC_Damage", hit, area);
                if (AllDestroyed(__instance)) { if (RuleLog.Due("MineRockBlastPatch.Prefix:gone", 2d)) ModLog.Every("MineRockBlastPatch.Prefix:gone", 2d, "MineRockBlastPatch.Prefix: stops the loop (break) because AllDestroyed(__instance) (line 49)"); break; }
            }
            ModLog.Decision("blast on mine rock", "all pieces", $"{__instance.name}: {n} colliders in {hit.m_radius:0.#} m (the game's buffer holds {BlastRockRules.GameBuffer}), {Pieces.Count} rock pieces found, {hitPieces} hit");
            if (RuleLog.Due("MineRockBlastPatch.Prefix:done", 2d)) ModLog.Every("MineRockBlastPatch.Prefix:done", 2d, "MineRockBlastPatch.Prefix: returns false (the mod hit every piece) (line 52)");
            return false;
        }
    }
}
