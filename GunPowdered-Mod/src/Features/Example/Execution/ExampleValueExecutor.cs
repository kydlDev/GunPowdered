using System;
using KydlCore.Core;
using GunPowdered.Core;

namespace GunPowdered.Features.Example.Execution
{
    /// <summary>
    /// EXAMPLE executor (Architecture V2): checks ownership FIRST, then runs the side effect through a delegate bound ONCE in the constructor
    /// (no closure per call). Returns what was issued. Game-free; the delegate is a method of the Infrastructure adapter.
    /// </summary>
    internal sealed class ExampleValueExecutor
    {
        internal const string NotOwner = "not the owner", Applied = "applied";
        readonly IOwnership _owner;
        readonly Action<float> _apply;

        internal ExampleValueExecutor(IOwnership owner, Action<float> apply)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"ExampleValueExecutor.ExampleValueExecutor(owner={RuleLog.Shown(owner)}, apply={RuleLog.Shown(apply)})");
            if (owner == null) throw new ArgumentNullException("owner");
            if (apply == null) { if (RuleLog.IsOn()) RuleLog.Verbose($"ExampleValueExecutor.ExampleValueExecutor: throws new ArgumentNullException(\"apply\") because apply == null (line 21)"); throw new ArgumentNullException("apply"); }
            _owner = owner;
            _apply = apply;
        }

        /// Expected outcome: Success with the applied value; Rejected (nothing applied) when this client is not the owner.
        internal OperationResponse<float> Apply(float value)
        {
            if (RuleLog.Due("ExampleValueExecutor.Apply", 2d)) RuleLog.Every("ExampleValueExecutor.Apply", 2d, $"ExampleValueExecutor.Apply(value={RuleLog.Shown(value)})");
            if (!_owner.IsOwner) return OperationResponse<float>.Rejected(NotOwner);
            _apply(value);
            { var returnedAt32 = OperationResponse<float>.Success(value, Applied); if (RuleLog.Due("ExampleValueExecutor.Apply:32", 2d)) RuleLog.Every("ExampleValueExecutor.Apply:32", 2d, $"ExampleValueExecutor.Apply: returns {returnedAt32} (line 32)"); return returnedAt32; }
        }
    }
}
