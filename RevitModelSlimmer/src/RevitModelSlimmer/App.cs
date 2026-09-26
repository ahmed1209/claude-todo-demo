using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using RevitModelSlimmer.Commands;

namespace RevitModelSlimmer
{
    /// <summary>Creates the "Model Slimmer" ribbon tab.</summary>
    public sealed class App : IExternalApplication
    {
        private const string TabName = "Model Slimmer";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Tab already exists (e.g. another add-in created it).
            }

            RibbonPanel clean = application.CreateRibbonPanel(TabName, "Clean");
            RibbonPanel optimize = application.CreateRibbonPanel(TabName, "Optimize");

            AddButton(clean, "Families", "Unused\nFamilies", typeof(PurgeUnusedFamiliesCommand), "families",
                "Lists loadable families with no instances and no references so you can choose which to delete.",
                "Scans instances, ElementId parameters and railing balusters, and on Revit 2024+ cross-checks with Revit's own purge engine. Deleting a family removes all of its types.");
            AddButton(clean, "Filters", "Unused\nFilters", typeof(PurgeUnusedFiltersCommand), "filters",
                "Lists view filters that no view or view template applies.",
                "Both rule-based and selection filters are checked. You pick which ones to delete.");
            AddButton(clean, "EmptyViews", "Empty\nViews", typeof(DeleteEmptyViewsCommand), "views",
                "Lists views with nothing in them, plus views that are not on a sheet.",
                "Empty views are pre-checked; views not on sheets are listed for review. Sheets, schedules, templates, the active view and the starting view are never touched.");
            AddButton(clean, "ViewTemplates", "Unused\nTemplates", typeof(PurgeUnusedViewTemplatesCommand), "templates",
                "Lists view templates that are not applied to any view or set as a default.",
                "Run this before 'Unused Filters' to also catch filters used only by unused templates.");
            AddButton(clean, "Worksets", "Clean\nWorksets", typeof(CleanWorksetsCommand), "worksets",
                "Lets you choose which user worksets to delete, moving or deleting their elements.",
                "Only available in workshared models. Worksets owned by other users cannot be deleted until relinquished.");

            AddButton(optimize, "Audit", "Performance\nAudit", typeof(PerformanceAuditCommand), "audit",
                "Finds what makes the file big and slow and deletes it after confirmation.",
                "Imported CAD, unused CAD/image/group definitions, unplaced rooms, missing links, point clouds, in-place families, oversized families, warnings and counts.");
            AddButton(optimize, "SlimModel", "Slim\nModel", typeof(SlimModelCommand), "slim",
                "Runs all clean-up steps in sequence, then compacts the file.",
                "Families, empty views, view templates, filters, worksets, performance audit and compact save, each with its own confirmation.");
            AddButton(optimize, "Compact", "Compact\nSave", typeof(CompactSaveCommand), "compact",
                "Saves (or synchronizes) with the Compact option so deleted content actually frees disk space.",
                "Deletions alone do not shrink an RVT file; a compact save rewrites it.");
            AddButton(optimize, "RevitPurge", "Revit\nPurge", typeof(RevitPurgeCommand), "purge",
                "Opens Revit's built-in Purge Unused dialog for materials, patterns, text types and other leftovers.",
                null);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static void AddButton(RibbonPanel panel, string name, string text, Type commandType, string icon, string tooltip, string longDescription)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            var data = new PushButtonData("ModelSlimmer_" + name, text, assemblyPath, commandType.FullName)
            {
                ToolTip = tooltip,
                LongDescription = longDescription ?? string.Empty
            };
            data.LargeImage = LoadIcon(icon + "32.png");
            data.Image = LoadIcon(icon + "16.png");
            panel.AddItem(data);
        }

        private static BitmapImage LoadIcon(string fileName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "RevitModelSlimmer.Resources.Icons." + fileName;
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
