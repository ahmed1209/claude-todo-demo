using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using RevitModelSlimmer.UI;

namespace RevitModelSlimmer.Analysis
{
    /// <summary>Builds the rows for the workset clean-up dialog.</summary>
    public static class WorksetAnalyzer
    {
        public static IList<WorksetRow> Analyze(Document doc)
        {
            WorksetId activeId = doc.GetWorksetTable().GetActiveWorksetId();
            IList<Workset> worksets = new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset).ToWorksets();

            var rows = new List<WorksetRow>();
            foreach (Workset workset in worksets)
            {
                int count;
                try
                {
                    count = new FilteredElementCollector(doc)
                        .WherePasses(new ElementWorksetFilter(workset.Id))
                        .GetElementCount();
                }
                catch
                {
                    count = -1;
                }

                var notes = new List<string>();
                if (workset.IsDefaultWorkset) notes.Add("default workset");
                if (workset.Id == activeId) notes.Add("active workset");
                if (!workset.IsOpen) notes.Add("closed");
                if (!workset.IsEditable) notes.Add("not editable by you");
                if (count == 0) notes.Add("empty");

                rows.Add(new WorksetRow
                {
                    Id = workset.Id,
                    Name = workset.Name,
                    ElementCount = count,
                    Owner = string.IsNullOrEmpty(workset.Owner) ? "-" : workset.Owner,
                    IsEditable = workset.IsEditable,
                    IsDefault = workset.IsDefaultWorkset,
                    IsActive = workset.Id == activeId,
                    Notes = string.Join(", ", notes)
                });
            }

            return rows.OrderBy(r => r.Name).ToList();
        }
    }
}
