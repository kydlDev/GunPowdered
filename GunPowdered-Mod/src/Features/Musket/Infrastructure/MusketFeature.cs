
using KydlCore.Core;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;

namespace GunPowdered.Features.Musket.Infrastructure
{
    /// <summary>
    /// The Iron Musket (D-045 item 3, D-076): the item and its ammunition chain (Paper, Iron Bullet, Paper Cartridge: D-080 / D-081), cloned from vanilla placeholders by the ItemRegistry. This adapter queues them and turns its recipe on and
    /// off with [General] Enable Musket. Loading, aiming and the shot come with the next T-009c stages.
    /// </summary>
    internal sealed class MusketFeature : IFeature
    {
        internal const string FeatureId = "musket";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableMusket);

        internal MusketFeature()
        {
            ItemRegistry.Register(FeatureId, ItemCatalog.Musket, ItemCatalog.Paper, ItemCatalog.IronBullet, ItemCatalog.PaperCartridge);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}
