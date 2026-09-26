using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitModelSlimmer.Services;
using RevitModelSlimmer.UI;

namespace RevitModelSlimmer.Commands
{
    /// <summary>Shared "list -> pick -> confirm -> delete -> report" flow used by most clean-up commands.</summary>
    internal static class CleanupFlow
    {
        public static StepResult RunSelectionStep(
            UIApplication app,
            Document doc,
            string stepName,
            string header,
            string subHeader,
            IList<SelectionItem> items,
            string nothingFoundMessage,
            IEnumerable<string> analysisReportLines = null,
            Action<SelectionWindow> configure = null)
        {
            var result = new StepResult { StepName = stepName };
            if (analysisReportLines != null) result.ReportLines.AddRange(analysisReportLines);

            if (items == null || items.Count == 0)
            {
                result.Summary = nothingFoundMessage;
                result.ReportLines.Add(nothingFoundMessage);
                RevitHelpers.Info("Model Slimmer - " + stepName, nothingFoundMessage);
                return result;
            }

            var window = new SelectionWindow(app.MainWindowHandle, "Model Slimmer - " + stepName, header, subHeader, items);
            configure?.Invoke(window);
            bool? ok = window.ShowDialog();
            if (ok != true)
            {
                return StepResult.CancelledStep(stepName);
            }

            IList<SelectionItem> chosen = window.CheckedItems;
            if (chosen.Count == 0)
            {
                return StepResult.CancelledStep(stepName);
            }

            int elementCount = chosen.Sum(i => Math.Max(1, i.ElementIds.Count));
            string preview = string.Join("\n", chosen.Take(12).Select(i => "  - " + i.Name));
            if (chosen.Count > 12) preview += "\n  ... and " + (chosen.Count - 12) + " more";

            bool confirmed = RevitHelpers.ConfirmDeletion(
                "Model Slimmer - " + stepName,
                "Delete " + chosen.Count + " item(s) (" + elementCount + " element(s))?",
                "This removes the items below from the model. You can undo with Ctrl+Z as long as the file is not saved.\n\n" + preview);
            if (!confirmed)
            {
                return StepResult.CancelledStep(stepName);
            }

            var candidates = new List<DeletionCandidate>();
            foreach (SelectionItem item in chosen)
            {
                if (item.ElementIds.Count == 0) continue;
                for (int i = 0; i < item.ElementIds.Count; i++)
                {
                    string name = item.ElementIds.Count == 1 ? item.Name : item.Name + " [" + (i + 1) + "/" + item.ElementIds.Count + "]";
                    candidates.Add(new DeletionCandidate(item.ElementIds[i], name));
                }
            }

            DeletionResult deletion = DeletionService.Delete(doc, "Model Slimmer: " + stepName, candidates);

            result.ItemsRequested = deletion.Requested;
            result.ItemsDeleted = deletion.Succeeded;
            result.ElementsRemoved = deletion.TotalElementsRemoved;
            result.ReportLines.Add(string.Empty);
            result.ReportLines.Add("Deletion");
            result.ReportLines.AddRange(ReportWriter.Describe(deletion));
            result.Summary = deletion.Succeeded + " of " + deletion.Requested + " item(s) deleted, " + deletion.TotalElementsRemoved + " element(s) removed" +
                             (deletion.Failed > 0 ? ", " + deletion.Failed + " failed" : string.Empty) + ".";

            string reportPath = null;
            try { reportPath = ReportWriter.Write(doc, stepName, result.ReportLines); } catch { }

            string content = result.Summary;
            if (deletion.Failed > 0)
            {
                content += "\n\nFailed:\n" + string.Join("\n", deletion.Failures.Take(8).Select(f => "  - " + f));
                if (deletion.Failures.Count > 8) content += "\n  ... see report for the rest";
            }
            RevitHelpers.ShowResult("Model Slimmer - " + stepName, stepName + " finished", content, reportPath);
            return result;
        }
    }
}
