// kydl:read-only-observer - test-only: cameras and measurements for the towed cannon on slopes (D-143); it drives no creature
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Features.MobileCannon.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>D-143 lab hooks (split out of AsksvinTowTestHooks for the class size, G04): a close camera on the hitch and the trail tip's height.</summary>
    internal static class TowSlopeTestHooks
    {
        /// <summary>
        /// D-143: the free camera close beside the hitch (the cannon's tow ring, where the harness chain ends), <paramref name="metres"/> m out to the
        /// Asksvin's side at <paramref name="azimuth"/> degrees, level with the ring: the picture shows whether the cart tip and the harness end meet on a
        /// slope. Returns "free camera on the hitch at ..." or why not.
        /// </summary>
        [TestHook("GunPowdered.freeCamOnHitch")]
        static string FreeCamOnHitch(double azimuth, double metres)
        {
            ModLog.Verbose($"AsksvinTowTestHooks.FreeCamOnHitch(azimuth={azimuth}, metres={metres})");
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            if (AsksvinTowTestHooks.Animal == null || tow == null || tow.Ring == null) { ModLog.Verbose("AsksvinTowTestHooks.FreeCamOnHitch: no Asksvin, cannon or ring - returns why"); return "no Asksvin, cannon or ring"; }
            string placed = AsksvinTowTestHooks.FreeCamOnAsksvin(azimuth, 0, metres);
            GameObject go = GameObject.Find("gp_freecam");
            if (go == null) { ModLog.Verbose("AsksvinTowTestHooks.FreeCamOnHitch: " + placed); return placed; }
            Vector3 centre = tow.Ring.position;
            Vector3 dir = Quaternion.AngleAxis((float)azimuth, Vector3.up) * Vector3.ProjectOnPlane(AsksvinTowTestHooks.Animal.transform.forward, Vector3.up).normalized;
            HitchCamFollow follow = go.GetComponent<HitchCamFollow>();
            if (follow == null) follow = go.AddComponent<HitchCamFollow>();
            follow.enabled = true;
            follow.Ring = tow.Ring;
            follow.Offset = dir * (float)metres + Vector3.up * 0.3f;          // follows the ring every frame: the hitch moves on before the picture is taken
            go.transform.position = centre + follow.Offset;
            go.transform.LookAt(centre);
            string s = "free camera on the hitch at " + go.transform.position.ToString("0.0") + ", ring " + centre.ToString("0.00");
            ModLog.Verbose("AsksvinTowTestHooks.FreeCamOnHitch: returns " + s);
            return s;
        }

        /// <summary>
        /// D-143: how high the cannon's tow ring (the cart's trail tip) is above the terrain under it, and the hook's drop. Going uphill the hook used to hang
        /// about 2 m above the ground and lifted the trail tip with it. Returns "ring above ground H m, hook drop D m, low True/False" (low: H &lt;= <paramref name="max"/>).
        /// </summary>
        [TestHook("GunPowdered.towRingAboveGround")]
        static string RingAboveGround(double max)
        {
            ModLog.Verbose($"AsksvinTowTestHooks.RingAboveGround(max={max})");
            GameObject cannon = MobileCannonTestHooks.Cannon;
            MobileCannonTow tow = cannon != null ? cannon.GetComponent<MobileCannonTow>() : null;
            if (tow == null || tow.Ring == null || ZoneSystem.instance == null) { ModLog.Verbose("AsksvinTowTestHooks.RingAboveGround: no cannon or ring - returns why"); return "no cannon or ring"; }
            float above = tow.Ring.position.y - ZoneSystem.instance.GetGroundHeight(tow.Ring.position);
            string s = $"ring above ground {above:0.00} m, hook drop {AsksvinHarness.HookLocal.y - tow.HookNow.y:0.00} m, low {above <= max}";
            ModLog.Info("AsksvinTowTestHooks.RingAboveGround: returns " + s);
            return s;
        }
    }
}
