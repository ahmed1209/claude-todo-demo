using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.PointClouds;
using RevitModelSlimmer.Services;

namespace RevitModelSlimmer.Analysis
{
    public enum Severity
    {
        Info,
        Low,
        Medium,
        High
    }

    /// <summary>One deletable element found by the audit.</summary>
    public sealed class Finding
    {
        public string Check { get; set; }
        public string Name { get; set; }
        public string Details { get; set; }
        public Severity Severity { get; set; }
        public ElementId Id { get; set; }
        /// <summary>True when deleting it is safe enough to pre-check in the dialog.</summary>
        public bool Recommended { get; set; }
    }

    public sealed class AuditResult
    {
        public List<Finding> Findings { get; } = new List<Finding>();
        public List<string> SummaryLines { get; } = new List<string>();
    }

    public sealed class AuditOptions
    {
        /// <summary>Open every loaded family to measure its size. Accurate but slow on big models.</summary>
        public bool DeepScanFamilies { get; set; }
        public long HeavyFamilyBytes { get; set; } = 1024L * 1024L;        // 1 MB
        public long VeryHeavyFamilyBytes { get; set; } = 3L * 1024L * 1024L; // 3 MB
        public int ManyTypesThreshold { get; set; } = 100;
        public int ManyInstancesThreshold { get; set; } = 2000;
    }

    /// <summary>
    /// Looks for the usual suspects that make a Revit model big and slow, and reports each one as a
    /// deletable finding (with a recommendation) or as a summary line when nothing can be deleted safely.
    /// </summary>
    public static class PerformanceAuditor
    {
        public static AuditResult Run(Document doc, AuditOptions options)
        {
            var result = new AuditResult();
            options = options ?? new AuditOptions();

            AddFileStats(doc, result);
            AuditImportedCad(doc, result);
            AuditCadLinkTypes(doc, result);
            AuditImages(doc, result);
            AuditGroups(doc, result);
            AuditUnplacedSpatialElements(doc, result);
            AuditRevitLinks(doc, result);
            AuditPointClouds(doc, result);
            AuditInPlaceFamilies(doc, result);
            AuditFamilies(doc, result, options);
            AuditWarnings(doc, result);
            AuditCounts(doc, result);

            return result;
        }

        private static void AddFileStats(Document doc, AuditResult result)
        {
            result.SummaryLines.Add("Document: " + doc.Title);
            try
            {
                if (!string.IsNullOrEmpty(doc.PathName) && File.Exists(doc.PathName))
                {
                    long size = new FileInfo(doc.PathName).Length;
                    result.SummaryLines.Add("File size on disk: " + RevitHelpers.FormatBytes(size) + "  (" + doc.PathName + ")");
                }
                else
                {
                    result.SummaryLines.Add("File size on disk: n/a (unsaved or cloud model)");
                }
            }
            catch { }
            result.SummaryLines.Add("Workshared: " + (doc.IsWorkshared ? "yes" : "no"));
        }

        private static void AuditImportedCad(Document doc, AuditResult result)
        {
            int imported = 0, linked = 0;
            foreach (ImportInstance instance in new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).Cast<ImportInstance>())
            {
                bool isLinked = false;
                try { isLinked = instance.IsLinked; } catch { }
                if (isLinked)
                {
                    linked++;
                    continue;
                }
                imported++;

                string typeName = "(unknown)";
                try
                {
                    Element type = doc.GetElement(instance.GetTypeId());
                    if (type != null) typeName = RevitHelpers.SafeName(type);
                }
                catch { }

                string owner = "model";
                try
                {
                    if (instance.ViewSpecific && instance.OwnerViewId != ElementId.InvalidElementId)
                    {
                        owner = "view: " + RevitHelpers.SafeName(doc.GetElement(instance.OwnerViewId));
                    }
                }
                catch { }

                result.Findings.Add(new Finding
                {
                    Check = "Imported CAD",
                    Name = typeName,
                    Details = "Imported (not linked) CAD geometry in " + owner + ". Imports bloat the file, add layers/line patterns and slow regeneration. Prefer linking.",
                    Severity = Severity.High,
                    Id = instance.Id,
                    Recommended = true
                });
            }
            result.SummaryLines.Add("CAD imports: " + imported + " imported (deletable), " + linked + " linked");
        }

        private static void AuditCadLinkTypes(Document doc, AuditResult result)
        {
            var usedTypeIds = new HashSet<ElementId>();
            foreach (ImportInstance instance in new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).Cast<ImportInstance>())
            {
                try { usedTypeIds.Add(instance.GetTypeId()); } catch { }
            }

            int unused = 0;
            foreach (CADLinkType type in new FilteredElementCollector(doc).OfClass(typeof(CADLinkType)).Cast<CADLinkType>())
            {
                if (usedTypeIds.Contains(type.Id)) continue;
                unused++;
                result.Findings.Add(new Finding
                {
                    Check = "Unused CAD type",
                    Name = RevitHelpers.SafeName(type),
                    Details = "CAD link/import definition with no instance in any view. Still stored in the file.",
                    Severity = Severity.Medium,
                    Id = type.Id,
                    Recommended = true
                });
            }
            result.SummaryLines.Add("Unused CAD link/import types: " + unused);
        }

        private static void AuditImages(Document doc, AuditResult result)
        {
            var used = new HashSet<ElementId>();
            int instances = 0;
            foreach (ImageInstance image in new FilteredElementCollector(doc).OfClass(typeof(ImageInstance)).Cast<ImageInstance>())
            {
                instances++;
                try { used.Add(image.GetTypeId()); } catch { }
            }

            int unused = 0;
            foreach (ImageType type in new FilteredElementCollector(doc).OfClass(typeof(ImageType)).Cast<ImageType>())
            {
                if (used.Contains(type.Id)) continue;
                unused++;
                result.Findings.Add(new Finding
                {
                    Check = "Unused image",
                    Name = RevitHelpers.SafeName(type),
                    Details = "Raster image stored in the project but not placed in any view.",
                    Severity = Severity.Medium,
                    Id = type.Id,
                    Recommended = true
                });
            }
            result.SummaryLines.Add("Raster images: " + instances + " placed, " + unused + " unused image type(s)");
        }

        private static void AuditGroups(Document doc, AuditResult result)
        {
            int unused = 0, modelInstances = 0, detailInstances = 0;
            foreach (GroupType type in new FilteredElementCollector(doc).OfClass(typeof(GroupType)).Cast<GroupType>())
            {
                int count = 0;
                try { count = type.Groups.Size; } catch { }
                bool isModel = false;
                try { isModel = type.Category != null && type.Category.Name.IndexOf("Model", StringComparison.OrdinalIgnoreCase) >= 0; } catch { }
                if (isModel) modelInstances += count; else detailInstances += count;

                if (count > 0) continue;
                unused++;
                result.Findings.Add(new Finding
                {
                    Check = "Unused group",
                    Name = RevitHelpers.SafeName(type),
                    Details = (isModel ? "Model" : "Detail/attached") + " group type with no placed instances.",
                    Severity = Severity.Low,
                    Id = type.Id,
                    Recommended = true
                });
            }
            result.SummaryLines.Add("Groups: " + modelInstances + " model, " + detailInstances + " detail instances, " + unused + " unused group type(s)");
        }

        private static void AuditUnplacedSpatialElements(Document doc, AuditResult result)
        {
            int unplaced = 0, notEnclosed = 0;
            foreach (SpatialElement spatial in new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).Cast<SpatialElement>())
            {
                bool placed;
                try { placed = spatial.Location != null; } catch { placed = true; }
                string kind = RevitHelpers.SafeCategoryName(spatial);

                if (!placed)
                {
                    unplaced++;
                    result.Findings.Add(new Finding
                    {
                        Check = "Unplaced " + kind.TrimEnd('s'),
                        Name = SpatialName(spatial),
                        Details = kind + " that exists in schedules only and is not placed in the model.",
                        Severity = Severity.Low,
                        Id = spatial.Id,
                        Recommended = true
                    });
                    continue;
                }

                double area = 0;
                try { area = spatial.Area; } catch { }
                if (area <= 0)
                {
                    notEnclosed++;
                    result.Findings.Add(new Finding
                    {
                        Check = "Not enclosed " + kind.TrimEnd('s'),
                        Name = SpatialName(spatial),
                        Details = kind + " placed but not enclosed or redundant (zero area). Produces warnings on every regeneration.",
                        Severity = Severity.Low,
                        Id = spatial.Id,
                        Recommended = false
                    });
                }
            }
            result.SummaryLines.Add("Rooms/areas/spaces: " + unplaced + " unplaced, " + notEnclosed + " not enclosed / redundant");
        }

        private static string SpatialName(SpatialElement spatial)
        {
            string number = string.Empty;
            try { number = spatial.Number; } catch { }
            string name = RevitHelpers.SafeName(spatial);
            return string.IsNullOrEmpty(number) ? name : number + " - " + name;
        }

        private static void AuditRevitLinks(Document doc, AuditResult result)
        {
            int loaded = 0, problem = 0;
            foreach (RevitLinkType type in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkType)).Cast<RevitLinkType>())
            {
                LinkedFileStatus status;
                try { status = type.GetLinkedFileStatus(); } catch { status = LinkedFileStatus.Invalid; }
                if (status == LinkedFileStatus.Loaded)
                {
                    loaded++;
                    continue;
                }
                problem++;
                result.Findings.Add(new Finding
                {
                    Check = "Revit link (" + status + ")",
                    Name = RevitHelpers.SafeName(type),
                    Details = "Link is " + status + ". Unloaded or missing links still keep instances, worksets and settings in the file. Delete only if it is no longer needed.",
                    Severity = status == LinkedFileStatus.NotFound ? Severity.Medium : Severity.Low,
                    Id = type.Id,
                    Recommended = status == LinkedFileStatus.NotFound
                });
            }
            result.SummaryLines.Add("Revit links: " + loaded + " loaded, " + problem + " unloaded/missing");
        }

        private static void AuditPointClouds(Document doc, AuditResult result)
        {
            int count = 0;
            foreach (Element type in new FilteredElementCollector(doc).OfClass(typeof(PointCloudType)))
            {
                count++;
                result.Findings.Add(new Finding
                {
                    Check = "Point cloud",
                    Name = RevitHelpers.SafeName(type),
                    Details = "Point clouds are heavy to display. Remove when no longer needed for modelling.",
                    Severity = Severity.Medium,
                    Id = type.Id,
                    Recommended = false
                });
            }
            if (count > 0) result.SummaryLines.Add("Point clouds: " + count);
        }

        private static void AuditInPlaceFamilies(Document doc, AuditResult result)
        {
            int count = 0;
            foreach (Family family in new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>())
            {
                bool inPlace = false;
                try { inPlace = family.IsInPlace; } catch { }
                if (!inPlace) continue;
                count++;
                result.Findings.Add(new Finding
                {
                    Check = "In-place family",
                    Name = RevitHelpers.SafeName(family),
                    Details = "In-place " + RevitHelpers.SafeCategoryName(family) + ". In-place families are slow and cannot be reused; consider re-modelling as a loadable family. Deleting removes the geometry.",
                    Severity = Severity.Medium,
                    Id = family.Id,
                    Recommended = false
                });
            }
            result.SummaryLines.Add("In-place families: " + count);
        }

        private static void AuditFamilies(Document doc, AuditResult result, AuditOptions options)
        {
            List<Family> families = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>().Where(f => !f.IsInPlace).ToList();

            var instanceCounts = new Dictionary<ElementId, int>();
            var symbolToFamily = new Dictionary<ElementId, ElementId>();
            foreach (Family family in families)
            {
                foreach (ElementId sid in family.GetFamilySymbolIds()) symbolToFamily[sid] = family.Id;
            }
            foreach (FamilyInstance instance in new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>())
            {
                ElementId typeId = instance.GetTypeId();
                if (!symbolToFamily.TryGetValue(typeId, out ElementId familyId)) continue;
                instanceCounts.TryGetValue(familyId, out int c);
                instanceCounts[familyId] = c + 1;
            }

            result.SummaryLines.Add("Loadable families: " + families.Count);

            // Families with an excessive number of types.
            foreach (Family family in families)
            {
                int typeCount = family.GetFamilySymbolIds().Count;
                if (typeCount < options.ManyTypesThreshold) continue;
                instanceCounts.TryGetValue(family.Id, out int instances);
                result.Findings.Add(new Finding
                {
                    Check = "Family with many types",
                    Name = RevitHelpers.SafeName(family),
                    Details = typeCount + " types, " + instances + " instances (" + RevitHelpers.SafeCategoryName(family) + "). Every type is stored in the file; purge the unused types or split the family.",
                    Severity = Severity.Low,
                    Id = family.Id,
                    Recommended = false
                });
            }

            // Top families by instance count (report only).
            List<KeyValuePair<ElementId, int>> top = instanceCounts.OrderByDescending(kv => kv.Value).Take(10).ToList();
            if (top.Count > 0)
            {
                result.SummaryLines.Add("Most placed families:");
                foreach (KeyValuePair<ElementId, int> kv in top)
                {
                    Element family = doc.GetElement(kv.Key);
                    string flag = kv.Value >= options.ManyInstancesThreshold ? "  <-- heavy usage" : string.Empty;
                    result.SummaryLines.Add("    " + kv.Value.ToString().PadLeft(6) + "  " + RevitHelpers.SafeName(family) + flag);
                }
            }

            if (!options.DeepScanFamilies) return;

            // Deep scan: open each family and measure its saved size.
            string tempDir = Path.Combine(Path.GetTempPath(), "RevitModelSlimmer", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var sizes = new List<Tuple<Family, long, int>>();
            int failed = 0;
            try
            {
                foreach (Family family in families)
                {
                    if (!family.IsEditable) continue;
                    Document familyDoc = null;
                    try
                    {
                        familyDoc = doc.EditFamily(family);
                        string path = Path.Combine(tempDir, family.Id + ".rfa");
                        familyDoc.SaveAs(path, new SaveAsOptions { OverwriteExistingFile = true });
                        long bytes = new FileInfo(path).Length;
                        int elements = new FilteredElementCollector(familyDoc).WhereElementIsNotElementType().GetElementCount();
                        sizes.Add(Tuple.Create(family, bytes, elements));
                        try { File.Delete(path); } catch { }
                    }
                    catch
                    {
                        failed++;
                    }
                    finally
                    {
                        try { familyDoc?.Close(false); } catch { }
                    }
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }

            long total = sizes.Sum(s => s.Item2);
            result.SummaryLines.Add("Deep family scan: " + sizes.Count + " families measured, total " + RevitHelpers.FormatBytes(total) + (failed > 0 ? ", " + failed + " could not be opened" : string.Empty));
            foreach (Tuple<Family, long, int> entry in sizes.OrderByDescending(s => s.Item2).Take(15))
            {
                result.SummaryLines.Add("    " + RevitHelpers.FormatBytes(entry.Item2).PadLeft(9) + "  " + RevitHelpers.SafeName(entry.Item1));
            }

            foreach (Tuple<Family, long, int> entry in sizes.Where(s => s.Item2 >= options.HeavyFamilyBytes))
            {
                instanceCounts.TryGetValue(entry.Item1.Id, out int instances);
                bool veryHeavy = entry.Item2 >= options.VeryHeavyFamilyBytes;
                result.Findings.Add(new Finding
                {
                    Check = veryHeavy ? "Very heavy family" : "Heavy family",
                    Name = RevitHelpers.SafeName(entry.Item1),
                    Details = RevitHelpers.FormatBytes(entry.Item2) + ", " + entry.Item3 + " elements inside, " + instances + " instance(s) placed. " +
                              (instances == 0 ? "Not placed anywhere: safe to delete." : "Deleting removes its instances; consider replacing it with a lighter family."),
                    Severity = veryHeavy ? Severity.High : Severity.Medium,
                    Id = entry.Item1.Id,
                    Recommended = instances == 0
                });
            }
        }

        private static void AuditWarnings(Document doc, AuditResult result)
        {
            try
            {
                IList<FailureMessage> warnings = doc.GetWarnings();
                result.SummaryLines.Add("Warnings: " + warnings.Count + (warnings.Count > 500 ? "  <-- high; review with Manage > Warnings" : string.Empty));
                var byText = warnings.GroupBy(w => SafeDescription(w)).OrderByDescending(g => g.Count()).Take(8);
                foreach (IGrouping<string, FailureMessage> group in byText)
                {
                    result.SummaryLines.Add("    " + group.Count().ToString().PadLeft(6) + "  " + group.Key);
                }
            }
            catch
            {
                result.SummaryLines.Add("Warnings: could not be read");
            }
        }

        private static string SafeDescription(FailureMessage message)
        {
            try
            {
                string text = message.GetDescriptionText();
                return text.Length > 110 ? text.Substring(0, 110) + "..." : text;
            }
            catch
            {
                return "(unknown warning)";
            }
        }

        private static void AuditCounts(Document doc, AuditResult result)
        {
            try
            {
                int elements = new FilteredElementCollector(doc).WhereElementIsNotElementType().GetElementCount();
                int types = new FilteredElementCollector(doc).WhereElementIsElementType().GetElementCount();
                int views = RevitHelpers.AllViews(doc).Count(v => !v.IsTemplate && !(v is ViewSheet));
                int sheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)).GetElementCount();
                int templates = RevitHelpers.AllViews(doc).Count(v => v.IsTemplate);
                int filters = new FilteredElementCollector(doc).OfClass(typeof(FilterElement)).GetElementCount();
                int detailLines = new FilteredElementCollector(doc).OfClass(typeof(CurveElement)).Cast<CurveElement>().Count(c => c.CurveElementType == CurveElementType.DetailCurve);
                int designOptions = new FilteredElementCollector(doc).OfClass(typeof(DesignOption)).GetElementCount();
                int materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).GetElementCount();

                result.SummaryLines.Add("Elements: " + elements + " instances, " + types + " types, " + materials + " materials");
                result.SummaryLines.Add("Views: " + views + " views, " + sheets + " sheets, " + templates + " view templates, " + filters + " filters" +
                                        (views > 500 ? "  <-- many views; run 'Empty Views'" : string.Empty));
                result.SummaryLines.Add("Detail lines: " + detailLines + (detailLines > 20000 ? "  <-- very many; consider detail components" : string.Empty));
                if (designOptions > 0) result.SummaryLines.Add("Design options: " + designOptions + " (accept primary options that are decided)");
            }
            catch { }
        }
    }
}
