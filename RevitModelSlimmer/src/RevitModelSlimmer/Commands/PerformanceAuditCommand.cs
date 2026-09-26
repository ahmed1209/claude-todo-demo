using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitModelSlimmer.Analysis;
using RevitModelSlimmer.Services;
using RevitModelSlimmer.UI;

namespace RevitModelSlimmer.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class PerformanceAuditCommand : SlimmerCommandBase
    {
        public const string Name = "Performance Audit";
        protected override string StepName { get { return Name; } }

        protected override StepResult Run(UIApplication app, Document doc)
        {
            return RunStep(app, doc);
        }

        public static StepResult RunStep(UIApplication app, Document doc)
        {
            var dialog = new TaskDialog("Model Slimmer - " + Name)
            {
                TitleAutoPrefix = false,
                MainInstruction = "How thorough should the audit be?",
                MainContent = "The audit looks for imported CAD, unused CAD/image/group definitions, unplaced rooms, broken links, " +
                              "point clouds, in-place families, oversized families and warning hot-spots. Nothing is deleted until you confirm.",
                CommonButtons = TaskDialogCommonButtons.Cancel,
                DefaultButton = TaskDialogResult.Cancel
            };
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Quick audit", "Scans the project database only. Takes seconds.");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Deep audit (measures every family)", "Also opens each loaded family to measure its real size. Can take minutes on large models.");
            TaskDialogResult choice = dialog.Show();
            if (choice != TaskDialogResult.CommandLink1 && choice != TaskDialogResult.CommandLink2)
            {
                return StepResult.CancelledStep(Name);
            }

            var options = new AuditOptions { DeepScanFamilies = choice == TaskDialogResult.CommandLink2 };
            AuditResult audit = PerformanceAuditor.Run(doc, options);

            var items = new List<SelectionItem>();
            foreach (Finding finding in audit.Findings)
            {
                var item = new SelectionItem
                {
                    Name = finding.Name,
                    Group = finding.Check,
                    Details = finding.Details,
                    Status = finding.Severity + (finding.Recommended ? " - delete" : " - review"),
                    Recommended = finding.Recommended,
                    Weight = (int)finding.Severity * 2 + (finding.Recommended ? 1 : 0),
                    Tag = finding
                };
                item.ElementIds.Add(finding.Id);
                items.Add(item);
            }

            var report = new List<string> { "Audit summary" };
            report.AddRange(audit.SummaryLines.Select(l => "  " + l));
            report.Add(string.Empty);
            report.Add("Findings: " + audit.Findings.Count);
            report.AddRange(audit.Findings.Select(f => "  [" + f.Severity + "] " + f.Check + ": " + f.Name + (f.Recommended ? " (recommended)" : string.Empty)));

            string summary = string.Join("\n", audit.SummaryLines);

            if (items.Count == 0)
            {
                string path = null;
                try { path = ReportWriter.Write(doc, Name, report); } catch { }
                RevitHelpers.ShowResult("Model Slimmer - " + Name, "No deletable performance problems were found.", summary, path);
                return new StepResult { StepName = Name, Summary = "No deletable findings." };
            }

            return CleanupFlow.RunSelectionStep(
                app, doc, Name,
                "Performance audit",
                "Each row is something that makes the file bigger or slower. Rows marked 'delete' are safe to remove and are pre-checked; " +
                "rows marked 'review' change the model's content (in-place families, heavy families with instances, unloaded links) and are left for you to decide.",
                items,
                "No deletable performance problems were found.",
                report,
                w =>
                {
                    w.SetColumnHeaders("Element", "Check", "Why it matters", "Severity");
                    w.SetSummary("Audit summary (file, counts, warnings, heaviest families)", summary);
                });
        }
    }
}
