using System;
using GunPowdered.Core;

namespace GunPowdered.Features.Musket.Domain
{
    /// <summary>
    /// The musket animations as numbers (D-092, user 2026-11-19: own hold, run, shoot and reload animations). The game animates the player; the mod then writes
    /// the arm and finger muscles of one frame, blended from the authored <see cref="MusketPoses"/>: the shouldered hold, the carry across the chest while
    /// running, a short recoil after a shot, and the reload (musket muzzle up, one ramrod stroke per cartridge). Pure, so the unit tests check it.
    /// </summary>
    internal static class MusketPoseRules
    {
        /// <summary>How long the recoil of one shot lasts.</summary>
        internal const float FireSeconds = 0.35f;

        /// <summary>The shooter has not shared any action yet (the action counter is not in the player's ZDO).</summary>
        internal const int NoActionYet = -1;

        /// <summary>
        /// D-098: another player's rig follows the action counter the shooter's client writes into the player's ZDO. Decides whether the counter
        /// <paramref name="seq"/> (<see cref="NoActionYet"/> when absent) starts a new action, given the counter this rig saw last (<paramref name="seen"/>,
        /// -1 = this rig has not looked yet). The first look at a shooter WITH a history only records it (an old shot is not replayed to someone who comes
        /// near later); a first look at a shooter WITHOUT one records 0, so the shooter's very first action still plays (it was swallowed before: lab
        /// a17bjornita3, DrakesTestLab Bjornita never showed the first shot). Expected outcome: true when the action must start; <paramref name="newSeen"/>
        /// is what the rig remembers.
        /// </summary>
        internal static bool IsNewAction(int seq, int seen, out int newSeen)
        {
            if (RuleLog.Due("MusketPoseRules.IsNewAction", 2d)) RuleLog.Every("MusketPoseRules.IsNewAction", 2d, $"MusketPoseRules.IsNewAction(seq={seq}, seen={seen})");
            if (seq < 0)
            {
                newSeen = seen < 0 ? 0 : seen;
                if (RuleLog.Due("MusketPoseRules.IsNewAction:none", 2d)) RuleLog.Every("MusketPoseRules.IsNewAction:none", 2d, "MusketPoseRules.IsNewAction: no action shared yet - returns false");
                return false;
            }
            newSeen = seq;
            bool start = seen >= 0 && seq != seen;
            if (RuleLog.Due("MusketPoseRules.IsNewAction:result", 2d)) RuleLog.Every("MusketPoseRules.IsNewAction:result", 2d, "MusketPoseRules.IsNewAction: returns " + start);
            return start;
        }

        /// <summary>One cartridge per 1.2 s (user 2026-11-19); one ramrod stroke each.</summary>
        internal const float ReloadSecondsPerCartridge = 1.2f;

        /// <summary>How long the arms take to go into the reload pose or into the carry.</summary>
        internal const float BlendSeconds = 0.25f;

        /// <summary>Running faster than this (m/s) carries the musket across the chest.</summary>
        internal const float CarrySpeed = 5f;

        /// <summary>
        /// Writes the arm and finger muscles of one frame into <paramref name="into"/> (<see cref="MusketPoses.Count"/> values). <paramref name="seconds"/> is the
        /// time since the action began (ignored for Hold); <paramref name="carry"/> is how far the player is in the running carry (0 .. 1).
        /// Expected outcome: true when every value was written (false for a too short array).
        /// </summary>
        internal static bool Evaluate(MusketAction action, float seconds, float carry, float[] into)
        {
            if (RuleLog.Due("MusketPoseRules.Evaluate", 2d)) RuleLog.Every("MusketPoseRules.Evaluate", 2d, $"MusketPoseRules.Evaluate(action={RuleLog.Shown(action)}, seconds={RuleLog.Shown(seconds)}, carry={RuleLog.Shown(carry)}, into={RuleLog.Shown(into)})");
            if (into == null || into.Length < MusketPoses.Count) { if (RuleLog.Due("MusketPoseRules.Evaluate:33", 2d)) RuleLog.Every("MusketPoseRules.Evaluate:33", 2d, "MusketPoseRules.Evaluate: returns false (line 33)"); return false; }
            float t = seconds > 0f ? seconds : 0f;
            Blend(MusketPoses.Hold, MusketPoses.Carry, Clamp01(carry), into);
            switch (action)
            {
                case MusketAction.Fire:
                    float k = 1f - Clamp01(t / FireSeconds);
                    BlendOnto(MusketPoses.Recoil, k * k, into);
                    break;
                case MusketAction.Reload:
                    float w = Clamp01(t / BlendSeconds);
                    float s = Stroke(t);
                    for (int i = 0; i < MusketPoses.Count; i++)
                    {
                        float reload = MusketPoses.ReloadLow[i] + (MusketPoses.ReloadHigh[i] - MusketPoses.ReloadLow[i]) * s;
                        into[i] += (reload - into[i]) * w;
                    }
                    break;
            }
            { var returnedAt52 = into.Length >= MusketPoses.Count; if (RuleLog.Due("MusketPoseRules.Evaluate:52", 2d)) RuleLog.Every("MusketPoseRules.Evaluate:52", 2d, $"MusketPoseRules.Evaluate: returns {RuleLog.Shown(returnedAt52)} (line 52)"); return returnedAt52; }
        }

        /// <summary>Where the left hand is in the ramrod stroke of one cartridge: 0 (low) at the start and end of the 1.2 s, 1 (at the muzzle) in the middle.</summary>
        internal static float Stroke(float seconds)
        {
            if (RuleLog.Due("MusketPoseRules.Stroke", 2d)) RuleLog.Every("MusketPoseRules.Stroke", 2d, $"MusketPoseRules.Stroke(seconds={RuleLog.Shown(seconds)})");
            if (seconds <= 0f) { if (RuleLog.Due("MusketPoseRules.Stroke:59", 2d)) RuleLog.Every("MusketPoseRules.Stroke:59", 2d, "MusketPoseRules.Stroke: returns 0f (line 59)"); return 0f; }
            float u = seconds % ReloadSecondsPerCartridge / ReloadSecondsPerCartridge;
            { var returnedAt61 = (float)Math.Sin(Math.PI * u); if (RuleLog.Due("MusketPoseRules.Stroke:61", 2d)) RuleLog.Every("MusketPoseRules.Stroke:61", 2d, $"MusketPoseRules.Stroke: returns {RuleLog.Shown(returnedAt61)} (line 61)"); return returnedAt61; }
        }

        /// <summary>How far the carry is after one frame: it follows the running speed at one full change per <see cref="BlendSeconds"/>.</summary>
        internal static float NextCarry(float carry, float speed, float deltaSeconds)
        {
            if (RuleLog.Due("MusketPoseRules.NextCarry", 2d)) RuleLog.Every("MusketPoseRules.NextCarry", 2d, $"MusketPoseRules.NextCarry(carry={RuleLog.Shown(carry)}, speed={RuleLog.Shown(speed)}, deltaSeconds={RuleLog.Shown(deltaSeconds)})");
            float target = speed > CarrySpeed ? 1f : 0f;
            float step = deltaSeconds > 0f ? deltaSeconds / BlendSeconds : 0f;
            float c = Clamp01(carry);
            { var returnedAt71 = c < target ? Math.Min(target, c + step) : Math.Max(target, c - step); if (RuleLog.Due("MusketPoseRules.NextCarry:71", 2d)) RuleLog.Every("MusketPoseRules.NextCarry:71", 2d, $"MusketPoseRules.NextCarry: returns {RuleLog.Shown(returnedAt71)} (line 71)"); return returnedAt71; }
        }

        /// <summary>How far the musket has moved from the aiming grip to the reload grip (0 .. 1): it goes over in <see cref="BlendSeconds"/> when a reload begins.</summary>
        internal static float ReloadWeight(MusketAction action, float seconds) => action == MusketAction.Reload ? Clamp01(seconds / BlendSeconds) : 0f;

        /// <summary>How far the ramrod's top end stands above the muzzle at this moment of a reload (m): from <see cref="MusketPoses.RodHigh"/> down to <see cref="MusketPoses.RodLow"/> and back, once per cartridge.</summary>
        internal static float RodAboveMuzzle(float seconds) => MusketPoses.RodLow + (MusketPoses.RodHigh - MusketPoses.RodLow) * Stroke(seconds);

        /// <summary>
        /// How steady the spine is held (0 = the animation as it is, 1 = only its slow average, D-099): full while aiming, none while running or
        /// reloading (the spine moves on purpose there), in between while the carry blends.
        /// </summary>
        internal static float SpineSteadiness(MusketAction action, float carry)
        {
            if (RuleLog.Due("MusketPoseRules.SpineSteadiness", 2d)) RuleLog.Every("MusketPoseRules.SpineSteadiness", 2d, $"MusketPoseRules.SpineSteadiness(action={RuleLog.Shown(action)}, carry={RuleLog.Shown(carry)})");
            if (action == MusketAction.Reload) { if (RuleLog.Due("MusketPoseRules.SpineSteadiness:87", 2d)) RuleLog.Every("MusketPoseRules.SpineSteadiness:87", 2d, "MusketPoseRules.SpineSteadiness: returns 0f (line 87)"); return 0f; }
            { var returnedAt88 = 1f - Clamp01(carry); if (RuleLog.Due("MusketPoseRules.SpineSteadiness:88", 2d)) RuleLog.Every("MusketPoseRules.SpineSteadiness:88", 2d, $"MusketPoseRules.SpineSteadiness: returns {RuleLog.Shown(returnedAt88)} (line 88)"); return returnedAt88; }
        }

        /// <summary>The share of the way a slow follower moves towards its target in <paramref name="dt"/> seconds with time constant <paramref name="seconds"/> (0 .. 1).</summary>
        internal static float FollowFactor(float dt, float seconds)
        {
            if (RuleLog.Due("MusketPoseRules.FollowFactor", 2d)) RuleLog.Every("MusketPoseRules.FollowFactor", 2d, $"MusketPoseRules.FollowFactor(dt={RuleLog.Shown(dt)}, seconds={RuleLog.Shown(seconds)})");
            if (dt <= 0f) { if (RuleLog.Due("MusketPoseRules.FollowFactor:95", 2d)) RuleLog.Every("MusketPoseRules.FollowFactor:95", 2d, "MusketPoseRules.FollowFactor: returns 0f (line 95)"); return 0f; }
            if (seconds <= 0f) { if (RuleLog.Due("MusketPoseRules.FollowFactor:96", 2d)) RuleLog.Every("MusketPoseRules.FollowFactor:96", 2d, "MusketPoseRules.FollowFactor: returns 1f (line 96)"); return 1f; }
            { var returnedAt97 = Clamp01(1f - (float)Math.Exp(-dt / seconds)); if (RuleLog.Due("MusketPoseRules.FollowFactor:97", 2d)) RuleLog.Every("MusketPoseRules.FollowFactor:97", 2d, $"MusketPoseRules.FollowFactor: returns {RuleLog.Shown(returnedAt97)} (line 97)"); return returnedAt97; }
        }

        internal static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        static void Blend(float[] a, float[] b, float w, float[] into)
        {
            if (RuleLog.Due("MusketPoseRules.Blend", 2d)) RuleLog.Every("MusketPoseRules.Blend", 2d, $"MusketPoseRules.Blend(a={RuleLog.Shown(a)}, b={RuleLog.Shown(b)}, w={RuleLog.Shown(w)}, into={RuleLog.Shown(into)})");
            for (int i = 0; i < MusketPoses.Count; i++) into[i] = a[i] + (b[i] - a[i]) * w;
        }

        static void BlendOnto(float[] target, float w, float[] into)
        {
            if (RuleLog.Due("MusketPoseRules.BlendOnto", 2d)) RuleLog.Every("MusketPoseRules.BlendOnto", 2d, $"MusketPoseRules.BlendOnto(target={RuleLog.Shown(target)}, w={RuleLog.Shown(w)}, into={RuleLog.Shown(into)})");
            for (int i = 0; i < MusketPoses.Count; i++) into[i] += (target[i] - into[i]) * w;
        }
    }
}
