
using KydlCore.Core;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;
using KydlCore.Common;

namespace GunPowdered.Features.ExplosiveArrows.Infrastructure
{
    /// <summary>
    /// The Explosive Arrow (D-045 item 5, D-076): the item, cloned from a vanilla placeholder by the ItemRegistry. This adapter queues it and turns its recipe on
    /// and off with [General] Enable Explosive Arrows. Firing it from a bow and the burst on impact come with the next T-009d stages.
    /// </summary>
    internal sealed class ExplosiveArrowsFeature : IFeature
    {
        internal const string FeatureId = "arrows";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableExplosiveArrows);

        internal ExplosiveArrowsFeature()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ExplosiveArrowsFeature.ExplosiveArrowsFeature()");
            ItemRegistry.Register(FeatureId, ItemCatalog.ExplosiveArrow);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}
