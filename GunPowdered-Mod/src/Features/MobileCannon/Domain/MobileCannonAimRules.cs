using System;

namespace GunPowdered.Features.MobileCannon.Domain
{
    /// <summary>
    /// The Mobile Cannon's elevation crank (D-104: look at the crank, the mouse wheel turns it; the barrel stays where it was set). Pure, so the unit tests
    /// check it: one wheel notch is one step, the pitch stays inside its limits, and the crank and the elevating screw follow the pitch.
    /// </summary>
    internal static class MobileCannonAimRules
    {
        /// <summary>How far the crank turns per degree of barrel elevation (a screw: many crank turns for little elevation).</summary>
        internal const float CrankDegreesPerPitch = 30f;

        /// <summary>How far the breech end over the screw is from the trunnions (m): the screw sinks by this x sin(pitch).</summary>
        internal const float BreechArm = 0.43f;

        /// <summary>The new pitch after <paramref name="notches"/> wheel notches (+ = muzzle up), <paramref name="step"/> degrees each, kept inside min .. max.</summary>
        internal static float Turn(float pitch, int notches, float step, float min, float max)
        {
            float lo = Math.Min(min, max), hi = Math.Max(min, max);
            float next = pitch + notches * Math.Abs(step);
            return next < lo ? lo : next > hi ? hi : next;
        }

        /// <summary>Wheel notches from the game's scroll value (one notch per sign; 0 when it did not move).</summary>
        internal static int Notches(float scroll) => scroll > 0.001f ? 1 : scroll < -0.001f ? -1 : 0;

        /// <summary>The crank's turn in degrees for a pitch.</summary>
        internal static float CrankAngle(float pitch) => pitch * CrankDegreesPerPitch;

        /// <summary>How far the elevating screw sinks for a pitch (m; negative pitch raises it).</summary>
        internal static float ScrewDrop(float pitch) => BreechArm * (float)Math.Sin(pitch * Math.PI / 180.0);
    }
}
