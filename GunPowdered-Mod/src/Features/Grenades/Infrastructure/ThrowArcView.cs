using System.Collections.Generic;
using System.Reflection;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Features.Grenades.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.Grenades.Infrastructure
{
    /// <summary>
    /// While the local player holds a Spiked Grenade: a dashed line along the flight and a ring where it lands (D-047, user: "the player can see a
    /// drawn arch there the explosive might land"). The flight is stepped with ThrowArcRules exactly like the game's Projectile, from the spawn point
    /// and aim the game's Attack uses (GetProjectileSpawnPoint: origin + up x height + forward x range + right x offset, aim = look direction), so the
    /// thrown grenade follows the line. The ring is red when the thrower stands inside the blast. One object for the whole game, made by the feature.
    /// </summary>
    internal sealed class ThrowArcView : MonoBehaviour
    {
        const int RingSegments = 48;
        const float RingLift = 0.08f, LineWidth = 0.05f;
        static readonly Color Safe = new Color(1f, 0.85f, 0.45f, 0.9f), Danger = new Color(1f, 0.2f, 0.15f, 0.95f);

        readonly List<ArcVector> _points = new List<ArcVector>(400);
        Vector3[] _linePositions = new Vector3[400];
        readonly Vector3[] _ringPositions = new Vector3[RingSegments];
        LineRenderer _line, _ring;
        int _mask;
        bool _shown;
        string _stoppedBy = "nothing";

        /// <summary>The last computed landing point and whether the thrower was inside the blast (read by the TestLab hook that compares it with the real throw).</summary>
        internal static Vector3? LastLanding { get; private set; }
        internal static bool LastInBlast { get; private set; }

        /// <summary>Makes the one arc view. Expected outcome: a live view object (kept across scenes).</summary>
        internal static ThrowArcView Create()
        {
            var go = new GameObject("GP_ThrowArcView");
            DontDestroyOnLoad(go);
            var view = go.AddComponent<ThrowArcView>();
            ModLog.Verbose("ThrowArcView: created");
            return view;
        }

        void Awake()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ThrowArcView.Awake()");
            var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = DashTexture() };
            _line = MakeLine("GP_ArcLine", mat, LineWidth, false);
            _line.textureMode = LineTextureMode.Tile;
            _ring = MakeLine("GP_ArcRing", new Material(Shader.Find("Sprites/Default")), LineWidth * 1.6f, true);
            Show(false);
        }

        LineRenderer MakeLine(string name, Material mat, float width, bool loop)
        {
            if (RuleLog.IsOn()) ModLog.Every("ThrowArcView.MakeLine", 2d, $"ThrowArcView.MakeLine(name={RuleLog.Shown(name)}, mat={RuleLog.Shown(mat)}, width={RuleLog.Shown(width)}, loop={RuleLog.Shown(loop)})");
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var lr = child.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.widthMultiplier = width;
            lr.loop = loop;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCapVertices = 2;
            { var returnedAt68 = lr; if (RuleLog.IsOn()) ModLog.Every("ThrowArcView.MakeLine:68", 2d, $"ThrowArcView.MakeLine: returns {RuleLog.Shown(returnedAt68)} (line 68)"); return returnedAt68; }
        }

        // white dashes on a clear background; the line tiles it once per metre-ish (width-relative tiling)
        static Texture2D DashTexture()
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ThrowArcView.DashTexture()");
            var tex = new Texture2D(8, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            for (int x = 0; x < 8; x++) tex.SetPixel(x, 0, x < 5 ? Color.white : Color.clear);
            tex.Apply();
            { var returnedAt78 = tex; if (RuleLog.IsOn()) ModLog.Verbose($"ThrowArcView.DashTexture: returns {RuleLog.Shown(returnedAt78)} (line 78)"); return returnedAt78; }
        }

        void LateUpdate()
        {
            Player player = Player.m_localPlayer;
            ItemDrop.ItemData held = player != null ? player.GetCurrentWeapon() : null;
            bool want = ModSettings.FeatureOn(ModSettings.EnableGrenades) && player != null && !player.IsDead() && GrenadeSetup.IsGrenade(held)
                && !InventoryGui.IsVisible() && !Menu.IsVisible();
            if (!want)
            {
                if (_shown) ModLog.Verbose("ThrowArcView: hidden (" + (player == null ? "no player" : !GrenadeSetup.IsGrenade(held) ? "no grenade in hand" : "menu open, dead or feature off") + ")");
                Show(false);
                { if (RuleLog.IsOn()) ModLog.Every("ThrowArcView.LateUpdate:91", 2d, "ThrowArcView.LateUpdate: returns (line 91)"); return; }
            }
            GrenadeSetup.ApplyIfChanged();     // the flight always has the values the arc is drawn with
            Draw(player, held.m_shared.m_attack);
        }

        void Draw(Player player, Attack attack)
        {
            Vector3 spawn, aim;
            SpawnAndAim(player, attack, out spawn, out aim);
            ArcResult r = ThrowArcRules.Simulate(ToArc(spawn), ThrowArcRules.LaunchVelocity(ToArc(aim), ModSettings.GrenadeSpeed.Value),
                ModSettings.GrenadeGravity.Value, 0f, Time.fixedDeltaTime, ModSettings.GrenadeFuseSeconds.Value, HitFraction, _points);

            if (_linePositions.Length < _points.Count) _linePositions = new Vector3[_points.Count * 2];
            for (int i = 0; i < _points.Count; i++) _linePositions[i] = ToUnity(_points[i]);
            _line.positionCount = _points.Count;
            _line.SetPositions(_linePositions);

            float radius = ModSettings.GrenadeRadius.Value;
            Vector3 landing = ToUnity(r.Landing);
            bool inBlast = ThrowArcRules.InBlast(ToArc(player.GetCenterPoint()), r.Landing, radius);
            Color c = inBlast ? Danger : Safe;
            _line.startColor = c;
            _line.endColor = c;
            _ring.startColor = c;
            _ring.endColor = c;
            if (r.Landed) PlaceRing(landing, radius);
            Show(true);
            _ring.enabled = r.Landed;   // no landing within the fuse: the line ends in the air and there is no ring

            if (inBlast != LastInBlast) ModLog.State("grenade arc ring", LastInBlast ? "red" : "normal", inBlast ? "red" : "normal",
                "thrower " + player.GetCenterPoint().ToString("0.0") + " is " + Vector3.Distance(player.GetCenterPoint(), landing).ToString("0.0") + " m from the landing, radius " + radius + " m");
            LastLanding = r.Landed ? landing : (Vector3?)null;
            LastInBlast = inBlast;
            if (ModLog.IsDue("grenade arc", 3f))
                ModLog.Every("grenade arc", 3f, "spawn " + spawn.ToString("0.00") + ", aim " + aim.ToString("0.00") + ", speed " + ModSettings.GrenadeSpeed.Value
                    + " m/s, " + (r.Landed ? "lands at " + landing.ToString("0.0") + " after " + r.Seconds.ToString("0.00") + " s, " + Vector3.Distance(spawn, landing).ToString("0.0") + " m" : "no landing within the fuse")
                    + ", " + _points.Count + " points, ring " + (inBlast ? "red" : "normal") + ", last stopped by " + _stoppedBy);
        }

        /// <summary>
        /// The spawn point and aim of the throw as Attack.GetProjectileSpawnPoint computes them (1.0.21). The player turns to the look direction when
        /// the attack starts, so forward is the flat look direction rather than the body's current facing.
        /// </summary>
        internal static void SpawnAndAim(Player player, Attack attack, out Vector3 spawn, out Vector3 aim)
        {
            if (RuleLog.IsOn()) ModLog.Every("ThrowArcView.SpawnAndAim", 2d, $"ThrowArcView.SpawnAndAim(player={RuleLog.Shown(player)}, attack={RuleLog.Shown(attack)})");
            aim = player.GetLookDir();
            Transform origin = string.IsNullOrEmpty(attack.m_attackOriginJoint) ? player.transform : Utils.FindChild(player.GetVisual().transform, attack.m_attackOriginJoint);
            if (origin == null) origin = player.transform;
            Vector3 forward = new Vector3(aim.x, 0f, aim.z);
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : player.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            spawn = origin.position + (Vector3.up * attack.m_attackHeight) + (forward * attack.m_attackRange) + (right * attack.m_attackOffset);
            Vector3 chosen;
            if (GrenadeAim.AimFor(spawn, attack.m_launchAngle, out chosen)) aim = chosen;     // D-050: aiming (or just thrown) = the direction onto the chosen spot
            if (attack.m_launchAngle != 0f) aim = Quaternion.AngleAxis(attack.m_launchAngle, Vector3.Cross(Vector3.up, aim)) * aim;
        }

        // the game's projectile collides with s_rayMaskSolids; read it so the preview hits the same things (fallback: the usual solid layers)
        float? HitFraction(ArcVector from, ArcVector to)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"ThrowArcView.HitFraction(from={RuleLog.Shown(from)}, to={RuleLog.Shown(to)})");
            if (_mask == 0) _mask = SolidsMask();
            Vector3 a = ToUnity(from), b = ToUnity(to), d = b - a;
            float len = d.magnitude;
            if (len <= 0f) { if (RuleLog.IsOn()) ModLog.Verbose("ThrowArcView.HitFraction: returns null (line 157)"); return null; }
            RaycastHit hit;
            if (Physics.Raycast(a, d / len, out hit, len, _mask, QueryTriggerInteraction.Ignore) && !IsLocalPlayer(hit.collider))
            {
                _stoppedBy = hit.collider.name + " (layer " + LayerMask.LayerToName(hit.collider.gameObject.layer) + ", root " + hit.collider.transform.root.name + ")";
                { var returnedAt162 = hit.distance / len; if (RuleLog.IsOn()) ModLog.Verbose($"ThrowArcView.HitFraction: returns {RuleLog.Shown(returnedAt162)} (line 162)"); return returnedAt162; }
            }
            { if (RuleLog.IsOn()) ModLog.Verbose("ThrowArcView.HitFraction: returns null (line 164)"); return null; }
        }

        static bool IsLocalPlayer(Collider c) => Player.m_localPlayer != null && c.GetComponentInParent<Player>() == Player.m_localPlayer;

        static int _solidsMask;

        /// <summary>The layers the game's projectile hits (cached); the aim drops the chosen spot onto the same ground the grenade lands on.</summary>
        internal static int SolidsMask()
        {
            if (RuleLog.IsOn()) ModLog.Every("ThrowArcView.SolidsMask", 2d, $"ThrowArcView.SolidsMask()");
            if (_solidsMask == 0) _solidsMask = ReadSolidsMask();
            { var returnedAt176 = _solidsMask; if (RuleLog.IsOn()) ModLog.Every("ThrowArcView.SolidsMask:176", 2d, $"ThrowArcView.SolidsMask: returns {RuleLog.Shown(returnedAt176)} (line 176)"); return returnedAt176; }
        }

        static int ReadSolidsMask()
        {
            FieldInfo f = typeof(global::Projectile).GetField("s_rayMaskSolids", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            int game = f != null ? (int)f.GetValue(null) : 0;
            int mask = game != 0 ? game : LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net",
                "character_ghost", "hitbox", "character_noenv", "vehicle");
            ModLog.Verbose("ThrowArcView: ray mask " + mask + (game != 0 ? " (the game's projectile mask)" : " (fallback, the game's mask was not set yet)"));
            return mask;
        }

        // the ring sits on the ground around the landing: each point is dropped onto what is below it
        void PlaceRing(Vector3 centre, float radius)
        {
            if (RuleLog.IsOn()) ModLog.Every("ThrowArcView.PlaceRing", 2d, $"ThrowArcView.PlaceRing(centre={RuleLog.Shown(centre)}, radius={RuleLog.Shown(radius)})");
            for (int i = 0; i < RingSegments; i++)
            {
                float a = i * Mathf.PI * 2f / RingSegments;
                Vector3 p = centre + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                RaycastHit hit;
                if (Physics.Raycast(p + (Vector3.up * 3f), Vector3.down, out hit, 8f, _mask, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
                _ringPositions[i] = p + (Vector3.up * RingLift);
            }
            _ring.positionCount = RingSegments;
            _ring.SetPositions(_ringPositions);
        }

        void Show(bool on)
        {
            if (_shown == on) { if (RuleLog.IsOn()) ModLog.Every("ThrowArcView.Show:207", 2d, "ThrowArcView.Show: returns (line 207)"); return; }
            _shown = on;
            if (_line != null) _line.enabled = on;
            if (_ring != null) _ring.enabled = on;
            if (!on) LastLanding = null;
            ModLog.State("grenade arc", on ? "hidden" : "shown", on ? "shown" : "hidden", on ? "a Spiked Grenade is in hand" : "no grenade in hand / menu / feature off");
        }

        void OnDestroy() => ModLog.Verbose("ThrowArcView: destroyed");

        static ArcVector ToArc(Vector3 v) => new ArcVector(v.x, v.y, v.z);

        static Vector3 ToUnity(ArcVector v) => new Vector3(v.X, v.Y, v.Z);
    }
}
