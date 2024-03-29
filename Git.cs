using System;
using System.IO;
using LibGit2Sharp;

namespace DocFxForUnity
{
    public sealed class Git
    {
        /// <summary>
        /// Fetches changes and hard resets the specified repository to the latest commit of a specified branch. If no
        /// repository is found, it will be cloned before.
        /// </summary>
        /// <param name="sourceUrl">The url of the repository.</param>
        /// <param name="path">The directory path where to find/clone the repository.</param>
        /// <param name="branch">The branch use on the repository.</param>
        /// <returns>The synced repository on the latest commit of the specified branch.</returns>
        public static Repository GetSyncRepository(string sourceUrl, string path, string branch = "main")
        {
            // Clone this repository to the specified branch if it doesn't exist
            bool clone = !Directory.Exists(path);
            if (clone)
            {
                Console.WriteLine($"Cloning {sourceUrl} to {path}");

                var options = new CloneOptions() { BranchName = branch };
                Repository.Clone(sourceUrl, path, options);
            }

            var repository = new Repository(path);

            // Otherwise fetch changes and checkout to the specified branch
            if (!clone)
            {
                Console.WriteLine($"Hard reset '{path}' to HEAD");
                repository.Reset(ResetMode.Hard);
                repository.RemoveUntrackedFiles();

                Console.WriteLine($"Fetching changes from 'origin' in '{path}'");
                var remote = repository.Network.Remotes["origin"];
                Commands.Fetch(repository, remote.Name, Array.Empty<string>(), null, null); // WTF is this API libgit2sharp?

                Console.WriteLine($"Checking out '{path}' to '{branch}' branch");
                var remoteBranch = $"origin/{branch}";
                Commands.Checkout(repository, remoteBranch);
            }

            Console.WriteLine();

            return repository;
        }
    }
}