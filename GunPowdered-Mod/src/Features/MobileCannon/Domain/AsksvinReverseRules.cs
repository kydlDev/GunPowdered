using GunPowdered.Core;
namespace GunPowdered.Features.MobileCannon.Domain
{
    /// <summary>
    /// D-144 (T-014): backing the Asksvin up to hitch the cannon. Ridden, S walks it backwards at half its walk speed (A / D turn it slowly); on foot the
    /// player grabs the saddle's chest rope with E (toggle) and leads it: stepping into it backs it up, stepping away leads it forward. A hitched cannon is
    /// kept straight behind the Asksvin while it backs, so it goes back along its own path instead of jack-knifing. Pure, so the unit tests link it in.
    /// </summary>
    public static class AsksvinReverseRules
    {
        /// <summary>Backwards speed as a share of the Asksvin's walk speed (user, D-144: half its walk speed).</summary>
        public const float BackSpeedFactor = 0.5f;

        /// <summary>How fast (deg/s) A / D turn an Asksvin that backs up under its rider.</summary>
        public const float BackTurnRate = 30f;

        /// <summary>Where the leading player stands in front of the chest rope (m) when the Asksvin stays put; the band around it.</summary>
        public const float LeadRest = 1.4f, LeadBackBand = 0.25f, LeadForwardBand = 0.6f;

        /// <summary>Beyond this (m) from the chest rope the player lets go of it.</summary>
        public const float LeadLetGo = 6f;

        /// <summary>Hover text of the chest rope.</summary>
        public const string LeadHover = "Lead (walk into it to back it up)", LetGoHover = "Let go of the chest rope";

        /// <summary>Whether the rider asks to back up. Expected outcome: true while S is held (move z below -0.5) and not running.</summary>
        public static bool WantsBack(float moveZ, bool run) => !run && moveZ < -0.5f;

        /// <summary>The rider's turn input while backing (A / D). Expected outcome: -1, 0 or 1 (a dead zone of 0.5).</summary>
        public static int TurnInput(float moveX) => moveX > 0.5f ? 1 : moveX < -0.5f ? -1 : 0;

        /// <summary>How far (deg) a backing Asksvin turns this step. Expected outcome: the input (clamped to -1..1) times <see cref="BackTurnRate"/> times dt.</summary>
        public static float TurnStep(float turn, float dt)
        {
            if (RuleLog.IsOn()) RuleLog.Every("AsksvinReverseRules.TurnStep", 2d, $"AsksvinReverseRules.TurnStep(turn={RuleLog.Shown(turn)}, dt={RuleLog.Shown(dt)})");
            float t = turn > 1f ? 1f : turn < -1f ? -1f : turn;
            float step = t * BackTurnRate * dt;
            if (RuleLog.IsOn()) RuleLog.Every("AsksvinReverseRules.TurnStep:end", 2d, $"AsksvinReverseRules.TurnStep: returns {RuleLog.Shown(step)}");
            return step;
        }

        /// <summary>
        /// What a led Asksvin does, from how far (m) the player stands in front of its chest rope (<paramref name="ahead"/>, along its facing; negative when
        /// beside or behind) and how far from the rope at all (<paramref name="distance"/>). Expected outcome: "let go" beyond <see cref="LeadLetGo"/>;
        /// "back" when the player steps closer than the rest band; "forward" when the player walks off past it; else "stay".
        /// </summary>
        public static string Lead(float ahead, float distance)
        {
            if (RuleLog.IsOn()) RuleLog.Every("AsksvinReverseRules.Lead", 2d, $"AsksvinReverseRules.Lead(ahead={RuleLog.Shown(ahead)}, distance={RuleLog.Shown(distance)})");
            string what = distance > LeadLetGo ? "let go" : ahead < LeadRest - LeadBackBand ? "back" : ahead > LeadRest + LeadForwardBand ? "forward" : "stay";
            if (RuleLog.IsOn()) RuleLog.Every("AsksvinReverseRules.Lead:end", 2d, $"AsksvinReverseRules.Lead: returns {RuleLog.Shown(what)}");
            return what;
        }

        /// <summary>
        /// The yaw spin (rad/s) that keeps a backing cannon straight behind the Asksvin, from the angle (deg) between the cannon's tow line (axle to ring) and the
        /// Asksvin's facing. Expected outcome: 0 when straight; towards straight otherwise, at most <paramref name="maxSpin"/>.
        /// </summary>
        public static float StraightSpin(float angleDeg, float maxSpin)
        {
            if (RuleLog.IsOn()) RuleLog.Every("AsksvinReverseRules.StraightSpin", 2d, $"AsksvinReverseRules.StraightSpin(angleDeg={RuleLog.Shown(angleDeg)}, maxSpin={RuleLog.Shown(maxSpin)})");
            float m = System.Math.Abs(maxSpin);
            float spin = angleDeg * 0.0174533f * 3f;
            spin = spin > m ? m : spin < -m ? -m : spin;
            if (RuleLog.IsOn()) RuleLog.Every("AsksvinReverseRules.StraightSpin:end", 2d, $"AsksvinReverseRules.StraightSpin: returns {RuleLog.Shown(spin)}");
            return spin;
        }
    }
}
