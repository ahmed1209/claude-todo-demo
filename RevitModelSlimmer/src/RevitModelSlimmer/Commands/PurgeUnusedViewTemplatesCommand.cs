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
    public sealed class PurgeUnusedViewTemplatesCommand : SlimmerCommandBase
    {
        public const string Name = "Unused View Templates";
        protected override string StepName { get { return Name; } }

        protected override StepResult Run(UIApplication app, Document doc)
        {
            return RunStep(app, doc);
        }

        public static StepResult RunStep(UIApplication app, Document doc)
        {
            IList<ViewTemplateInfo> analysis = ViewTemplateAnalyzer.Analyze(doc);
            List<ViewTemplateInfo> unused = analysis.Where(t => t.IsUnused).ToList();

            var items = new List<SelectionItem>();
            foreach (ViewTemplateInfo info in unused)
            {
                var item = new SelectionItem
                {
                    Name = info.Name,
                    Group = info.Kind,
                    Details = "Not applied to any view and not the default template of any view type.",
                    Status = "Unused",
                    Recommended = true,
                    Weight = 1,
                    Tag = info
                };
                item.ElementIds.Add(info.Template.Id);
                items.Add(item);
            }

            var report = new List<string>
            {
                "View templates analysed: " + analysis.Count,
                "Unused view templates:   " + unused.Count,
                string.Empty,
                "In use:"
            };
            report.AddRange(analysis.Where(t => !t.IsUnused).Select(t => "  " + t.Name + " (views: " + t.AppliedToViews + ", default for view types: " + t.DefaultForViewTypes + ")"));
            report.Add(string.Empty);
            report.Add("Unused:");
            report.AddRange(unused.Select(t => "  " + t.Name + " [" + t.Kind + "]"));

            return CleanupFlow.RunSelectionStep(
                app, doc, Name,
                "Unused view templates",
                "View templates that are not applied to any view and are not the default template for a view type. " +
                "Keep templates that belong to your office standard even if this file does not use them yet.",
                items,
                "No unused view templates were found.",
                report,
                w => w.SetColumnHeaders("View template", "View type", "Details", "Status"));
        }
    }
}
