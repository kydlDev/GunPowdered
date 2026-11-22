using KydlCore.Core;
using KydlCore.Common;
using GunPowdered.Configuration;
using GunPowdered.Bootstrap;
using GunPowdered.Core;

namespace GunPowdered.Features.Skilling.Infrastructure
{
    /// <summary>
    /// D-157 (user 2026-11-23): the Explosives, Muskets and Cannons skills. The skills themselves are registered once in Plugin.Awake (GunSkills.Register: the
    /// game needs them before any character loads, and a skill cannot be taken away while playing without losing the players' levels), so this adapter only
    /// follows [General] Enable Mod; the weapons read their skill through GunSkills / SkillRules.
    /// </summary>
    internal sealed class SkillingFeature : IFeature
    {
        internal const string FeatureId = "skills";
        readonly FeatureLifecycle _lifecycle = new FeatureLifecycle(FeatureId, () => ModSettings.EnableMod);

        public string Id { get { return _lifecycle.Id; } }

        public bool IsEnabled() { return _lifecycle.IsEnabled(); }

        public bool Start() { return _lifecycle.Start(() => GunSkills.Register()); }

        public bool Stop() { return _lifecycle.Stop(() => { ModLog.Verbose("SkillingFeature: the skills stay registered (players keep their levels)"); return true; }); }
    }
}
