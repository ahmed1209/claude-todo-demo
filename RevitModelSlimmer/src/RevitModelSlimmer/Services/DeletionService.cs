using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RevitModelSlimmer.Services
{
    /// <summary>
    /// Deletes elements one by one inside a single undoable transaction. Each element is wrapped in
    /// a sub-transaction so one element that refuses to be deleted does not abort the whole batch.
    /// </summary>
    public static class DeletionService
    {
        public static DeletionResult Delete(Document doc, string transactionName, IEnumerable<DeletionCandidate> candidates)
        {
            var result = new DeletionResult();
            List<DeletionCandidate> list = candidates.ToList();
            result.Requested = list.Count;
            if (list.Count == 0)
            {
                return result;
            }

            using (var tx = new Transaction(doc, transactionName))
            {
                tx.Start();
                FailureHandlingOptions options = tx.GetFailureHandlingOptions();
                options.SetFailuresPreprocessor(new FailureSwallower());
                options.SetClearAfterRollback(true);
                tx.SetFailureHandlingOptions(options);

                foreach (DeletionCandidate candidate in list)
                {
                    if (candidate.Id == null || candidate.Id == ElementId.InvalidElementId)
                    {
                        result.Failed++;
                        result.Failures.Add(candidate.Name + ": invalid element id");
                        continue;
                    }

                    if (doc.GetElement(candidate.Id) == null)
                    {
                        // Already gone, most likely as a dependent of something deleted earlier in this batch.
                        result.Succeeded++;
                        result.Log.Add(candidate.Name + ": already removed (dependent of an earlier deletion)");
                        continue;
                    }

                    using (var sub = new SubTransaction(doc))
                    {
                        try
                        {
                            sub.Start();
                            ICollection<ElementId> removed = doc.Delete(candidate.Id);
                            sub.Commit();
                            result.Succeeded++;
                            int removedCount = removed?.Count ?? 1;
                            result.TotalElementsRemoved += removedCount;
                            result.Log.Add(candidate.Name + ": deleted (" + removedCount + " element(s) removed)");
                        }
                        catch (Exception ex)
                        {
                            if (sub.HasStarted() && !sub.HasEnded())
                            {
                                sub.RollBack();
                            }
                            result.Failed++;
                            result.Failures.Add(candidate.Name + ": " + ex.Message);
                        }
                    }
                }

                if (result.Succeeded > 0)
                {
                    TransactionStatus status = tx.Commit();
                    if (status != TransactionStatus.Committed)
                    {
                        result.Failures.Add("Transaction '" + transactionName + "' was rolled back by Revit (status " + status + "). Nothing was deleted.");
                        result.Failed += result.Succeeded;
                        result.Succeeded = 0;
                        result.TotalElementsRemoved = 0;
                    }
                }
                else
                {
                    tx.RollBack();
                }
            }

            return result;
        }
    }
}
