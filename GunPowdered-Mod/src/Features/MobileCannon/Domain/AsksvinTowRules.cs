using GunPowdered.Core;
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
            if (RuleLog.Due("AsksvinTowRules.WhyNotHarness", 2d)) RuleLog.Every("AsksvinTowRules.WhyNotHarness", 2d, $"AsksvinTowRules.WhyNotHarness(tamed={RuleLog.Shown(tamed)}, saddled={RuleLog.Shown(saddled)}, harnessed={RuleLog.Shown(harnessed)})");
            if (!tamed) return NotTamed;
            if (!saddled) { var returnedAt21 = NoSaddle; if (RuleLog.Due("AsksvinTowRules.WhyNotHarness:21", 2d)) RuleLog.Every("AsksvinTowRules.WhyNotHarness:21", 2d, $"AsksvinTowRules.WhyNotHarness: returns {returnedAt21} because !saddled (line 21)"); return returnedAt21; }
            { var returnedAt22 = harnessed ? HasHarness : null; if (RuleLog.Due("AsksvinTowRules.WhyNotHarness:22", 2d)) RuleLog.Every("AsksvinTowRules.WhyNotHarness:22", 2d, $"AsksvinTowRules.WhyNotHarness: returns {returnedAt22} (line 22)"); return returnedAt22; }
        }

        /// <summary>Why the cannon cannot be hitched. Expected outcome: null when it can, else the message shown to the player.</summary>
        public static string WhyNotHitch(bool towed, bool heldByPlayer, bool animalNear, float range)
        {
            if (RuleLog.Due("AsksvinTowRules.WhyNotHitch", 2d)) RuleLog.Every("AsksvinTowRules.WhyNotHitch", 2d, $"AsksvinTowRules.WhyNotHitch(towed={RuleLog.Shown(towed)}, heldByPlayer={RuleLog.Shown(heldByPlayer)}, animalNear={RuleLog.Shown(animalNear)}, range={RuleLog.Shown(range)})");
            if (towed) return AlreadyTowed;
            if (heldByPlayer) { var returnedAt30 = HeldByPlayer; if (RuleLog.Due("AsksvinTowRules.WhyNotHitch:30", 2d)) RuleLog.Every("AsksvinTowRules.WhyNotHitch:30", 2d, $"AsksvinTowRules.WhyNotHitch: returns {returnedAt30} because heldByPlayer (line 30)"); return returnedAt30; }
            { var returnedAt31 = animalNear ? null : NoAnimal(range); if (RuleLog.Due("AsksvinTowRules.WhyNotHitch:31", 2d)) RuleLog.Every("AsksvinTowRules.WhyNotHitch:31", 2d, $"AsksvinTowRules.WhyNotHitch: returns {returnedAt31} (line 31)"); return returnedAt31; }
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
            if (RuleLog.Due("AsksvinTowRules.SaddleWeight", 2d)) RuleLog.Every("AsksvinTowRules.SaddleWeight", 2d, $"AsksvinTowRules.SaddleWeight(z={RuleLog.Shown(z)}, front={RuleLog.Shown(front)}, rear={RuleLog.Shown(rear)})");
            if (front <= rear) return z >= front ? 1f : 0f;
            float t = (z - rear) / (front - rear);
            { var returnedAt52 = t <= 0f ? 0f : t >= 1f ? 1f : t; if (RuleLog.Due("AsksvinTowRules.SaddleWeight:52", 2d)) RuleLog.Every("AsksvinTowRules.SaddleWeight:52", 2d, $"AsksvinTowRules.SaddleWeight: returns {returnedAt52} (line 52)"); return returnedAt52; }
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

        /// <summary>Highest spin (rad/s) a towed cannon may have: the game's default 7 let one joint yank roll it over between two keeper steps.</summary>
        public const float MaxTowSpin = 3.5f;

        /// <summary>Of two signed excesses, the larger one (it needs the bigger correction). Expected outcome: 0 only when both are 0.</summary>
        public static float Tighter(float a, float b) => System.Math.Abs(a) >= System.Math.Abs(b) ? a : b;

        /// <summary>How far (deg) a tilt is past its limit. Expected outcome: 0 inside +-limit, else the signed excess the cannon is turned back by.</summary>
        public static float Excess(float angleDeg, float limit)
        {
            if (RuleLog.Due("AsksvinTowRules.Excess", 2d)) RuleLog.Every("AsksvinTowRules.Excess", 2d, $"AsksvinTowRules.Excess(angleDeg={RuleLog.Shown(angleDeg)}, limit={RuleLog.Shown(limit)})");
            if (angleDeg > limit) return angleDeg - limit;
            if (angleDeg < -limit) { var returnedAt81 = angleDeg + limit; if (RuleLog.Due("AsksvinTowRules.Excess:81", 2d)) RuleLog.Every("AsksvinTowRules.Excess:81", 2d, $"AsksvinTowRules.Excess: returns {returnedAt81} because angleDeg < -limit (line 81)"); return returnedAt81; }
            { if (RuleLog.Due("AsksvinTowRules.Excess:82", 2d)) RuleLog.Every("AsksvinTowRules.Excess:82", 2d, $"AsksvinTowRules.Excess: returns 0f (line 82)"); return 0f; }
        }

        /// <summary>How far (m) the tow hook may move down or up from its place on level ground, to follow the ground under it on a slope.</summary>
        public const float MaxHookDrop = 3f;

        /// <summary>How fast (m/s) the tow hook follows the ground under it: quick enough for a cliff face, slow enough that the cannon is not yanked.</summary>
        public const float HookDropSpeed = 4f;

        /// <summary>
        /// D-143: the Asksvin's body stays level on a slope, so a hook fixed behind it hangs high above the ground going uphill (2 m on the template's 39 degree
        /// cliff) and lifts the cannon's trail tip far above the harness traces. The hook is lowered by how much lower the ground under it is than the ground
        /// under the animal (raised when it is higher), clamped to <see cref="MaxHookDrop"/>, moving at most <paramref name="maxStep"/> m from
        /// <paramref name="current"/> per call. Expected outcome: 0 on level ground; positive (down) going uphill; negative (up) going downhill.
        /// </summary>
        public static float HookDrop(float groundUnderAnimal, float groundUnderHook, float current, float maxStep)
        {
            if (RuleLog.Due("AsksvinTowRules.HookDrop", 2d)) RuleLog.Every("AsksvinTowRules.HookDrop", 2d, $"AsksvinTowRules.HookDrop(groundUnderAnimal={RuleLog.Shown(groundUnderAnimal)}, groundUnderHook={RuleLog.Shown(groundUnderHook)}, current={RuleLog.Shown(current)}, maxStep={RuleLog.Shown(maxStep)})");
            float target = System.Math.Max(-MaxHookDrop, System.Math.Min(MaxHookDrop, groundUnderAnimal - groundUnderHook));
            float step = System.Math.Abs(maxStep);
            float drop = target > current + step ? current + step : target < current - step ? current - step : target;
            if (RuleLog.Due("AsksvinTowRules.HookDrop:end", 2d)) RuleLog.Every("AsksvinTowRules.HookDrop:end", 2d, $"AsksvinTowRules.HookDrop: returns {RuleLog.Shown(drop)}");
            return drop;
        }

        /// <summary>
        /// The client that owns the towing animal simulates the cannon too. Expected outcome: "push" when this client owns the cannon but another client owns the
        /// animal (hand the cannon over), "pull" when this client owns the animal but not the cannon (take it, also when the old owner never hands it back),
        /// null when nothing changes.
        /// </summary>
        public static string OwnerMove(long me, long cannonOwner, long animalOwner)
        {
            if (RuleLog.Due("AsksvinTowRules.OwnerMove", 2d)) RuleLog.Every("AsksvinTowRules.OwnerMove", 2d, $"AsksvinTowRules.OwnerMove(me={RuleLog.Shown(me)}, cannonOwner={RuleLog.Shown(cannonOwner)}, animalOwner={RuleLog.Shown(animalOwner)})");
            if (animalOwner == 0L) return null;
            if (cannonOwner == me && animalOwner != me) { if (RuleLog.Due("AsksvinTowRules.OwnerMove:94", 2d)) RuleLog.Every("AsksvinTowRules.OwnerMove:116", 2d, "AsksvinTowRules.OwnerMove: returns \"push\" (line 116)"); return "push"; }
            if (cannonOwner != me && animalOwner == me) { if (RuleLog.Due("AsksvinTowRules.OwnerMove:95", 2d)) RuleLog.Every("AsksvinTowRules.OwnerMove:117", 2d, "AsksvinTowRules.OwnerMove: returns \"pull\" (line 117)"); return "pull"; }
            return null;
        }
    }
}
