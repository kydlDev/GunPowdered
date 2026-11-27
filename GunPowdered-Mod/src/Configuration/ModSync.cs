using KydlCore.Common;
using KydlCore.ServerSync;
using GunPowdered.Bootstrap;

namespace GunPowdered.Configuration
{
    /// <summary>
    /// Network compatibility through KydlCore 1.2.0's KyNetwork (ServerSync inside KydlCore; no Jotunn). EveryoneMustHave: every player on a server needs this
    /// mod in exactly this version (the old EveryoneMustHaveMod + Patch): the mod adds items, pieces, deposits and weapons that live in the shared world and talk
    /// through RPCs and saved values.
    /// </summary>
    internal static class ModSync
    {
        /// <summary>The mod's ServerSync instance (made by KydlCore on first use, which is <see cref="Start"/> in Plugin.Awake).</summary>
        public static readonly ConfigSync Sync = KyNetwork.Register(Plugin.ModGuid, Plugin.ModName, Plugin.ModVersion, KyNetworkLevel.EveryoneMustHave);

        /// <summary>Starts the version check (the first use of this class does it). Called once in Plugin.Awake. Expected outcome: true when the check is registered.</summary>
        public static bool Start()
        {
            ModLog.Verbose($"ModSync.Start: network check active for {Sync.Name} {Sync.CurrentVersion} (required on every player: {Sync.ModRequired})");
            return Sync != null;
        }
    }
}
