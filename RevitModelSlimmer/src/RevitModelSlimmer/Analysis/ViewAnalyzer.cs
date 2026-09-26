using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitModelSlimmer.Services;

namespace RevitModelSlimmer.Analysis
{
    public sealed class ViewInfo
    {
        public View View { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public int ElementCount { get; set; }
        public bool OnSheet { get; set; }
        public bool IsDependent { get; set; }
        public bool HasTemplate { get; set; }
        public bool IsEmpty { get { return ElementCount == 0; } }
    }

    /// <summary>
    /// Lists graphical views that are candidates for deletion: views with nothing visible in them
    /// ("empty") and, as a secondary group, views that are not placed on any sheet.
    /// Sheets, schedules, view templates, the active view and the starting view are never listed.
    /// </summary>
    public static class ViewAnalyzer
    {
        private static readonly BuiltInCategory[] IgnoredCategories =
        {
            BuiltInCategory.OST_Cameras,
            BuiltInCategory.OST_Viewers,
            BuiltInCategory.OST_Views,
            BuiltInCategory.OST_Elev,
            BuiltInCategory.OST_Levels,
            BuiltInCategory.OST_Grids,
            BuiltInCategory.OST_CLines,
            BuiltInCategory.OST_SectionBox,
            BuiltInCategory.OST_VolumeOfInterest
        };

        private static readonly ViewType[] SkippedViewTypes =
        {
            ViewType.Undefined,
            ViewType.Internal,
            ViewType.ProjectBrowser,
            ViewType.SystemBrowser,
            ViewType.DrawingSheet,
            ViewType.Schedule,
            ViewType.ColumnSchedule,
            ViewType.PanelSchedule,
            ViewType.CostReport,
            ViewType.LoadsReport,
            ViewType.PresureLossReport
        };

        public static IList<ViewInfo> Analyze(Document doc)
        {
            HashSet<ElementId> onSheets = RevitHelpers.ViewIdsOnSheets(doc);
            ElementId activeId = doc.ActiveView?.Id ?? ElementId.InvalidElementId;
            ElementId startingId = ElementId.InvalidElementId;
            try
            {
                startingId = StartingViewSettings.GetStartingViewSettings(doc)?.ViewId ?? ElementId.InvalidElementId;
            }
            catch { }

            var ignoreFilter = new ElementMulticategoryFilter(IgnoredCategories.ToList(), true);
            var results = new List<ViewInfo>();

            foreach (View view in RevitHelpers.AllViews(doc))
            {
                try
                {
                    if (view.IsTemplate) continue;
                    if (SkippedViewTypes.Contains(view.ViewType)) continue;
                    if (view is ViewSheet || view is ViewSchedule) continue;
                    if (view.Id == activeId || view.Id == startingId) continue;

                    int count;
                    try
                    {
                        count = new FilteredElementCollector(doc, view.Id)
                            .WhereElementIsNotElementType()
                            .WherePasses(ignoreFilter)
                            .GetElementCount();
                    }
                    catch
                    {
                        // Some views (e.g. certain reports or broken views) cannot be enumerated; treat as non-empty.
                        count = -1;
                    }

                    ElementId primary = ElementId.InvalidElementId;
                    try { primary = view.GetPrimaryViewId(); } catch { }

                    results.Add(new ViewInfo
                    {
                        View = view,
                        Name = RevitHelpers.SafeName(view),
                        Kind = RevitHelpers.ViewTypeLabel(view),
                        ElementCount = count,
                        OnSheet = onSheets.Contains(view.Id),
                        IsDependent = primary != null && primary != ElementId.InvalidElementId,
                        HasTemplate = view.ViewTemplateId != null && view.ViewTemplateId != ElementId.InvalidElementId
                    });
                }
                catch
                {
                    // Skip views that misbehave rather than abort the whole scan.
                }
            }

            return results.OrderBy(r => r.Kind).ThenBy(r => r.Name).ToList();
        }
    }
}
