namespace GunPowdered.Core
{
    /// <summary>
    /// D-149 (user 2026-11-21 12:25): gunpowder is loud, and using it is a penalty - mobs hear it. "The dynamite burning ... should attract mobs a bit,
    /// similar to how doing 1 hit of a pickaxe does"; "the dynamite explosion should alert a lot of mobs ... and go towards it"; "same with musket /
    /// cannons, should make the mobs alert when the player is using them". Hostile creatures within the radius (m) are alerted, take the player who did
    /// it as their target and walk to where the noise was (GunNoise). Pure, so the unit tests pin the numbers.
    /// </summary>
    public static class NoiseRules
    {
        /// <summary>A fuse being lit: like a pickaxe hit (the game's hit noise reaches about this far, inside a creature's own hearing).</summary>
        public const float FuseRadius = 15f;

        /// <summary>A dynamite stick / the Deep North pack blowing up.</summary>
        public const float StickBlastRadius = 60f, PackBlastRadius = 90f;

        /// <summary>A musket shot, a hand cannon shot, a Mobile Cannon shot.</summary>
        public const float MusketShotRadius = 40f, CannonShotRadius = 60f, MobileCannonShotRadius = 70f;

        /// <summary>
        /// Whether a creature <paramref name="distance"/> m from the noise reacts to it. Expected outcome: true for a hostile, living, untamed creature
        /// inside the radius; never for a tame one or one that is already fighting (it keeps its own target).
        /// </summary>
        public static bool Reacts(float distance, float radius, bool hostile, bool tamed, bool dead, bool hasTarget)
        {
            bool reacts = hostile && !tamed && !dead && !hasTarget && distance >= 0f && distance <= radius;
            if (RuleLog.Due("NoiseRules.Reacts", 2d)) RuleLog.Every("NoiseRules.Reacts", 2d, $"NoiseRules.Reacts(distance={distance}, radius={radius}, hostile={hostile}, tamed={tamed}, dead={dead}, hasTarget={hasTarget}): returns {reacts}");
            return reacts;
        }
    }
}
