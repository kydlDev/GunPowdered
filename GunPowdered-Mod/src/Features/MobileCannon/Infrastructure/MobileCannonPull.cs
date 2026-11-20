using System.Collections.Generic;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Features.MobileCannon.Domain;
using GunPowdered.Core;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// The player who moves a Mobile Cannon by hand (D-105, D-106): the cart joint does the moving; this part caps the walking speed (through the
    /// Harmony speed patch), turns the player to face the cannon, uses stamina while moving, and lets go when the stamina is gone.
    /// </summary>
    internal sealed class MobileCannonPull : MonoBehaviour
    {
        static readonly AccessTools.FieldRef<Vagon, GameObject> AttachedRef = AccessTools.FieldRefAccess<Vagon, GameObject>("m_attachedObject");
        static readonly List<MobileCannonPull> Instances = new List<MobileCannonPull>();

        Vagon _vagon;
        GameObject _lastHitched;

        /// <summary>Since the last hitch: the fastest the hitched local player moved (m/s) and the stamina used (for the test hooks).</summary>
        internal float MaxSpeedSeen { get; private set; }

        internal float StaminaUsed { get; private set; }

        void Awake()
        {
            if (RuleLog.On) ModLog.Verbose($"MobileCannonPull.Awake()");
            _vagon = GetComponent<Vagon>();
            Instances.Add(this);
        }

        void OnDestroy() => Instances.Remove(this);

        Transform _gripA, _gripB;

        /// <summary>True when the hands were put on the push handle in the last frame (for the test hooks).</summary>
        internal bool HandsOnHandle { get; private set; }

        void LateUpdate()
        {
            if (RuleLog.On) ModLog.Every("MobileCannonPull.LateUpdate", 2d, $"MobileCannonPull.LateUpdate()");
            Character c = Hitched;
            if (c == null)
            {
                HandsOnHandle = false;
                { if (RuleLog.On) ModLog.Every("MobileCannonPull.LateUpdate:49", 2d, "MobileCannonPull.LateUpdate: returns (line 49)"); return; }
            }
            if (_gripA == null)
            {
                Transform model = transform.Find(MobileCannonSetup.ModelName);
                _gripA = model != null ? model.Find(MobileCannonSetup.GripA) : null;
                _gripB = model != null ? model.Find(MobileCannonSetup.GripB) : null;
            }
            Vector3 towards = transform.position - c.transform.position;
            towards.y = 0f;
            HandsOnHandle = MobileCannonHands.Hold(c.GetComponentInChildren<Animator>(), _gripA, _gripB, towards.normalized);
        }

        /// <summary>The character hitched to this cannon (null when none).</summary>
        internal Character Hitched
        {
            get
            {
                GameObject go = _vagon != null ? AttachedRef(_vagon) : null;
                return go != null ? go.GetComponent<Character>() : null;
            }
        }

        /// <summary>The cannon a character is hitched to (null when none): used by the speed and turning patches.</summary>
        internal static MobileCannonPull PulledBy(Character c)
        {
            if (RuleLog.On) ModLog.Every("MobileCannonPull.PulledBy", 2d, $"MobileCannonPull.PulledBy(c={c})");
            if (c == null) { if (RuleLog.On) ModLog.Every("MobileCannonPull.PulledBy:76", 2d, "MobileCannonPull.PulledBy: returns null (line 76)"); return null; }
            foreach (MobileCannonPull p in Instances)
                if (p != null && p.Hitched == c) { var returnedAt78 = p; if (RuleLog.On) ModLog.Every("MobileCannonPull.PulledBy:78", 2d, $"MobileCannonPull.PulledBy: returns {returnedAt78} (line 78)"); return returnedAt78; }
            { if (RuleLog.On) ModLog.Every("MobileCannonPull.PulledBy:79", 2d, "MobileCannonPull.PulledBy: returns null (line 79)"); return null; }
        }

        /// <summary>Where the character should face: towards the cannon's body, flat.</summary>
        internal Quaternion FacingFor(Character c)
        {
            if (RuleLog.On) ModLog.Every("MobileCannonPull.FacingFor", 2d, $"MobileCannonPull.FacingFor(c={c})");
            Vector3 to = transform.position - c.transform.position;
            to.y = 0f;
            { var returnedAt88 = to.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(to.normalized) : c.transform.rotation; if (RuleLog.On) ModLog.Every("MobileCannonPull.FacingFor:88", 2d, $"MobileCannonPull.FacingFor: returns {returnedAt88} (line 88)"); return returnedAt88; }
        }

        void Update()
        {
            Character c = Hitched;
            GameObject now = c != null ? c.gameObject : null;
            if (now != _lastHitched)
            {
                ModLog.Verbose("MobileCannonPull: " + name + " " + (c != null ? "hitched to " + c.name : "let go"));
                _lastHitched = now;
                MaxSpeedSeen = 0f;
                StaminaUsed = 0f;
            }
            Player p = c as Player;
            if (p == null || p != Player.m_localPlayer) { if (RuleLog.On) ModLog.Every("MobileCannonPull.Update:103", 2d, "MobileCannonPull.Update: returns (line 103)"); return; }
            Vector3 v = p.GetVelocity();
            float speed = new Vector3(v.x, 0f, v.z).magnitude;
            float cost = MobileCannonPullRules.StaminaCost(speed, Time.deltaTime, ModSettings.MobileCannonPullStamina.Value);
            if (cost > 0f) p.UseStamina(cost);
            StaminaUsed += cost;
            if (speed > MaxSpeedSeen) MaxSpeedSeen = speed;
            ModLog.Every("mobilecannon-pull", 2d, "MobileCannonPull: " + p.GetPlayerName() + " moves the cannon at " + speed.ToString("0.0") + " m/s, stamina " + p.GetStamina().ToString("0"));
            if (MobileCannonPullRules.LetsGo(p.GetStamina()))
            {
                _vagon.Interact(p, false, false);        // a second use lets go (the cart detaches in its Update)
                p.Message(MessageHud.MessageType.Center, "Too tired to move the cannon");
                ModLog.Decision("mobile cannon pull", "let go", "no stamina left");
            }
        }
    }
}
