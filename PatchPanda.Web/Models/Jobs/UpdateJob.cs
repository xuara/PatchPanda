namespace PatchPanda.Web.Models.Jobs;

internal record UpdateJob(
    long Sequence,
    int ContainerId,
    int TargetVersionId,
    string TargetVersionNumber,
    bool IsAutomatic = false
) : AbstractJob(Sequence);
