using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitModelSlimmer.Services
{
    /// <summary>Outcome of one clean-up step, used by the "Slim Model" wizard to chain steps.</summary>
    public sealed class StepResult
    {
        public string StepName { get; set; }
        public bool Cancelled { get; set; }
        public bool Skipped { get; set; }
        public int ItemsRequested { get; set; }
        public int ItemsDeleted { get; set; }
        public int ElementsRemoved { get; set; }
        public string Summary { get; set; }
        public List<string> ReportLines { get; } = new List<string>();

        public static StepResult SkippedStep(string name, string reason)
        {
            return new StepResult { StepName = name, Skipped = true, Summary = reason };
        }

        public static StepResult CancelledStep(string name)
        {
            return new StepResult { StepName = name, Cancelled = true, Summary = "Cancelled by user." };
        }
    }

    /// <summary>A single thing the user can choose to delete.</summary>
    public sealed class DeletionCandidate
    {
        public DeletionCandidate(ElementId id, string name)
        {
            Id = id;
            Name = name;
        }

        public ElementId Id { get; }
        public string Name { get; }
    }

    public sealed class DeletionResult
    {
        public int Requested { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public int TotalElementsRemoved { get; set; }
        public List<string> Failures { get; } = new List<string>();
        public List<string> Log { get; } = new List<string>();
    }
}
