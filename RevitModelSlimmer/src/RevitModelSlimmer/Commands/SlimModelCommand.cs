using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitModelSlimmer.Services;

namespace RevitModelSlimmer.Commands
{
    /// <summary>Runs every clean-up step in sequence, then offers a compact save.</summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class SlimModelCommand : SlimmerCommandBase
    {
        public const string Name = "Slim Model";
        protected override string StepName { get { return Name; } }

        protected override StepResult Run(UIApplication app, Document doc)
        {
            var intro = new TaskDialog("Model Slimmer - " + Name)
            {
                TitleAutoPrefix = false,
                MainInstruction = "Run all clean-up steps?",
                MainContent = "The wizard runs these steps one after another. Each step shows its own list where you choose what to keep; " +
                              "cancelling a step skips it and moves on.\n\n" +
                              "  1. Unused families\n  2. Empty views\n  3. Unused view templates\n  4. Unused filters\n" +
                              "  5. Worksets (workshared models only)\n  6. Performance audit\n  7. Compact save",
                CommonButtons = TaskDialogCommonButtons.Cancel,
                DefaultButton = TaskDialogResult.Cancel
            };
            intro.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Start", "Go through all steps.");
            if (intro.Show() != TaskDialogResult.CommandLink1)
            {
                return StepResult.CancelledStep(Name);
            }

            var results = new List<StepResult>
            {
                PurgeUnusedFamiliesCommand.RunStep(app, doc),
                DeleteEmptyViewsCommand.RunStep(app, doc),
                PurgeUnusedViewTemplatesCommand.RunStep(app, doc),
                PurgeUnusedFiltersCommand.RunStep(app, doc)
            };
            if (doc.IsWorkshared)
            {
                results.Add(CleanWorksetsCommand.RunStep(app, doc));
            }
            results.Add(PerformanceAuditCommand.RunStep(app, doc));
            results.Add(CompactSaveCommand.RunStep(app, doc));

            var sb = new StringBuilder();
            foreach (StepResult r in results)
            {
                string state = r.Cancelled ? "skipped by user" : r.Skipped ? "skipped" : "done";
                sb.AppendLine(r.StepName + " (" + state + "): " + (r.Summary ?? string.Empty));
            }
            int totalItems = results.Sum(r => r.ItemsDeleted);
            int totalElements = results.Sum(r => r.ElementsRemoved);

            var report = new List<string> { "Slim Model wizard summary", string.Empty };
            report.AddRange(sb.ToString().Split('\n').Select(l => l.TrimEnd()));
            report.Add(string.Empty);
            report.Add("Total items deleted: " + totalItems + ", total elements removed: " + totalElements);
            string path = null;
            try { path = ReportWriter.Write(doc, Name, report); } catch { }

            RevitHelpers.ShowResult("Model Slimmer - " + Name, totalItems + " item(s) deleted, " + totalElements + " element(s) removed in total.", sb.ToString().TrimEnd(), path);
            return new StepResult { StepName = Name, ItemsDeleted = totalItems, ElementsRemoved = totalElements, Summary = "Wizard finished." };
        }
    }
}
