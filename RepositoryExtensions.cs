using System;
using LibGit2Sharp;

namespace DocFxForUnity;

/// <summary>
/// Extension methods for <see cref="Repository"/>.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Hard resets the specified <see cref="Repository"/> to the specified commit.
    /// </summary>
    /// <param name="repository">The <see cref="Repository"/> to hard reset to <paramref name="commit"/>.</param>
    /// <param name="commit">The name of the commit where to reset <paramref name="repository"/>.</param>
    public static void HardReset(this Repository repository, string commit)
    {
        Console.WriteLine($"Hard reset to {commit}");

        repository.Reset(ResetMode.Hard, commit);

        try
        {
            Console.WriteLine($"Removing untracked files");

            repository.RemoveUntrackedFiles();
        }
        catch (Exception) { }
    }
}