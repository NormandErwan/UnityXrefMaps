using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using UnityXrefMaps.DocFX;

namespace UnityXrefMaps.Commands;

internal sealed partial class BuildCommand : RootCommand
{
    public BuildCommand(ILogger<BuildCommand> logger)
    {
        Option<string> repositoryUrlOption = new("--repositoryUrl")
        {
            Description = "The Git repository url.",
            DefaultValueFactory = _ => Constants.DefaultUnityRepositoryUrl
        };
        Option<string> repositoryBranchOption = new("--repositoryBranch")
        {
            Description = "The Git repository branch.",
            DefaultValueFactory = _ => Constants.DefaultUnityRepositoryBranch
        };
        Option<string> repositoryPathOption = new("--repositoryPath")
        {
            Description = "The Git repository path to git clone. " +
                "If the clone has already been made, it will reset so that the clone can be reused.",
            DefaultValueFactory = _ => Constants.DefaultUnityRepositoryPath
        };
        Option<string[]> repositoryTagsOption = new("--repositoryTags")
        {
            Description = "The repository tags to use to generate the documentation. " +
                "That is, the versions of the Unity editor (6000.0.1f1, 6000.1.1f1) or the versions of the Unity package (1.0.0, 1.1.0).",
            Required = true
        };
        Option<string> apiUrlOption = new("--apiUrl")
        {
            Description = "The root path of the Unity editor API documentation or the Unity package. " +
                "{0} is replaced with the Unity editor short version (example: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/) or " +
                "the Unity package short version (example: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/)}) in the case of a package.",
            DefaultValueFactory = _ => Constants.DefaultUnityApiUrl
        };
        Option<string> docFxConfigurationFilePathOption = new("--docFxConfigurationFilePath")
        {
            Description = "The path to the DocFX configuration file.",
            DefaultValueFactory = _ => Constants.DefaultDocFxConfigurationFilePath
        };
        Option<string> docFxAdditionalArgumentsOption = new("--docFxAdditionalArguments")
        {
            Description = "Additional arguments for DocFX."
        };
        Option<string> xrefMapsPathOption = new("--xrefMapsPath")
        {
            Description = $"The path where the final {Constants.DefaultXrefMapFileName} files will be generated. " +
                $"{{0}} is replaced with the Unity editor version (example: {string.Format(Constants.DefaultXrefMapsPath, "6000.0.1f1")}) or " +
                $"the Unity package version (example: {string.Format(Constants.DefaultXrefMapsPath, "1.0.0")}) in the case of a package.",
            DefaultValueFactory = _ => Constants.DefaultXrefMapsPath
        };
        Option<string[]> trimNamespacesOption = new("--trimNamespaces")
        {
            Description = "Namespaces for trimming."
        };

        Options.Add(repositoryUrlOption);
        Options.Add(repositoryBranchOption);
        Options.Add(repositoryPathOption);
        Options.Add(repositoryTagsOption);
        Options.Add(apiUrlOption);
        Options.Add(docFxConfigurationFilePathOption);
        Options.Add(docFxAdditionalArgumentsOption);
        Options.Add(xrefMapsPathOption);
        Options.Add(trimNamespacesOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            bool result = true;

            string? repositoryUrl = parseResult.GetValue(repositoryUrlOption);
            string? repositoryBranch = parseResult.GetValue(repositoryBranchOption);
            string? repositoryPath = parseResult.GetValue(repositoryPathOption);
            string[]? repositoryTags = parseResult.GetValue(repositoryTagsOption);
            string? apiUrl = parseResult.GetValue(apiUrlOption);
            string? docFxFilePath = parseResult.GetValue(docFxConfigurationFilePathOption);
            string? docFxAdditionalArguments = parseResult.GetValue(docFxAdditionalArgumentsOption);
            string? xrefMapsPath = parseResult.GetValue(xrefMapsPathOption);
            string[]? trimNamespaces = parseResult.GetValue(trimNamespacesOption);

            using Stream docFxStream = File.OpenRead(docFxFilePath!);

            DocFxConfiguration? docFxConfiguration = await JsonSerializer.DeserializeAsync<DocFxConfiguration>(docFxStream, cancellationToken: cancellationToken);

            string docFxFileDirectoryPath = Path.GetDirectoryName(docFxFilePath)!;

            string generatedDocsPath = Path.Combine(docFxFileDirectoryPath, docFxConfiguration!.Build!.Destination!);
            string generatedXrefMapPath = Path.Combine(generatedDocsPath, Constants.DefaultXrefMapFileName!);

            logger.LogInformation("Sync the Unity repository in '{RepositoryPath}'", Path.GetFullPath(repositoryPath!));

            using Repository repository = Git.GetSyncRepository(repositoryUrl!, repositoryPath!, repositoryBranch!, logger);

            string docFxArguments = docFxFilePath!;

            if (!string.IsNullOrEmpty(docFxAdditionalArguments))
            {
                docFxArguments += ' ' + docFxAdditionalArguments;
            }

            foreach (string repositoryTag in repositoryTags!)
            {
                Match versionMatch = VersionRegex().Match(repositoryTag);

                string shortVersion = $"{versionMatch.Groups["majorVersion"].Value}.{versionMatch.Groups["minorVersion"].Value}";

                logger.LogInformation("Generating Unity '{ShortVersion}' xref map", shortVersion);

                repository.HardReset(repositoryTag, logger);

                string xrefMapPath = string.Format(xrefMapsPath!, repositoryTag); // ./<version>/xrefmap.yml

                logger.LogInformation("Running DocFX on '{RepositoryTag}'", repositoryTag);

                await Utils.RunCommand(
                    "docfx", docFxArguments,
                    value =>
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            logger.LogInformation("{Message}", value);
                        }
                    },
                    value =>
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            logger.LogError("{Message}", value);
                        }
                    },
                    cancellationToken);

                if (!File.Exists(generatedXrefMapPath))
                {
                    result = false;

                    logger.LogError("Error: '{XrefMapFilePath}' for Unity '{RepositoryTag}' not generated", generatedXrefMapPath, repositoryTag);

                    continue;
                }

                logger.LogInformation("Fixing hrefs in '{XrefMapFilePath}'", Path.GetFullPath(xrefMapPath));

                await Utils.CopyFile(generatedXrefMapPath, xrefMapPath, cancellationToken);

                XrefMap xrefMap = await XrefMap.Load(xrefMapPath, cancellationToken);

                xrefMap.FixHrefs(string.Format(apiUrl!, shortVersion), trimNamespaces!);

                await xrefMap.Save(xrefMapPath, cancellationToken);
            }

            return result ? 0 : 1;
        });
    }

    [GeneratedRegex(@"(?<majorVersion>\d+)\.(?<minorVersion>\d+)\.(?<patchVersion>\d+)")]
    private static partial Regex VersionRegex();
}
