using System.Text.Json;
using System.Text.RegularExpressions;

namespace PatchPanda.Web.Helpers;

internal static partial class ParsingHelper
{
    [GeneratedRegex(@"https://github.com\/([a-zA-Z0-9_.-]+)\/([a-zA-Z0-9_.-]+)")]
    private static partial Regex GitHubUrlRegex();

    [GeneratedRegex(@"ghcr.io\/([a-zA-Z0-9_.-]+)\/([a-zA-Z0-9_.-]+)")]
    private static partial Regex GhcrUrlRegex();

    [GeneratedRegex(@"([a-zA-Z0-9_.-]+)\/([a-zA-Z0-9_.-]+):")]
    private static partial Regex ImageUrlRegex();

    internal static async Task SetGitHubRepo(
        this Container container,
        ContainerListResponse response,
        IVersionService versionService,
        ILogger logger
    )
    {
        if (container.OverrideGitHubRepo is not null)
            return;

        List<string> repos = [];

        var fullResponse = JsonSerializer.Serialize(response);

        var githubMatches = GitHubUrlRegex().Matches(fullResponse);

        var ghcrMatches = GhcrUrlRegex().Matches(fullResponse);

        var imageMatches = ImageUrlRegex().Matches(response.Image);

        Dictionary<Tuple<string, string>, IReadOnlyList<Octokit.Release>> versionCounts = [];

        foreach (
            var match in githubMatches
                .Concat(ghcrMatches)
                .Concat(imageMatches)
                .Select(x => new Tuple<string, string>(
                    x.Groups[1].Value.ToLower(),
                    x.Groups[2].Value.ToLower()
                ))
                .Distinct()
        )
        {
            try
            {
                var versions = await versionService.GetVersions(match);

                versionCounts.Add(match, versions);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogInformation(ex, "Failed to get versions for combination {Match}", match);
            }
        }

        versionCounts = versionCounts
            .Where(x => x.Value.Any())
            .GroupBy(x => x.Value.ElementAt(0).Url)
            .Select(x =>
                x.OrderByDescending(y => x.Key.Contains(y.Key.Item1) && x.Key.Contains(y.Key.Item2))
                    .First()
            )
            .ToDictionary();

        if (versionCounts.Count > 0)
        {
            var bestChoice = versionCounts
                .OrderByDescending(x =>
                    x.Value.Any(y => container.Version?.IsSameVersionAs(y.TagName) == true)
                )
                .ThenByDescending(x => x.Value.Count)
                .First();

            container.GitHubRepo = bestChoice.Key;
            container.GitHubVersionRegex = bestChoice.Value.Count > 0
                ? VersionHelper.BuildRegexFromVersion(bestChoice.Value[0].TagName)
                : null;

            if (versionCounts.Count > 1)
            {
                container.SecondaryGitHubRepos = [.. versionCounts.Where(x => x.Key != bestChoice.Key).Select(x => x.Key)];
            }
        }
    }
}
