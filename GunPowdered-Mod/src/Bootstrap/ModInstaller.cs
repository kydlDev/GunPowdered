using BepInEx.Configuration;
using KydlCore.Common;
using KydlCore.Core;
using GunPowdered.Features.Cannon.Infrastructure;
using GunPowdered.Features.Dynamite.Infrastructure;
using GunPowdered.Features.ExplosiveArrows.Infrastructure;
using GunPowdered.Features.Example.Infrastructure;
using GunPowdered.Features.Grenades.Infrastructure;
using GunPowdered.Features.Gunpowder.Infrastructure;
using GunPowdered.Features.Guano.Infrastructure;
using GunPowdered.Features.MobileCannon.Infrastructure;
using GunPowdered.Features.Musket.Infrastructure;
using GunPowdered.Features.Saltpeter.Infrastructure;
using GunPowdered.Features.Sulfur.Infrastructure;
using GunPowdered.Features.Water.Infrastructure;
using GunPowdered.Presentation.Hub.Registration;
using GunPowdered.Core;

namespace GunPowdered.Bootstrap
{
    /// <summary>
    /// The composition root (spec "Bootstrap"): builds the shared services (<see cref="DependencyRegistry"/>), the mod's KY hub page and every
    /// feature, and hands the features to the <see cref="FeatureRegistry"/> (MIGRATION_DECISIONS A4), which starts / stops each one live by
    /// its Enable switch. A new feature = one <c>Add(new XFeature(...))</c> line here.
    /// </summary>
    internal sealed class ModInstaller
    {
        /// The running installer (test hooks and Harmony patches ask it which features run). Null before Awake / after unload.
        internal static ModInstaller Current { get; private set; }

        readonly ConfigFile _config;
        internal DependencyRegistry Deps { get; }
        internal FeatureRegistry Features { get; }

        internal ModInstaller(ConfigFile config)
        {
            if (RuleLog.On) ModLog.Verbose($"ModInstaller.ModInstaller(config={RuleLog.Shown(config)})");
            _config = config;
            Deps = new DependencyRegistry();
            Features = new FeatureRegistry(Deps.Trace);
        }

        /// Expected outcome: true when the hub page is registered and every enabled feature started.
        internal bool Install()
        {
            Current = this;
            bool page = HubTabs.RegisterModPage(_config);
            Add(new ExampleFeature(Deps));
            Add(new GuanoFeature());
            Add(new WaterFeature());
            Add(new SaltpeterFeature());
            Add(new SulfurFeature());
            Add(new GunpowderFeature());
            Add(new GrenadesFeature());
            Add(new CannonFeature());
            Add(new MusketFeature());
            Add(new ExplosiveArrowsFeature());
            Add(new DynamiteFeature());
            Add(new MobileCannonFeature());
            bool started = Refresh();
            ModLog.Verbose("ModInstaller: installed " + Features.Count + " feature(s), hub page " + page + ", started " + started);
            return page && started;
        }

        /// Called on every config change. Expected outcome: true when every feature is in the state its switch asks for.
        internal bool Refresh()
        {
            OperationResponse<int> r = Features.Refresh();
            if (r.Status == ResponseStatus.Failed) ModLog.Warn("ModInstaller: " + r.Reason);
            else ModLog.Verbose("ModInstaller: refresh - " + r);
            return r.IsSuccess;
        }

        /// Mod unload. Expected outcome: true when every feature stopped and the hub page is gone.
        internal bool Uninstall()
        {
            OperationResponse<int> r = Features.StopAll();
            if (!r.IsSuccess) ModLog.Warn("ModInstaller: " + r.Reason);
            bool gone = HubTabs.UnregisterModPage();
            if (Current == this) Current = null;
            { var returnedAt81 = r.IsSuccess && gone; if (RuleLog.On) ModLog.Verbose($"ModInstaller.Uninstall: returns {RuleLog.Shown(returnedAt81)} (line 81)"); return returnedAt81; }
        }

        void Add(IFeature feature)
        {
            OperationResponse<int> r = Features.Add(feature);
            if (!r.IsSuccess) ModLog.Error("ModInstaller: feature not added - " + r.Reason);
        }
    }
}
