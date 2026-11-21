using GunPowdered.Core;

namespace GunPowdered.Features.Cannon.Domain
{
    /// <summary>
    /// Which cannon ball a cannon takes (D-145), pure. Every ball has a tier: the Iron Cannon Ball 1, the Flametal Cannon Ball 2. A cannon takes
    /// every ball of its lowest tier or higher: the Hand Cannon Iron or better, the Mobile Cannon Flametal or better. When a player carries several
    /// balls the cannon takes, loading uses the lowest-tier one (the better balls are kept for where only they fit; Claude's choice, D-145).
    /// </summary>
    internal static class CannonAmmoRules
    {
        internal const int IronTier = 1, FlametalTier = 2;
        internal const int HandCannonMinTier = IronTier, MobileCannonMinTier = FlametalTier;

        /// <summary>The balls from the lowest tier up: the index + 1 is the tier.</summary>
        internal static readonly string[] Balls = { GunPowderedPrefabs.CannonBall, GunPowderedPrefabs.FlametalCannonBall };

        /// <summary>The tier of a ball prefab, 0 when it is no cannon ball.</summary>
        internal static int TierOf(string ball)
        {
            if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.TierOf", 2d, $"CannonAmmoRules.TierOf(ball={RuleLog.Shown(ball)})");
            int tier = 0;
            for (int i = 0; i < Balls.Length; i++)
                if (Balls[i] == ball) tier = i + 1;
            { var returnedAt25 = tier; if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.TierOf:25", 2d, $"CannonAmmoRules.TierOf: returns {RuleLog.Shown(returnedAt25)} (line 25)"); return returnedAt25; }
        }

        /// <summary>A cannon whose lowest tier is <paramref name="minTier"/> takes this ball.</summary>
        internal static bool Takes(int minTier, string ball)
        {
            if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.Takes", 2d, $"CannonAmmoRules.Takes(minTier={RuleLog.Shown(minTier)}, ball={RuleLog.Shown(ball)})");
            int tier = TierOf(ball);
            { var returnedAt33 = tier > 0 && tier >= minTier; if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.Takes:33", 2d, $"CannonAmmoRules.Takes: returns {RuleLog.Shown(returnedAt33)} (line 33)"); return returnedAt33; }
        }

        /// <summary>
        /// The ball a load uses: the lowest-tier ball of <paramref name="minTier"/> or higher that the player carries. <paramref name="carried"/> holds
        /// how many of each ball of <see cref="Balls"/> the player carries, in the same order. Expected outcome: the prefab, or null when none fits.
        /// </summary>
        internal static string Choose(int minTier, int[] carried)
        {
            if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.Choose", 2d, $"CannonAmmoRules.Choose(minTier={RuleLog.Shown(minTier)}, carried={RuleLog.Shown(carried)})");
            string chosen = null;
            for (int i = Balls.Length - 1; i >= 0; i--)
                if (i + 1 >= minTier && carried != null && i < carried.Length && carried[i] > 0) chosen = Balls[i];
            { var returnedAt46 = chosen; if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.Choose:46", 2d, $"CannonAmmoRules.Choose: returns {RuleLog.Shown(returnedAt46)} (line 46)"); return returnedAt46; }
        }

        /// <summary>How many balls the player carries that a cannon of <paramref name="minTier"/> takes (for the "you need a ball" check).</summary>
        internal static int CountTaken(int minTier, int[] carried)
        {
            if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.CountTaken", 2d, $"CannonAmmoRules.CountTaken(minTier={RuleLog.Shown(minTier)}, carried={RuleLog.Shown(carried)})");
            int n = 0;
            for (int i = 0; i < Balls.Length; i++)
                if (i + 1 >= minTier && carried != null && i < carried.Length && carried[i] > 0) n += carried[i];
            { var returnedAt56 = n; if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.CountTaken:56", 2d, $"CannonAmmoRules.CountTaken: returns {RuleLog.Shown(returnedAt56)} (line 56)"); return returnedAt56; }
        }

        /// <summary>The ball that comes back from a loaded cannon: the stored one, or the Iron Cannon Ball for a cannon loaded before D-145 (nothing stored).</summary>
        internal static string LoadedOrOld(string stored)
        {
            if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.LoadedOrOld", 2d, $"CannonAmmoRules.LoadedOrOld(stored={RuleLog.Shown(stored)})");
            { var returnedAt63 = TierOf(stored) > 0 ? stored : GunPowderedPrefabs.CannonBall; if (RuleLog.IsOn()) RuleLog.Every("CannonAmmoRules.LoadedOrOld:63", 2d, $"CannonAmmoRules.LoadedOrOld: returns {RuleLog.Shown(returnedAt63)} (line 63)"); return returnedAt63; }
        }
    }
}
