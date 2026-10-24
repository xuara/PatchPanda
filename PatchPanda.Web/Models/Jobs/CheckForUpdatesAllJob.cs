namespace PatchPanda.Web.Models.Jobs;

internal record CheckForUpdatesAllJob(long Sequence) : AbstractJob(Sequence);
