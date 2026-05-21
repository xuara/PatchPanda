namespace PatchPanda.Web.Models.Jobs;

internal record RestartStackJob(long Sequence, int StackId) : AbstractJob(Sequence);
