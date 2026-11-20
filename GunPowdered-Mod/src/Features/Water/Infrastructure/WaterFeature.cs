using KydlCore.Core;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;
using KydlCore.Common;

namespace GunPowdered.Features.Water.Infrastructure
{
    /// <summary>
    /// Water: the Water item, the Bucket (empty / full) and the Rain Barrel (RainBarrelRules). The items, pieces and deposits are cloned from vanilla placeholders by the ItemRegistry; this adapter queues them and turns their
    /// recipes and pieces on and off with the feature switch. The behaviour (what the pieces do in the world) comes with PLAN T-007.
    /// </summary>
    internal sealed class WaterFeature : IFeature
    {
        internal const string FeatureId = "water";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableWater);

        internal WaterFeature()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"WaterFeature.WaterFeature()");
            ItemRegistry.Register(FeatureId, ItemCatalog.Water, ItemCatalog.Bucket, ItemCatalog.BucketFull, ItemCatalog.RainBarrel);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}