using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Features.Saltpeter.Infrastructure
{
    /// <summary>
    /// One Leaching Station extension piece in the world (D-129): the Second Leaching Barrel, Settling Tank, Black Metal-banded Boiler, Dvergr Drip Pipes or
    /// Flametal Heater. Every station counts the DIFFERENT kinds within <see cref="Range"/> m (LeachingUpgradeRules.Step), like the game's own station
    /// extensions. The live pieces keep themselves in one list, so a station looks them up without a physics query. A piece that is still being placed
    /// (the build ghost) does not count.
    /// </summary>
    internal sealed class LeachingExtension : MonoBehaviour
    {
        /// <summary>How far from the station an extension counts (m), like the game's 5 m for workbench extensions.</summary>
        internal const float Range = 5f;

        static readonly List<LeachingExtension> Live = new List<LeachingExtension>();

        /// <summary>The extension's kind: its prefab name. Two pieces of one kind count once.</summary>
        internal string Kind { get; set; }

        void OnEnable()
        {
            if (Kind == null) Kind = Utils.GetPrefabName(gameObject);
            if (Player.IsPlacementGhost(gameObject)) { if (RuleLog.IsOn()) ModLog.Verbose($"LeachingExtension.OnEnable: {Kind} is a build ghost, not counted - returns"); return; }
            if (!Live.Contains(this)) Live.Add(this);
            if (RuleLog.IsOn()) ModLog.Verbose($"LeachingExtension.OnEnable: {Kind} at {transform.position} counted, {Live.Count} live extension(s)");
        }

        void OnDisable()
        {
            Live.Remove(this);
            if (RuleLog.IsOn()) ModLog.Verbose($"LeachingExtension.OnDisable: {Kind} removed, {Live.Count} live extension(s)");
        }

        /// <summary>The kinds of the extensions within <see cref="Range"/> m of a point (duplicates included; the rules count each kind once).</summary>
        internal static List<string> KindsNear(Vector3 point)
        {
            var kinds = new List<string>();
            float rangeSq = Range * Range;
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                LeachingExtension e = Live[i];
                if (e == null) { Live.RemoveAt(i); continue; }
                if ((e.transform.position - point).sqrMagnitude <= rangeSq) kinds.Add(e.Kind);
            }
            if (RuleLog.IsOn()) ModLog.Every("LeachingExtension.KindsNear", 2d, $"LeachingExtension.KindsNear({point}): returns {kinds.Count} of {Live.Count} live extension(s)");
            return kinds;
        }
    }
}
