using KydlCore.Core;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Integrations.Items;

namespace GunPowdered.Features.Grenades.Infrastructure
{
    /// <summary>
    /// The Spiked Grenade (D-045 item 1, D-047): the item (cloned from the Ooze Bomb for its throw), its own flight and blast prefabs (GrenadeSetup) and
    /// the throwing arc (ThrowArcView). [General] Enable Grenades turns the recipe and the arc on and off at once.
    /// </summary>
    internal sealed class GrenadesFeature : IFeature
    {
        internal const string FeatureId = "grenades";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableGrenades);
        ThrowArcView _view;

        internal GrenadesFeature()
        {
            if (RuleLog.On) ModLog.Verbose($"GrenadesFeature.GrenadesFeature()");
            ItemRegistry.Register(FeatureId, ItemCatalog.SpikedGrenade);
        }

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => Switch(true)); }

        public bool Stop() { return _lifecycle.Stop(() => Switch(false)); }

        // the item switch and the arc view both always run (no short-circuit): a failed item switch must not leave the arc view in the old state
        bool Switch(bool on)
        {
            bool items = ItemRegistry.SetEnabled(FeatureId, on);
            bool arc = ShowArc(on);
            ModLog.Verbose("GrenadesFeature: switched " + (on ? "on" : "off") + ", items " + items + ", arc view " + arc);
            return items && arc;
        }

        // the arc view lives while the feature runs (it also checks the switch itself every frame)
        bool ShowArc(bool on)
        {
            if (on && _view == null) _view = ThrowArcView.Create();
            else if (!on && _view != null)
            {
                Object.Destroy(_view.gameObject);
                _view = null;
            }
            ModLog.Verbose("GrenadesFeature: arc view " + (on ? "made" : "removed"));
            return on ? _view != null : _view == null;
        }
    }
}
