using System.Reflection;
using System.Runtime.Serialization;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;

namespace PatchPanda.Units.Helpers;

public class ParsingHelperTests
{
    private readonly Mock<ILogger<VersionService>> _logger;
    private readonly Mock<IConfiguration> _configuration;
    private readonly Mock<IAiService> _aiService;

    public ParsingHelperTests()
    {
        _logger = new Mock<ILogger<VersionService>>();
        _configuration = new Mock<IConfiguration>();
        _aiService = new Mock<IAiService>();
    }

    [Fact]
    public async Task SetGitHubRepo_DoesNothing_WhenOverrideIsSet()
    {
        var stack = Helper.GetTestStack();
        var container = stack.Apps[0];
        container.OverrideGitHubRepo = new Tuple<string, string>(
            TestData.GithubOwner,
            TestData.GithubRepo
        );

        var response = new ContainerListResponse { Image = TestData.IMAGE };

        var versionService = new VersionService(
            _logger.Object,
            _configuration.Object,
            Helper.CreateInMemoryFactory(),
            _aiService.Object
        );

        var logger = new Mock<ILogger>().Object;

        await container.SetGitHubRepo(response, versionService, logger);

        Assert.Equal(
            new Tuple<string, string>(TestData.GithubOwner, TestData.GithubRepo),
            container.OverrideGitHubRepo
        );
        Assert.Null(container.GitHubRepo);
    }

    [Fact]
    public async Task SetGitHubRepo_DoesNotSet_WhenNoMatchesFound()
    {
        var stack = Helper.GetTestStack(TestData.VERSION, null, TestData.IMAGE);
        var container = stack.Apps[0];

        var response = new ContainerListResponse { Image = TestData.AlpineImage };

        var versionService = new VersionService(
            _logger.Object,
            _configuration.Object,
            Helper.CreateInMemoryFactory(),
            _aiService.Object
        );

        var logger = new Mock<ILogger>().Object;

        await container.SetGitHubRepo(response, versionService, logger);

        Assert.Null(container.GitHubRepo);
        Assert.Null(container.GitHubVersionRegex);
    }

    [Fact]
    public async Task SetGitHubRepo_SetsRepoAndRegex_WhenGithubUrlFound()
    {
        var stack = Helper.GetTestStack(TestData.VERSION, null, TestData.IMAGE);
        var container = stack.Apps[0];

        var response = new ContainerListResponse { Image = TestData.GithubUrl };

        var release = CreateRelease(TestData.ReleaseTag, TestData.GithubUrl);

        var versionServiceMock = new Mock<IVersionService>();

        versionServiceMock
            .Setup(vs =>
                vs.GetVersions(
                    It.Is<Tuple<string, string>>(t =>
                        t.Item1 == TestData.GithubOwner && t.Item2 == TestData.GithubRepo
                    )
                )
            )
            .ReturnsAsync([release]);

        var logger = new Mock<ILogger>().Object;

        await container.SetGitHubRepo(response, versionServiceMock.Object, logger);

        Assert.Equal(
            new Tuple<string, string>(TestData.GithubOwner, TestData.GithubRepo),
            container.GitHubRepo
        );
        Assert.Equal(
            VersionHelper.BuildRegexFromVersion(TestData.ReleaseTag),
            container.GitHubVersionRegex
        );
    }

    [Fact]
    public async Task SetGitHubRepo_PicksBestRepoAndAddsSecondary_WhenMultipleRepos()
    {
        var stack = Helper.GetTestStack(TestData.VERSION, null, TestData.IMAGE);
        var container = stack.Apps[0];

        var response = new ContainerListResponse { Image = TestData.MultiImage };

        var relA1 = CreateRelease(TestData.ReleaseTagA, TestData.UrlA);
        var relB1 = CreateRelease(TestData.ReleaseTagB, TestData.UrlB);

        var versionServiceMock = new Mock<IVersionService>();

        versionServiceMock
            .Setup(vs =>
                vs.GetVersions(
                    It.Is<Tuple<string, string>>(t =>
                        t.Item1 == TestData.OwnerA && t.Item2 == TestData.RepoA
                    )
                )
            )
            .ReturnsAsync([relA1]);

        versionServiceMock
            .Setup(vs =>
                vs.GetVersions(
                    It.Is<Tuple<string, string>>(t =>
                        t.Item1 == TestData.OwnerB && t.Item2 == TestData.RepoB
                    )
                )
            )
            .ReturnsAsync([relB1]);

        var logger = new Mock<ILogger>().Object;

        await container.SetGitHubRepo(response, versionServiceMock.Object, logger);

        Assert.Equal(
            new Tuple<string, string>(TestData.OwnerA, TestData.RepoA),
            container.GitHubRepo
        );
        Assert.NotNull(container.SecondaryGitHubRepos);
        Assert.Contains(
            new Tuple<string, string>(TestData.OwnerB, TestData.RepoB),
            container.SecondaryGitHubRepos
        );
        Assert.Equal(
            VersionHelper.BuildRegexFromVersion(TestData.ReleaseTagA),
            container.GitHubVersionRegex
        );
    }

    [Fact]
    public async Task SetGitHubRepo_CollapsesDifferentCandidates_WhenReleaseUrlsMatch()
    {
        var stack = Helper.GetTestStack(TestData.VERSION, null, TestData.IMAGE);
        var container = stack.Apps[0];

        var response = new ContainerListResponse
        {
            Image =
                $"ghcr.io/{TestData.GithubOwner}/{TestData.GithubRepo}:1.0,https://github.com/{TestData.OwnerB}/{TestData.RepoB}",
        };

        var sharedRelease = CreateRelease(TestData.ReleaseTag, TestData.GithubUrl);

        var versionServiceMock = new Mock<IVersionService>();

        versionServiceMock
            .Setup(vs =>
                vs.GetVersions(
                    It.Is<Tuple<string, string>>(t =>
                        t.Item1 == TestData.GithubOwner && t.Item2 == TestData.GithubRepo
                    )
                )
            )
            .ReturnsAsync([sharedRelease]);

        versionServiceMock
            .Setup(vs =>
                vs.GetVersions(
                    It.Is<Tuple<string, string>>(t =>
                        t.Item1 == TestData.OwnerB && t.Item2 == TestData.RepoB
                    )
                )
            )
            .ReturnsAsync([sharedRelease]);

        var logger = new Mock<ILogger>().Object;

        await container.SetGitHubRepo(response, versionServiceMock.Object, logger);

        Assert.Equal(
            new Tuple<string, string>(TestData.GithubOwner, TestData.GithubRepo),
            container.GitHubRepo
        );
        Assert.Null(container.SecondaryGitHubRepos);
        Assert.Equal(
            VersionHelper.BuildRegexFromVersion(TestData.ReleaseTag),
            container.GitHubVersionRegex
        );
    }

    [Fact]
    public async Task SetGitHubRepo_SetsRepo_WhenRepositoryContainsDot()
    {
        var image = $"ghcr.io/{TestData.DotOwner}/{TestData.DotRepo}:{TestData.VERSION}";
        var repoUrl = $"https://github.com/{TestData.DotOwner}/{TestData.DotRepo}";

        var stack = Helper.GetTestStack(TestData.VERSION, null, image);
        var container = stack.Apps[0];

        var response = new ContainerListResponse { Image = image };

        var release = CreateRelease(TestData.NewVersion, repoUrl);

        var versionServiceMock = new Mock<IVersionService>();

        versionServiceMock
            .Setup(vs =>
                vs.GetVersions(
                    It.Is<Tuple<string, string>>(t =>
                        t.Item1 == TestData.DotOwner && t.Item2 == TestData.DotRepo
                    )
                )
            )
            .ReturnsAsync([release]);

        var logger = new Mock<ILogger>().Object;

        await container.SetGitHubRepo(response, versionServiceMock.Object, logger);

        Assert.Equal(
            new Tuple<string, string>(TestData.DotOwner, TestData.DotRepo),
            container.GitHubRepo
        );
        Assert.Equal(
            VersionHelper.BuildRegexFromVersion(TestData.NewVersion),
            container.GitHubVersionRegex
        );
    }

    private static Release CreateRelease(string tagName, string url)
    {
        var relType = typeof(Release);
#pragma warning disable SYSLIB0050 // Type or member is obsolete
        var rel = (Release)FormatterServices.GetUninitializedObject(relType)!;
#pragma warning restore SYSLIB0050 // Type or member is obsolete

        var tagField = relType.GetField(
            "<TagName>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (tagField is not null)
            tagField.SetValue(rel, tagName);
        else
        {
            var tagProp = relType.GetProperty(
                "TagName",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            tagProp?.SetValue(rel, tagName);
        }

        var urlField = relType.GetField(
            "<Url>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (urlField is not null)
            urlField.SetValue(rel, url);
        else
        {
            var urlProp = relType.GetProperty(
                "Url",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            urlProp?.SetValue(rel, url);
        }

        return rel;
    }
}
