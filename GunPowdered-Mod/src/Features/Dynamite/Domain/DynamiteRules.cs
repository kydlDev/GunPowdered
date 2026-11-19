namespace GunPowdered.Features.Dynamite.Domain
{
    /// <summary>
    /// The Dynamite rules the user approved (D-098 proposal, D-100): a stick is placed on the ground, lit with a Torch, and blows after its fuse. The blast
    /// wrecks buildings, rocks, trees and ore deposits; creatures take only a fifth of that damage ("Creatures take 20% of building damage"). Pure, so the
    /// unit tests check it.
    /// </summary>
    internal static class DynamiteRules
    {
        /// <summary>Seconds from lighting to the blast.</summary>
        internal const float FuseSeconds = 4f;

        /// <summary>The share of the building damage a creature takes (user: 20 %).</summary>
        internal const float CreatureShare = 0.2f;

        /// <summary>How far in front of the player a stick is set down (m).</summary>
        internal const float PlaceDistance = 1.2f;

        internal const string NeedTorch = "Hold a Torch to light the fuse", Burning = "The fuse is burning - get away!", NoGround = "No place for the dynamite here";

        /// <summary>The damage a creature takes from a blast that does <paramref name="buildingDamage"/> to buildings (never below 0).</summary>
        internal static float CreatureDamage(float buildingDamage)
        {
            return buildingDamage > 0f ? buildingDamage * CreatureShare : 0f;
        }

        /// <summary>Seconds of fuse left for a stick lit at <paramref name="litAt"/> (s) at <paramref name="now"/>; the full fuse for an unlit one (litAt below 0).</summary>
        internal static float FuseLeft(double litAt, double now)
        {
            if (litAt < 0d) return FuseSeconds;
            double left = FuseSeconds - (now - litAt);
            return left > 0d ? (float)left : 0f;
        }

        /// <summary>Whether a stick lit at <paramref name="litAt"/> blows at <paramref name="now"/> (never for an unlit one).</summary>
        internal static bool Blows(double litAt, double now)
        {
            return litAt >= 0d && now - litAt >= FuseSeconds;
        }

        /// <summary>Why a stick cannot be lit (null = it can): it burns already, or the player holds no Torch.</summary>
        internal static string WhyNotLight(bool lit, bool torchInHand)
        {
            if (lit) return Burning;
            return torchInHand ? null : NeedTorch;
        }
    }
}
