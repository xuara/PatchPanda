namespace PatchPanda.Web.Models.Jobs;

internal record ResetAllJob(long Sequence) : AbstractJob(Sequence);
