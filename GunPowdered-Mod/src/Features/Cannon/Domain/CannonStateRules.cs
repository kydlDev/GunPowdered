using GunPowdered.Core;
namespace GunPowdered.Features.Cannon.Domain
{
    /// <summary>What a Cannon Stock holds, as its network object stores it.</summary>
    internal readonly struct CannonStatus
    {
        internal CannonStatus(bool mounted, bool loaded, bool lit)
        {
            if (RuleLog.On) RuleLog.Every("CannonStatus.CannonStatus", 2d, $"CannonStatus.CannonStatus(mounted={RuleLog.Shown(mounted)}, loaded={RuleLog.Shown(loaded)}, lit={RuleLog.Shown(lit)})");
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
            if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotMount", 2d, $"CannonStateRules.WhyNotMount(s={RuleLog.Shown(s)}, haveCannon={RuleLog.Shown(haveCannon)})");
            if (s.Mounted) { var returnedAt36 = AlreadyMounted; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotMount:36", 2d, $"CannonStateRules.WhyNotMount: returns {RuleLog.Shown(returnedAt36)} (line 36)"); return returnedAt36; }
            { var returnedAt37 = haveCannon ? null : NeedCannon; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotMount:37", 2d, $"CannonStateRules.WhyNotMount: returns {RuleLog.Shown(returnedAt37)} (line 37)"); return returnedAt37; }
        }

        /// <summary>Taking the cannon off needs a mounted cannon whose fuse is not burning.</summary>
        internal static string WhyNotUnmount(CannonStatus s)
        {
            if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotUnmount", 2d, $"CannonStateRules.WhyNotUnmount(s={RuleLog.Shown(s)})");
            if (!s.Mounted) { var returnedAt44 = NotMounted; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotUnmount:44", 2d, $"CannonStateRules.WhyNotUnmount: returns {RuleLog.Shown(returnedAt44)} (line 44)"); return returnedAt44; }
            { var returnedAt45 = s.Lit ? FuseBurning : null; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotUnmount:45", 2d, $"CannonStateRules.WhyNotUnmount: returns {RuleLog.Shown(returnedAt45)} (line 45)"); return returnedAt45; }
        }

        /// <summary>Loading needs a mounted, empty cannon, powderNeed gunpowder and one ball. The gunpowder reason names how much is needed.</summary>
        internal static string WhyNotLoad(CannonStatus s, int powderHave, int powderNeed, int ballsHave)
        {
            if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotLoad", 2d, $"CannonStateRules.WhyNotLoad(s={RuleLog.Shown(s)}, powderHave={RuleLog.Shown(powderHave)}, powderNeed={RuleLog.Shown(powderNeed)}, ballsHave={RuleLog.Shown(ballsHave)})");
            if (!s.Mounted) { var returnedAt52 = NotMounted; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotLoad:52", 2d, $"CannonStateRules.WhyNotLoad: returns {RuleLog.Shown(returnedAt52)} (line 52)"); return returnedAt52; }
            if (s.Loaded) { var returnedAt53 = AlreadyLoaded; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotLoad:53", 2d, $"CannonStateRules.WhyNotLoad: returns {RuleLog.Shown(returnedAt53)} (line 53)"); return returnedAt53; }
            if (powderHave < powderNeed) { var returnedAt54 = "You need " + powderNeed + " Gunpowder"; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotLoad:54", 2d, $"CannonStateRules.WhyNotLoad: returns {RuleLog.Shown(returnedAt54)} (line 54)"); return returnedAt54; }
            { var returnedAt55 = ballsHave >= 1 ? null : NeedBall; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotLoad:55", 2d, $"CannonStateRules.WhyNotLoad: returns {RuleLog.Shown(returnedAt55)} (line 55)"); return returnedAt55; }
        }

        /// <summary>Aiming needs a loaded cannon.</summary>
        internal static string WhyNotAim(CannonStatus s)
        {
            if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotAim", 2d, $"CannonStateRules.WhyNotAim(s={RuleLog.Shown(s)})");
            if (!s.Mounted) { var returnedAt62 = NotMounted; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotAim:62", 2d, $"CannonStateRules.WhyNotAim: returns {RuleLog.Shown(returnedAt62)} (line 62)"); return returnedAt62; }
            { var returnedAt63 = s.Loaded ? null : NotLoaded; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotAim:63", 2d, $"CannonStateRules.WhyNotAim: returns {RuleLog.Shown(returnedAt63)} (line 63)"); return returnedAt63; }
        }

        /// <summary>Lighting the fuse needs a loaded cannon, no fuse burning yet and a torch in hand.</summary>
        internal static string WhyNotLight(CannonStatus s, bool torchInHand)
        {
            if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotLight", 2d, $"CannonStateRules.WhyNotLight(s={RuleLog.Shown(s)}, torchInHand={RuleLog.Shown(torchInHand)})");
            string aim = WhyNotAim(s);
            if (aim != null) { var returnedAt71 = aim; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotLight:71", 2d, $"CannonStateRules.WhyNotLight: returns {RuleLog.Shown(returnedAt71)} (line 71)"); return returnedAt71; }
            if (s.Lit) { var returnedAt72 = FuseBurning; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotLight:72", 2d, $"CannonStateRules.WhyNotLight: returns {RuleLog.Shown(returnedAt72)} (line 72)"); return returnedAt72; }
            { var returnedAt73 = torchInHand ? null : NeedTorch; if (RuleLog.On) RuleLog.Every("CannonStateRules.WhyNotLight:73", 2d, $"CannonStateRules.WhyNotLight: returns {RuleLog.Shown(returnedAt73)} (line 73)"); return returnedAt73; }
        }
    }
}
