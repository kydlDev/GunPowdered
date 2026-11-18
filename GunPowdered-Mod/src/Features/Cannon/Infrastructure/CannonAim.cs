// kydl:read-only-observer - Player here is only the local player who aims; no creature is moved, commanded or spawned
using System.Collections.Generic;
using System.Globalization;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Features.Cannon.Infrastructure
{
    /// <summary>
    /// Aiming the mounted Hand Cannon (D-052 addendum, D-053): the local player takes control of one stock by interacting with it; the spot the camera
    /// looks at (within the cannon's range) is the aimed spot, the stock and barrel turn to it and a short line shows the first metres of the arc.
    /// The attack button with a torch in hand lights the fuse (CannonAttackPatch); the aim is then held until the shot. Use again, walking away or
    /// the shot ends the aim. One aim at a time, local only.
    /// </summary>
    internal static class CannonAim
    {
        const float LeaveRange = 4f, RayLength = 200f, LineWidth = 0.035f;
        static readonly List<ArcVector> Points = new List<ArcVector>();
        static readonly RaycastHit[] Hits = new RaycastHit[24];
        static int _startFrame;
        static LineRenderer _line;
        static Vector3 _velocity;
        static bool _locked;

        /// <summary>The stock being aimed now (null when nobody aims here).</summary>
        internal static CannonMount Current { get; private set; }
        /// <summary>The aimed spot and its flat distance from the stock.</summary>
        internal static Vector3? Target { get; private set; }
        internal static float TargetDistance { get; private set; }
        /// <summary>Test only: a fixed flat distance and side angle (degrees off the stock's forward) instead of the camera's spot; null = the camera.</summary>
        internal static Vector2? TestAim { get; set; }
        /// <summary>The answer of the last try to light the fuse ("lit" or the reason).</summary>
        internal static string LastLight { get; private set; } = "never tried";
        /// <summary>Why the last aim ended.</summary>
        internal static string LastEnd { get; private set; } = "never aimed";

        /// <summary>Takes control of the stock. Expected outcome: true when aiming started.</summary>
        internal static bool Begin(CannonMount mount, Player p)
        {
            if (mount == null || p == null) return false;
            if (Current != null && Current != mount) End("another stock was taken");
            Current = mount;
            _startFrame = Time.frameCount;
            _locked = false;
            mount.GetComponent<ZNetView>().ClaimOwnership();
            p.Message(MessageHud.MessageType.Center, "Aim with the camera - attack with a torch in hand lights the fuse");
            ModLog.Verbose("CannonAim: aiming started at " + mount.transform.position.ToString("0.0"));
            return true;
        }

        /// <summary>Gives the stock back. Expected outcome: nobody aims, the line is hidden.</summary>
        internal static void End(string why)
        {
            if (Current == null) return;
            Current = null;
            Target = null;
            _locked = false;
            if (_line != null) _line.enabled = false;
            LastEnd = why;
            ModLog.Verbose("CannonAim: aiming ended - " + why);
        }

        /// <summary>One frame of aiming, called by the aimed stock's Update.</summary>
        internal static void Tick(CannonMount mount)
        {
            Player p = Player.m_localPlayer;
            if (p == null || p.IsDead() || Vector3.Distance(p.transform.position, mount.transform.position) > LeaveRange) { End("the player left the stock"); return; }
            if (Time.frameCount > _startFrame && (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse"))) { End("the player stopped aiming"); return; }
            if (!mount.Status().Loaded) { End("the cannon is not loaded"); return; }
            if (!_locked)
            {
                Vector3 spot = Spot(mount, p);
                bool reachable;
                _velocity = mount.AimAt(spot, out reachable);
                Target = spot;
                TargetDistance = Flat(mount.transform.position, spot);
            }
            DrawLine(mount);
            ModLog.Every("cannon-aim", 2d, "CannonAim: aiming at " + Fmt(TargetDistance) + " m, velocity " + _velocity.ToString("0.0") + ", locked " + _locked);
        }

        /// <summary>
        /// The attack button while aiming (CannonAttackPatch): lights the fuse when a torch is in hand. Expected outcome: true when the button belongs
        /// to the cannon (the game's own attack is skipped), false when nobody aims.
        /// </summary>
        internal static bool AttackPressed(Player p)
        {
            if (Current == null || p == null || p != Player.m_localPlayer) return false;
            if (_locked) return true;
            string why = Current.Light(p, _velocity, MuzzleTargetFlat());
            LastLight = why ?? "lit";
            if (why != null) p.Message(MessageHud.MessageType.Center, why);
            else _locked = true;
            ModLog.Verbose("CannonAim: light the fuse - " + LastLight);
            return true;
        }

        // the aimed spot's flat distance from the muzzle (the landing rule counts from where the ball leaves)
        static float MuzzleTargetFlat() => Current != null && Current.Muzzle != null && Target.HasValue ? Flat(Current.Muzzle.position, Target.Value) : TargetDistance;

        // where the camera looks, clamped into the cannon's range, on the ground
        static Vector3 Spot(CannonMount mount, Player p)
        {
            Vector3 origin = mount.transform.position, dir;
            float wanted;
            if (TestAim.HasValue)
            {
                dir = Quaternion.Euler(0f, TestAim.Value.y, 0f) * Flatten(mount.transform.forward);
                wanted = TestAim.Value.x;
            }
            else
            {
                Vector3 look = LookPoint(mount, p);
                dir = Flatten(look - origin);
                wanted = Flat(origin, look);
            }
            float d = CannonRules.ClampAim(wanted, ModSettings.CannonMinRange.Value, ModSettings.CannonRange.Value);
            Vector3 spot = origin + (dir * d);
            spot.y = GroundY(spot);
            return spot;
        }

        static Vector3 LookPoint(CannonMount mount, Player p)
        {
            Transform cam = GameCamera.instance != null ? GameCamera.instance.transform : p.transform;
            int n = Physics.RaycastNonAlloc(cam.position, cam.forward, Hits, RayLength, CannonBallFlight.Mask(), QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 point = cam.position + (cam.forward * RayLength);
            for (int i = 0; i < n; i++)
            {
                Transform t = Hits[i].collider != null ? Hits[i].collider.transform : null;
                if (t == null || t.IsChildOf(p.transform) || t.IsChildOf(mount.transform) || Hits[i].distance >= best) continue;
                best = Hits[i].distance;
                point = Hits[i].point;
            }
            return point;
        }

        static void DrawLine(CannonMount mount)
        {
            float meters = ModSettings.CannonPreviewMeters.Value;
            if (_line == null) _line = MakeLine();
            if (meters <= 0f || mount.Muzzle == null || _velocity.sqrMagnitude <= 0f) { _line.enabled = false; return; }
            ArcVector start = ToArc(mount.Muzzle.position);
            float len = 0f;
            ThrowArcRules.Simulate(start, ToArc(_velocity), ModSettings.CannonGravity.Value, 0f, Time.fixedDeltaTime, 2f,
                (a, b) => { len += a.DistanceTo(b); return len >= meters ? 1f : (float?)null; }, Points);
            _line.positionCount = Points.Count;
            for (int i = 0; i < Points.Count; i++) _line.SetPosition(i, new Vector3(Points[i].X, Points[i].Y, Points[i].Z));
            _line.enabled = true;
        }

        static LineRenderer MakeLine()
        {
            var go = new GameObject("GP_CannonAimLine");
            Object.DontDestroyOnLoad(go);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            lr.startWidth = LineWidth;
            lr.endWidth = LineWidth * 0.4f;
            lr.startColor = new Color(1f, 0.75f, 0.3f, 0.9f);
            lr.endColor = new Color(1f, 0.55f, 0.2f, 0.2f);
            lr.useWorldSpace = true;
            lr.numCapVertices = 2;
            lr.enabled = false;
            return lr;
        }

        static float GroundY(Vector3 spot)
        {
            RaycastHit h;
            int ground = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
            if (Physics.Raycast(spot + (Vector3.up * 60f), Vector3.down, out h, 200f, ground, QueryTriggerInteraction.Ignore)) return h.point.y;
            return ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(spot) : spot.y;
        }

        static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        static float Flat(Vector3 a, Vector3 b) => new Vector2(b.x - a.x, b.z - a.z).magnitude;
        static ArcVector ToArc(Vector3 v) => new ArcVector(v.x, v.y, v.z);
        static string Fmt(float f) => f.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
