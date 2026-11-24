using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// User 2026-11-24: the Mobile Cannon is immune to fire and lava (MobileCannonSetup.MakeFireProof). These hooks read the settings on the placed cannon,
    /// hit it with fire (or blunt, for the negative test) through the game's own WearNTear.Damage, and compare its health afterwards.
    /// </summary>
    internal static class MobileCannonFireTestHooks
    {
        static float _healthBefore = float.NaN;

        static WearNTear Wear()
        {
            GameObject cannon = MobileCannonTestHooks.Cannon;
            if (RuleLog.IsOn()) ModLog.Verbose("MobileCannonFireTestHooks.Wear: returns the WearNTear of " + (cannon != null ? cannon.name : "<none>"));
            return cannon != null ? cannon.GetComponent<WearNTear>() : null;
        }

        /// <summary>Expected outcome: "fire Immune, burnable False, lava / ash immune True". "no cannon" before mobileCannonPlace.</summary>
        [TestHook("GunPowdered.mobileCannonFireProof")]
        static string FireProof()
        {
            WearNTear w = Wear();
            if (w == null) { if (RuleLog.IsOn()) ModLog.Verbose("MobileCannonFireTestHooks.FireProof: returns \"no cannon\" because w == null (line 27)"); return "no cannon"; }
            string r = "fire " + w.m_damages.m_fire + ", burnable " + w.m_burnable + ", lava / ash immune " + w.m_ashDamageImmune + ", health " + w.m_health.ToString("0");
            ModLog.Info("MobileCannonFireTestHooks: " + r);
            return r;
        }

        /// <summary>Hits the cannon with <paramref name="damage"/> of <paramref name="kind"/> ("fire" or "blunt") and remembers its health first. Returns "hit KIND D, health before H".</summary>
        [TestHook("GunPowdered.mobileCannonHit")]
        static string Hit(string kind, double damage)
        {
            WearNTear w = Wear();
            if (w == null) { if (RuleLog.IsOn()) ModLog.Verbose("MobileCannonFireTestHooks.Hit: returns \"no cannon\" because w == null (line 38)"); return "no cannon"; }
            _healthBefore = w.GetHealthPercentage();
            var hit = new HitData { m_point = w.transform.position + Vector3.up, m_dir = Vector3.down, m_toolTier = 100 };
            if (kind == "fire") hit.m_damage.m_fire = (float)damage; else hit.m_damage.m_blunt = (float)damage;
            w.Damage(hit);
            string r = "hit " + kind + " " + damage.ToString("0") + ", health before " + _healthBefore.ToString("0.000");
            ModLog.Info("MobileCannonFireTestHooks: " + r);
            return r;
        }

        /// <summary>Expected outcome after a fire hit: "same health H". Otherwise "health dropped A -> B".</summary>
        [TestHook("GunPowdered.mobileCannonHealthSame")]
        static string HealthSame()
        {
            WearNTear w = Wear();
            if (w == null || float.IsNaN(_healthBefore)) { if (RuleLog.IsOn()) ModLog.Verbose("MobileCannonFireTestHooks.HealthSame: returns \"nothing to compare\" because w == null || float.IsNaN(_healthBefore) (line 53)"); return "nothing to compare"; }
            float now = w.GetHealthPercentage();
            string r = Mathf.Abs(now - _healthBefore) < 0.0005f ? "same health " + now.ToString("0.000") : "health dropped " + _healthBefore.ToString("0.000") + " -> " + now.ToString("0.000");
            ModLog.Info("MobileCannonFireTestHooks: " + r);
            return r;
        }
    }
}