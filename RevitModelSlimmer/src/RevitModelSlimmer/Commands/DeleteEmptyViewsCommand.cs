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
    public sealed class DeleteEmptyViewsCommand : SlimmerCommandBase
    {
        public const string Name = "Empty Views";
        protected override string StepName { get { return Name; } }

        protected override StepResult Run(UIApplication app, Document doc)
        {
            return RunStep(app, doc);
        }

        public static StepResult RunStep(UIApplication app, Document doc)
        {
            IList<ViewInfo> analysis = ViewAnalyzer.Analyze(doc);

            // Empty views are recommended. Views that are not on a sheet are offered as well, but unchecked,
            // so the user can prune working views in the same pass if they want to.
            List<ViewInfo> candidates = analysis.Where(v => v.IsEmpty || !v.OnSheet).ToList();

            var items = new List<SelectionItem>();
            foreach (ViewInfo info in candidates)
            {
                var flags = new List<string>();
                flags.Add(info.ElementCount < 0 ? "elements: unknown" : "elements: " + info.ElementCount);
                flags.Add(info.OnSheet ? "on a sheet" : "not on any sheet");
                if (info.IsDependent) flags.Add("dependent view");
                if (info.HasTemplate) flags.Add("has template");

                bool recommended = info.IsEmpty && !info.OnSheet;
                var item = new SelectionItem
                {
                    Name = info.Name,
                    Group = info.Kind,
                    Details = string.Join(", ", flags),
                    Status = info.IsEmpty ? (info.OnSheet ? "Empty, on sheet" : "Empty") : "Not on sheet",
                    Recommended = recommended,
                    Weight = recommended ? 2 : (info.IsEmpty ? 1 : 0),
                    Tag = info
                };
                item.ElementIds.Add(info.View.Id);
                items.Add(item);
            }

            var report = new List<string>
            {
                "Views analysed:                    " + analysis.Count,
                "Empty views:                       " + analysis.Count(v => v.IsEmpty),
                "Views not placed on any sheet:     " + analysis.Count(v => !v.OnSheet),
                string.Empty,
                "Candidates:"
            };
            report.AddRange(candidates.Select(v => "  " + v.Kind + " / " + v.Name + " (elements: " + v.ElementCount + ", on sheet: " + (v.OnSheet ? "yes" : "no") + ")"));

            return CleanupFlow.RunSelectionStep(
                app, doc, Name,
                "Empty and unplaced views",
                "Empty views (nothing visible in them, ignoring levels, grids and view markers) are pre-checked. Views that are not on a " +
                "sheet are listed too but left unchecked. Sheets, schedules, view templates, the active view and the starting view are never listed.",
                items,
                "No empty views were found and every view is placed on a sheet.",
                report,
                w => w.SetColumnHeaders("View", "View type", "Details", "Status"));
        }
    }
}
