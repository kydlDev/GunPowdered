using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Cannon.Domain
{
    /// <summary>
    /// One part of a shot's damage, before the target's resistances (D-145): the weapon's part and the ammunition's part are each one of these, the
    /// shot deals their sum. Fire is per second of the burning the hit sets (5 s, BurnRules); the tool tier is the toughest material the part breaks.
    /// </summary>
    internal readonly struct ShotDamage
    {
        internal ShotDamage(float pierce, float blunt, float pickaxe, float firePerSecond, int toolTier)
        {
            if (RuleLog.IsOn()) RuleLog.Every("ShotDamage.ShotDamage", 2d, $"ShotDamage.ShotDamage(pierce={RuleLog.Shown(pierce)}, blunt={RuleLog.Shown(blunt)}, pickaxe={RuleLog.Shown(pickaxe)}, firePerSecond={RuleLog.Shown(firePerSecond)}, toolTier={RuleLog.Shown(toolTier)})");
            Pierce = pierce;
            Blunt = blunt;
            Pickaxe = pickaxe;
            FirePerSecond = firePerSecond;
            ToolTier = toolTier;
        }

        internal float Pierce { get; }
        internal float Blunt { get; }
        internal float Pickaxe { get; }
        internal float FirePerSecond { get; }
        internal int ToolTier { get; }

        /// <summary>
        /// The shot: the weapon's part plus the ammunition's part (D-145). Damage adds up; the tool tier is the higher of the two (a part without a
        /// tool tier, 0, does not lower the other's).
        /// </summary>
        internal static ShotDamage Sum(ShotDamage weapon, ShotDamage ammo)
        {
            if (RuleLog.IsOn()) RuleLog.Every("ShotDamage.Sum", 2d, $"ShotDamage.Sum(weapon={RuleLog.Shown(weapon)}, ammo={RuleLog.Shown(ammo)})");
            { var returnedAt35 = new ShotDamage(weapon.Pierce + ammo.Pierce, weapon.Blunt + ammo.Blunt, weapon.Pickaxe + ammo.Pickaxe, weapon.FirePerSecond + ammo.FirePerSecond, Math.Max(weapon.ToolTier, ammo.ToolTier)); if (RuleLog.IsOn()) RuleLog.Every("ShotDamage.Sum:35", 2d, $"ShotDamage.Sum: returns {RuleLog.Shown(returnedAt35)} (line 35)"); return returnedAt35; }
        }

        /// <summary>This damage times a factor (the cannon ball's momentum after it passed a tree or creature); the tool tier stays.</summary>
        internal ShotDamage Times(float factor)
        {
            if (RuleLog.IsOn()) RuleLog.Every("ShotDamage.Times", 2d, $"ShotDamage.Times(factor={RuleLog.Shown(factor)})");
            { var returnedAt42 = new ShotDamage(Pierce * factor, Blunt * factor, Pickaxe * factor, FirePerSecond * factor, ToolTier); if (RuleLog.IsOn()) RuleLog.Every("ShotDamage.Times:42", 2d, $"ShotDamage.Times: returns {RuleLog.Shown(returnedAt42)} (line 42)"); return returnedAt42; }
        }

        public override string ToString() => "pierce " + Pierce + ", blunt " + Blunt + ", pickaxe " + Pickaxe + ", fire " + FirePerSecond + "/s, tool tier " + ToolTier;
    }
}
