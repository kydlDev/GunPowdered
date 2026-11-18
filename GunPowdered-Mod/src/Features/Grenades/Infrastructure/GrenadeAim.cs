using UnityEngine;
using KydlCore.Common;
using GunPowdered.Configuration;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Features.Grenades.Infrastructure
{
    /// <summary>
    /// The game side of the new Spiked Grenade controls (D-050): the left button held = aiming (the camera stays, the mouse moves the landing spot),
    /// letting go throws onto that spot, the right button while aiming cancels. The Harmony patches only call FilterControls / FilterLook / AimFor;
    /// the decisions are GrenadeAimRules, AimSpot and LaunchSolver (pure, unit tested). Local player only, only with a grenade in hand.
    /// </summary>
    internal static class GrenadeAim
    {
        /// How long the chosen spot stays the aim after the release: the throw animation delays the real launch by a moment.
        const float LatchSeconds = 2f;
        const float DefaultStartDistance = 8f;

        static AimPhase _phase = AimPhase.Idle;
        static AimSpot _spot;
        static Vector3 _forward = Vector3.forward;
        static Vector3 _latchedTarget;
        static float _latchedUntil;
        static bool _testHold, _testBlock, _testActive;
        static Vector2 _testMouse;

        /// <summary>True while the left button is held on a grenade and the landing spot is being chosen (the preview then follows the spot).</summary>
        internal static bool Aiming => _phase == AimPhase.Aiming;

        /// <summary>The landing spot in the world: where the thrower aims while Aiming, or the spot just chosen until the latch runs out; null otherwise.</summary>
        internal static Vector3? Target
        {
            get
            {
                if (_phase == AimPhase.Aiming && Player.m_localPlayer != null) return TargetPoint(Player.m_localPlayer, _spot);
                return Time.time < _latchedUntil ? _latchedTarget : (Vector3?)null;
            }
        }

        /// <summary>Test input (TestLab): from now on the grenade controls come from these instead of the real mouse, until cleared.</summary>
        internal static void SetTestInput(bool active, bool hold, bool block)
        {
            _testActive = active;
            _testHold = hold;
            _testBlock = block;
            if (!active) _testMouse = Vector2.zero;
            ModLog.Verbose("GrenadeAim: test input " + (active ? "on, hold " + hold + ", block " + block : "off"));
        }

        /// <summary>The aiming as a line for the TestLab: phase, the chosen spot and its flat distance from the player.</summary>
        internal static string Describe()
        {
            Vector3? t = Target;
            Player p = Player.m_localPlayer;
            string where = t.HasValue && p != null ? "target " + t.Value.ToString("0.0") + ", " + Vector2.Distance(new Vector2(p.transform.position.x, p.transform.position.z), new Vector2(t.Value.x, t.Value.z)).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " m" : "no target";
            return _phase + ", spot " + _spot + ", " + where;
        }

        /// <summary>Test input: mouse movement the next look filter treats as the player's own (right, up in game mouse units).</summary>
        internal static void AddTestMouse(float right, float up)
        {
            _testMouse += new Vector2(right, up);
            ModLog.Verbose("GrenadeAim: test mouse " + _testMouse.ToString("0.0"));
        }

        /// <summary>Resets the aiming (menu opened, weapon changed, test cleanup). Expected outcome: Idle, no latch.</summary>
        internal static void Reset(string why)
        {
            if (_phase != AimPhase.Idle) ModLog.State("grenade aim", _phase.ToString(), AimPhase.Idle.ToString(), why);
            _phase = AimPhase.Idle;
            _latchedUntil = 0f;
        }

        /// <summary>
        /// Player.SetControls prefix: runs the aim rules on the player's buttons and rewrites them (the left button is held back until the release,
        /// then it is one attack press; the block is off while aiming). Expected outcome: untouched buttons for anything but a grenade in hand.
        /// </summary>
        internal static void FilterControls(Player player, ref bool attack, ref bool attackHold, ref bool block, ref bool blockHold)
        {
            if (!Applies(player))
            {
                if (_phase != AimPhase.Idle) Reset("no grenade in hand, menu open or feature off");
                return;
            }
            if (_testActive)     // TestLab: the buttons are the test's, the real ones would override a synthetic press every physics step
            {
                attack = false;
                attackHold = _testHold;
                block = _testBlock;
                blockHold = _testBlock;
            }
            AimStep step = GrenadeAimRules.Next(_phase, attack || attackHold, block);
            if (step.Happened != AimEvent.None) Handle(player, step.Happened);
            _phase = step.Next;
            if (!step.Intercept) return;
            attack = step.Attack;
            attackHold = false;
            if (step.SuppressBlock)
            {
                block = false;
                blockHold = false;
            }
        }

        /// <summary>
        /// Player.SetMouseLook prefix: while aiming the mouse moves the landing spot and the camera does not turn (the look is zeroed).
        /// Expected outcome: the spot moved by this frame's mouse movement, or nothing when not aiming.
        /// </summary>
        internal static void FilterLook(Player player, ref Vector2 look)
        {
            if (_phase != AimPhase.Aiming || player != Player.m_localPlayer) return;
            Vector2 mouse = _testActive ? _testMouse : look;
            _testMouse = Vector2.zero;
            if (mouse != Vector2.zero) _spot = _spot.Move(mouse.x, mouse.y, Tuning());
            look = Vector2.zero;
        }

        /// <summary>
        /// Humanoid.GetAimDir postfix: the direction that lands the grenade on the chosen spot from the given spawn point (already turned back by the
        /// attack's launch angle, which the game adds afterwards). Expected outcome: false when there is no chosen spot (the look direction stays).
        /// </summary>
        internal static bool AimFor(Vector3 spawn, float launchAngle, out Vector3 aim)
        {
            aim = Vector3.zero;
            Vector3? target = Target;
            if (!target.HasValue) return false;
            LaunchSolution s = LaunchSolver.Solve(ToArc(spawn), ToArc(target.Value), ModSettings.GrenadeSpeed.Value, ModSettings.GrenadeGravity.Value, Time.fixedDeltaTime);
            aim = new Vector3(s.Direction.X, s.Direction.Y, s.Direction.Z);
            if (launchAngle != 0f) aim = Quaternion.AngleAxis(-launchAngle, Vector3.Cross(Vector3.up, aim)) * aim;
            if (!s.Reachable && ModLog.IsDue("grenade aim out of reach", 2f))
                ModLog.Every("grenade aim out of reach", 2f, "the spot " + target.Value.ToString("0.0") + " is beyond the throw; aiming as far towards it as possible (" + s.ElevationDegrees.ToString("0.0") + " deg)");
            return true;
        }

        static void Handle(Player player, AimEvent happened)
        {
            switch (happened)
            {
                case AimEvent.Started:
                    Begin(player);
                    break;
                case AimEvent.Thrown:
                    _latchedTarget = TargetPoint(player, _spot);
                    _latchedUntil = Time.time + LatchSeconds;
                    ModLog.Info("GrenadeAim: thrown onto " + _latchedTarget.ToString("0.0") + " (spot " + _spot + ")");
                    break;
                case AimEvent.Cancelled:
                    _latchedUntil = 0f;
                    ModLog.Info("GrenadeAim: cancelled with the right button, nothing thrown");
                    break;
                default:
                    ModLog.Verbose("GrenadeAim: nothing to do for " + happened);
                    break;
            }
        }

        // the spot starts where the arc already points, the camera direction is frozen for the whole aim
        static void Begin(Player player)
        {
            Vector3 look = player.GetLookDir();
            _forward = new Vector3(look.x, 0f, look.z);
            _forward = _forward.sqrMagnitude > 1e-6f ? _forward.normalized : player.transform.forward;
            Vector3? landing = ThrowArcView.LastLanding;
            float distance = DefaultStartDistance;
            if (landing.HasValue)
            {
                Vector3 d = landing.Value - player.transform.position;
                distance = new Vector2(d.x, d.z).magnitude;
            }
            _spot = AimSpot.Start(distance, Tuning());
            _latchedUntil = 0f;
            ModLog.Info("GrenadeAim: aiming started at " + _spot + (landing.HasValue ? " (where the arc pointed)" : " (no arc yet, default distance)"));
        }

        static bool Applies(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.IsDead()) return false;
            if (!ModSettings.FeatureOn(ModSettings.EnableGrenades)) return false;
            if (InventoryGui.IsVisible() || Menu.IsVisible()) return false;
            return GrenadeSetup.IsGrenade(player.GetCurrentWeapon());
        }

        // the landing spot dropped onto the ground below it, so the solver aims at the floor and not at the air
        static Vector3 TargetPoint(Player player, AimSpot spot)
        {
            Vector3 dir = Quaternion.AngleAxis(spot.SideDegrees, Vector3.up) * _forward;
            Vector3 p = player.transform.position + (dir * spot.Distance);
            RaycastHit hit;
            if (Physics.Raycast(p + (Vector3.up * 30f), Vector3.down, out hit, 80f, ThrowArcView.SolidsMask(), QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
            return p;
        }

        static AimTuning Tuning() => new AimTuning(ModSettings.GrenadeAimMetersPerUnit.Value, ModSettings.GrenadeAimDegreesPerUnit.Value,
            ModSettings.GrenadeAimMinDistance.Value, ModSettings.GrenadeAimMaxDistance.Value, ModSettings.GrenadeAimMaxSide.Value);

        static ArcVector ToArc(Vector3 v) => new ArcVector(v.x, v.y, v.z);
    }
}
