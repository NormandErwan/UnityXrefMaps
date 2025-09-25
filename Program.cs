using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace DocFxForUnity
{
    /// <summary>
    /// Generates the xref maps of the APIs of all the Unity versions.
    ///
    /// Usage: Generate
    ///
    /// </summary>
    /// <remarks>
    /// [.NET](https://dotnet.microsoft.com) >= 9.0 and [DocFX](https://dotnet.github.io/docfx/) must be installed
    /// on your system.
    /// </remarks>
    partial class Program
    {
        /// <summary>
        /// Gets the default URL of the online API documentation of Unity.
        /// </summary>
        private const string DefaultUnityApiUrl = "https://docs.unity3d.com/{0}/Documentation/ScriptReference/";

        /// <summary>
        /// The default path of the Unity repository.
        /// </summary>
        private const string DefaultUnityRepositoryPath = "UnityCsReference";

        /// <summary>
        /// The default URL of the Unity repository.
        /// </summary>
        private const string DefaultUnityRepositoryUrl = "https://github.com/Unity-Technologies/UnityCsReference.git";

        /// <summary>
        /// The default branch of the Unity repository.
        /// </summary>
        private const string DefaultUnityRepositoryBranch = "master";

        // https://github.com/dotnet/docfx/blob/1c4e9ff4a2d236206eee04066847a98343c6a3f7/src/Docfx.Build/XRefMaps/XRefArchive.cs#L14
        /// <summary>
        /// The default xref map filename.
        /// </summary>
        private const string DefaultXrefMapFileName = "xrefmap.yml";

        /// <summary>
        /// The default path where to copy the xref maps.
        /// </summary>
        private const string DefaultXrefMapsPath = $"_site/{{0}}/{DefaultXrefMapFileName}";

        /// <summary>
        /// The default DocFX config file path.
        /// </summary>
        private const string DefaultDocFxConfigurationFilePath = "docfx.json";

        /// <summary>
        /// Entry point of this program.
        /// </summary>
        public static async Task Main(string[] args)
        {
            RootCommand rootCommand = [];

            Option<string> repositoryUrlOption = new("--repositoryUrl")
            {
                Description = "The Git repository url.",
                DefaultValueFactory = _ => DefaultUnityRepositoryUrl
            };
            Option<string> repositoryBranchOption = new("--repositoryBranch")
            {
                Description = "The Git repository branch.",
                DefaultValueFactory = _ => DefaultUnityRepositoryBranch
            };
            Option<string> repositoryPathOption = new("--repositoryPath")
            {
                Description = "The Git repository path to git clone. " +
                    "If the clone has already been made, it will reset so that the clone can be reused.",
                DefaultValueFactory = _ => DefaultUnityRepositoryPath
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
                DefaultValueFactory = _ => DefaultUnityApiUrl
            };
            Option<string> docFxConfigurationFilePathOption = new("--docFxConfigurationFilePath")
            {
                Description = "The path to the DocFX configuration file.",
                DefaultValueFactory = _ => DefaultDocFxConfigurationFilePath
            };
            Option<string> xrefMapsPathOption = new("--xrefMapsPath")
            {
                Description = $"The path where the final {DefaultXrefMapFileName} files will be generated. " +
                    $"{{0}} is replaced with the Unity editor version (example: {string.Format(DefaultXrefMapsPath, "6000.0.1f1")}) or " +
                    $"the Unity package version (example: {string.Format(DefaultXrefMapsPath, "1.0.0")}) in the case of a package.",
                DefaultValueFactory = _ => DefaultXrefMapsPath
            };

            rootCommand.Options.Add(repositoryUrlOption);
            rootCommand.Options.Add(repositoryBranchOption);
            rootCommand.Options.Add(repositoryPathOption);
            rootCommand.Options.Add(repositoryTagsOption);
            rootCommand.Options.Add(apiUrlOption);
            rootCommand.Options.Add(docFxConfigurationFilePathOption);
            rootCommand.Options.Add(xrefMapsPathOption);

            rootCommand.SetAction(async (parseResult, cancellationToken) =>
            {
                bool result = true;

                string? repositoryUrl = parseResult.GetValue(repositoryUrlOption);
                string? repositoryBranch = parseResult.GetValue(repositoryBranchOption);
                string? repositoryPath = parseResult.GetValue(repositoryPathOption);
                string[]? repositoryTags = parseResult.GetValue(repositoryTagsOption);
                string? apiUrl = parseResult.GetValue(apiUrlOption);
                string? docFxFilePath = parseResult.GetValue(docFxConfigurationFilePathOption);
                string? xrefMapsPath = parseResult.GetValue(xrefMapsPathOption);

                using Stream docFxStream = File.OpenRead(docFxFilePath!);

                DocFxConfiguration? docFxConfiguration = await JsonSerializer.DeserializeAsync<DocFxConfiguration>(docFxStream, cancellationToken: cancellationToken);

                string generatedDocsPath = docFxConfiguration!.Build!.Destination!;
                string generatedXrefMapPath = Path.Combine(generatedDocsPath, DefaultXrefMapFileName!);

                Console.WriteLine($"Sync the Unity repository in '{Path.GetFullPath(repositoryPath!)}'");

                using Repository repository = Git.GetSyncRepository(repositoryUrl!, repositoryPath!, repositoryBranch!);

                foreach (string repositoryTag in repositoryTags!)
                {
                    Match versionMatch = VersionRegex().Match(repositoryTag);

                    string shortVersion = $"{versionMatch.Groups["majorVersion"].Value}.{versionMatch.Groups["minorVersion"].Value}";

                    Console.WriteLine($"Generating Unity '{shortVersion}' xref map");

                    repository.HardReset(repositoryTag);

                    string xrefMapPath = string.Format(xrefMapsPath!, repositoryTag); // ./<version>/xrefmap.yml

                    Console.WriteLine($"Running DocFX on '{repositoryTag}'");

                    await Utils.RunCommand("dotnet", $"docfx {docFxFilePath}", Console.WriteLine, Console.WriteLine, cancellationToken);

                    if (!File.Exists(generatedXrefMapPath))
                    {
                        result = false;

                        Console.Error.WriteLine($"Error: '{generatedXrefMapPath}' for Unity '{repositoryTag}' not generated");
                        Console.Error.WriteLine("\n");

                        continue;
                    }

                    Console.WriteLine($"Fixing hrefs in '{Path.GetFullPath(xrefMapPath)}'");

                    await Utils.CopyFile(generatedXrefMapPath, xrefMapPath, cancellationToken);

                    XrefMap xrefMap = await XrefMap.Load(xrefMapPath, cancellationToken);

                    xrefMap.FixHrefs(apiUrl: string.Format(apiUrl!, shortVersion));

                    await xrefMap.Save(xrefMapPath, cancellationToken);

                    Console.WriteLine("\n");
                }

                return result ? 0 : 1;
            });

            Option<string> xrefPathOption = new("--xrefPath")
            {
                Description = $"The path to the {DefaultXrefMapFileName} file.",
                Required = true
            };

            Command testCommand = new("test", $"Check that the links in the {DefaultXrefMapFileName} file are valid.")
            {
                xrefPathOption
            };

            testCommand.SetAction(async (parseResult, cancellationToken) =>
            {
                bool result = true;

                string? xrefPath = parseResult.GetValue(xrefPathOption);

                XrefMap xrefMap = await XrefMap.Load(xrefPath!, cancellationToken);

                foreach (XrefMapReference reference in xrefMap.References!)
                {
                    if (!await Utils.TestUriExists(reference.Href, cancellationToken))
                    {
                        result = false;

                        Console.WriteLine($"Warning: invalid URL {reference.Href} for {reference.Uid} uid");
                    }
                }

                return result ? 0 : 1;
            });

            rootCommand.Subcommands.Add(testCommand);

            await rootCommand.Parse(args).InvokeAsync();
        }

        [GeneratedRegex(@"(?<majorVersion>\d+)\.(?<minorVersion>\d+)\.(?<patchVersion>\d+)")]
        private static partial Regex VersionRegex();
    }
}
