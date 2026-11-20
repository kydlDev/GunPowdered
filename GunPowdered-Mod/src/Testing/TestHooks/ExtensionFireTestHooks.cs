// kydl:read-only-observer - test-only: reads the oven fire's state and measures what a camera in front of the panes really sees
using System.Collections.Generic;
using System.Globalization;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Integrations.Valheim;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the fire of the Flametal Heater and the Flametal Drying Oven (D-138). <c>fireState</c> reads the ExtensionFire component;
    /// the picture hooks render the oven's see-through panes with a test camera 0.9 m in front of them (the way a player sees them) and average the
    /// middle of the picture: a lit fire must make the panes clearly brighter and more orange than the same oven idle, measured the same way.
    /// </summary>
    internal static class ExtensionFireTestHooks
    {
        const int Size = 128;
        static readonly Dictionary<int, Color> Idle = new Dictionary<int, Color>();
        static Vector3? _returnTo;
        static Quaternion _returnRotation;
        static float _returnDistance;
        static readonly HarmonyLib.AccessTools.FieldRef<GameCamera, float> CameraDistance = HarmonyLib.AccessTools.FieldRefAccess<GameCamera, float>("m_distance");

        /// "burning &lt;bool&gt;, flames &lt;bool&gt;, light &lt;intensity&gt;" of the tagged oven, or why not.
        [TestHook("GunPowdered.fireState")]
        static string FireState(GameObject target)
        {
            var fire = target != null ? target.GetComponent<ExtensionFire>() : null;
            if (fire == null) return "no oven fire on " + (target != null ? target.name : "null");
            string answer = string.Format(CultureInfo.InvariantCulture, "burning {0}, flames {1}, light {2:0.00}", fire.Burning, fire.FlamesShown, fire.LightNow);
            ModLog.Verbose("ExtensionFireTestHooks: " + target.name + " " + answer);
            return answer;
        }

        /// Renders the panes of the tagged oven now and keeps the result as its IDLE picture. Returns the brightness (0..1, the panes' mean luminance).
        [TestHook("GunPowdered.fireMarkIdle")]
        static double FireMarkIdle(GameObject target)
        {
            Color c = Panes(target);
            if (target != null) Idle[target.GetInstanceID()] = c;
            return Luma(c);
        }

        /// Renders the panes now and returns how many times brighter they are than the oven's IDLE picture (fireMarkIdle); -1 without one.
        [TestHook("GunPowdered.fireBrightnessGain")]
        static double FireBrightnessGain(GameObject target)
        {
            if (target == null || !Idle.TryGetValue(target.GetInstanceID(), out Color idle)) return -1d;
            double gain = Luma(Panes(target)) / System.Math.Max(0.01d, Luma(idle));
            ModLog.Verbose($"ExtensionFireTestHooks: {target.name} brightness x{gain:0.00} of idle");
            return gain;
        }

        /// Renders the panes now and returns how much more orange they are than the IDLE picture: the gain of (red - blue), in 0..1 units; -2 without an idle picture.
        [TestHook("GunPowdered.fireOrangeGain")]
        static double FireOrangeGain(GameObject target)
        {
            if (target == null || !Idle.TryGetValue(target.GetInstanceID(), out Color idle)) return -2d;
            Color now = Panes(target);
            double gain = (now.r - now.b) - (idle.r - idle.b);
            ModLog.Verbose($"ExtensionFireTestHooks: {target.name} orange {now.r - now.b:0.000} now, {idle.r - idle.b:0.000} idle, gain {gain:0.000}");
            return gain;
        }

        /// <summary>
        /// Puts the player 1.6 m in front of the tagged oven's panes in first person (the camera zoomed all the way in, so the player's body does not
        /// hide the oven, user 2026-11-20), looking at the panes, so a GameProbe screenshot (ProbeApi.Screenshot, named by GameProbe in the
        /// mod's _testruns session) shows what a player sees. <c>viewBack</c> returns the player. Returns "in front of &lt;oven&gt;" or why not.
        /// </summary>
        [TestHook("GunPowdered.viewPanes")]
        static string ViewPanes(GameObject target)
        {
            Player p = Player.m_localPlayer;
            Renderer glass = Glass(target);
            if (p == null || glass == null) return "no player or no panes";
            Vector3 centre = glass.bounds.center, front = Front(target, centre);
            GameCamera cam = GameCamera.instance;
            if (!_returnTo.HasValue)
            {
                _returnTo = p.transform.position;
                _returnRotation = p.transform.rotation;
                _returnDistance = cam != null ? CameraDistance(cam) : 4f;
            }
            Vector3 at = centre + front * 1.6f;
            at.y = target.transform.position.y;
            p.transform.SetPositionAndRotation(at, Quaternion.LookRotation(-front));
            if (cam != null) CameraDistance(cam) = 0f;                          // first person
            p.SetLookDir((centre - p.GetEyePoint()).normalized);
            ModLog.Verbose("ExtensionFireTestHooks: player in front of the panes of " + target.name + " at " + at);
            return "in front of " + Utils.GetPrefabName(target);
        }

        /// Returns the player to where viewPanes found them. Returns "back" or "nothing to undo".
        [TestHook("GunPowdered.viewBack")]
        static string ViewBack()
        {
            Player p = Player.m_localPlayer;
            if (p == null || !_returnTo.HasValue) return "nothing to undo";
            p.transform.SetPositionAndRotation(_returnTo.Value, _returnRotation);
            p.SetLookDir(_returnRotation * Vector3.forward);
            if (GameCamera.instance != null) CameraDistance(GameCamera.instance) = _returnDistance;
            _returnTo = null;
            return "back";
        }

        static Renderer Glass(GameObject oven)
        {
            if (oven != null) foreach (Renderer r in oven.GetComponentsInChildren<Renderer>(true)) if (r.name.EndsWith("_glass")) return r;
            return null;
        }

        // the panes face the side of the oven they sit on
        static Vector3 Front(GameObject oven, Vector3 centre)
        {
            Vector3 local = oven.transform.InverseTransformPoint(centre);
            return oven.transform.TransformDirection(new Vector3(0f, 0f, local.z >= 0f ? 1f : -1f));
        }

        static double Luma(Color c) => 0.2126d * c.r + 0.7152d * c.g + 0.0722d * c.b;

        // the mean colour of the middle half of a picture of the panes, taken by a test camera in front of them
        static Color Panes(GameObject oven)
        {
            Renderer glass = Glass(oven);
            if (glass == null) { ModLog.Warn("ExtensionFireTestHooks: no panes ('*_glass') on " + (oven != null ? oven.name : "null")); return Color.black; }
            Vector3 centre = glass.bounds.center;
            Vector3 front = Front(oven, centre);

            var go = new GameObject("gp_test_fire_camera");
            var rt = RenderTexture.GetTemporary(Size, Size, 24);
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            RenderTexture before = RenderTexture.active;
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.fieldOfView = 30f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 30f;
                cam.transform.position = centre + front * 0.9f;
                cam.transform.LookAt(centre);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                tex.Apply();
            }
            finally
            {
                RenderTexture.active = before;
                RenderTexture.ReleaseTemporary(rt);
                Object.Destroy(go);
            }
            float r0 = 0f, g0 = 0f, b0 = 0f;
            int n = 0;
            for (int y = Size / 4; y < Size * 3 / 4; y++)
                for (int x = Size / 4; x < Size * 3 / 4; x++) { Color c = tex.GetPixel(x, y); r0 += c.r; g0 += c.g; b0 += c.b; n++; }
            Object.Destroy(tex);
            var mean = new Color(r0 / n, g0 / n, b0 / n);
            ModLog.Verbose($"ExtensionFireTestHooks: panes of {oven.name} rendered from {front:0.0}: mean {mean.r:0.000} / {mean.g:0.000} / {mean.b:0.000}, luma {Luma(mean):0.000}");
            return mean;
        }
    }
}
