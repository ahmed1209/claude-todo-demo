using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitModelSlimmer.Services;

namespace RevitModelSlimmer.Commands
{
    /// <summary>
    /// Saves (or synchronizes) with the Compact option, which rewrites the file and reclaims the space
    /// freed by deleted elements. Without a compact save, deletions barely change the file size.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class CompactSaveCommand : SlimmerCommandBase
    {
        public const string Name = "Compact Save";
        protected override string StepName { get { return Name; } }

        protected override StepResult Run(UIApplication app, Document doc)
        {
            return RunStep(app, doc);
        }

        public static StepResult RunStep(UIApplication app, Document doc)
        {
            var result = new StepResult { StepName = Name };
            long before = FileSize(doc);

            if (doc.IsWorkshared)
            {
                bool detached = false;
                try { detached = doc.IsDetached; } catch { }
                if (detached)
                {
                    result.Skipped = true;
                    result.Summary = "The model is detached from central. Use File > Save As with Options > Compact File to shrink it.";
                    RevitHelpers.Info("Model Slimmer - " + Name, result.Summary);
                    return result;
                }

                bool ok = RevitHelpers.YesNo(
                    "Model Slimmer - " + Name,
                    "Synchronize with central and compact the central file?",
                    "This performs a normal Synchronize with Central (relinquishing all your borrowed elements and worksets) with the " +
                    "'Compact Central Model' option switched on. Other users will receive your deletions.");
                if (!ok)
                {
                    return StepResult.CancelledStep(Name);
                }

                var syncOptions = new SynchronizeWithCentralOptions
                {
                    Compact = true,
                    Comment = "Model Slimmer clean-up",
                    SaveLocalBefore = true,
                    SaveLocalAfter = true
                };
                syncOptions.SetRelinquishOptions(new RelinquishOptions(true));
                doc.SynchronizeWithCentral(new TransactWithCentralOptions(), syncOptions);
                result.Summary = "Synchronized with central with compaction.";
            }
            else
            {
                if (string.IsNullOrEmpty(doc.PathName))
                {
                    result.Skipped = true;
                    result.Summary = "The model has never been saved. Save it first, then run Compact Save.";
                    RevitHelpers.Info("Model Slimmer - " + Name, result.Summary);
                    return result;
                }

                bool ok = RevitHelpers.YesNo(
                    "Model Slimmer - " + Name,
                    "Save and compact the file?",
                    "This saves " + doc.PathName + " with the 'Compact File' option, which rewrites the file and reclaims space from deleted elements.");
                if (!ok)
                {
                    return StepResult.CancelledStep(Name);
                }

                doc.Save(new SaveOptions { Compact = true });
                result.Summary = "Saved with compaction.";
            }

            long after = FileSize(doc);
            if (before > 0 && after > 0)
            {
                result.Summary += " Size: " + RevitHelpers.FormatBytes(before) + " -> " + RevitHelpers.FormatBytes(after) +
                                  " (" + (100.0 * (before - after) / before).ToString("0.0") + "% smaller).";
            }
            RevitHelpers.Info("Model Slimmer - " + Name, "Compact save finished", result.Summary);
            return result;
        }

        private static long FileSize(Document doc)
        {
            try
            {
                return !string.IsNullOrEmpty(doc.PathName) && File.Exists(doc.PathName) ? new FileInfo(doc.PathName).Length : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
