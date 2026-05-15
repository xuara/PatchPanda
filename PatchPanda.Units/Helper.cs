namespace PatchPanda.Units;

internal static class Helper
{
    internal static AppVersion GetTestAppVersion(string githubVersion) =>
        new AppVersion
        {
            Body = "Testing body",
            Breaking = false,
            Name = "Test update",
            VersionNumber = githubVersion,
            Prerelease = false
        };

    internal static ComposeStack GetTestStack(
        string version,
        string? githubNewVersion,
        string targetImage
    ) =>
        GetTestStack(
            version,
            githubNewVersion,
            targetImage,
            VersionHelper.BuildRegexFromVersion(version),
            githubNewVersion is null ? null : VersionHelper.BuildRegexFromVersion(githubNewVersion)
        );

    internal static ComposeStack GetTestStack(
        string version,
        string? githubNewVersion,
        string targetImage,
        string regex,
        string? githubVersionRegex
    )
    {
        var stack = new ComposeStack
        {
            Id = 1,
            StackName = "TestStack",
            ConfigFile = "docker-compose.yml",
            Apps =
            [
                new Container
                {
                    Id = 1,
                    Name = "TestApp",
                    IsSecondary = false,
                    Regex = regex,
                    GitHubVersionRegex = githubVersionRegex,
                    Version = version,
                    TargetImage = targetImage,
                    StackId = 1,
                    NewerVersions = [],
                    CurrentSha = TestData.SHA,
                    Uptime = TestData.UPTIME
                }
            ]
        };

        if (githubNewVersion is not null)
            stack.Apps[0].NewerVersions.Add(GetTestAppVersion(githubNewVersion));

        return stack;
    }

    internal static ComposeStack GetTestStack() =>
        GetTestStack(TestData.VERSION, TestData.NewVersion, TestData.IMAGE);

    internal static IDbContextFactory<DataContext> CreateInMemoryFactory()
    {
        var serviceProvider = new ServiceCollection()
            .AddDbContextFactory<DataContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString())
            )
            .BuildServiceProvider();

        return serviceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
    }
}
