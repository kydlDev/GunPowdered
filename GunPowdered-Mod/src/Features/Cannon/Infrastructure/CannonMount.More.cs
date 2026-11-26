using System;
using System.Collections;
using System.Globalization;
using HarmonyLib;
using KydlCore.Common;
using UnityEngine;
using GunPowdered.Configuration;
using GunPowdered.Core;
using GunPowdered.Features.Cannon.Domain;
using GunPowdered.Features.Grenades.Domain;

namespace GunPowdered.Features.Cannon.Infrastructure
{
    /// <summary>Cannon mount, second half (G04 split).</summary>
    internal partial class CannonMount
    {
        void GiveLoad(Player p)
        {
            if (RuleLog.Due("CannonMount.GiveLoad", 2d)) ModLog.Every("CannonMount.GiveLoad", 2d, $"CannonMount.GiveLoad(p={RuleLog.Shown(p)})");
            CannonInventory.Give(p, LoadedBall(), 1);
            CannonInventory.Give(p, GunPowderedPrefabs.Gunpowder, PowderPerShot());
        }

        bool Load(Player p, CannonStatus s)
        {
            if (RuleLog.Due("CannonMount.Load", 2d)) ModLog.Every("CannonMount.Load", 2d, $"CannonMount.Load(p={RuleLog.Shown(p)}, s={RuleLog.Shown(s)})");
            Inventory inv = p.GetInventory();
            string why = CannonStateRules.WhyNotLoad(s, CannonInventory.Count(inv, GunPowderedPrefabs.Gunpowder), PowderPerShot(),
                CannonAmmoRules.CountTaken(CannonAmmoRules.HandCannonMinTier, CannonInventory.BallsCarried(inv)));
            if (why != null) { var returnedAt236 = Tell(p, why); if (RuleLog.Due("CannonMount.Load:236", 2d)) ModLog.Every("CannonMount.Load:236", 2d, $"CannonMount.Load: returns {returnedAt236} because why != null (line 236)"); return returnedAt236; }
            StartCoroutine(LoadAtMuzzle(p));
            { if (RuleLog.Due("CannonMount.Load:238", 2d)) ModLog.Every("CannonMount.Load:238", 2d, $"CannonMount.Load: returns true (line 238)"); return true; }
        }

        // D-053: 4 seconds at the muzzle with the cast ball in the player's hand; walking away or dying stops it, nothing is used up then
        IEnumerator LoadAtMuzzle(Player p)
        {
            _loading = true;
            float seconds = GunPowdered.Features.Skilling.Domain.SkillRules.CannonLoadSeconds(ModSettings.CannonLoadSeconds.Value, GunPowdered.Features.Skilling.Infrastructure.GunSkills.Level(p, GunPowdered.Features.Skilling.Infrastructure.GunSkills.Cannons)), end = Time.time + seconds, nextAnim = Time.time + AnimEvery;
            bool shown = ShowBallInHand(p, seconds, CannonAmmoRules.Choose(CannonAmmoRules.HandCannonMinTier, CannonInventory.BallsCarried(p.GetInventory())));
            ModLog.Verbose("CannonMount: loading for " + Fmt(seconds) + " s, ball in hand " + shown);
            while (Time.time < end)
            {
                if (p == null || p.IsDead() || !IsValid || Vector3.Distance(p.transform.position, transform.position) > UseRange)
                {
                    _loading = false;
                    if (p != null) p.Message(MessageHud.MessageType.Center, "Loading stopped");
                    ModLog.Verbose("CannonMount: loading stopped (the player left)");
                    yield break;
                }
                // user 2026-11-26: the moment the cannon is filled (another player, an earlier load) the fill animation stops; none starts in the last AnimTail s
                CannonLoadStep step = CannonLoadRules.Step(Status().Loaded, Time.time, nextAnim, end, AnimTail);
                if (step == CannonLoadStep.StopFilled)
                {
                    _loading = false;
                    Tell(p, CannonStateRules.AlreadyLoaded);
                    ModLog.Decision("hand cannon", "loading stopped", "the cannon is already filled - no more fill animation");
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
            string why = CannonStateRules.WhyNotLoad(Status(), CannonInventory.Count(inv, GunPowderedPrefabs.Gunpowder), PowderPerShot(),
                CannonAmmoRules.CountTaken(CannonAmmoRules.HandCannonMinTier, CannonInventory.BallsCarried(inv)));
            if (why != null) { Tell(p, why); { if (RuleLog.Due("CannonMount.LoadAtMuzzle:268", 2d)) ModLog.Every("CannonMount.LoadAtMuzzle:268", 2d, $"CannonMount.LoadAtMuzzle: stops (yield break) because why != null (line 268)"); yield break; } }
            CannonInventory.Take(inv, GunPowderedPrefabs.Gunpowder, PowderPerShot());
            string ball = CannonAmmoRules.Choose(CannonAmmoRules.HandCannonMinTier, CannonInventory.BallsCarried(inv));
            CannonInventory.Take(inv, ball, 1);
            Claim();
            _nview.GetZDO().Set(KeyLoaded, true);
            _nview.GetZDO().Set(KeyBall, ball);
            _nview.GetZDO().Set(KeyFuse, 0L);
            ModLog.Verbose("CannonMount: loaded with " + ball);
            p.Message(MessageHud.MessageType.TopLeft, ItemCatalog.Cannon.Name + " loaded");
        }

        // the cast ball shows in the right hand for the loading time, for everybody (the game's own "use item" hand visual, as for a feast)
        static bool ShowBallInHand(Player p, float seconds, string ballPrefab)
        {
            GameObject ball = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(CannonAmmoRules.LoadedOrOld(ballPrefab)) : null;
            if (UseHandVisual == null || ball == null) { if (RuleLog.Due("CannonMount.ShowBallInHand:284", 2d)) ModLog.Every("CannonMount.ShowBallInHand:284", 2d, $"CannonMount.ShowBallInHand: returns false because UseHandVisual == null || ball == null (line 284)"); return false; }
            try
            {
                UseHandVisual.Invoke(p, new object[] { ball, seconds });
                { if (RuleLog.Due("CannonMount.ShowBallInHand:288", 2d)) ModLog.Every("CannonMount.ShowBallInHand:288", 2d, $"CannonMount.ShowBallInHand: returns true (line 288)"); return true; }
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("CannonMount: the ball could not be shown in the hand: NullReferenceException - an object it used is not set or was taken away - " + (e.InnerException ?? e).Message + "\n" + e.StackTrace);
                return false;
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("CannonMount: the ball could not be shown in the hand: MissingReferenceException - a destroyed Unity object was used - " + (e.InnerException ?? e).Message + "\n" + e.StackTrace);
                return false;
            }
            catch (Exception e)
            {
                ModLog.Warn("CannonMount: the ball could not be shown in the hand: " + (e.InnerException ?? e).Message + "\n" + e.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Lights the fuse (the aiming player, torch in hand): the shot follows after the fuse seconds with the given launch. Expected outcome: null when
        /// lit, else the reason.
        /// </summary>
        internal string Light(Player p, Vector3 velocity, float targetFlat)
        {
            string why = CannonStateRules.WhyNotLight(Status(), CannonInventory.HoldsTorch(p));
            if (why != null || _fireQueued) { var returnedAt314 = why ?? CannonStateRules.FuseBurning; if (RuleLog.Due("CannonMount.Light:314", 2d)) ModLog.Every("CannonMount.Light:314", 2d, $"CannonMount.Light: returns {returnedAt314} because why != null || _fireQueued (line 314)"); return returnedAt314; }
            Claim();
            _nview.GetZDO().Set(KeyFuse, (long)(ZNet.instance.GetTimeSeconds() * 1000d));
            _fireQueued = true;
            StartCoroutine(FireAfterFuse(p, velocity, targetFlat));
            ModLog.Verbose("CannonMount: fuse lit, " + Fmt(ModSettings.CannonFuseSeconds.Value) + " s to the shot");
            return null;
        }

        // sparks and smoke out of the touch hole while the fuse burns, then the blast at the muzzle and the shot (D-052 addendum). The effects are
        // networked vanilla ones: this client makes them, the others see them.
        IEnumerator FireAfterFuse(Player p, Vector3 velocity, float targetFlat)
        {
            if (RuleLog.Due("CannonMount.FireAfterFuse", 2d)) ModLog.Every("CannonMount.FireAfterFuse", 2d, $"CannonMount.FireAfterFuse(p={RuleLog.Shown(p)}, velocity={RuleLog.Shown(velocity)}, targetFlat={RuleLog.Shown(targetFlat)})");
            float end = Time.time + ModSettings.CannonFuseSeconds.Value;
            while (Time.time < end)
            {
                if (_touchHole != null) CannonEffects.PlayFuse(_touchHole.position);
                yield return new WaitForSeconds(Mathf.Min(FuseSparkEvery, Mathf.Max(0.01f, end - Time.time)));
            }
            _fireQueued = false;
            if (!IsValid || !Status().Loaded) { if (RuleLog.Due("CannonMount.FireAfterFuse:335", 2d)) ModLog.Every("CannonMount.FireAfterFuse:335", 2d, $"CannonMount.FireAfterFuse: stops (yield break) because !IsValid || !Status().Loaded (line 335)"); yield break; }
            if (_muzzle != null) CannonEffects.PlayMuzzle(_muzzle.position, velocity);
            Fire(p, velocity, targetFlat);
        }

        void Fire(Player p, Vector3 velocity, float targetFlat)
        {
            Claim();
            string ball = LoadedBall();
            _nview.GetZDO().Set(KeyLoaded, false);
            GameObject flightPrefab = CannonSetup.ProjectileFor(ball);
            if (flightPrefab == null || _muzzle == null) { ModLog.Warn("CannonMount: no ball flight prefab, the shot is lost"); return; }
            GameObject shot = Instantiate(flightPrefab, _muzzle.position, Quaternion.LookRotation(velocity));
            CannonBallFlight flight = shot.GetComponent<CannonBallFlight>();
            if (flight != null) flight.Arm(false, ball);
            bool flies = flight != null && flight.Launch(p, transform, velocity, targetFlat);
            GunPowdered.Integrations.Valheim.GunNoise.Alert(_muzzle.position, NoiseRules.CannonShotRadius, p, "hand cannon shot");      // D-149: mobs hear it
            if (p != null && ModSettings.CannonRecoil.Value > 0f) p.ApplyPushback(-new Vector3(velocity.x, 0f, velocity.z).normalized, ModSettings.CannonRecoil.Value);
            ModLog.Verbose("CannonMount: fired, ball flies " + flies + ", velocity " + velocity.ToString("0.0") + ", aimed " + Fmt(targetFlat) + " m");
            if (CannonAim.Current == this) CannonAim.End("fired");
        }

        // broken or dismantled: the cannon (and its load) drop next to the stock's own wood and resin (D-052 addendum)
        void OnDestroyed()
        {
            try
            {
                if (!IsValid) { if (RuleLog.IsOn()) ModLog.Verbose($"CannonMount.OnDestroyed: returns because !IsValid (line 362)"); return; }
                CannonStatus s = Status();
                Vector3 at = transform.position + (Vector3.up * 0.5f);
                if (s.Mounted) CannonInventory.Drop(GunPowderedPrefabs.Cannon, 1, at);
                if (s.Mounted && s.Loaded)
                {
                    CannonInventory.Drop(LoadedBall(), 1, at);
                    CannonInventory.Drop(GunPowderedPrefabs.Gunpowder, PowderPerShot(), at);
                }
                ModLog.Verbose("CannonMount: stock destroyed, dropped cannon " + s.Mounted + ", load " + (s.Mounted && s.Loaded));
            }
            catch (System.NullReferenceException e)
            {
                ModLog.Warn("CannonMount.OnDestroyed: NullReferenceException - an object it used is not set or was taken away - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (UnityEngine.MissingReferenceException e)
            {
                ModLog.Warn("CannonMount.OnDestroyed: MissingReferenceException - a destroyed Unity object was used - " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
            catch (Exception e)
            {
                ModLog.Warn("CannonMount.OnDestroyed: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            }
        }

        void Claim()
        {
            if (RuleLog.Due("CannonMount.Claim", 2d)) ModLog.Every("CannonMount.Claim", 2d, $"CannonMount.Claim()");
            if (!_nview.IsOwner()) _nview.ClaimOwnership();
        }

        static bool Tell(Player p, string why)
        {
            if (RuleLog.Due("CannonMount.Tell", 2d)) ModLog.Every("CannonMount.Tell", 2d, $"CannonMount.Tell(p={RuleLog.Shown(p)}, why={RuleLog.Shown(why)})");
            p.Message(MessageHud.MessageType.Center, why);
            { if (RuleLog.Due("CannonMount.Tell:397", 2d)) ModLog.Every("CannonMount.Tell:397", 2d, $"CannonMount.Tell: returns false (line 397)"); return false; }
        }

        internal static int PowderPerShot() => Mathf.Max(1, Mathf.RoundToInt(ModSettings.CannonPowderPerShot.Value));

        static Transform Deep(Transform root, string name)
        {
            if (RuleLog.IsOn()) ModLog.Verbose($"CannonMount.Deep(root={RuleLog.Shown(root)}, name={RuleLog.Shown(name)})");
            if (root == null) return null;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) { var returnedAt406 = t; if (RuleLog.IsOn()) ModLog.Verbose($"CannonMount.Deep: returns {returnedAt406} because t.name == name (line 406)"); return returnedAt406; }
            { if (RuleLog.IsOn()) ModLog.Verbose($"CannonMount.Deep: returns null (line 407)"); return null; }
        }

        static ArcVector ToArc(Vector3 v) => new ArcVector(v.x, v.y, v.z);
        static string Fmt(float f) => f.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
