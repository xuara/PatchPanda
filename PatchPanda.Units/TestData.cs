namespace PatchPanda.Units;

internal static class TestData
{
    internal const string IMAGE = "example/image:v1.0.0";
    internal const string ImageNewVersion = "example/image:v1.1.0";

    internal const string VERSION = "v1.0.0";
    internal const string NewVersion = "v1.1.0";

    internal const string REGEX = "^v\\d+\\.\\d+\\.\\d+$";
    internal const string SHA = "sha256:blabla";

    internal const string UPTIME = "Up 1 hour";

    internal const string GithubOwner = "some";
    internal const string GithubRepo = "repo";
    internal const string GithubUrl = "https://github.com/some/repo";
    internal const string ReleaseTag = "v1.2.3";

    internal const string OwnerA = "ownera";
    internal const string RepoA = "repoa";
    internal const string UrlA = "https://github.com/ownera/repoa";
    internal const string OwnerB = "ownerb";
    internal const string RepoB = "repob";
    internal const string UrlB = "https://github.com/ownerb/repob";
    internal const string MultiImage = "ghcr.io/ownera/repoa:1.0.0,https://github.com/ownerb/repob";
    internal const string ReleaseTagA = "v1.0.0";
    internal const string ReleaseTagB = "v2.0.0";
    internal const string AlpineImage = "alpine:3.16";

    internal const string DotOwner = "dgtlmoon";
    internal const string DotRepo = "changedetection.io";
}
