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
    public sealed class PurgeUnusedFiltersCommand : SlimmerCommandBase
    {
        public const string Name = "Unused Filters";
        protected override string StepName { get { return Name; } }

        protected override StepResult Run(UIApplication app, Document doc)
        {
            return RunStep(app, doc);
        }

        public static StepResult RunStep(UIApplication app, Document doc)
        {
            IList<FilterUsageInfo> analysis = FilterUsageAnalyzer.Analyze(doc);
            List<FilterUsageInfo> unused = analysis.Where(f => f.IsUnused).ToList();

            var items = new List<SelectionItem>();
            foreach (FilterUsageInfo info in unused)
            {
                var item = new SelectionItem
                {
                    Name = info.Name,
                    Group = info.Kind,
                    Details = "Not applied to any view or view template.",
                    Status = "Unused",
                    Recommended = true,
                    Weight = 1,
                    Tag = info
                };
                item.ElementIds.Add(info.Filter.Id);
                items.Add(item);
            }

            var report = new List<string>
            {
                "Filters analysed:      " + analysis.Count,
                "Unused filters found:  " + unused.Count,
                string.Empty,
                "Filters in use:"
            };
            report.AddRange(analysis.Where(f => !f.IsUnused).Select(f => "  " + f.Name + " (views: " + f.ViewCount + ", templates: " + f.TemplateCount + ")"));
            report.Add(string.Empty);
            report.Add("Unused filters:");
            report.AddRange(unused.Select(f => "  " + f.Name + " [" + f.Kind + "]"));

            return CleanupFlow.RunSelectionStep(
                app, doc, Name,
                "Unused view filters",
                "Rule-based and selection filters that no view and no view template applies. Run 'Unused View Templates' first if " +
                "you also want filters that are only referenced by unused templates to show up here.",
                items,
                "No unused filters were found.",
                report,
                w => w.SetColumnHeaders("Filter", "Kind", "Details", "Status"));
        }
    }
}
