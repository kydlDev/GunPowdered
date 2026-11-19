
using KydlCore.Core;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;

namespace GunPowdered.Features.Dynamite.Infrastructure
{
    /// <summary>
    /// Dynamite (D-045 item 6, D-076): the item, cloned from a vanilla placeholder by the ItemRegistry. This adapter queues it and turns its recipe on and off
    /// with [General] Enable Dynamite. Setting it down, lighting it with a Torch, the blast and the crater: DynamiteSetup / DynamiteCharge (D-100).
    /// </summary>
    internal sealed class DynamiteFeature : IFeature
    {
        internal const string FeatureId = "dynamite";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableDynamite);

        internal DynamiteFeature()
        {
            ItemRegistry.Register(FeatureId, ItemCatalog.Dynamite);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}
