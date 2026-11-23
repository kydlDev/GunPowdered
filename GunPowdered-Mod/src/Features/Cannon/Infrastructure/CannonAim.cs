// kydl:read-only-observer - Player here is only the local player who aims; no creature is moved, commanded or spawned
using System.Collections.Generic;
using System.Globalization;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Grenades.Domain;
using GunPowdered.Core;

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
            if (mount == null || p == null) { if (RuleLog.Due("CannonAim.Begin:44", 2d)) ModLog.Every("CannonAim.Begin:44", 2d, $"CannonAim.Begin: returns false because mount == null || p == null (line 44)"); return false; }
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
            if (Current == null) { if (RuleLog.Due("CannonAim.End:58", 2d)) ModLog.Every("CannonAim.End:58", 2d, $"CannonAim.End: returns because Current == null (line 58)"); return; }
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
            if (p == null || p.IsDead() || Vector3.Distance(p.transform.position, mount.transform.position) > LeaveRange) { End("the player left the stock"); { if (RuleLog.Due("CannonAim.Tick:71", 2d)) ModLog.Every("CannonAim.Tick:71", 2d, $"CannonAim.Tick: returns because p == null || p.IsDead() || Vector3.Distance(p.transform.position, mount.transform.position) > LeaveRange (line 71)"); return; } }
            if (Time.frameCount > _startFrame && (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse"))) { End("the player stopped aiming"); { if (RuleLog.Due("CannonAim.Tick:72", 2d)) ModLog.Every("CannonAim.Tick:72", 2d, $"CannonAim.Tick: returns because Time.frameCount > _startFrame && (ZInput.GetButtonDown(\"Use\") || ZInput.GetButtonDown(\"JoyUse\")) (line 72)"); return; } }
            if (!mount.Status().Loaded) { End("the cannon is not loaded"); { if (RuleLog.Due("CannonAim.Tick:73", 2d)) ModLog.Every("CannonAim.Tick:73", 2d, $"CannonAim.Tick: returns because !mount.Status().Loaded (line 73)"); return; } }
            if (!_locked)
            {
                Vector3 spot = Spot(mount, p);
                bool reachable;
                _velocity = mount.AimAt(spot, out reachable);
                Target = spot;
                TargetDistance = Flat(mount.transform.position, spot);
            }
            DrawLine(mount);
            if (RuleLog.Due("cannon-aim", 2d)) ModLog.Every("cannon-aim", 2d, "CannonAim: aiming at " + Fmt(TargetDistance) + " m, velocity " + _velocity.ToString("0.0") + ", locked " + _locked);
        }

        /// <summary>
        /// The attack button while aiming (CannonAttackPatch): lights the fuse when a torch is in hand. Expected outcome: true when the button belongs
        /// to the cannon (the game's own attack is skipped), false when nobody aims.
        /// </summary>
        internal static bool AttackPressed(Player p)
        {
            if (Current == null || p == null || p != Player.m_localPlayer) { if (RuleLog.Due("CannonAim.AttackPressed:92", 2d)) ModLog.Every("CannonAim.AttackPressed:92", 2d, $"CannonAim.AttackPressed: returns false because Current == null || p == null || p != Player.m_localPlayer (line 92)"); return false; }
            if (_locked) { if (RuleLog.Due("CannonAim.AttackPressed:93", 2d)) ModLog.Every("CannonAim.AttackPressed:93", 2d, $"CannonAim.AttackPressed: returns true because _locked (line 93)"); return true; }
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
            if (RuleLog.Due("CannonAim.Spot", 2d)) ModLog.Every("CannonAim.Spot", 2d, $"CannonAim.Spot(mount={RuleLog.Shown(mount)}, p={RuleLog.Shown(p)})");
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
            { var returnedAt125 = spot; if (RuleLog.Due("CannonAim.Spot:125", 2d)) ModLog.Every("CannonAim.Spot:125", 2d, $"CannonAim.Spot: returns {returnedAt125} (line 125)"); return returnedAt125; }
        }

        static Vector3 LookPoint(CannonMount mount, Player p)
        {
            if (RuleLog.Due("CannonAim.LookPoint", 2d)) ModLog.Every("CannonAim.LookPoint", 2d, $"CannonAim.LookPoint(mount={RuleLog.Shown(mount)}, p={RuleLog.Shown(p)})");
            Transform cam = GameCamera.instance != null ? GameCamera.instance.transform : p.transform;
            int n = Physics.RaycastNonAlloc(cam.position, cam.forward, Hits, RayLength, CannonBallFlight.Mask(), QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 point = cam.position + (cam.forward * RayLength);
            for (int i = 0; i < n; i++)
            {
                Transform t = Hits[i].collider != null ? Hits[i].collider.transform : null;
                if (t == null || t.IsChildOf(p.transform) || t.IsChildOf(mount.transform) || Hits[i].distance >= best) { if (RuleLog.Due("CannonAim.LookPoint:138", 2d)) ModLog.Every("CannonAim.LookPoint:138", 2d, $"CannonAim.LookPoint: skips to the next item (continue) because t == null || t.IsChildOf(p.transform) || t.IsChildOf(mount.transform) || Hits[i].distance >= best (line 138)"); continue; }
                best = Hits[i].distance;
                point = Hits[i].point;
            }
            { var returnedAt142 = point; if (RuleLog.Due("CannonAim.LookPoint:142", 2d)) ModLog.Every("CannonAim.LookPoint:142", 2d, $"CannonAim.LookPoint: returns {returnedAt142} (line 142)"); return returnedAt142; }
        }

        static void DrawLine(CannonMount mount)
        {
            if (RuleLog.Due("CannonAim.DrawLine", 2d)) ModLog.Every("CannonAim.DrawLine", 2d, $"CannonAim.DrawLine(mount={RuleLog.Shown(mount)})");
            float meters = ModSettings.CannonPreviewMeters.Value;
            if (_line == null) _line = MakeLine();
            if (meters <= 0f || mount.Muzzle == null || _velocity.sqrMagnitude <= 0f) { _line.enabled = false; { if (RuleLog.Due("CannonAim.DrawLine:150", 2d)) ModLog.Every("CannonAim.DrawLine:150", 2d, $"CannonAim.DrawLine: returns because meters <= 0f || mount.Muzzle == null || _velocity.sqrMagnitude <= 0f (line 150)"); return; } }
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
            if (RuleLog.Due("CannonAim.MakeLine", 2d)) ModLog.Every("CannonAim.MakeLine", 2d, $"CannonAim.MakeLine()");
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
            { var returnedAt174 = lr; if (RuleLog.Due("CannonAim.MakeLine:174", 2d)) ModLog.Every("CannonAim.MakeLine:174", 2d, $"CannonAim.MakeLine: returns {returnedAt174} (line 174)"); return returnedAt174; }
        }

        static float GroundY(Vector3 spot)
        {
            if (RuleLog.Due("CannonAim.GroundY", 2d)) ModLog.Every("CannonAim.GroundY", 2d, $"CannonAim.GroundY(spot={RuleLog.Shown(spot)})");
            RaycastHit h;
            int ground = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
            if (Physics.Raycast(spot + (Vector3.up * 60f), Vector3.down, out h, 200f, ground, QueryTriggerInteraction.Ignore)) { var returnedAt182 = h.point.y; if (RuleLog.Due("CannonAim.GroundY:182", 2d)) ModLog.Every("CannonAim.GroundY:182", 2d, $"CannonAim.GroundY: returns {returnedAt182} because Physics.Raycast(spot + (Vector3.up * 60f), Vector3.down, out h, 200f, ground, QueryTriggerInteraction.Ignore) (line 182)"); return returnedAt182; }
            { var returnedAt183 = ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(spot) : spot.y; if (RuleLog.Due("CannonAim.GroundY:183", 2d)) ModLog.Every("CannonAim.GroundY:183", 2d, $"CannonAim.GroundY: returns {returnedAt183} (line 183)"); return returnedAt183; }
        }

        static Vector3 Flatten(Vector3 v)
        {
            if (RuleLog.Due("CannonAim.Flatten", 2d)) ModLog.Every("CannonAim.Flatten", 2d, $"CannonAim.Flatten(v={RuleLog.Shown(v)})");
            v.y = 0f;
            { var returnedAt190 = v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward; if (RuleLog.Due("CannonAim.Flatten:190", 2d)) ModLog.Every("CannonAim.Flatten:190", 2d, $"CannonAim.Flatten: returns {returnedAt190} (line 190)"); return returnedAt190; }
        }

        static float Flat(Vector3 a, Vector3 b) => new Vector2(b.x - a.x, b.z - a.z).magnitude;
        static ArcVector ToArc(Vector3 v) => new ArcVector(v.x, v.y, v.z);
        static string Fmt(float f) => f.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
