namespace GunPowdered.Features.MobileCannon.Domain
{
    /// <summary>
    /// The Asksvin harness and towing the Mobile Cannon (D-063, D-116): the harness goes on a saddled tame Asksvin; E on the cannon's tow ring hitches
    /// it to the nearest harnessed Asksvin; ridden or following (unridden), the Asksvin tows it. Pure, so the unit tests link it in.
    /// </summary>
    public static class AsksvinTowRules
    {
        public const string NotTamed = "Tame the Asksvin first";
        public const string NoSaddle = "Put a saddle on the Asksvin first";
        public const string HasHarness = "This Asksvin already wears a harness";
        public const string HeldByPlayer = "Let go of the cannon first";
        public const string AlreadyTowed = "The cannon is already hitched";

        /// <summary>Why the harness cannot go on this animal. Expected outcome: null when it can, else the message shown to the player.</summary>
        public static string WhyNotHarness(bool tamed, bool saddled, bool harnessed)
        {
            if (!tamed) return NotTamed;
            if (!saddled) return NoSaddle;
            return harnessed ? HasHarness : null;
        }

        /// <summary>Why the cannon cannot be hitched. Expected outcome: null when it can, else the message shown to the player.</summary>
        public static string WhyNotHitch(bool towed, bool heldByPlayer, bool animalNear, float range)
        {
            if (towed) return AlreadyTowed;
            if (heldByPlayer) return HeldByPlayer;
            return animalNear ? null : NoAnimal(range);
        }

        /// <summary>The message when no harnessed Asksvin is near the tow ring.</summary>
        public static string NoAnimal(float range) => "No harnessed Asksvin within " + range.ToString("0") + " m of the tow ring";

        /// <summary>Whether a hitched cannon stays hitched. Expected outcome: false once the animal is gone, dead or lost its harness.</summary>
        public static bool KeepsTowing(bool animalExists, bool animalDead, bool harnessed) => animalExists && !animalDead && harnessed;

        /// <summary>The nearer of two candidates (distance in m; a negative distance means "none yet"). Expected outcome: true when the new one wins.</summary>
        public static bool Nearer(float candidate, float best, float range) => candidate <= range && (best < 0f || candidate < best);
    }
}
