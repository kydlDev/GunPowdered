
using KydlCore.Core;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;

namespace GunPowdered.Features.Cannon.Infrastructure
{
    /// <summary>
    /// The mounted Hand Cannon (D-051, D-052, D-053): the Cannon item, the Cannon Ball and the Cannon Stock it is mounted on. The items and the piece are cloned
    /// from vanilla placeholders by the ItemRegistry; this adapter queues them and turns their recipes and the piece on and off with [General] Enable Cannons (shared with the Mobile Cannon).
    /// The behaviour (mounting, loading at the muzzle, aiming, the torch fuse, the flight) comes with the next T-009b stages.
    /// </summary>
    internal sealed class CannonFeature : IFeature
    {
        internal const string FeatureId = "cannon";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableCannon);

        internal CannonFeature()
        {
            ItemRegistry.Register(FeatureId, ItemCatalog.CannonBall, ItemCatalog.Cannon, ItemCatalog.CannonStock);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}
