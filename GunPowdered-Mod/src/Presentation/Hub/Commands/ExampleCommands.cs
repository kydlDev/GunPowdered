using KydlCore.Common;
using KydlCore.Core;
using GunPowdered.Features.Example.Application;
using GunPowdered.Core;

namespace GunPowdered.Presentation.Hub.Commands
{
    /// <summary>The hub's callbacks turned into commands (spec "UI interaction flow": KydlCore UI -> mod callback -> dispatcher -> handler).</summary>
    internal static class ExampleCommands
    {
        /// Expected outcome: the handler's response (logged; Failed is a warning).
        internal static OperationResponse<float> SetValue(CommandDispatcher commands, float requested)
        {
            var r = commands.Dispatch(new SetExampleValueCommand(requested));
            if (r.Status == ResponseStatus.Failed) ModLog.Warn("ExampleCommands: SetValue " + requested + " - " + r);
            else ModLog.Verbose("ExampleCommands: SetValue " + requested + " - " + r);
            { var returnedAt17 = r; if (RuleLog.Due("ExampleCommands.SetValue:17", 2d)) ModLog.Every("ExampleCommands.SetValue:17", 2d, $"ExampleCommands.SetValue: returns {returnedAt17} (line 17)"); return returnedAt17; }
        }
    }
}
