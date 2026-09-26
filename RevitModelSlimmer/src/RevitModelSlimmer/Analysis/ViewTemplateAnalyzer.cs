using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitModelSlimmer.Services;

namespace RevitModelSlimmer.Analysis
{
    public sealed class ViewTemplateInfo
    {
        public View Template { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public int AppliedToViews { get; set; }
        public int DefaultForViewTypes { get; set; }
        public bool IsUnused { get { return AppliedToViews == 0 && DefaultForViewTypes == 0; } }
    }

    /// <summary>Finds view templates that are neither applied to a view nor set as default on a view family type.</summary>
    public static class ViewTemplateAnalyzer
    {
        public static IList<ViewTemplateInfo> Analyze(Document doc)
        {
            var applied = new Dictionary<ElementId, int>();
            var defaults = new Dictionary<ElementId, int>();
            var templates = new List<View>();

            foreach (View view in RevitHelpers.AllViews(doc))
            {
                if (view.IsTemplate)
                {
                    templates.Add(view);
                    continue;
                }
                ElementId templateId;
                try { templateId = view.ViewTemplateId; } catch { continue; }
                if (templateId == null || templateId == ElementId.InvalidElementId) continue;
                applied.TryGetValue(templateId, out int count);
                applied[templateId] = count + 1;
            }

            foreach (ViewFamilyType vft in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>())
            {
                ElementId templateId;
                try { templateId = vft.DefaultTemplateId; } catch { continue; }
                if (templateId == null || templateId == ElementId.InvalidElementId) continue;
                defaults.TryGetValue(templateId, out int count);
                defaults[templateId] = count + 1;
            }

            var results = new List<ViewTemplateInfo>();
            foreach (View template in templates)
            {
                applied.TryGetValue(template.Id, out int a);
                defaults.TryGetValue(template.Id, out int d);
                results.Add(new ViewTemplateInfo
                {
                    Template = template,
                    Name = RevitHelpers.SafeName(template),
                    Kind = RevitHelpers.ViewTypeLabel(template),
                    AppliedToViews = a,
                    DefaultForViewTypes = d
                });
            }

            return results.OrderBy(r => r.Kind).ThenBy(r => r.Name).ToList();
        }
    }
}
