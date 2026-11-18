using KydlCore.Core;

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
            if (!IsUsable(current) || !IsUsable(capacity) || !IsUsable(ratePerMinute) || !IsUsable(elapsedSeconds))
            {
                return OperationResponse<float>.Rejected(BadInput);
            }
            if (current >= capacity) return OperationResponse<float>.Success(capacity, Full);
            if (isCovered) return OperationResponse<float>.Success(current, Covered);
            if (!isRaining) return OperationResponse<float>.Success(current, NotRaining);
            float grown = current + ratePerMinute * elapsedSeconds / 60f;
            return OperationResponse<float>.Success(System.Math.Min(grown, capacity), Filled);
        }

        /// <summary>
        /// How many whole units come out when the player asks for some. Expected outcome: Success with min(requested, whole units inside);
        /// Rejected when nothing can be taken (empty barrel, request below 1, bad input).
        /// </summary>
        internal static OperationResponse<int> Take(float current, int requested)
        {
            if (!IsUsable(current) || requested < 1) return OperationResponse<int>.Rejected(BadInput);
            int available = (int)current;
            if (available < 1) return OperationResponse<int>.Rejected(Empty);
            return OperationResponse<int>.Success(System.Math.Min(requested, available), Taken);
        }

        static bool IsUsable(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
