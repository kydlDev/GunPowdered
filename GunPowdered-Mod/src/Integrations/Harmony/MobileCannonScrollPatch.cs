// kydl:read-only-observer - no creature is moved, commanded or spawned here; the patch only takes the mouse wheel while the player looks at a cannon crank
using System;
using System.Reflection;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.MobileCannon.Domain;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Integrations.HarmonyPatches
{
    /// <summary>
    /// D-104: while the local player looks at a Mobile Cannon crank, the mouse wheel turns the crank (one notch per frame with a wheel movement) and the game
    /// gets 0 (no camera zoom). Patches every float ZInput.GetMouseScrollWheel overload.
    /// </summary>
    [HarmonyPatch]
    internal static class MobileCannonScrollPatch
    {
        static int _lastFrame = -1;

        static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(ZInput)))
                if (m.Name == "GetMouseScrollWheel" && m.ReturnType == typeof(float) && !m.IsGenericMethod) yield return m;
        }

        static void Postfix(ref float __result)
        {
            try
            {
                if (__result == 0f) return;
                MobileCannonCrank crank = MobileCannonCrank.Hovered();
                if (crank == null || crank.Aim == null) return;
                if (_lastFrame != Time.frameCount)
                {
                    _lastFrame = Time.frameCount;
                    int notches = MobileCannonAimRules.Notches(__result);
                    bool sent = crank.Aim.Turn(notches);
                    ModLog.Verbose("MobileCannonScrollPatch: wheel " + __result.ToString("0.00") + " -> " + notches + " notch(es), sent " + sent);
                }
                __result = 0f;
            }
            catch (NullReferenceException e)
            {
                ModLog.Warn("MobileCannonScrollPatch: " + e.Message + " - the wheel goes to the game");
            }
        }
    }
}
