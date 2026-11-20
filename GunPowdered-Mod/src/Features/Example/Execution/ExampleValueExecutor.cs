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
            if (RuleLog.On) RuleLog.Verbose($"ExampleValueExecutor.ExampleValueExecutor(owner={RuleLog.Shown(owner)}, apply={RuleLog.Shown(apply)})");
            if (owner == null) throw new ArgumentNullException("owner");
            if (apply == null) throw new ArgumentNullException("apply");
            _owner = owner;
            _apply = apply;
        }

        /// Expected outcome: Success with the applied value; Rejected (nothing applied) when this client is not the owner.
        internal OperationResponse<float> Apply(float value)
        {
            if (RuleLog.On) RuleLog.Every("ExampleValueExecutor.Apply", 2d, $"ExampleValueExecutor.Apply(value={RuleLog.Shown(value)})");
            if (!_owner.IsOwner) { var returnedAt31 = OperationResponse<float>.Rejected(NotOwner); if (RuleLog.On) RuleLog.Every("ExampleValueExecutor.Apply:31", 2d, $"ExampleValueExecutor.Apply: returns {RuleLog.Shown(returnedAt31)} (line 31)"); return returnedAt31; }
            _apply(value);
            { var returnedAt33 = OperationResponse<float>.Success(value, Applied); if (RuleLog.On) RuleLog.Every("ExampleValueExecutor.Apply:33", 2d, $"ExampleValueExecutor.Apply: returns {RuleLog.Shown(returnedAt33)} (line 33)"); return returnedAt33; }
        }
    }
}
