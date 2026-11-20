// kydl:read-only-observer - test-only, drives no creature: points the game camera for GameProbe pictures
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Bootstrap;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// A fixed camera for GameProbe screenshots (ProbeApi.Screenshot): the game's camera script (GameCamera) is paused and the camera is placed and aimed by the
    /// test; <see cref="GiveBack"/> lets the game drive it again. Valheim's third-person camera could not be aimed reliably for pictures (D-141: a14fire1..5 it
    /// stayed behind the subject or swung over it). Every pause gives itself back after <see cref="SafetySeconds"/> s if the test never does (a test that
    /// fails in between must not leave the camera paused); a newer pause is never ended by an older timer.
    /// </summary>
    internal static class TestCamera
    {
        internal const float SafetySeconds = 10f;
        static int _pause;

        /// Pauses the game camera and puts it at <paramref name="at"/>, looking at <paramref name="target"/>. Expected outcome: true when the camera was placed.
        internal static bool Place(Vector3 at, Vector3 target, string why)
        {
            GameCamera game = GameCamera.instance;
            Camera cam = game != null ? game.GetComponent<Camera>() : null;
            if (cam == null) { ModLog.Verbose("TestCamera.Place(" + why + "): no game camera - returns false"); return cam != null; }
            game.enabled = false;
            cam.transform.position = at;
            cam.transform.LookAt(target);
            Plugin.Instance.StartCoroutine(GiveBackLater(++_pause, SafetySeconds));
            ModLog.Verbose($"TestCamera.Place({why}): camera paused at {at:0.00}, looking at {target:0.00}");
            return !game.enabled;
        }

        /// Lets the game drive the camera again. Expected outcome: true when it runs (also when it was not paused).
        internal static bool GiveBack(string why)
        {
            GameCamera game = GameCamera.instance;
            if (game == null) { ModLog.Verbose("TestCamera.GiveBack(" + why + "): no game camera - returns false"); return game != null; }
            bool wasPaused = !game.enabled;
            game.enabled = true;
            ModLog.Verbose($"TestCamera.GiveBack({why}): paused before {wasPaused} - returns {game.enabled}");
            return game.enabled;
        }

        static System.Collections.IEnumerator GiveBackLater(int pause, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (pause == _pause && GameCamera.instance != null && !GameCamera.instance.enabled)
            {
                ModLog.Info($"TestCamera: still paused {seconds:0} s after Place (the test never gave it back) - running again");
                GiveBack("safety timer");
            }
        }
    }
}
