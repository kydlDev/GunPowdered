using System;
using KydlCore.Core;
using GunPowdered.Features.Example.Domain;
using GunPowdered.Features.Example.Execution;
using GunPowdered.Core;

namespace GunPowdered.Features.Example.Application
{
    /// <summary>
    /// EXAMPLE handler: pipeline (validation steps, then the domain decision) -> executor (the side effect). Built once; Handle allocates nothing
    /// beyond the trace line while debug is on. Game-free: tested with a fake ownership + a recording action (tests/Integration).
    /// </summary>
    internal sealed class SetExampleValueHandler : ICommandHandler<SetExampleValueCommand, float>
    {
        internal const string NoCommand = "no command";
        readonly OperationPipeline<SetExampleValueCommand, float> _pipeline;
        readonly ExampleValueExecutor _executor;

        internal SetExampleValueHandler(ExampleValueExecutor executor, Action<string> trace)
        {
            if (RuleLog.IsOn()) RuleLog.Verbose($"SetExampleValueHandler.SetExampleValueHandler(executor={RuleLog.Shown(executor)}, trace={RuleLog.Shown(trace)})");
            if (executor == null) throw new ArgumentNullException("executor");
            _executor = executor;
            _pipeline = new OperationPipeline<SetExampleValueCommand, float>("SetExampleValue", c => ExampleRules.Decide(c.Requested))
                .Validate("command given", c => ValidationResult.RejectUnless(c != null, NoCommand));
            _pipeline.Trace = trace;
        }

        /// Expected outcome: the executor's response when the rules accept the value, else the rules' Rejected / Ignored response.
        public OperationResponse<float> Handle(SetExampleValueCommand command)
        {
            if (RuleLog.Due("SetExampleValueHandler.Handle", 2d)) RuleLog.Every("SetExampleValueHandler.Handle", 2d, $"SetExampleValueHandler.Handle(command={RuleLog.Shown(command)})");
            var decision = _pipeline.Run(command);
            { var returnedAt35 = decision.IsSuccess ? _executor.Apply(decision.Value) : decision; if (RuleLog.Due("SetExampleValueHandler.Handle:35", 2d)) RuleLog.Every("SetExampleValueHandler.Handle:35", 2d, $"SetExampleValueHandler.Handle: returns {RuleLog.Shown(returnedAt35)} (line 35)"); return returnedAt35; }
        }
    }
}
