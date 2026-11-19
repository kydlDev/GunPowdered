namespace GunPowdered.Features.Musket.Domain
{
    /// <summary>
    /// The Iron Musket's magazine and firing rules (D-080, D-081, user 2026-11-19), pure so the unit tests check them. The player loads PAPER CARTRIDGES (never loose
    /// bullets), at most <see cref="Capacity"/> at a time. The player may reload whenever fewer than <see cref="Capacity"/> are loaded; the load never goes above it.
    /// A shot uses one loaded cartridge: only the bullet leaves the barrel, the paper and powder burn up in the pan.
    /// </summary>
    internal static class MusketRules
    {
        /// <summary>The most cartridges the musket holds (user: "the player loads 5 cartridges at any time ... they never go above 5").</summary>
        internal const int Capacity = 5;

        internal const string Full = "The Iron Musket is fully loaded", NoCartridges = "You need Paper Cartridges", Empty = "The Iron Musket is empty: reload it";

        /// <summary>How many cartridges one reload moves from the backpack into the musket: as many as fit and as many as the player carries. Never below 0.</summary>
        internal static int CartridgesToLoad(int loaded, int carried)
        {
            int free = Capacity - ClampLoaded(loaded);
            if (free <= 0 || carried <= 0) return 0;
            return carried < free ? carried : free;
        }

        /// <summary>Why a reload is refused (null = allowed): the musket is full, or the player carries no cartridge.</summary>
        internal static string WhyNotReload(int loaded, int carried)
        {
            if (ClampLoaded(loaded) >= Capacity) return Full;
            return carried >= 1 ? null : NoCartridges;
        }

        /// <summary>Why a shot is refused (null = allowed): nothing is loaded.</summary>
        internal static string WhyNotFire(int loaded)
        {
            return ClampLoaded(loaded) >= 1 ? null : Empty;
        }

        /// <summary>The load after one shot: one cartridge less, never below 0.</summary>
        internal static int AfterShot(int loaded)
        {
            int l = ClampLoaded(loaded);
            return l > 0 ? l - 1 : 0;
        }

        /// <summary>The load after a reload from <paramref name="carried"/> cartridges: it grows by <see cref="CartridgesToLoad"/> and never passes <see cref="Capacity"/>.</summary>
        internal static int AfterReload(int loaded, int carried)
        {
            return ClampLoaded(loaded) + CartridgesToLoad(loaded, carried);
        }

        /// <summary>A stored load outside 0..Capacity (a damaged save, a changed constant) counts as the nearest valid load.</summary>
        internal static int ClampLoaded(int loaded)
        {
            if (loaded < 0) return 0;
            return loaded > Capacity ? Capacity : loaded;
        }
    }
}
