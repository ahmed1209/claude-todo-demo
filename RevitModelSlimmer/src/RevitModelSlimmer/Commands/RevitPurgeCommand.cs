using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitModelSlimmer.Services;

namespace RevitModelSlimmer.Commands
{
    /// <summary>Opens Revit's own Purge Unused dialog, which handles materials, line patterns, text types and other leftovers.</summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class RevitPurgeCommand : SlimmerCommandBase
    {
        public const string Name = "Revit Purge Unused";
        protected override string StepName { get { return Name; } }

        protected override StepResult Run(UIApplication app, Document doc)
        {
            RevitCommandId id = RevitCommandId.LookupPostableCommandId(PostableCommand.PurgeUnused);
            if (id == null || !app.CanPostCommand(id))
            {
                RevitHelpers.Info("Model Slimmer", "Purge Unused is not available right now.");
                return StepResult.SkippedStep(Name, "Purge Unused not available.");
            }
            app.PostCommand(id);
            return new StepResult { StepName = Name, Summary = "Purge Unused posted." };
        }
    }
}
