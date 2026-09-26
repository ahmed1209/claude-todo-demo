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
    public sealed class PurgeUnusedFamiliesCommand : SlimmerCommandBase
    {
        public const string Name = "Unused Families";
        protected override string StepName { get { return Name; } }

        protected override StepResult Run(UIApplication app, Document doc)
        {
            return RunStep(app, doc);
        }

        public static StepResult RunStep(UIApplication app, Document doc)
        {
            IList<FamilyUsageInfo> analysis = FamilyUsageAnalyzer.Analyze(doc);
            List<FamilyUsageInfo> unused = analysis.Where(f => f.IsUnused).ToList();

            var items = new List<SelectionItem>();
            foreach (FamilyUsageInfo info in unused)
            {
                var item = new SelectionItem
                {
                    Name = info.Name,
                    Group = info.Category,
                    Details = info.TypeCount + " type(s), 0 instances, 0 references" + (string.IsNullOrEmpty(info.Note) ? string.Empty : ". " + info.Note),
                    Status = info.Verified ? "Unused (verified by Revit)" : "Unused (heuristic)",
                    Recommended = true,
                    Weight = info.Verified ? 2 : 1,
                    Tag = info
                };
                item.ElementIds.Add(info.Family.Id);
                items.Add(item);
            }

            var report = new List<string>
            {
                "Loadable families analysed: " + analysis.Count,
                "Unused families found:      " + unused.Count,
                string.Empty,
                "Unused families:"
            };
            report.AddRange(unused.Select(f => "  " + f.Category + " / " + f.Name + " (" + f.TypeCount + " types)" + (f.Verified ? " [verified]" : " [heuristic]")));

            return CleanupFlow.RunSelectionStep(
                app, doc, Name,
                "Unused families",
                "Families with no placed instances and no references from other elements. Untick any family you want to keep " +
                "(for example families you will place later). Deleting a family removes all its types.",
                items,
                "No unused families were found. Every loadable family has at least one placed instance or reference.",
                report,
                w => w.SetColumnHeaders("Family", "Category", "Details", "Status"));
        }
    }
}
