using KydlCore.Core;
using GunPowdered.Core;

namespace GunPowdered.Features.Water.Domain
{
    /// <summary>
    /// The Rain Barrel (D-009, D-010, D-012): fills only while it rains and nothing covers it, at a fixed rate per REAL-LIFE minute, up to its
    /// capacity; the player takes whole units out. Pure: facts in, decision out, no Unity / Valheim / ModLog.
    /// </summary>
    internal static class RainBarrelRules
    {
        internal const float DefaultCapacity = 200f, DefaultRatePerMinute = 5f;
        internal const string Filled = "filled", Full = "already full", NotRaining = "not raining", Covered = "covered by a roof", BadInput = "invalid input", Empty = "barrel is empty", Taken = "taken";

        /// <summary>
        /// The new content after elapsedSeconds of real time. Expected outcome: Success with a value between the old content and the capacity
        /// (reason says why it did or did not grow); Rejected for NaN / negative inputs. A barrel above its capacity (a lowered setting) is cut to it.
        /// </summary>
        internal static OperationResponse<float> Fill(float current, float capacity, float ratePerMinute, float elapsedSeconds, bool isRaining, bool isCovered)
        {
            if (RuleLog.Due("RainBarrelRules.Fill", 2d)) RuleLog.Every("RainBarrelRules.Fill", 2d, $"RainBarrelRules.Fill(current={RuleLog.Shown(current)}, capacity={RuleLog.Shown(capacity)}, ratePerMinute={RuleLog.Shown(ratePerMinute)}, elapsedSeconds={RuleLog.Shown(elapsedSeconds)}, isRaining={RuleLog.Shown(isRaining)}, isCovered={RuleLog.Shown(isCovered)})");
            if (!IsUsable(current) || !IsUsable(capacity) || !IsUsable(ratePerMinute) || !IsUsable(elapsedSeconds))
            {
                { var returnedAt24 = OperationResponse<float>.Rejected(BadInput); if (RuleLog.Due("RainBarrelRules.Fill:24", 2d)) RuleLog.Every("RainBarrelRules.Fill:24", 2d, $"RainBarrelRules.Fill: returns {returnedAt24} (line 24)"); return returnedAt24; }
            }
            if (current >= capacity) { var returnedAt26 = OperationResponse<float>.Success(capacity, Full); if (RuleLog.Due("RainBarrelRules.Fill:26", 2d)) RuleLog.Every("RainBarrelRules.Fill:26", 2d, $"RainBarrelRules.Fill: returns {returnedAt26} because current >= capacity (line 26)"); return returnedAt26; }
            if (isCovered) { var returnedAt27 = OperationResponse<float>.Success(current, Covered); if (RuleLog.Due("RainBarrelRules.Fill:27", 2d)) RuleLog.Every("RainBarrelRules.Fill:27", 2d, $"RainBarrelRules.Fill: returns {returnedAt27} because isCovered (line 27)"); return returnedAt27; }
            if (!isRaining) { var returnedAt28 = OperationResponse<float>.Success(current, NotRaining); if (RuleLog.Due("RainBarrelRules.Fill:28", 2d)) RuleLog.Every("RainBarrelRules.Fill:28", 2d, $"RainBarrelRules.Fill: returns {returnedAt28} because !isRaining (line 28)"); return returnedAt28; }
            float grown = current + ratePerMinute * elapsedSeconds / 60f;
            { var returnedAt30 = OperationResponse<float>.Success(System.Math.Min(grown, capacity), Filled); if (RuleLog.Due("RainBarrelRules.Fill:30", 2d)) RuleLog.Every("RainBarrelRules.Fill:30", 2d, $"RainBarrelRules.Fill: returns {returnedAt30} (line 30)"); return returnedAt30; }
        }

        /// <summary>
        /// How many whole units come out when the player asks for some. Expected outcome: Success with min(requested, whole units inside);
        /// Rejected when nothing can be taken (empty barrel, request below 1, bad input).
        /// </summary>
        internal static OperationResponse<int> Take(float current, int requested)
        {
            if (RuleLog.Due("RainBarrelRules.Take", 2d)) RuleLog.Every("RainBarrelRules.Take", 2d, $"RainBarrelRules.Take(current={RuleLog.Shown(current)}, requested={RuleLog.Shown(requested)})");
            if (!IsUsable(current) || requested < 1) return OperationResponse<int>.Rejected(BadInput);
            int available = (int)current;
            if (available < 1) { var returnedAt42 = OperationResponse<int>.Rejected(Empty); if (RuleLog.Due("RainBarrelRules.Take:42", 2d)) RuleLog.Every("RainBarrelRules.Take:42", 2d, $"RainBarrelRules.Take: returns {returnedAt42} because available < 1 (line 42)"); return returnedAt42; }
            { var returnedAt43 = OperationResponse<int>.Success(System.Math.Min(requested, available), Taken); if (RuleLog.Due("RainBarrelRules.Take:43", 2d)) RuleLog.Every("RainBarrelRules.Take:43", 2d, $"RainBarrelRules.Take: returns {returnedAt43} (line 43)"); return returnedAt43; }
        }

        static bool IsUsable(float value)
        {
            if (RuleLog.Due("RainBarrelRules.IsUsable", 2d)) RuleLog.Every("RainBarrelRules.IsUsable", 2d, $"RainBarrelRules.IsUsable(value={RuleLog.Shown(value)})");
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
