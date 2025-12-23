using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using UnityXrefMaps.DocFX;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace UnityXrefMaps.Commands;

internal sealed partial class BuildCommand : RootCommand
{
    public BuildCommand(ILoggerFactory loggerFactory)
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
            Description = "The repository tags to use to generate the xrefmap file. " +
                "That is, the versions of the Unity editor (6000.0.1f1, 6000.1.1f1) or the versions of the Unity package (1.0.0, 1.1.0). " +
                "If not set, it will resolve all tags in the repository and generate a xrefmap file for each x.y (examples: 6000.0, 6000.1) based on the most recent patch."
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
        Option<string> packageRegexOption = new("--packageRegex")
        {
            Description = "The regular expression to check if it is a package.",
            DefaultValueFactory = _ => Constants.DefaultPackageRegex
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
        Options.Add(packageRegexOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            bool result = true;

            XrefMapService xrefMapService = new(loggerFactory.CreateLogger<XrefMapService>());

            string? repositoryUrl = parseResult.GetValue(repositoryUrlOption);
            string? repositoryBranch = parseResult.GetValue(repositoryBranchOption);
            string? repositoryPath = parseResult.GetValue(repositoryPathOption);
            string[]? repositoryTags = parseResult.GetValue(repositoryTagsOption);
            string? apiUrl = parseResult.GetValue(apiUrlOption);
            string? docFxFilePath = parseResult.GetValue(docFxConfigurationFilePathOption);
            string? docFxAdditionalArguments = parseResult.GetValue(docFxAdditionalArgumentsOption);
            string? xrefMapsPath = parseResult.GetValue(xrefMapsPathOption);
            string[]? trimNamespaces = parseResult.GetValue(trimNamespacesOption);
            string? packageRegex = parseResult.GetValue(packageRegexOption);

            bool isPackage = Regex.IsMatch(apiUrl!, packageRegex!);

            using Stream docFxStream = File.OpenRead(docFxFilePath!);

            DocFxConfiguration? docFxConfiguration = await JsonSerializer.DeserializeAsync<DocFxConfiguration>(docFxStream, cancellationToken: cancellationToken);

            string docFxFileDirectoryPath = Path.GetDirectoryName(docFxFilePath)!;

            string generatedDocsPath = Path.Combine(docFxFileDirectoryPath, docFxConfiguration!.Build!.Destination!);
            string generatedXrefMapPath = Path.Combine(generatedDocsPath, Constants.DefaultXrefMapFileName!);

            ILogger logger = loggerFactory.CreateLogger<BuildCommand>();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Sync the Unity repository in '{RepositoryPath}'", Path.GetFullPath(repositoryPath!));
            }

            using Repository repository = Git.GetSyncRepository(repositoryUrl!, repositoryPath!, repositoryBranch!, logger);

            string docFxArguments = docFxFilePath!;

            if (!string.IsNullOrEmpty(docFxAdditionalArguments))
            {
                docFxArguments += ' ' + docFxAdditionalArguments;
            }

            if (repositoryTags == null || repositoryTags.Length == 0)
            {
                repositoryTags = [.. repository.GetLatestVersions().Select(v => v.release)];
            }

            foreach (string repositoryTag in repositoryTags)
            {
                Match versionMatch = VersionRegex().Match(repositoryTag);

                string shortVersion = $"{versionMatch.Groups["majorVersion"].Value}.{versionMatch.Groups["minorVersion"].Value}";

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Generating Unity '{ShortVersion}' xref map", shortVersion);
                }

                repository.HardReset(repositoryTag, logger);

                string xrefMapPath = string.Format(xrefMapsPath!, repositoryTag); // ./<version>/xrefmap.yml

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Running DocFX on '{RepositoryTag}'", repositoryTag);
                }

                await Utils.RunCommand(
                    "docfx", docFxArguments,
                    value =>
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (logger.IsEnabled(LogLevel.Information))
                            {
                                logger.LogInformation("{Message}", value);
                            }
                        }
                    },
                    value =>
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (logger.IsEnabled(LogLevel.Error))
                            {
                                logger.LogError("{Message}", value);
                            }
                        }
                    },
                    cancellationToken);

                if (!File.Exists(generatedXrefMapPath))
                {
                    result = false;

                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError("Error: '{XrefMapFilePath}' for Unity '{RepositoryTag}' not generated", generatedXrefMapPath, repositoryTag);
                    }

                    continue;
                }

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Fixing hrefs in '{XrefMapFilePath}'", Path.GetFullPath(xrefMapPath));
                }

                await Utils.CopyFile(generatedXrefMapPath, xrefMapPath, cancellationToken);

                XrefMap xrefMap = await xrefMapService.Load(xrefMapPath, cancellationToken);

                xrefMap.References = [.. xrefMapService.Process(string.Format(apiUrl!, shortVersion), xrefMap.References!, trimNamespaces!, isPackage)];

                await xrefMapService.Save(xrefMapPath, xrefMap, cancellationToken);
            }

            return result ? 0 : 1;
        });
    }

    [GeneratedRegex(@"(?<majorVersion>\d+)\.(?<minorVersion>\d+)\.(?<patchVersion>\d+)")]
    private static partial Regex VersionRegex();
}
