using GunPowdered.Core;

namespace GunPowdered.Features.MobileCannon.Domain
{
    /// <summary>
    /// D-182 (user 2026-11-24, shot-007: "I could not reload the mobile cannon, it kept giving me the Use [E] message which is for pushing the cannon"): the
    /// whole cart answered E as the game's cart "Use" (grab and push), and the muzzle's own E spot was a 0.3 m box, so the player hit the cart and never the
    /// muzzle. Now only the handle (the trail end the cart is pulled by) grabs; looking anywhere else on the cannon loads it. Pure, so the unit tests check it.
    /// </summary>
    internal static class CartUseRules
    {
        /// <summary>How far (m) from the handle the looked-at point may be and still grab the cart.</summary>
        internal const float HandleReach = 1.2f;

        /// <summary>Whether the player looks at the handle: the looked-at point is within <see cref="HandleReach"/> m of it. A NaN distance (nothing found) is not the handle.</summary>
        internal static bool OnHandle(float metresFromHandle)
        {
            bool on = !float.IsNaN(metresFromHandle) && metresFromHandle <= HandleReach;
            if (RuleLog.Due("CartUseRules.OnHandle", 2d)) RuleLog.Every("CartUseRules.OnHandle", 2d, $"CartUseRules.OnHandle(metresFromHandle={RuleLog.Shown(metresFromHandle)}): returns {on} (reach {HandleReach} m)");
            return on;
        }
    }
}
