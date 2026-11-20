using KydlCore.Core;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;
using KydlCore.Common;

namespace GunPowdered.Features.Gunpowder.Infrastructure
{
    /// <summary>
    /// Gunpowder: the Powder Mill station and the Gunpowder item (GunpowderRules). The items, pieces and deposits are cloned from vanilla placeholders by the ItemRegistry; this adapter queues them and turns their
    /// recipes and pieces on and off with the feature switch. The behaviour (what the pieces do in the world) comes with PLAN T-007.
    /// </summary>
    internal sealed class GunpowderFeature : IFeature
    {
        internal const string FeatureId = "gunpowder";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableGunpowder);

        internal GunpowderFeature()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"GunpowderFeature.GunpowderFeature()");
            ItemRegistry.Register(FeatureId, ItemCatalog.Gunpowder, ItemCatalog.PowderMill);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}