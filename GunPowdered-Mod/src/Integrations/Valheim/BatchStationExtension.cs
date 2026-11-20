using System.Collections.Generic;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Integrations.Valheim
{
    /// <summary>
    /// One extension piece of an automatic batch station in the world (D-129 Leaching Station, D-137 Powder Mill). A station counts the DIFFERENT kinds
    /// of ITS extensions (same <see cref="Station"/>) within <see cref="Range"/> m (BatchStationRules.Step), like the game's own station extensions.
    /// The live pieces keep themselves in one list, so a station looks them up without a physics query. A piece that is still being placed (the build
    /// ghost) does not count.
    /// </summary>
    internal sealed class BatchStationExtension : MonoBehaviour
    {
        /// <summary>How far from the station an extension counts (m), like the game's 5 m for workbench extensions.</summary>
        internal const float Range = 5f;

        static readonly List<BatchStationExtension> Live = new List<BatchStationExtension>();

        // serialized fields: Unity copies only serialized fields when the game instantiates the piece from the registered prefab - a plain C# property
        // set on the prefab came back null on every placed piece, so no extension counted (found 2026-11-20 while adding ExtensionFire)
        [SerializeField] string _station;
        [SerializeField] string _kind;

        /// <summary>Which station this extension belongs to (the station's prefab name).</summary>
        internal string Station { get { return _station; } set { _station = value; } }

        /// <summary>The extension's kind: its prefab name. Two pieces of one kind count once.</summary>
        internal string Kind { get { return _kind; } set { _kind = value; } }

        bool Placed { get { var nview = GetComponent<ZNetView>(); return nview != null && nview.IsValid(); } }

        void OnEnable()
        {
            if (Kind == null) Kind = Utils.GetPrefabName(gameObject);
            if (Player.IsPlacementGhost(gameObject)) { if (RuleLog.IsOn()) ModLog.Verbose($"BatchStationExtension.OnEnable: {Kind} is a build ghost, not counted - returns"); return; }
            if (!Live.Contains(this)) Live.Add(this);
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStationExtension.OnEnable: {Kind} for {Station} at {transform.position} counted, {Live.Count} live extension(s)");
        }

        void OnDisable()
        {
            Live.Remove(this);
            if (RuleLog.IsOn()) ModLog.Verbose($"BatchStationExtension.OnDisable: {Kind} for {Station} removed, {Live.Count} live extension(s)");
        }

        /// <summary>The kinds of the extensions of one station type within <see cref="Range"/> m of a point (duplicates included; the rules count each kind once).</summary>
        internal static List<string> KindsNear(string station, Vector3 point)
        {
            var kinds = new List<string>();
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                BatchStationExtension e = Live[i];
                if (e == null) { Live.RemoveAt(i); continue; }
                if (!e.Placed) continue;                                       // a build ghost (enabled before the game marks it) never counts
                Vector3 d = e.transform.position - point;
                if (e.Station == station && StationPlacementRules.InRange(d.x, d.y, d.z, Range)) kinds.Add(e.Kind);   // D-140: a 5 m bubble, up and down count
            }
            if (RuleLog.IsOn()) ModLog.Every("BatchStationExtension.KindsNear:" + station, 2d, $"BatchStationExtension.KindsNear({station}, {point}): returns {kinds.Count} of {Live.Count} live extension(s)");
            return kinds;
        }
    }
}
