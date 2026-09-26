using System;
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
    public sealed class CleanWorksetsCommand : SlimmerCommandBase
    {
        public const string Name = "Worksets";
        protected override string StepName { get { return Name; } }

        protected override StepResult Run(UIApplication app, Document doc)
        {
            return RunStep(app, doc);
        }

        public static StepResult RunStep(UIApplication app, Document doc)
        {
            var result = new StepResult { StepName = Name };

            if (!doc.IsWorkshared)
            {
                result.Skipped = true;
                result.Summary = "The model is not workshared, so there are no user worksets to clean.";
                RevitHelpers.Info("Model Slimmer - " + Name, result.Summary);
                return result;
            }

            IList<WorksetRow> rows = WorksetAnalyzer.Analyze(doc);
            if (rows.Count <= 1)
            {
                result.Summary = "The model has only one user workset; it cannot be deleted.";
                RevitHelpers.Info("Model Slimmer - " + Name, result.Summary);
                return result;
            }

            result.ReportLines.Add("User worksets: " + rows.Count);
            result.ReportLines.AddRange(rows.Select(r => "  " + r.Name + " (elements: " + r.ElementCount + ", owner: " + r.Owner + ", editable: " + r.EditableText + (string.IsNullOrEmpty(r.Notes) ? string.Empty : ", " + r.Notes) + ")"));

            var window = new WorksetWindow(app.MainWindowHandle, rows);
            if (window.ShowDialog() != true)
            {
                return StepResult.CancelledStep(Name);
            }

            IList<WorksetRow> chosen = window.CheckedRows;
            if (chosen.Count == 0)
            {
                return StepResult.CancelledStep(Name);
            }

            bool deleteElements = window.DeleteElements;
            WorksetRow target = window.TargetWorkset;
            int affected = chosen.Sum(r => Math.Max(0, r.ElementCount));

            string what = deleteElements
                ? "The " + affected + " element(s) in these worksets will be DELETED as well."
                : "The " + affected + " element(s) in these worksets will be moved to '" + (target?.Name ?? "?") + "'.";
            bool confirmed = RevitHelpers.ConfirmDeletion(
                "Model Slimmer - " + Name,
                "Delete " + chosen.Count + " workset(s)?",
                what + "\n\n" + string.Join("\n", chosen.Take(12).Select(r => "  - " + r.Name + " (" + r.ElementCount + " elements)")) +
                (chosen.Count > 12 ? "\n  ... and " + (chosen.Count - 12) + " more" : string.Empty));
            if (!confirmed)
            {
                return StepResult.CancelledStep(Name);
            }

            int deleted = 0;
            var failures = new List<string>();
            using (var tx = new Transaction(doc, "Model Slimmer: Delete worksets"))
            {
                tx.Start();
                FailureHandlingOptions options = tx.GetFailureHandlingOptions();
                options.SetFailuresPreprocessor(new FailureSwallower());
                tx.SetFailureHandlingOptions(options);

                foreach (WorksetRow row in chosen)
                {
                    using (var sub = new SubTransaction(doc))
                    {
                        try
                        {
                            sub.Start();
                            DeleteWorksetSettings settings = deleteElements
                                ? new DeleteWorksetSettings(DeleteWorksetOption.DeleteAllElements, WorksetId.InvalidWorksetId)
                                : new DeleteWorksetSettings(DeleteWorksetOption.MoveElementsToWorkset, target.Id);
                            WorksetTable.DeleteWorkset(doc, row.Id, settings);
                            sub.Commit();
                            deleted++;
                            result.ReportLines.Add("  OK   " + row.Name + " deleted" + (deleteElements ? " with its elements" : ", elements moved to " + target.Name));
                        }
                        catch (Exception ex)
                        {
                            if (sub.HasStarted() && !sub.HasEnded()) sub.RollBack();
                            failures.Add(row.Name + ": " + ex.Message);
                            result.ReportLines.Add("  FAIL " + row.Name + ": " + ex.Message);
                        }
                    }
                }

                if (deleted > 0)
                {
                    if (tx.Commit() != TransactionStatus.Committed)
                    {
                        failures.Add("Revit rolled the transaction back; no workset was deleted.");
                        deleted = 0;
                    }
                }
                else
                {
                    tx.RollBack();
                }
            }

            result.ItemsRequested = chosen.Count;
            result.ItemsDeleted = deleted;
            result.ElementsRemoved = deleteElements ? affected : 0;
            result.Summary = deleted + " of " + chosen.Count + " workset(s) deleted" + (failures.Count > 0 ? ", " + failures.Count + " failed" : string.Empty) + ".";

            string reportPath = null;
            try { reportPath = ReportWriter.Write(doc, Name, result.ReportLines); } catch { }

            string content = result.Summary;
            if (failures.Count > 0)
            {
                content += "\n\nFailed (usually because another user owns the workset or elements in it):\n" +
                           string.Join("\n", failures.Take(8).Select(f => "  - " + f));
            }
            content += "\n\nRemember to synchronize with central so the change reaches the other users.";
            RevitHelpers.ShowResult("Model Slimmer - " + Name, "Workset clean-up finished", content, reportPath);
            return result;
        }
    }
}
