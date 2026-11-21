using KydlCore.Core;
using GunPowdered.Features.Example.Domain;
using GunPowdered.Features.Example.Infrastructure;
using GunPowdered.Presentation.Hub.Commands;
using GunPowdered.Ui;
using GunPowdered.Core;
using KydlCore.Common;

namespace GunPowdered.Presentation.Hub.ViewModels
{
    /// <summary>
    /// WHAT the example tab shows (MIGRATION_DECISIONS A6): built from the feature's state every time the tab opens. The data contract
    /// (<see cref="ExamplePage"/>) lives in the Unity project's Runtime folder so the Unity preview can fill it too; HOW it is drawn is
    /// ExampleTabView there. A UI change goes through the command dispatcher, never straight into the feature.
    /// </summary>
    internal static class ExamplePageModel
    {
        internal static ExamplePage Build(ExampleFeature feature, CommandDispatcher commands)
        {
            if (RuleLog.Due("ExamplePageModel.Build", 2d)) ModLog.Every("ExamplePageModel.Build", 2d, $"ExamplePageModel.Build(feature={RuleLog.Shown(feature)}, commands={RuleLog.Shown(commands)})");
            return new ExamplePage(feature.Value, ExampleRules.Min, ExampleRules.Max, v => ExampleCommands.SetValue(commands, v));
        }
    }
}
