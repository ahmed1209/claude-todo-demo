using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitModelSlimmer.Services;

namespace RevitModelSlimmer.Commands
{
    /// <summary>Common guard rails for every ribbon command.</summary>
    public abstract class SlimmerCommandBase : IExternalCommand
    {
        protected abstract string StepName { get; }
        protected virtual bool RequiresProjectDocument { get { return true; } }

        protected abstract StepResult Run(UIApplication app, Document doc);

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication app = commandData.Application;
            UIDocument uidoc = app.ActiveUIDocument;
            if (uidoc == null)
            {
                message = "Open a project before running Model Slimmer.";
                return Result.Cancelled;
            }

            Document doc = uidoc.Document;
            if (RequiresProjectDocument && doc.IsFamilyDocument)
            {
                RevitHelpers.Info("Model Slimmer", "This tool works on project files, not on family documents.");
                return Result.Cancelled;
            }

            if (doc.IsReadOnly)
            {
                RevitHelpers.Info("Model Slimmer", "The document is read-only. Nothing can be deleted.");
                return Result.Cancelled;
            }

            try
            {
                StepResult result = Run(app, doc);
                return result != null && result.Cancelled ? Result.Cancelled : Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                RevitHelpers.Info("Model Slimmer - " + StepName, "The command failed.", ex.GetType().Name + ": " + ex.Message + "\n\n" + ex.StackTrace);
                return Result.Failed;
            }
        }
    }
}
