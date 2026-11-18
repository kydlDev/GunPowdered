
using KydlCore.Core;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// The Ashlands Mobile Cannon and the Asksvin Harness that tows it (D-045 item 4, D-063, D-064, D-076): the build piece and the harness item, cloned from vanilla
    /// placeholders by the ItemRegistry. This adapter queues them and turns the recipe and the piece on and off with [General] Enable Mobile Cannon. Loading one
    /// shot, the elevation crank, the rolling wheels and the tow joint come with the next T-009f stages.
    /// </summary>
    internal sealed class MobileCannonFeature : IFeature
    {
        internal const string FeatureId = "mobilecannon";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableMobileCannon);

        internal MobileCannonFeature()
        {
            ItemRegistry.Register(FeatureId, ItemCatalog.MobileCannon, ItemCatalog.AsksvinHarness);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}
