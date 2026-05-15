namespace PatchPanda.Web.Entities;

internal class ComposeStack : AbstractEntity
{
    internal required string StackName { get; set; }

    internal required string? ConfigFile { get; set; }

    internal bool PortainerManaged { get; set; }

    internal virtual List<Container> Apps { get; } = [];
    internal virtual List<UpdateAttempt> UpdateAttempts { get; } = [];
}
