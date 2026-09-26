using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitModelSlimmer.Services;

namespace RevitModelSlimmer.Analysis
{
    public sealed class FilterUsageInfo
    {
        public FilterElement Filter { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public int ViewCount { get; set; }
        public int TemplateCount { get; set; }
        public bool IsUnused { get { return ViewCount == 0 && TemplateCount == 0; } }
    }

    /// <summary>Finds view filters (rule-based and selection-based) that no view or view template applies.</summary>
    public static class FilterUsageAnalyzer
    {
        public static IList<FilterUsageInfo> Analyze(Document doc)
        {
            var viewUse = new Dictionary<ElementId, int>();
            var templateUse = new Dictionary<ElementId, int>();

            foreach (View view in RevitHelpers.AllViews(doc))
            {
                ICollection<ElementId> filterIds;
                try
                {
                    if (!view.AreGraphicsOverridesAllowed()) continue;
                    filterIds = view.GetFilters();
                }
                catch
                {
                    continue;
                }

                Dictionary<ElementId, int> bucket = view.IsTemplate ? templateUse : viewUse;
                foreach (ElementId id in filterIds)
                {
                    bucket.TryGetValue(id, out int count);
                    bucket[id] = count + 1;
                }
            }

            var results = new List<FilterUsageInfo>();
            foreach (FilterElement filter in new FilteredElementCollector(doc).OfClass(typeof(FilterElement)).Cast<FilterElement>())
            {
                viewUse.TryGetValue(filter.Id, out int views);
                templateUse.TryGetValue(filter.Id, out int templates);
                results.Add(new FilterUsageInfo
                {
                    Filter = filter,
                    Name = RevitHelpers.SafeName(filter),
                    Kind = filter is SelectionFilterElement ? "Selection filter" : "Rule-based filter",
                    ViewCount = views,
                    TemplateCount = templates
                });
            }

            return results.OrderBy(r => r.Name).ToList();
        }
    }
}
