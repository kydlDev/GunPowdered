using System;
using System.Collections;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Cannon.Infrastructure;
using GunPowdered.Features.MobileCannon.Domain;

namespace GunPowdered.Features.MobileCannon.Infrastructure
{
    /// <summary>
    /// Loading and firing one Mobile Cannon (D-045, D-104). At the muzzle the player loads gunpowder and one Flametal Cannon Ball or better (D-145) (a few seconds, staying close);
    /// with a torch in hand on the touch hole the fuse burns (sparks), then the ball leaves the barrel the way the crank set it and the carriage rolls
    /// back. The load and the fuse live in the piece's ZDO, so a loaded cannon stays loaded after a relog. The ball's flight is the Hand
    /// Cannon's (CannonBallFlight, D-055 / D-057); its damage is the Mobile Cannon's part plus the ball's (D-145).
    /// </summary>
    internal sealed class MobileCannonGun : MonoBehaviour
    {
        internal const string KeyLoaded = "gp_mc_loaded", KeyFuse = "gp_mc_fuse", KeyBall = "gp_mc_ball";   // D-145: which ball is loaded
        const string Barrel = "cannon_barrel", MuzzleName = "Muzzle", TouchHoleName = "TouchHole";
        const float UseRange = 3.5f, AnimEvery = 1.2f, AnimTail = 0.9f, SparkEvery = 0.12f;

        ZNetView _nview;
        Rigidbody _body;
        Transform _barrel, _muzzle, _touchHole;
        bool _loading, _fireQueued;

        internal bool Loaded => IsValid && _nview.GetZDO().GetBool(KeyLoaded, false);

        /// <summary>The ball in the barrel (D-145; a cannon loaded before D-145 holds an Iron Cannon Ball).</summary>
        internal string LoadedBall => CannonAmmoRules.LoadedOrOld(IsValid ? _nview.GetZDO().GetString(KeyBall, "") : "");

        internal bool Lit => _fireQueued;

        internal bool Loading => _loading;

        /// <summary>"loading...", "fuse burning!", "loaded" or "empty".</summary>
        internal string StateText => Loading ? "loading..." : Lit ? "fuse burning!" : Loaded ? "loaded" : "empty";

        /// <summary>D-182: what the player sees looking at the muzzle - or anywhere on the cannon but its handle.</summary>
        internal string LoadHoverText() => Localization.instance.Localize("Mobile Cannon (" + StateText + ")"
            + (CannonLoadRules.OffersLoad(Loaded, Lit, Loading) ? "\n[<color=yellow><b>$KEY_Use</b></color>] Load gunpowder and a Flametal Cannon Ball" : ""));   // no refill prompt until it is empty (user 2026-11-26)

        internal bool IsValid => _nview != null && _nview.IsValid();

        internal Transform MuzzlePoint => _muzzle;

        internal Transform TouchHolePoint => _touchHole;

        void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _body = GetComponent<Rigidbody>();
            Transform model = transform.Find(MobileCannonSetup.ModelName);
            _barrel = model != null ? model.Find(Barrel) : null;
            if (_barrel != null && model != null)
            {
                // the muzzle and touch hole are empties of the model; they ride on the barrel so they follow the crank
                _muzzle = Point(model.Find(MuzzleName), "gp_muzzle");
                _touchHole = Point(model.Find(TouchHoleName), "gp_touchhole");
                // D-182: the muzzle keeps its small box - a 0.7 m solid box is part of the cart's body and tipped its nose down (a19play1 mobilecannon-fire:
                // the ball landed 2 m away); looking anywhere on the cannon but its handle loads it instead (MobileCannonUsePatch)
                Handle<MobileCannonMuzzle>(_muzzle, new Vector3(0.3f, 0.3f, 0.35f));
                Handle<MobileCannonTouchHole>(_touchHole, new Vector3(0.2f, 0.2f, 0.2f));
            }
            WearNTear wnt = GetComponent<WearNTear>();
            if (wnt != null) wnt.m_onDestroyed = (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(OnDestroyed));
            ModLog.Verbose("MobileCannonGun: " + name + " barrel " + (_barrel != null) + ", muzzle " + (_muzzle != null) + ", touch hole " + (_touchHole != null) + ", loaded " + Loaded);
        }

        Transform Point(Transform empty, string name)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonGun.Point(empty={RuleLog.Shown(empty)}, name={RuleLog.Shown(name)})");
            if (empty == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(_barrel, false);
            go.transform.position = empty.position;
            go.transform.rotation = _barrel.rotation;
            { var returnedAt72 = go.transform; if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonGun.Point: returns {returnedAt72} (line 72)"); return returnedAt72; }
        }

        void Handle<T>(Transform at, Vector3 size) where T : MobileCannonPart
        {
            if (RuleLog.Due("MobileCannonGun.Handle", 2d)) ModLog.Every("MobileCannonGun.Handle", 2d, $"MobileCannonGun.Handle(at={RuleLog.Shown(at)}, size={RuleLog.Shown(size)})");
            if (at == null) return;
            at.gameObject.layer = gameObject.layer;
            BoxCollider box = at.gameObject.AddComponent<BoxCollider>();
            box.size = size;
            at.gameObject.AddComponent<T>().Gun = this;
        }

        /// <summary>Starts loading at the muzzle. Expected outcome: true when the loading started (false with the reason shown to the player).</summary>
        internal bool Load(Player p)
        {
            if (RuleLog.Due("MobileCannonGun.Load", 2d)) ModLog.Every("MobileCannonGun.Load", 2d, $"MobileCannonGun.Load(p={RuleLog.Shown(p)})");
            if (p == null || !IsValid) return false;
            if (_loading) { var returnedAt90 = Tell(p, "Already loading"); if (RuleLog.Due("MobileCannonGun.Load:90", 2d)) ModLog.Every("MobileCannonGun.Load:90", 2d, $"MobileCannonGun.Load: returns {returnedAt90} because _loading (line 90)"); return returnedAt90; }
            Inventory inv = p.GetInventory();
            string why = MobileCannonFireRules.WhyNotLoad(Loaded, CannonInventory.Count(inv, GunPowderedPrefabs.Gunpowder), Powder(), CannonAmmoRules.CountTaken(CannonAmmoRules.MobileCannonMinTier, CannonInventory.BallsCarried(inv)));
            if (why != null) { var returnedAt93 = Tell(p, why); if (RuleLog.Due("MobileCannonGun.Load:93", 2d)) ModLog.Every("MobileCannonGun.Load:93", 2d, $"MobileCannonGun.Load: returns {returnedAt93} because why != null (line 93)"); return returnedAt93; }
            StartCoroutine(LoadAtMuzzle(p));
            { if (RuleLog.Due("MobileCannonGun.Load:95", 2d)) ModLog.Every("MobileCannonGun.Load:95", 2d, $"MobileCannonGun.Load: returns true (line 95)"); return true; }
        }

        IEnumerator LoadAtMuzzle(Player p)
        {
            _loading = true;
            float seconds = GunPowdered.Features.Skilling.Domain.SkillRules.CannonLoadSeconds(ModSettings.MobileCannonLoadSeconds.Value, GunPowdered.Features.Skilling.Infrastructure.GunSkills.Level(p, GunPowdered.Features.Skilling.Infrastructure.GunSkills.Cannons)), end = Time.time + seconds, nextAnim = Time.time;
            ModLog.Verbose("MobileCannonGun: loading for " + seconds.ToString("0.0") + " s");
            while (Time.time < end)
            {
                if (p == null || p.IsDead() || !IsValid || _muzzle == null || Vector3.Distance(p.transform.position, _muzzle.position) > UseRange)
                {
                    _loading = false;
                    if (p != null) p.Message(MessageHud.MessageType.Center, "Loading stopped");
                    ModLog.Verbose("MobileCannonGun: loading stopped (the player left)");
                    yield break;
                }
                // user 2026-11-26: the moment the cannon is filled (another player, an earlier load) the fill animation stops; none starts in the last AnimTail s
                CannonLoadStep step = CannonLoadRules.Step(Loaded, Time.time, nextAnim, end, AnimTail);
                if (step == CannonLoadStep.StopFilled)
                {
                    _loading = false;
                    Tell(p, MobileCannonFireRules.AlreadyLoaded);
                    ModLog.Decision("mobile cannon", "loading stopped", "the cannon is already filled - no more fill animation");
                    yield break;
                }
                if (step == CannonLoadStep.Animate)
                {
                    p.GetZAnim().SetTrigger("interact");
                    nextAnim += AnimEvery;
                }
                yield return null;
            }
            _loading = false;
            Inventory inv = p.GetInventory();
            string why = MobileCannonFireRules.WhyNotLoad(Loaded, CannonInventory.Count(inv, GunPowderedPrefabs.Gunpowder), Powder(), CannonAmmoRules.CountTaken(CannonAmmoRules.MobileCannonMinTier, CannonInventory.BallsCarried(inv)));
            if (why != null) { Tell(p, why); { if (RuleLog.Due("MobileCannonGun.LoadAtMuzzle:122", 2d)) ModLog.Every("MobileCannonGun.LoadAtMuzzle:122", 2d, $"MobileCannonGun.LoadAtMuzzle: stops (yield break) because why != null (line 122)"); yield break; } }
            CannonInventory.Take(inv, GunPowderedPrefabs.Gunpowder, Powder());
            string ball = CannonAmmoRules.Choose(CannonAmmoRules.MobileCannonMinTier, CannonInventory.BallsCarried(inv));
            CannonInventory.Take(inv, ball, 1);
            Claim();
            _nview.GetZDO().Set(KeyLoaded, true);
            _nview.GetZDO().Set(KeyBall, ball);
            _nview.GetZDO().Set(KeyFuse, 0L);
            ModLog.Decision("mobile cannon", "loaded", Powder() + " gunpowder + 1 " + ball);
            p.Message(MessageHud.MessageType.TopLeft, "Mobile Cannon loaded");
        }

        /// <summary>Lights the fuse with the torch in the player's hand. Expected outcome: null when lit, else the reason (also shown to the player).</summary>
        internal string Light(Player p)
        {
            if (p == null || !IsValid) { if (RuleLog.Due("MobileCannonGun.Light:137", 2d)) ModLog.Every("MobileCannonGun.Light:137", 2d, $"MobileCannonGun.Light: returns \"no cannon\" because p == null || !IsValid (line 137)"); return "no cannon"; }
            string why = MobileCannonFireRules.WhyNotLight(Loaded, _fireQueued, CannonInventory.HoldsTorch(p));
            if (why != null) { Tell(p, why); { var returnedAt139 = why; if (RuleLog.Due("MobileCannonGun.Light:139", 2d)) ModLog.Every("MobileCannonGun.Light:139", 2d, $"MobileCannonGun.Light: returns {returnedAt139} because why != null (line 139)"); return returnedAt139; } }
            Claim();
            _nview.GetZDO().Set(KeyFuse, (long)(ZNet.instance.GetTimeSeconds() * 1000d));
            _fireQueued = true;
            StartCoroutine(FireAfterFuse(p));
            ModLog.Decision("mobile cannon", "fuse lit", ModSettings.MobileCannonFuseSeconds.Value.ToString("0.0") + " s to the shot");
            return null;
        }

        IEnumerator FireAfterFuse(Player p)
        {
            if (RuleLog.Due("MobileCannonGun.FireAfterFuse", 2d)) ModLog.Every("MobileCannonGun.FireAfterFuse", 2d, $"MobileCannonGun.FireAfterFuse(p={RuleLog.Shown(p)})");
            float end = Time.time + ModSettings.MobileCannonFuseSeconds.Value;
            while (Time.time < end)
            {
                if (_touchHole != null) CannonEffects.PlayFuse(_touchHole.position);
                yield return new WaitForSeconds(Mathf.Min(SparkEvery, Mathf.Max(0.01f, end - Time.time)));
            }
            _fireQueued = false;
            if (!IsValid || !Loaded) { if (RuleLog.Due("MobileCannonGun.FireAfterFuse:158", 2d)) ModLog.Every("MobileCannonGun.FireAfterFuse:158", 2d, $"MobileCannonGun.FireAfterFuse: stops (yield break) because !IsValid || !Loaded (line 158)"); yield break; }
            Fire(p);
        }

        void Fire(Player p)
        {
            Claim();
            string ball = LoadedBall;
            _nview.GetZDO().Set(KeyLoaded, false);
            GameObject prefab = CannonSetup.ProjectileFor(ball);
            if (prefab == null || _muzzle == null || _barrel == null) { ModLog.Warn("MobileCannonGun: no ball flight prefab or muzzle, the shot is lost"); return; }
            Vector3 dir = _barrel.forward;
            float speed = ModSettings.MobileCannonMuzzleSpeed.Value;
            float pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            float range = MobileCannonFireRules.Range(speed, pitch, ModSettings.CannonGravity.Value);
            Vector3 velocity = dir * speed;
            CannonEffects.PlayMuzzle(_muzzle.position, velocity);
            GunPowdered.Integrations.Valheim.GunNoise.Alert(_muzzle.position, NoiseRules.MobileCannonShotRadius, p, "mobile cannon shot");      // D-149
            GameObject shot = Instantiate(prefab, _muzzle.position + dir * 0.1f, Quaternion.LookRotation(velocity));
            CannonBallFlight flight = shot.GetComponent<CannonBallFlight>();
            if (flight != null) flight.Arm(true, ball);
            bool flies = flight != null && flight.Launch(p, transform, velocity, range);
            float recoil = ModSettings.MobileCannonRecoil.Value;
            if (_body != null && recoil > 0f) _body.AddForce(-new Vector3(dir.x, 0f, dir.z).normalized * recoil, ForceMode.VelocityChange);
            ModLog.Decision("mobile cannon", "fired", "ball flies " + flies + ", " + speed.ToString("0") + " m/s at " + pitch.ToString("0.0") + " deg, expected range " + range.ToString("0") + " m, recoil " + recoil.ToString("0.0") + " m/s");
        }

        // broken or dismantled while loaded: the load drops next to the wreck
        void OnDestroyed()
        {
            try
            {
                if (!IsValid || !Loaded) { if (RuleLog.IsOn()) ModLog.Verbose($"MobileCannonGun.OnDestroyed: returns because !IsValid || !Loaded (line 190)"); return; }
                Vector3 at = transform.position + Vector3.up * 0.6f;
                CannonInventory.Drop(LoadedBall, 1, at);
                CannonInventory.Drop(GunPowderedPrefabs.Gunpowder, Powder(), at);
                ModLog.Verbose("MobileCannonGun: destroyed while loaded - the load dropped");
            }
            catch (NullReferenceException e)
            {
                ModLog.Warn("MobileCannonGun.OnDestroyed: " + e.Message);
            }
        }

        void Claim()
        {
            if (RuleLog.Due("MobileCannonGun.Claim", 2d)) ModLog.Every("MobileCannonGun.Claim", 2d, $"MobileCannonGun.Claim()");
            if (!_nview.IsOwner()) _nview.ClaimOwnership();
        }

        static bool Tell(Player p, string why)
        {
            if (RuleLog.Due("MobileCannonGun.Tell", 2d)) ModLog.Every("MobileCannonGun.Tell", 2d, $"MobileCannonGun.Tell(p={RuleLog.Shown(p)}, why={RuleLog.Shown(why)})");
            p.Message(MessageHud.MessageType.Center, why);
            { if (RuleLog.Due("MobileCannonGun.Tell:212", 2d)) ModLog.Every("MobileCannonGun.Tell:212", 2d, $"MobileCannonGun.Tell: returns false (line 212)"); return false; }
        }

        static int Powder() => Mathf.Max(1, ModSettings.MobileCannonPowderPerShot.Value);
    }
}
