using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RevitModelSlimmer.Services;

namespace RevitModelSlimmer.Analysis
{
    public sealed class FamilyUsageInfo
    {
        public Family Family { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int TypeCount { get; set; }
        public int InstanceCount { get; set; }
        public int ReferenceCount { get; set; }
        public bool IsUnused { get; set; }
        /// <summary>True when Revit's own purge engine (2024+) confirmed the family is unused.</summary>
        public bool Verified { get; set; }
        public string Note { get; set; }
    }

    /// <summary>
    /// Works out which loadable families are not used anywhere in the project.
    ///
    /// A family counts as "used" when any of its types has a placed instance, or when any element or
    /// type in the model points at one of its types through an ElementId parameter (nested/shared
    /// families, profiles on sweeps and mullions, tags in legends, balusters on railings, and so on).
    /// On Revit 2024 and newer the result is cross-checked with Document.GetAllUnusedElements, the
    /// same engine that drives the built-in Purge Unused command.
    /// </summary>
    public static class FamilyUsageAnalyzer
    {
        public static IList<FamilyUsageInfo> Analyze(Document doc)
        {
            List<Family> families = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .Where(f => !f.IsInPlace)
                .ToList();

            var symbolToFamily = new Dictionary<ElementId, Family>();
            foreach (Family family in families)
            {
                foreach (ElementId symbolId in family.GetFamilySymbolIds())
                {
                    symbolToFamily[symbolId] = family;
                }
            }

            // 1. Direct instances.
            var instanceCounts = new Dictionary<ElementId, int>();
            foreach (FamilyInstance instance in new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>())
            {
                ElementId typeId = instance.GetTypeId();
                if (typeId == null || typeId == ElementId.InvalidElementId) continue;
                if (!symbolToFamily.TryGetValue(typeId, out Family family)) continue;
                instanceCounts.TryGetValue(family.Id, out int count);
                instanceCounts[family.Id] = count + 1;
            }

            // 2. Indirect references through ElementId parameters on every element and type.
            var referenceCounts = new Dictionary<ElementId, int>();
            ScanParameterReferences(new FilteredElementCollector(doc).WhereElementIsNotElementType(), symbolToFamily, referenceCounts);
            ScanParameterReferences(new FilteredElementCollector(doc).WhereElementIsElementType(), symbolToFamily, referenceCounts);
            ScanRailingReferences(doc, symbolToFamily, referenceCounts);

            // 3. Revit's own purge engine, when available.
            HashSet<ElementId> revitUnused = null;
            bool revitChecked = false;
#if REVIT2024_OR_GREATER
            try
            {
                var toCheck = new HashSet<ElementId>();
                foreach (Family family in families)
                {
                    toCheck.Add(family.Id);
                    foreach (ElementId symbolId in family.GetFamilySymbolIds()) toCheck.Add(symbolId);
                }
                ICollection<ElementId> unused = doc.GetAllUnusedElements(toCheck);
                revitUnused = new HashSet<ElementId>(unused);
                revitChecked = true;
            }
            catch
            {
                revitUnused = null;
                revitChecked = false;
            }
#endif

            var results = new List<FamilyUsageInfo>();
            foreach (Family family in families)
            {
                ISet<ElementId> symbolIds = family.GetFamilySymbolIds();
                instanceCounts.TryGetValue(family.Id, out int instances);
                referenceCounts.TryGetValue(family.Id, out int references);

                bool heuristicUnused = instances == 0 && references == 0;
                bool unused = heuristicUnused;
                bool verified = false;
                string note = string.Empty;

                if (revitChecked && revitUnused != null)
                {
                    bool revitSaysUnused = revitUnused.Contains(family.Id) ||
                                           (symbolIds.Count > 0 && symbolIds.All(revitUnused.Contains));
                    if (revitSaysUnused && heuristicUnused)
                    {
                        verified = true;
                    }
                    else if (revitSaysUnused && !heuristicUnused)
                    {
                        // Revit purge says unused but we found an ElementId reference: trust Revit, but flag it.
                        unused = true;
                        verified = true;
                        note = "Referenced only by parameters Revit considers purgeable.";
                    }
                    else if (!revitSaysUnused && heuristicUnused)
                    {
                        // Revit knows about a usage we cannot see (e.g. inside a nested family). Keep it.
                        unused = false;
                        note = "Revit reports this family as in use.";
                    }
                }

                if (!family.IsEditable)
                {
                    note = (note + " Non-editable family.").Trim();
                }

                results.Add(new FamilyUsageInfo
                {
                    Family = family,
                    Name = RevitHelpers.SafeName(family),
                    Category = RevitHelpers.SafeCategoryName(family),
                    TypeCount = symbolIds.Count,
                    InstanceCount = instances,
                    ReferenceCount = references,
                    IsUnused = unused,
                    Verified = verified,
                    Note = note
                });
            }

            return results.OrderBy(r => r.Category).ThenBy(r => r.Name).ToList();
        }

        private static void ScanParameterReferences(FilteredElementCollector collector, Dictionary<ElementId, Family> symbolToFamily, Dictionary<ElementId, int> referenceCounts)
        {
            foreach (Element element in collector)
            {
                ParameterSet parameters;
                try
                {
                    parameters = element.Parameters;
                }
                catch
                {
                    continue;
                }

                foreach (Parameter parameter in parameters)
                {
                    try
                    {
                        if (parameter.StorageType != StorageType.ElementId || !parameter.HasValue) continue;
                        ElementId value = parameter.AsElementId();
                        if (value == null || value == ElementId.InvalidElementId) continue;
                        if (!symbolToFamily.TryGetValue(value, out Family family)) continue;
                        // A symbol's own "family" parameter must not count as a usage of itself.
                        if (element is FamilySymbol symbol && family.Id == symbol.Family.Id) continue;
                        referenceCounts.TryGetValue(family.Id, out int count);
                        referenceCounts[family.Id] = count + 1;
                    }
                    catch
                    {
                        // Some parameters throw when read on certain elements; ignore them.
                    }
                }
            }
        }

        private static void ScanRailingReferences(Document doc, Dictionary<ElementId, Family> symbolToFamily, Dictionary<ElementId, int> referenceCounts)
        {
            try
            {
                foreach (RailingType railingType in new FilteredElementCollector(doc).OfClass(typeof(RailingType)).Cast<RailingType>())
                {
                    BalusterPlacement placement;
                    try { placement = railingType.BalusterPlacement; } catch { continue; }
                    if (placement == null) continue;

                    var balusterIds = new List<ElementId>();
                    try
                    {
                        BalusterPattern pattern = placement.BalusterPattern;
                        if (pattern != null)
                        {
                            int count = pattern.GetBalusterCount();
                            for (int i = 0; i < count; i++)
                            {
                                BalusterInfo info = pattern.GetBaluster(i);
                                if (info != null) balusterIds.Add(info.BalusterFamilyId);
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        PostPattern posts = placement.PostPattern;
                        if (posts != null)
                        {
                            if (posts.StartPost != null) balusterIds.Add(posts.StartPost.BalusterFamilyId);
                            if (posts.CornerPost != null) balusterIds.Add(posts.CornerPost.BalusterFamilyId);
                            if (posts.EndPost != null) balusterIds.Add(posts.EndPost.BalusterFamilyId);
                        }
                    }
                    catch { }

                    foreach (ElementId id in balusterIds)
                    {
                        if (id == null || !symbolToFamily.TryGetValue(id, out Family family)) continue;
                        referenceCounts.TryGetValue(family.Id, out int c);
                        referenceCounts[family.Id] = c + 1;
                    }
                }
            }
            catch
            {
                // Railing API differences must never break the whole analysis.
            }
        }
    }
}
