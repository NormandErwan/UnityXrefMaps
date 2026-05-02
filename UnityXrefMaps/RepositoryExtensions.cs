using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace UnityXrefMaps;

/// <summary>
/// Extension methods for <see cref="Repository"/>.
/// </summary>
internal static partial class RepositoryExtensions
{
    /// <summary>
    /// Returns a collection of the latest tags of a specified <see cref="Repository"/>.
    /// </summary>
    /// <param name="repository">The <see cref="Repository"/> to use.</param>
    /// <returns>The collection of tags.</returns>
    public static IEnumerable<string> GetTags(this Repository repository)
    {
        return repository.Tags
            .OrderByDescending(tag => GetTaggedCommit(tag)?.Author.When ?? DateTimeOffset.MinValue)
            .Select(tag => tag.FriendlyName);
    }

    // Lightweight tags point directly to a Commit; annotated tags point to a TagAnnotation
    // whose own Target is the Commit (possibly through multiple layers of annotation).
    private static Commit? GetTaggedCommit(Tag tag)
    {
        GitObject target = tag.Target;
        while (target is TagAnnotation annotation)
            target = annotation.Target;
        return target as Commit;
    }

    /// <summary>
    /// Hard resets the specified <see cref="Repository"/> to the specified commit.
    /// </summary>
    /// <param name="repository">The <see cref="Repository"/> to hard reset to <paramref name="commit"/>.</param>
    /// <param name="commit">The name of the commit where to reset <paramref name="repository"/>.</param>
    public static void HardReset(this Repository repository, string commit, ILogger logger)
    {
        logger.LogInformation("Hard reset to {Commit}", commit);

        repository.Reset(ResetMode.Hard, commit);

        try
        {
            logger.LogInformation($"Removing untracked files");

            repository.RemoveUntrackedFiles();
        }
        catch (Exception) { }
    }

    public static IEnumerable<(string name, string release)> GetLatestVersions(this Repository unityRepository)
    {
        return unityRepository
            .GetTags()
            .Select(release => (name: UnityVersionRegex().Match(release).Value, release))
            .GroupBy(version => version.name)
            .Select(version => version.First())
            .ToArray();
    }

    [GeneratedRegex("\\d{4}\\.\\d")]
    private static partial Regex UnityVersionRegex();
}
