// kydl:read-only-observer - test-only: drives the grenade aiming controls (D-050) the way a player's buttons and mouse would
using System.Globalization;
using KydlCore.Common;
using GunPowdered.Features.Grenades.Infrastructure;

namespace GunPowdered.Testing.TestHooks
{
    /// <summary>
    /// TestLab hooks for the grenade aiming (D-050). The real mouse and buttons would override a synthetic press on every physics step, so the hooks
    /// feed GrenadeAim's own test input, which replaces the buttons inside the same Player.SetControls / SetMouseLook filters the real input goes through.
    /// </summary>
    internal static class GrenadeAimTestHooks
    {
        /// Presses the buttons: "hold" (left button down), "hold+block" (left and right down), "block" (right only), "release" (nothing down), "off" (real input again).
        /// Returns the aiming line after the call.
        [TestHook("GunPowdered.aimInput")]
        static string AimInput(string value)
        {
            switch (value)
            {
                case "hold": GrenadeAim.SetTestInput(true, true, false); break;
                case "hold+block": GrenadeAim.SetTestInput(true, true, true); break;
                case "block": GrenadeAim.SetTestInput(true, false, true); break;
                case "release":
                    if (GrenadeAim.Aiming)     // the letting go is the throw: follow it so grenadeFollowedArc can compare the blast with the chosen spot
                    {
                        GrenadeTestHooks.BeginWatch(GrenadeAim.Target);
                        GrenadeTestHooks.WatchBlastOnce();
                    }
                    GrenadeAim.SetTestInput(true, false, false);
                    break;
                default: GrenadeAim.SetTestInput(false, false, false); GrenadeAim.Reset("test input off"); break;
            }
            ModLog.Verbose("GrenadeAimTestHooks: input '" + value + "'");
            return GrenadeAim.Describe();
        }

        /// Moves the mouse while aiming: "right,up" in game mouse units (negative = left / down). Returns the aiming line after the next look update.
        [TestHook("GunPowdered.aimMouse")]
        static string AimMouse(string value)
        {
            string[] parts = value.Split(',');
            GrenadeAim.AddTestMouse(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture));
            ModLog.Verbose("GrenadeAimTestHooks: mouse '" + value + "'");
            return value;
        }

        /// "Idle|Aiming|Cancelled, spot .., target (x, y, z), N m" (flat metres from the player).
        [TestHook("GunPowdered.aimState")]
        static string AimState() => GrenadeAim.Describe();

        /// Grenades left in the player's inventory (a cancelled throw must leave all of them).
        [TestHook("GunPowdered.grenadesLeft")]
        static int GrenadesLeft()
        {
            int n = 0;
            if (Player.m_localPlayer != null)
                foreach (ItemDrop.ItemData item in Player.m_localPlayer.GetInventory().GetAllItems())
                    if (GrenadeSetup.IsGrenade(item)) n += item.m_stack;
            ModLog.Verbose("GrenadeAimTestHooks: " + n + " grenade(s) in the inventory");
            return n;
        }

        /// The camera yaw of the local player rounded to 30 degrees, 0-330 (face leaves a degree or two of offset; aiming must not turn the camera by more than that).
        [TestHook("GunPowdered.lookYaw")]
        static int LookYaw()
        {
            Player p = Player.m_localPlayer;
            return p == null ? 0 : UnityEngine.Mathf.RoundToInt(UnityEngine.Quaternion.LookRotation(p.GetLookDir()).eulerAngles.y / 30f) * 30 % 360;
        }
    }
}
