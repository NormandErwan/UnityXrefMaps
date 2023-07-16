using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace DocFxForUnity;

/// <summary>
/// Extension methods for <see cref="Repository"/>.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Returns a collection of the latest tags of a specified <see cref="Repository"/>.
    /// </summary>
    /// <param name="repository">The <see cref="Repository"/> to use.</param>
    /// <returns>The collection of tags.</returns>
    public static IEnumerable<string> GetTags(this Repository repository)
    {
        return repository.Tags
            .OrderByDescending(tag => (tag.Target as Commit).Author.When)
            .Select(tag => tag.FriendlyName);
    }

    /// <summary>
    /// Hard resets the specified <see cref="Repository"/> to the specified commit.
    /// </summary>
    /// <param name="repository">The <see cref="Repository"/> to hard reset to <paramref name="commit"/>.</param>
    /// <param name="commit">The name of the commit where to reset <paramref name="repository"/>.</param>
    public static void HardReset(this Repository repository, string commit)
    {
        repository.Reset(ResetMode.Hard, commit);

        try
        {
            repository.RemoveUntrackedFiles();
        }
        catch (System.Exception) { }
    }
}