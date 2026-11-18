namespace GunPowdered.Features.Cannon.Domain
{
    /// <summary>What a Cannon Stock holds, as its network object stores it.</summary>
    internal readonly struct CannonStatus
    {
        internal CannonStatus(bool mounted, bool loaded, bool lit)
        {
            Mounted = mounted;
            Loaded = loaded;
            Lit = lit;
        }

        internal bool Mounted { get; }
        internal bool Loaded { get; }
        /// <summary>The fuse burns (the shot follows on its own).</summary>
        internal bool Lit { get; }
    }

    /// <summary>
    /// Which action a player may take at a Cannon Stock (D-052 addendum, D-053), pure. Each check answers null when the action is allowed, else
    /// the reason shown to the player. Order of play: mount the cannon, load it at the muzzle (gunpowder + one Cannon Ball), aim from the stock,
    /// light the fuse with a torch in hand; the cannon can be taken off again (its load comes back too).
    /// </summary>
    internal static class CannonStateRules
    {
        internal const string NeedCannon = "You need a Hand Cannon to mount", AlreadyMounted = "A Hand Cannon is already mounted",
            NotMounted = "Mount a Hand Cannon first", AlreadyLoaded = "The Hand Cannon is already loaded", NotLoaded = "Load the Hand Cannon first",
            NeedBall = "You need a Cannon Ball", NeedTorch = "Hold a torch to light the fuse", FuseBurning = "The fuse is already burning";

        /// <summary>Mounting needs an empty stock and a Hand Cannon in the inventory.</summary>
        internal static string WhyNotMount(CannonStatus s, bool haveCannon)
        {
            if (s.Mounted) return AlreadyMounted;
            return haveCannon ? null : NeedCannon;
        }

        /// <summary>Taking the cannon off needs a mounted cannon whose fuse is not burning.</summary>
        internal static string WhyNotUnmount(CannonStatus s)
        {
            if (!s.Mounted) return NotMounted;
            return s.Lit ? FuseBurning : null;
        }

        /// <summary>Loading needs a mounted, empty cannon, powderNeed gunpowder and one ball. The gunpowder reason names how much is needed.</summary>
        internal static string WhyNotLoad(CannonStatus s, int powderHave, int powderNeed, int ballsHave)
        {
            if (!s.Mounted) return NotMounted;
            if (s.Loaded) return AlreadyLoaded;
            if (powderHave < powderNeed) return "You need " + powderNeed + " Gunpowder";
            return ballsHave >= 1 ? null : NeedBall;
        }

        /// <summary>Aiming needs a loaded cannon.</summary>
        internal static string WhyNotAim(CannonStatus s)
        {
            if (!s.Mounted) return NotMounted;
            return s.Loaded ? null : NotLoaded;
        }

        /// <summary>Lighting the fuse needs a loaded cannon, no fuse burning yet and a torch in hand.</summary>
        internal static string WhyNotLight(CannonStatus s, bool torchInHand)
        {
            string aim = WhyNotAim(s);
            if (aim != null) return aim;
            if (s.Lit) return FuseBurning;
            return torchInHand ? null : NeedTorch;
        }
    }
}
