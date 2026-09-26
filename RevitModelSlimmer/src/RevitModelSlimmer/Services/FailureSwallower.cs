using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitModelSlimmer.Services
{
    /// <summary>
    /// Suppresses warning dialogs during bulk deletion and auto-resolves failures that offer a resolution.
    /// Errors without a resolution roll the transaction back so the model is never left half-cleaned.
    /// </summary>
    public sealed class FailureSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failures = failuresAccessor.GetFailureMessages();
            bool unresolved = false;

            foreach (FailureMessageAccessor failure in failures)
            {
                FailureSeverity severity = failure.GetSeverity();
                if (severity == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
                else if (failure.HasResolutions())
                {
                    failuresAccessor.ResolveFailure(failure);
                }
                else
                {
                    unresolved = true;
                }
            }

            if (unresolved)
            {
                return FailureProcessingResult.ProceedWithRollBack;
            }

            return failures.Count > 0 ? FailureProcessingResult.ProceedWithCommit : FailureProcessingResult.Continue;
        }
    }
}
