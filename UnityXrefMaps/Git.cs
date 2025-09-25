using System.IO;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace UnityXrefMaps;

internal static class Git
{
    /// <summary>
    /// Fetches changes and hard resets the specified repository to the latest commit of a specified branch. If no
    /// repository is found, it will be cloned before.
    /// </summary>
    /// <param name="sourceUrl">The url of the repository.</param>
    /// <param name="path">The directory path where to find/clone the repository.</param>
    /// <param name="branch">The branch use on the repository.</param>
    /// <returns>The synced repository on the latest commit of the specified branch.</returns>
    public static Repository GetSyncRepository(string sourceUrl, string path, string branch, ILogger logger)
    {
        // Clone this repository to the specified branch if it doesn't exist
        bool clone = !Directory.Exists(path);
        if (clone)
        {
            logger.LogInformation("Cloning {SourceUrl} to {Path}", sourceUrl, path);

            var options = new CloneOptions() { BranchName = branch };
            Repository.Clone(sourceUrl, path, options);
        }

        var repository = new Repository(path);

        // Otherwise fetch changes and checkout to the specified branch
        if (!clone)
        {
            logger.LogInformation("Hard reset '{Path}' to HEAD", path);
            repository.Reset(ResetMode.Hard);
            repository.RemoveUntrackedFiles();

            logger.LogInformation("Fetching changes from 'origin' in '{Path}'", path);
            Remote remote = repository.Network.Remotes["origin"];
            LibGit2Sharp.Commands.Fetch(repository, remote.Name, [], null, null); // WTF is this API libgit2sharp?

            logger.LogInformation("Checking out '{Path}' to '{Branch}' branch", path, branch);
            string remoteBranch = $"origin/{branch}";
            LibGit2Sharp.Commands.Checkout(repository, remoteBranch);
        }

        return repository;
    }
}
