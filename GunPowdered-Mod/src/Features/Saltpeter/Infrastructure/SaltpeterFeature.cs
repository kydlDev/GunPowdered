using KydlCore.Core;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;
using KydlCore.Common;

namespace GunPowdered.Features.Saltpeter.Infrastructure
{
    /// <summary>
    /// Saltpeter: the Leaching Station (LeachingVatRules, D-024) and the Saltpeter item. The items, pieces and deposits are cloned from vanilla placeholders by the ItemRegistry; this adapter queues them and turns their
    /// recipes and pieces on and off with the feature switch. The behaviour (what the pieces do in the world) comes with PLAN T-007.
    /// </summary>
    internal sealed class SaltpeterFeature : IFeature
    {
        internal const string FeatureId = "saltpeter";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableSaltpeter);

        internal SaltpeterFeature()
        {
            if (RuleLog.On) ModLog.Verbose($"SaltpeterFeature.SaltpeterFeature()");
            ItemRegistry.Register(FeatureId, ItemCatalog.Saltpeter, ItemCatalog.LeachingVat);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}