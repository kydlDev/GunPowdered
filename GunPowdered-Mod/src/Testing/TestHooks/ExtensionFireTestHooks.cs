// kydl:read-only-observer - test-only, drives no creature: reads the oven fire's state, measures what a camera in front of the panes sees, points the camera for the GameProbe pictures
using System.Collections.Generic;
using System.Globalization;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;
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
        // where the picture camera stands: in front of the panes and a little above them (m)
        const float CameraDistance = 2.5f, CameraHeight = 0.6f;
        static readonly Dictionary<int, Color> Idle = new Dictionary<int, Color>();
        static readonly HarmonyLib.AccessTools.FieldRef<Character, GameObject> Visual = HarmonyLib.AccessTools.FieldRefAccess<Character, GameObject>("m_visual");


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
        /// Points the game camera at the tagged oven's panes for a GameProbe screenshot (ProbeApi.Screenshot, named by GameProbe in the mod's _testruns
        /// session): the game's camera script (GameCamera) is paused and the camera stands <see cref="CameraDistance"/> m in front of the panes, a little
        /// above them, looking at them; the player's body is hidden. Valheim's own third-person camera could not be aimed reliably: it stayed behind the
        /// oven (a14fire1, a14fire3) or swung over it (a14fire2 idle, a14fire4, a14fire5). <c>viewBack</c> gives the camera back to the game.
        /// Returns "in front of &lt;oven&gt;" or why not.
        /// </summary>
        [TestHook("GunPowdered.viewPanes")]
        static string ViewPanes(GameObject target)
        {
            Player p = Player.m_localPlayer;
            Renderer glass = Glass(target);
            if (p == null || glass == null) { ModLog.Verbose("ExtensionFireTestHooks.ViewPanes: no player or no panes"); return "no player or no panes"; }
            Vector3 centre = glass.bounds.center, front = Front(target, centre);
            if (!TestCamera.Place(centre + front * CameraDistance + Vector3.up * CameraHeight, centre, "panes of " + target.name)) return "no game camera";
            if (Visual(p) != null) Visual(p).SetActive(false);                // only for the picture: viewBack shows the body again
            ModLog.Verbose($"ExtensionFireTestHooks: camera in front of the panes of {target.name} at {centre:0.00}");
            return "in front of " + Utils.GetPrefabName(target);
        }

        /// Gives the camera back to the game and shows the player's body again (after viewPanes). Returns "back" or why not.
        [TestHook("GunPowdered.viewBack")]
        static string ViewBack()
        {
            Player p = Player.m_localPlayer;
            if (p != null && Visual(p) != null) Visual(p).SetActive(true);
            bool running = TestCamera.GiveBack("viewBack");
            ModLog.Verbose("ExtensionFireTestHooks.ViewBack: game camera running " + running);
            return running ? "back" : "no game camera";
        }

        static Renderer Glass(GameObject oven)
        {
            if (oven != null) foreach (Renderer r in oven.GetComponentsInChildren<Renderer>(true)) if (r.name.EndsWith("_glass")) return r;
            return null;
        }

        // the panes face the side of the oven they sit on (both ovens: the glass is at local +z, a14fire2 logged +0.33 m for the Drying Oven)
        static Vector3 Front(GameObject oven, Vector3 centre)
        {
            Vector3 local = oven.transform.InverseTransformPoint(centre);
            Vector3 front = oven.transform.TransformDirection(new Vector3(0f, 0f, local.z >= 0f ? 1f : -1f));
            ModLog.Verbose($"ExtensionFireTestHooks.Front({oven.name}): glass at {local:0.000} (local) -> front {front:0.00} (world)");
            return front;
        }

        /// <summary>
        /// For the screenshot: whether the game camera looks at the panes from their front, so the picture shows the fire. Valheim's third-person camera
        /// follows the look direction over a few frames and was once behind the oven (a14fire1 shot 4: the Drying Oven's back wall, the picture looked
        /// idle while the fire burned). Returns "camera in front (N deg)" when the camera is on the panes' side within 60 degrees of their facing,
        /// else "camera behind (N deg)" - pair it with a wait-until before the screenshot.
        /// </summary>
        [TestHook("GunPowdered.cameraOnPanes")]
        static string CameraOnPanes(GameObject target)
        {
            Renderer glass = Glass(target);
            Camera cam = GameCamera.instance != null ? GameCamera.instance.GetComponent<Camera>() : Camera.main;
            if (glass == null || cam == null) return "no panes or no camera";
            Vector3 centre = glass.bounds.center, front = Front(target, centre);
            Vector3 toCamera = Vector3.ProjectOnPlane(cam.transform.position - centre, Vector3.up);
            float angle = Vector3.Angle(Vector3.ProjectOnPlane(front, Vector3.up), toCamera);
            string answer = (angle <= 60f ? "camera in front (" : "camera behind (") + angle.ToString("0", CultureInfo.InvariantCulture) + " deg)";
            ModLog.Verbose($"ExtensionFireTestHooks: camera at {cam.transform.position:0.00}, panes of {target.name} at {centre:0.00} facing {front:0.00}: {answer}");
            return answer;
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
