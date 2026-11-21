using GunPowdered.Core;
namespace GunPowdered.Features.MobileCannon.Domain
{
    /// <summary>
    /// D-144 (T-014): backing the Asksvin up to hitch the cannon. Ridden, S walks it backwards at half its walk speed (A / D turn it slowly); on foot the
    /// player grabs the saddle's chest rope with E (toggle) and leads it: it moves with the player along its own axis (back or forward), full control. A hitched cannon is
    /// kept straight behind the Asksvin while it backs, so it goes back along its own path instead of jack-knifing. Pure, so the unit tests link it in.
    /// </summary>
    public static class AsksvinReverseRules
    {
        /// <summary>Backwards speed as a share of the Asksvin's walk speed (user, D-144: half its walk speed).</summary>
        public const float BackSpeedFactor = 0.5f;

        /// <summary>How fast (deg/s) A / D turn an Asksvin that backs up under its rider.</summary>
        public const float BackTurnRate = 30f;

        /// <summary>How far (m) the player may move along the Asksvin from where they took the rope before it moves with them (user 21:46: leading, not following).</summary>
        public const float LeadBand = 0.3f;

        /// <summary>Beyond this (m) from the chest rope the player lets go of it.</summary>
        public const float LeadLetGo = 6f;

        /// <summary>Hover text of the chest rope.</summary>
        public const string LeadHover = "Lead (walk back or forward with it)", LetGoHover = "Let go of the chest rope";

        /// <summary>Whether the rider asks to back up. Expected outcome: true while S is held (move z below -0.5) and not running.</summary>
        public static bool WantsBack(float moveZ, bool run) => !run && moveZ < -0.5f;

        /// <summary>The rider's turn input while backing (A / D). Expected outcome: -1, 0 or 1 (a dead zone of 0.5).</summary>
        public static int TurnInput(float moveX) => moveX > 0.5f ? 1 : moveX < -0.5f ? -1 : 0;

        /// <summary>How far (deg) a backing Asksvin turns this step. Expected outcome: the input (clamped to -1..1) times <see cref="BackTurnRate"/> times dt.</summary>
        public static float TurnStep(float turn, float dt)
        {
            if (RuleLog.Due("AsksvinReverseRules.TurnStep", 2d)) RuleLog.Every("AsksvinReverseRules.TurnStep", 2d, $"AsksvinReverseRules.TurnStep(turn={RuleLog.Shown(turn)}, dt={RuleLog.Shown(dt)})");
            float t = turn > 1f ? 1f : turn < -1f ? -1f : turn;
            float step = t * BackTurnRate * dt;
            if (RuleLog.Due("AsksvinReverseRules.TurnStep:end", 2d)) RuleLog.Every("AsksvinReverseRules.TurnStep:end", 2d, $"AsksvinReverseRules.TurnStep: returns {RuleLog.Shown(step)}");
            return step;
        }

        /// <summary>
        /// What a led Asksvin does (user 2026-11-20 21:46: the player holds the rope and the Asksvin moves WITH the player along its own axis - the player
        /// moves towards its back, it walks backwards; the player moves forward, it walks forward; full control, not following). <paramref name="ahead"/> is
        /// where the player stands along the Asksvin's facing from its chest rope now (m), <paramref name="grabAhead"/> the same when the rope was taken,
        /// <paramref name="distance"/> the flat distance to the rope. Expected outcome: "let go" beyond <see cref="LeadLetGo"/>; "back" when the player moved
        /// more than <see cref="LeadBand"/> towards its back; "forward" when more than that forward; else "stay".
        /// </summary>
        public static string Lead(float ahead, float grabAhead, float distance)
        {
            if (RuleLog.Due("AsksvinReverseRules.Lead", 2d)) RuleLog.Every("AsksvinReverseRules.Lead", 2d, $"AsksvinReverseRules.Lead(ahead={RuleLog.Shown(ahead)}, grabAhead={RuleLog.Shown(grabAhead)}, distance={RuleLog.Shown(distance)})");
            float moved = ahead - grabAhead;
            string what = distance > LeadLetGo ? "let go" : moved < -LeadBand ? "back" : moved > LeadBand ? "forward" : "stay";
            if (RuleLog.Due("AsksvinReverseRules.Lead:end", 2d)) RuleLog.Every("AsksvinReverseRules.Lead:end", 2d, $"AsksvinReverseRules.Lead: returns {RuleLog.Shown(what)}");
            return what;
        }

        /// <summary>
        /// The yaw spin (rad/s) that keeps a backing cannon straight behind the Asksvin, from the angle (deg) between the cannon's tow line (axle to ring) and the
        /// Asksvin's facing. Expected outcome: 0 when straight; towards straight otherwise, at most <paramref name="maxSpin"/>.
        /// </summary>
        public static float StraightSpin(float angleDeg, float maxSpin)
        {
            if (RuleLog.Due("AsksvinReverseRules.StraightSpin", 2d)) RuleLog.Every("AsksvinReverseRules.StraightSpin", 2d, $"AsksvinReverseRules.StraightSpin(angleDeg={RuleLog.Shown(angleDeg)}, maxSpin={RuleLog.Shown(maxSpin)})");
            float m = System.Math.Abs(maxSpin);
            float spin = angleDeg * 0.0174533f * 3f;
            spin = spin > m ? m : spin < -m ? -m : spin;
            if (RuleLog.Due("AsksvinReverseRules.StraightSpin:end", 2d)) RuleLog.Every("AsksvinReverseRules.StraightSpin:end", 2d, $"AsksvinReverseRules.StraightSpin: returns {RuleLog.Shown(spin)}");
            return spin;
        }
    }
}
