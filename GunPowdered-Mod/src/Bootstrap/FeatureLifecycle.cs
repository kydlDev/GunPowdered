using System;
using BepInEx.Configuration;
using KydlCore.Common;
using GunPowdered.Configuration;
using GunPowdered.Core;

namespace GunPowdered.Bootstrap
{
    /// <summary>
    /// What every GunPowdered feature adapter shares, by composition (the architecture test wants each adapter to declare <c>: IFeature</c> itself):
    /// its id, its gate (Enable Mod AND its own [General] Enable switch, read at call time so a config change applies at once, PROJECT_RULES 11 / 17)
    /// and the state log lines of Start / Stop (PROJECT_RULES 28). A feature passes what it adds to the game (recipes, pieces,
    /// patches, hub tabs) as the apply function of <see cref="Start"/> and <see cref="Stop"/>, which return the check of the state they were asked for.
    /// </summary>
    internal sealed class FeatureLifecycle
    {
        readonly Func<ConfigEntry<bool>> _ownEnable;
        bool _running;

        /// <param name="id">The feature id (FeatureRegistry key).</param>
        /// <param name="ownEnable">Reads the feature's own [General] Enable switch (a function, because the entry is bound after the field exists).</param>
        internal FeatureLifecycle(string id, Func<ConfigEntry<bool>> ownEnable)
        {
            if (RuleLog.On) ModLog.Verbose($"FeatureLifecycle.FeatureLifecycle(id={RuleLog.Shown(id)}, ownEnable={RuleLog.Shown(ownEnable)})");
            Id = id;
            _ownEnable = ownEnable;
        }

        internal string Id { get; }

        internal bool IsRunning { get { return _running; } }

        internal bool IsEnabled()
        {
            if (RuleLog.On) ModLog.Verbose($"FeatureLifecycle.IsEnabled()");
            { var returnedAt36 = ModSettings.FeatureOn(_ownEnable()); if (RuleLog.On) ModLog.Verbose($"FeatureLifecycle.IsEnabled: returns {RuleLog.Shown(returnedAt36)} (line 36)"); return returnedAt36; }
        }

        /// <summary>Expected outcome: true when the feature is running afterwards. <paramref name="apply"/> adds its contributions and returns whether they are in place.</summary>
        internal bool Start(Func<bool> apply)
        {
            bool inPlace = apply();
            ModLog.State(Id + " feature", "stopped", inPlace ? "started" : "start failed", "Enable Mod and Enable " + Id + " are on");
            _running = inPlace;
            { var returnedAt45 = _running; if (RuleLog.On) ModLog.Every("FeatureLifecycle.Start:45", 2d, $"FeatureLifecycle.Start: returns {RuleLog.Shown(returnedAt45)} (line 45)"); return returnedAt45; }
        }

        /// <summary>Expected outcome: true when the feature is stopped. <paramref name="apply"/> takes its contributions away and returns whether they are gone.</summary>
        internal bool Stop(Func<bool> apply)
        {
            bool gone = apply();
            ModLog.State(Id + " feature", "started", gone ? "stopped" : "stop failed", "a switch was turned off or the mod unloads");
            _running = !gone;
            { var returnedAt54 = !_running; if (RuleLog.On) ModLog.Every("FeatureLifecycle.Stop:54", 2d, $"FeatureLifecycle.Stop: returns {RuleLog.Shown(returnedAt54)} (line 54)"); return returnedAt54; }
        }
    }
}