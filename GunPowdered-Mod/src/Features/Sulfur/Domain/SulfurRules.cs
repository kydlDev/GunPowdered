using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Sulfur.Domain
{
    /// <summary>Where the new sulfur comes from (D-015). Amounts are the user's (Project description).</summary>
    public enum SulfurSource
    {
        /// Sulfurous Mud deposit mined with a pickaxe: 1-3.
        SulfurousMud,
        /// Swamp Blob (prefab Blob): 1-2.
        Blob,
        /// Oozer (prefab BlobElite): 1-2.
        Oozer
    }

    /// <summary>How much sulfur one deposit or kill gives. Pure: the caller hands in a source of numbers in [0, 1).</summary>
    internal static class SulfurRules
    {
        internal const int Min = 1;

        /// Chance that a killed Blob or Oozer drops sulfur at all: 10 % each (D-016).
        internal const double KillDropChance = 0.10;

        /// <summary>Expected outcome: true when the roll is a hit. Mud deposits always give sulfur; only kills use this.</summary>
        internal static bool KillDrops(double roll01)
        {
            return Roll.Chance(KillDropChance, roll01);
        }

        /// <summary>Expected outcome: a whole number from <see cref="Min"/> to <see cref="Max"/> of the source.</summary>
        internal static int Amount(SulfurSource source, Func<double> next01)
        {
            return Roll.IntInclusive(Min, Max(source), next01());
        }

        internal static int Max(SulfurSource source)
        {
            return source == SulfurSource.SulfurousMud ? 3 : 2;
        }
    }
}
