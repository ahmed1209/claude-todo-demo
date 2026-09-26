using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RevitModelSlimmer.Services
{
    /// <summary>Writes a plain-text report of what was analysed and deleted so the user has an audit trail.</summary>
    public static class ReportWriter
    {
        public static string ReportsDirectory
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RevitModelSlimmer", "Reports");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string Write(Document doc, string stepName, IEnumerable<string> lines)
        {
            string title = string.IsNullOrEmpty(doc.Title) ? "Untitled" : doc.Title;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                title = title.Replace(c, '_');
            }

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string safeStep = stepName.Replace(' ', '_');
            string path = Path.Combine(ReportsDirectory, title + "_" + safeStep + "_" + stamp + ".txt");

            var sb = new StringBuilder();
            sb.AppendLine("Revit Model Slimmer report");
            sb.AppendLine("Step:      " + stepName);
            sb.AppendLine("Document:  " + title);
            sb.AppendLine("Path:      " + (string.IsNullOrEmpty(doc.PathName) ? "(unsaved / cloud)" : doc.PathName));
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine(new string('-', 72));
            foreach (string line in lines)
            {
                sb.AppendLine(line);
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        public static void TryOpen(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            }
            catch
            {
                // Opening the report is a convenience only.
            }
        }

        public static IEnumerable<string> Describe(DeletionResult result)
        {
            yield return "Requested: " + result.Requested + ", deleted: " + result.Succeeded + ", failed: " + result.Failed +
                         ", total elements removed (incl. dependents): " + result.TotalElementsRemoved;
            yield return string.Empty;
            foreach (string line in result.Log)
            {
                yield return "  OK   " + line;
            }
            foreach (string line in result.Failures)
            {
                yield return "  FAIL " + line;
            }
            if (!result.Log.Any() && !result.Failures.Any())
            {
                yield return "  (nothing to do)";
            }
        }
    }
}
