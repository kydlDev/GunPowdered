namespace GunPowdered.Core
{
    /// <summary>
    /// Fire damage over time (D-107, user 2026-11-20: "5 fire damage/s for 5 seconds"). The game turns a hit's fire damage into burning (Character.RPC_Damage
    /// takes m_fire out of the hit and calls AddFireDamage; SE_Burning: 5 s, one tick per second, the amount split over the ticks). So a weapon that should
    /// burn N per second for 5 s puts N x 5 into m_fire. Creatures' fire resistance applies as usual.
    /// </summary>
    public static class BurnRules
    {
        /// <summary>How long the game's burning lasts (SE_Burning m_ttl, 1 s ticks).</summary>
        public const float Seconds = 5f;

        /// <summary>The fire damage to put on a hit so it burns <paramref name="perSecond"/> per second for <see cref="Seconds"/>. Expected outcome: 0 for 0 or less.</summary>
        public static float Total(float perSecond) => perSecond > 0f ? perSecond * Seconds : 0f;
    }
}
