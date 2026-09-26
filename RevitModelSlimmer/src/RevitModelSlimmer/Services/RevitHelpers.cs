using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitModelSlimmer.Services
{
    public static class RevitHelpers
    {
        public static string SafeName(Element element)
        {
            if (element == null)
            {
                return "(null)";
            }
            try
            {
                string name = element.Name;
                return string.IsNullOrEmpty(name) ? "(unnamed) " + element.Id : name;
            }
            catch
            {
                return "(id " + element.Id + ")";
            }
        }

        public static string SafeCategoryName(Element element)
        {
            try
            {
                return element?.Category?.Name ?? "(no category)";
            }
            catch
            {
                return "(no category)";
            }
        }

        public static string SafeCategoryName(Family family)
        {
            try
            {
                return family?.FamilyCategory?.Name ?? "(no category)";
            }
            catch
            {
                return "(no category)";
            }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L) return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("0.00") + " GB";
            if (bytes >= 1024L * 1024L) return (bytes / (1024.0 * 1024.0)).ToString("0.0") + " MB";
            if (bytes >= 1024L) return (bytes / 1024.0).ToString("0") + " KB";
            return bytes + " B";
        }

        /// <summary>Shows a Revit task dialog asking the user to confirm a deletion. Returns true to proceed.</summary>
        public static bool ConfirmDeletion(string title, string instruction, string details)
        {
            var dialog = new TaskDialog(title)
            {
                TitleAutoPrefix = false,
                MainIcon = TaskDialogIcon.TaskDialogIconWarning,
                MainInstruction = instruction,
                MainContent = details,
                CommonButtons = TaskDialogCommonButtons.Cancel,
                DefaultButton = TaskDialogResult.Cancel,
                AllowCancellation = true
            };
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Delete", "Remove the selected items from the model (undoable with Ctrl+Z).");
            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Cancel", "Keep everything as it is.");
            TaskDialogResult result = dialog.Show();
            return result == TaskDialogResult.CommandLink1;
        }

        public static void Info(string title, string instruction, string content = null)
        {
            var dialog = new TaskDialog(title)
            {
                TitleAutoPrefix = false,
                MainInstruction = instruction,
                MainContent = content ?? string.Empty,
                CommonButtons = TaskDialogCommonButtons.Close
            };
            dialog.Show();
        }

        public static bool YesNo(string title, string instruction, string content)
        {
            var dialog = new TaskDialog(title)
            {
                TitleAutoPrefix = false,
                MainInstruction = instruction,
                MainContent = content,
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
                DefaultButton = TaskDialogResult.No
            };
            return dialog.Show() == TaskDialogResult.Yes;
        }

        /// <summary>Shows a result dialog with an optional "Open report" link.</summary>
        public static void ShowResult(string title, string instruction, string content, string reportPath)
        {
            var dialog = new TaskDialog(title)
            {
                TitleAutoPrefix = false,
                MainInstruction = instruction,
                MainContent = content,
                CommonButtons = TaskDialogCommonButtons.Close
            };
            if (!string.IsNullOrEmpty(reportPath))
            {
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Open the report", reportPath);
            }
            if (dialog.Show() == TaskDialogResult.CommandLink1)
            {
                ReportWriter.TryOpen(reportPath);
            }
        }

        public static IEnumerable<View> AllViews(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>();
        }

        public static HashSet<ElementId> ViewIdsOnSheets(Document doc)
        {
            var ids = new HashSet<ElementId>();
            foreach (Viewport vp in new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>())
            {
                ids.Add(vp.ViewId);
            }
            foreach (ScheduleSheetInstance ssi in new FilteredElementCollector(doc).OfClass(typeof(ScheduleSheetInstance)).Cast<ScheduleSheetInstance>())
            {
                ids.Add(ssi.ScheduleId);
            }
            return ids;
        }

        public static string ViewTypeLabel(View view)
        {
            try
            {
                return view.ViewType.ToString();
            }
            catch
            {
                return "View";
            }
        }
    }
}
