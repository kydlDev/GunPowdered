using KydlCore.Common;
using UnityEngine;
using GunPowdered.Core;
using GunPowdered.Features.Skilling.Domain;
using GunPowdered.Features.Skilling.Infrastructure;

namespace GunPowdered.Features.Grenades.Infrastructure
{
    /// <summary>
    /// D-157 (user 2026-11-23: the Explosives skill, grenade = "Add a fuse after landing"): a Spiked Grenade no longer bursts the moment it hits. Where its
    /// flight ends this landed grenade appears (the flight's spawn-on-hit), lies on the ground with its fuse sparking, and bursts after 3 s (Explosives level 0)
    /// down to 1 s (level 100) of the THROWER - worked out on the thrower's client (the game hands it the owner through IProjectile.Setup) and kept in the
    /// object's ZDO, so whoever owns it later counts the same fuse. The burst is the grenade's own blast prefab, set up with the thrower as its owner.
    /// </summary>
    internal sealed class GrenadeFuse : MonoBehaviour, IProjectile
    {
        internal const string LitKey = "gp_gren_lit", FuseKey = "gp_gren_fuse";
        const float SparkEvery = 0.35f;

        ZNetView _nview;
        Character _owner;
        float _nextSpark;
        bool _burst;

        /// <summary>How many landed grenades this client has seen burst (test hooks).</summary>
        internal static int Bursts { get; private set; }

        /// <summary>The fuse (s) of the last landed grenade this client set up (test hooks).</summary>
        internal static float LastFuse { get; private set; }

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
            ModLog.Verbose("GrenadeFuse.Awake: at " + transform.position.ToString("0.0") + ", network view " + (_nview != null && _nview.IsValid()));
        }

        /// <summary>The game's spawn-on-hit hands over the thrower: the fuse is set here, on the thrower's client, from the thrower's Explosives level.</summary>
        public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
        {
            _owner = owner;
            float fuse = SkillRules.GrenadeFuseSeconds(GunSkills.Level(owner, GunSkills.Explosives));
            LastFuse = fuse;
            if (_nview != null && _nview.IsValid() && _nview.IsOwner())
            {
                _nview.GetZDO().Set(FuseKey, fuse);
                _nview.GetZDO().Set(LitKey, (long)(ZNet.instance.GetTimeSeconds() * 1000d));
            }
            if (owner == Player.m_localPlayer) GunSkills.Raise(owner, GunSkills.Explosives, SkillRules.GainLight);
            ModLog.Info("GrenadeFuse: a Spiked Grenade landed at " + transform.position.ToString("0.0") + ", fuse " + fuse.ToString("0.00") + " s (thrower " + (owner != null ? owner.name : "nobody") + ")");
        }

        public string GetTooltipString(int itemQuality) => "";

        void Update()
        {
            if (_burst || _nview == null || !_nview.IsValid() || !_nview.IsOwner()) { if (RuleLog.Due("GrenadeFuse.Update:skip", 2d)) ModLog.Every("GrenadeFuse.Update:skip", 2d, "GrenadeFuse.Update: burst, no view or not the owner - returns"); return; }
            ZDO zdo = _nview.GetZDO();
            long litMs = zdo.GetLong(LitKey, -1L);
            if (litMs < 0L) { zdo.Set(LitKey, (long)(ZNet.instance.GetTimeSeconds() * 1000d)); ModLog.Verbose("GrenadeFuse: no lit time (set up on another client) - starts now"); return; }
            float fuse = zdo.GetFloat(FuseKey, SkillRules.GrenadeFuseAtZero);
            double left = fuse - (ZNet.instance.GetTimeSeconds() - litMs / 1000d);
            if (Time.time >= _nextSpark) { _nextSpark = Time.time + SparkEvery; Spark(); }
            if (left > 0d) { if (RuleLog.Due("GrenadeFuse.Update:burning", 2d)) ModLog.Every("GrenadeFuse.Update:burning", 2d, "GrenadeFuse: " + left.ToString("0.0") + " s of fuse left"); return; }
            Burst();
        }

        // the fuse's sparks (a vanilla effect with a network view: made by the owner, seen by everyone)
        void Spark()
        {
            GameObject fx = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("vfx_torch_hit") : null;
            if (fx == null) { if (RuleLog.Due("GrenadeFuse.Spark:none", 2d)) ModLog.Every("GrenadeFuse.Spark:none", 2d, "GrenadeFuse.Spark: no vfx_torch_hit - returns"); return; }
            Object.Instantiate(fx, transform.position + Vector3.up * 0.15f, Quaternion.identity);
        }

        void Burst()
        {
            _burst = true;
            GameObject blast = GrenadeSetup.Explosion;
            if (blast != null)
            {
                GameObject b = Object.Instantiate(blast, transform.position, Quaternion.identity);
                IProjectile p = b.GetComponentInChildren<IProjectile>();
                if (p != null) p.Setup(_owner, Vector3.zero, 0f, null, null, null);      // the thrower owns the blast (friendly fire, who gets the kill)
            }
            bool hole = GunPowdered.Features.Dynamite.Infrastructure.CraterPlacer.Place(GrenadeSetup.Crater, transform.position, GunPowdered.Configuration.ModSettings.GrenadeCraterDepth.Value, "Spiked Grenade");   // D-182
            Bursts++;
            ModLog.Info("GrenadeFuse: burst at " + transform.position.ToString("0.0") + ", hole " + hole + (blast == null ? " (no blast prefab!)" : ""));
            _nview.Destroy();
        }
    }
}
