using GunPowdered.Core;

namespace GunPowdered.Features.Cannon.Domain
{
    /// <summary>What one frame of loading at the muzzle does.</summary>
    internal enum CannonLoadStep
    {
        /// <summary>Keep loading, no new animation this frame.</summary>
        Wait,

        /// <summary>Keep loading and play the fill (interact) animation once more.</summary>
        Animate,

        /// <summary>The cannon is already filled (by someone else or an earlier load): stop at once, no more fill animation.</summary>
        StopFilled,
    }

    /// <summary>
    /// Loading the Hand Cannon and the Mobile Cannon (user 2026-11-26: "once the cannon is filled, the character should no longer do the fill
    /// animation, and the UI should not allow the refill until it is empty"). Pure, so the unit tests check it; the game classes (CannonMount,
    /// MobileCannonGun) read the cannon's shared state and the clock, ask these rules and act on the answer.
    /// </summary>
    internal static class CannonLoadRules
    {
        /// <summary>
        /// One frame of loading: stop as soon as the shared state says the cannon is filled; otherwise animate when the next animation is due, but
        /// not in the last <paramref name="animTail"/> seconds (an animation started there would still play after the cannon is filled).
        /// </summary>
        internal static CannonLoadStep Step(bool filledNow, float now, float nextAnim, float end, float animTail)
        {
            CannonLoadStep step = filledNow ? CannonLoadStep.StopFilled
                : now >= nextAnim && end - now > animTail ? CannonLoadStep.Animate
                : CannonLoadStep.Wait;
            if (RuleLog.Due("CannonLoadRules.Step", 2d)) RuleLog.Every("CannonLoadRules.Step", 2d, $"CannonLoadRules.Step: {step} (filled {filledNow}, now {now:0.00}, next anim {nextAnim:0.00}, end {end:0.00}, tail {animTail:0.00} s)");
            return step;
        }

        /// <summary>The hover text offers "Load" only for an empty cannon that is not being loaded and has no burning fuse.</summary>
        internal static bool OffersLoad(bool filled, bool fuseBurning, bool loading)
        {
            bool offers = !filled && !fuseBurning && !loading;
            if (RuleLog.Due("CannonLoadRules.OffersLoad", 2d)) RuleLog.Every("CannonLoadRules.OffersLoad", 2d, $"CannonLoadRules.OffersLoad: {offers} (filled {filled}, fuse {fuseBurning}, loading {loading})");
            return offers;
        }
    }
}
