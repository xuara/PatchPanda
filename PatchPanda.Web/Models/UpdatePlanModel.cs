namespace PatchPanda.Web.Models;

internal class UpdatePlanModel
{
    public List<string>? Steps { get; init; }

    public string? FailReason { get; init; }

    public UpdatePlanModel(List<string> steps)
    {
        Steps = steps;
    }

    public UpdatePlanModel(string failReason)
    {
        FailReason = failReason;
    }
}
