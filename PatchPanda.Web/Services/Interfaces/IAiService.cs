namespace PatchPanda.Web.Services.Interfaces;

internal interface IAiService
{
    Task<SummaryResult?> SummarizeReleaseNotes(string releaseNotes);

    Task<SecurityAnalysisResult?> AnalyzeDiff(string diff);

    bool IsInitialized();
}

internal interface IAiResult { }

internal class SummaryResult : IAiResult
{
    public required string Summary { get; set; }

    public required bool Breaking { get; set; }
}

internal class SecurityAnalysisResult : IAiResult
{
    public required string Analysis { get; set; }

    public required bool IsSuspectedMalicious { get; set; }
}
