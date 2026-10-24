namespace PatchPanda.Web.Models;

internal class ContainerModel
{
    public string? OverrideGitHubRepoOwner { get; set; }
    public string? OverrideGitHubRepoName { get; set; }
    public bool IgnoreContainer { get; set; }
}
