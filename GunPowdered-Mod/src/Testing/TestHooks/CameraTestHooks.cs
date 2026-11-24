// kydl:read-only-observer - test-only: reads where the player is in the picture camera's view; changes nothing
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// D-175 (a19terrain-1, visual-dynamite-kneel shot "dynamite-kneel-front"): since D-174 the player walks to where the stick goes, and a free camera placed
    /// before the walk ended up in front of the player's new spot - the picture showed empty grass and the test stayed green. A picture step now first asks
    /// this hook whether the player is in the picture camera's view (the free camera "gp_freecam" if it exists, else the game camera).
    /// </summary>
    internal static class CameraTestHooks
    {
        const float Margin = 0.05f;

        /// <summary>
        /// Expected outcome: "player in view (x X, y Y, D m away, gp_freecam)" - the player's chest inside the picture with a 5 % margin, in front of the camera.
        /// Otherwise "player NOT in view (...)" with the same numbers, or "no player or camera".
        /// </summary>
        [TestHook("GunPowdered.freeCamSees")]
        static string Sees()
        {
            if (RuleLog.IsOn()) ModLog.Verbose("CameraTestHooks.Sees()");
            Player p = Player.m_localPlayer;
            GameObject free = GameObject.Find("gp_freecam");
            Camera cam = free != null ? free.GetComponent<Camera>() : Camera.main;
            if (p == null || cam == null) { if (RuleLog.IsOn()) ModLog.Verbose("CameraTestHooks.Sees: returns \"no player or camera\" because p == null || cam == null (line 28)"); return "no player or camera"; }
            Vector3 chest = p.transform.position + Vector3.up * 1.0f;
            Vector3 v = cam.WorldToViewportPoint(chest);
            bool inView = v.z > 0f && v.x >= Margin && v.x <= 1f - Margin && v.y >= Margin && v.y <= 1f - Margin;
            string r = (inView ? "player in view" : "player NOT in view") + " (x " + v.x.ToString("0.00") + ", y " + v.y.ToString("0.00") + ", " + v.z.ToString("0.0") + " m away, " + cam.name + ")";
            ModLog.Info("CameraTestHooks: " + r + " - camera at " + cam.transform.position.ToString("0.0") + ", player at " + p.transform.position.ToString("0.0"));
            if (RuleLog.IsOn()) ModLog.Verbose("CameraTestHooks.Sees: returns " + r + " (line 34)");
            return r;
        }
    }
}