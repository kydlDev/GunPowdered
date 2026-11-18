using KydlCore.Core;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;

namespace GunPowdered.Features.Guano.Infrastructure
{
    /// <summary>
    /// Bat guano: deposits in caves (rolled by GuanoDepositRules), and the Bat Guano item (deposits are mined with a pickaxe). The items, pieces and deposits are cloned from vanilla placeholders by the ItemRegistry; this adapter queues them and turns their
    /// recipes and pieces on and off with the feature switch. The behaviour (what the pieces do in the world) comes with PLAN T-007.
    /// </summary>
    internal sealed class GuanoFeature : IFeature
    {
        internal const string FeatureId = "guano";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableGuano);

        internal GuanoFeature()
        {
            ItemRegistry.Register(FeatureId, ItemCatalog.BatGuano, ItemCatalog.GuanoDeposit);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}