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

        /// <summary>
        /// How much of a harness vertex rides the saddle's spine bone (the rest stays on the animal's root), by its position along the animal (z, m; forward is +).
        /// Expected outcome: 1 at or in front of <paramref name="front"/> (the trace anchors), 0 at or behind <paramref name="rear"/> (the spreader), linear between.
        /// </summary>
        public static float SaddleWeight(float z, float front, float rear)
        {
            if (front <= rear) return z >= front ? 1f : 0f;
            float t = (z - rear) / (front - rear);
            return t <= 0f ? 0f : t >= 1f ? 1f : t;
        }

        /// <summary>Roll (about the cannon's length) a towed cannon may take before it is held: side slopes still tilt it, a yank cannot tip it over (deg).</summary>
        public const float MaxRoll = 25f;

        /// <summary>Pitch (nose up or down) a towed cannon may take before it is held (deg): the trail end is on the hook, so more than this is a somersault.</summary>
        public const float MaxPitch = 40f;

        /// <summary>
        /// The angular acceleration (rad/s^2, about the tilt axis) that pulls a towed cannon back towards the ground's tilt: a spring on the angle (deg) and a damper
        /// on its rate (rad/s). Expected outcome: 0 when level and still; against the tilt and against the rate otherwise.
        /// </summary>
        public static float UprightAccel(float angleDeg, float rateRad) => -angleDeg * 0.0174533f * 60f - rateRad * 14f;

        /// <summary>Roll / pitch against world up a towed cannon may never pass (deg), however the ground under it slopes: past these it would go over.</summary>
        public const float MaxWorldRoll = 30f, MaxWorldPitch = 45f;

        /// <summary>Of two signed excesses, the larger one (it needs the bigger correction). Expected outcome: 0 only when both are 0.</summary>
        public static float Tighter(float a, float b) => System.Math.Abs(a) >= System.Math.Abs(b) ? a : b;

        /// <summary>How far (deg) a tilt is past its limit. Expected outcome: 0 inside +-limit, else the signed excess the cannon is turned back by.</summary>
        public static float Excess(float angleDeg, float limit)
        {
            if (angleDeg > limit) return angleDeg - limit;
            if (angleDeg < -limit) return angleDeg + limit;
            return 0f;
        }

        /// <summary>
        /// The client that owns the towing animal simulates the cannon too. Expected outcome: "push" when this client owns the cannon but another client owns the
        /// animal (hand the cannon over), "pull" when this client owns the animal but not the cannon (take it, also when the old owner never hands it back),
        /// null when nothing changes.
        /// </summary>
        public static string OwnerMove(long me, long cannonOwner, long animalOwner)
        {
            if (animalOwner == 0L) return null;
            if (cannonOwner == me && animalOwner != me) return "push";
            if (cannonOwner != me && animalOwner == me) return "pull";
            return null;
        }
    }
}
