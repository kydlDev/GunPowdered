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
            Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable += AttachCaveClaim;
        }

        // D-036: every location proxy gets the part that decides who fills a cave with guano (always attached; it does nothing until a player asks)
        static void AttachCaveClaim()
        {
            Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable -= AttachCaveClaim;
            UnityEngine.GameObject proxy = Jotunn.Managers.PrefabManager.Instance.GetPrefab("LocationProxy");
            bool added = proxy != null && (proxy.GetComponent<CaveClaim>() != null || proxy.AddComponent<CaveClaim>() != null);
            KydlCore.Common.ModLog.Verbose("GuanoFeature: CaveClaim on the LocationProxy prefab " + (added ? "added" : "NOT added (prefab " + (proxy != null) + ")"));
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => ItemRegistry.SetEnabled(FeatureId, true)); }

        public bool Stop() { return _lifecycle.Stop(() => ItemRegistry.SetEnabled(FeatureId, false)); }
    }
}