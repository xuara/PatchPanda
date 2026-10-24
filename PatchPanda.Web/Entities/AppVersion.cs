namespace PatchPanda.Web.Entities;

internal class AppVersion : AbstractEntity
{
    internal required string VersionNumber { get; set; }

    internal required bool Prerelease { get; set; }

    internal required bool Breaking { get; set; }

    internal required string Name { get; set; }

    internal required string Body { get; set; }

    internal string? AISummary { get; set; }

    internal bool? AIBreaking { get; set; }

    internal bool Notified { get; set; }

    internal bool Ignored { get; set; }

    internal DateTime DateDiscovered { get; set; } = DateTime.Now;

    internal virtual List<Container> Applications { get; } = [];

    internal string? SecurityAnalysis { get; set; }

    internal bool? IsSuspectedMalicious { get; set; }
}
