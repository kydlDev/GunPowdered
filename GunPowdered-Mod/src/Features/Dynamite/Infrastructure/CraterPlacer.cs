using GunPowdered.Core;
using GunPowdered.Features.Dynamite.Domain;
using KydlCore.Common;
using UnityEngine;

namespace GunPowdered.Features.Dynamite.Infrastructure
{
    /// <summary>
    /// D-182: puts a blast's hole (a crater terrain operation) on the GROUND under the blast, not at the blast's own height: a charge lying on a rock dug
    /// nothing because the game lowers the ground only below the operation's height (<see cref="CraterRules"/>). Used by the dynamite, the pack and the grenade.
    /// </summary>
    internal static class CraterPlacer
    {
        /// <summary>The crater sinks this far (m) into the ground so its edge blends with the surface (the old placement used the same offset under the charge).</summary>
        const float Sink = 0.15f;

        /// <summary>Creates <paramref name="crater"/> under <paramref name="at"/> when the ground lies within <paramref name="depth"/> m below it. Expected outcome: true when a hole was made.</summary>
        internal static bool Place(GameObject crater, Vector3 at, float depth, string what)
        {
            if (crater == null) { ModLog.Warn($"CraterPlacer: {what} has no crater prefab - no hole at {at:F1}"); return false; }
            float ground = ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out float h) ? h : float.NaN;
            float top = CraterRules.Top(at.y, ground, depth);
            if (float.IsNaN(top))
            {
                ModLog.Decision("blast hole", "none", $"{what} at {at:F1}, ground {(float.IsNaN(ground) ? "<none>" : ground.ToString("F1"))} m, {at.y - ground:F1} m below the blast, depth {depth:F1} m - out of reach");
                return false;
            }
            Object.Instantiate(crater, new Vector3(at.x, top - Sink, at.z), Quaternion.identity);
            ModLog.Decision("blast hole", "dug", $"{what} at {at:F1}, ground {ground:F1} m, hole from {top:F1} m, {depth:F1} m deep");
            return true;
        }
    }
}
