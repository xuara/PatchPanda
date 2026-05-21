using System.Text.RegularExpressions;
using System.Globalization;

namespace PatchPanda.Web.Helpers;

internal static partial class VersionHelper
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitsRegex();

    [GeneratedRegex(@"\d+(?:\.\d+)+")]
    private static partial Regex ComparableVersionRegex();

    public static string BuildRegexFromVersion(string version)
    {
        string regex = "^";

        if (version.Contains('@'))
        {
            var atSplit = version.Split('@');
            regex += Regex.Escape(atSplit[0]) + "@";
        }
        else if (version.StartsWith('v'))
            regex += "v";

        string cleanedVersion = version.TrimStart('v');
        var periodSplit = cleanedVersion.Split('.');

        List<string> digitRegexes = [];
        for (int i = 0; i < periodSplit.Length; i++)
        {
            // If it's the last digit group (patch version), let it scale up dynamically via wildcard
            if (i == periodSplit.Length - 1)
            {
                digitRegexes.Add(@"\d+");
            }
            else
            {
                // Pin the explicit major/minor structural digits (e.g., "1" and "10") so it ignores older branches
                digitRegexes.Add(Regex.Escape(periodSplit[i]));
            }
        }

        regex += string.Join(@"\.", digitRegexes);

        regex += string.Join(@"\.", digitRegexes);

        regex += string.Join(@"\.", digitRegexes);

        var dashSplit = periodSplit[^1].Split('-');

        foreach (var dash in dashSplit[1..])
        {
            if (dash.StartsWith('r') && dash.Length > 1)
            {
                regex += @"-r\d+";
                continue;
            }
            if (dash.StartsWith("ls", StringComparison.Ordinal) && dash.Length > 2)
            {
                regex += @"-ls\d+";
                continue;
            }
            else
            {
                regex += "-" + dash;
            }
        }

        regex += "$";

        return regex;
    }

    public static string? ExtractVersionSegment(string input, params string?[] regexes)
    {
        foreach (var regex in regexes.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
        {
            var match = Regex.Match(input, regex!.TrimStart('^').TrimEnd('$'));

            if (match.Success)
                return match.Value;
        }

        return null;
    }

    public static string? ReplaceVersionSegment(
        string currentVersion,
        string targetVersion,
        string currentVersionRegex,
        string githubVersionRegex
    )
    {
        var currentSegment = ExtractVersionSegment(
            currentVersion,
            currentVersionRegex,
            githubVersionRegex
        );

        if (currentSegment is null)
            return null;

        var targetSegment = ExtractVersionSegment(targetVersion, currentVersionRegex);

        if (targetSegment is not null)
            return currentVersion.Replace(currentSegment, targetSegment);

        var targetComparableVersion = ComparableVersionRegex().Match(targetVersion).Value;

        if (string.IsNullOrWhiteSpace(targetComparableVersion))
            return null;

        var currentComparableVersionMatch = ComparableVersionRegex().Match(currentSegment);

        if (!currentComparableVersionMatch.Success)
            return currentVersion.Replace(currentSegment, targetComparableVersion);

        var updatedSegment =
            currentSegment[..currentComparableVersionMatch.Index]
            + targetComparableVersion
            + currentSegment[(currentComparableVersionMatch.Index + currentComparableVersionMatch.Length)..];

        return currentVersion.Replace(currentSegment, updatedSegment);
    }

    public static bool IsSameVersionAs(this string version1, string version2)
    {
        string cleanedVersion1 = version1.TrimStart('v');
        string cleanedVersion2 = version2.TrimStart('v');

        var numbers1 = DigitsRegex().Matches(cleanedVersion1);
        var numbers2 = DigitsRegex().Matches(cleanedVersion2);

        if (numbers1.Count != numbers2.Count)
            return false;

        for (int i = 0; i < numbers1.Count; i++)
        {
            int num1 = int.Parse(numbers1[i].Value, CultureInfo.InvariantCulture);
            int num2 = int.Parse(numbers2[i].Value, CultureInfo.InvariantCulture);
            if (num1 != num2)
                return false;
        }

        return true;
    }

    public static bool IsNewerThan(this string? version1, string? version2)
    {
        if (string.IsNullOrWhiteSpace(version1) && string.IsNullOrWhiteSpace(version2))
            return false;

        if (string.IsNullOrWhiteSpace(version1))
            return false;

        if (string.IsNullOrWhiteSpace(version2))
            return true;

        string cleanedVersion1 = version1.TrimStart('v');
        string cleanedVersion2 = version2.TrimStart('v');

        if (cleanedVersion1.Contains('@'))
            cleanedVersion1 = cleanedVersion1.Split('@')[1];

        if (cleanedVersion2.Contains('@'))
            cleanedVersion2 = cleanedVersion2.Split('@')[1];

        var numbers1 = DigitsRegex().Matches(cleanedVersion1);
        var numbers2 = DigitsRegex().Matches(cleanedVersion2);

        int count = Math.Min(numbers1.Count, numbers2.Count);

        for (int i = 0; i < count; i++)
        {
            int num1 = int.Parse(numbers1[i].Value, CultureInfo.InvariantCulture);
            int num2 = int.Parse(numbers2[i].Value, CultureInfo.InvariantCulture);

            if (num1 > num2) return true;
            if (num1 < num2) return false;
        }

        return numbers1.Count > numbers2.Count;
    }

    public static int NewerComparison(string? version1, string? version2)
    {
        if (string.IsNullOrWhiteSpace(version1) && string.IsNullOrWhiteSpace(version2))
            return 0;
        if (string.IsNullOrWhiteSpace(version1))
            return 1;
        if (string.IsNullOrWhiteSpace(version2))
            return -1;

        if (version1.IsNewerThan(version2))
            return -1;
        else if (version2.IsNewerThan(version1))
            return 1;
        else
            return 0;
    }
}