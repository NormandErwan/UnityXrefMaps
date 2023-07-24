﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    /// [.NET](https://dotnet.microsoft.com) >= 7.0 and [DocFX](https://dotnet.github.io/docfx/) must be installed
    /// on your system.
    /// </remarks>
    partial class Program
    {
        /// <summary>
        /// The path where the documentation of the Unity repository will be generated.
        /// </summary>
        private const string GeneratedDocsPath = $"{UnityRepoPath}/_site";

        /// <summary>
        /// The path of the xref map generated by DocFX.
        /// </summary>
        private static readonly string GeneratedXrefMapPath = Path.Combine(GeneratedDocsPath, XrefMapFileName);

        /// <summary>
        /// The path of the default xref map, pointing at <see cref="UnityApiUrl"/>.
        /// </summary>
        private static readonly string DefaultXrefMapPath = Path.Combine(XrefMapsPath, XrefMapFileName);

        /// <summary>
        /// Gets the URL of the online API documentation of Unity.
        /// </summary>
        private const string UnityApiUrl = "https://docs.unity3d.com/ScriptReference/";

        [GeneratedRegex("\\d{4}\\.\\d")]
        private static partial Regex UnityVersionRegex();

        /// <summary>
        /// The path of the Unity repository.
        /// </summary>
        private const string UnityRepoPath = "UnityCsReference";

        /// <summary>
        /// The URL of the Unity repository.
        /// </summary>
        private const string UnityRepoUrl = "https://github.com/Unity-Technologies/UnityCsReference.git";

        /// <summary>
        /// The xref map filename.
        /// </summary>
        private const string XrefMapFileName = "xrefmap.yml";

        /// <summary>
        /// The path where to copy the xref maps.
        /// </summary>
        private const string XrefMapsPath = "_site";

        /// <summary>
        /// Entry point of this program.
        /// </summary>
        public static void Main()
        {
            Console.WriteLine($"Sync the Unity repository in '{UnityRepoPath}'");
            using var unityRepo = Git.GetSyncRepository(UnityRepoUrl, UnityRepoPath, branch: "master");

            var versions = GetLatestVersions(unityRepo);
            var latestVersion = versions
                .OrderByDescending(version => version.name)
                .First(version => version.release.Contains('f'));

            foreach (var version in versions)
            {
                Console.WriteLine($"Generating Unity '{version.name}' xref map");
                unityRepo.HardReset(version.release);
                string xrefMapPath = Path.Combine(XrefMapsPath, version.name, XrefMapFileName); // ./<version>/xrefmap.yml

                Console.WriteLine($"Running DocFX on '{version.release}'");
                Utils.RunCommand("dotnet", "docfx", Console.WriteLine, Console.WriteLine);

                if (!File.Exists(GeneratedXrefMapPath))
                {
                    Console.WriteLine($"Error: '{GeneratedXrefMapPath}' for Unity '{version.name}' not generated");
                    Console.WriteLine("\n");
                    continue;
                }

                Console.WriteLine($"Fixing hrefs in '{xrefMapPath}'");
                Utils.CopyFile(GeneratedXrefMapPath, xrefMapPath);
                var xrefMap = XrefMap.Load(xrefMapPath);
                xrefMap.FixHrefs(apiUrl: $"https://docs.unity3d.com/{version.name}/Documentation/ScriptReference/");
                xrefMap.Save(xrefMapPath);

                // Set the last version's xref map as the default one
                if (version == latestVersion)
                {
                    Console.WriteLine($"Fixing hrefs in '{DefaultXrefMapPath}'");
                    Utils.CopyFile(GeneratedXrefMapPath, DefaultXrefMapPath);
                    xrefMap = XrefMap.Load(DefaultXrefMapPath);
                    xrefMap.FixHrefs(UnityApiUrl);
                    xrefMap.Save(DefaultXrefMapPath);
                }

                Console.WriteLine("\n");
            }
        }

        /// <summary>
        /// Returns a collection of the latest versions of a specified repository of Unity.
        /// </summary>
        /// <param name="unityRepository">The repository of Unity to use.</param>
        /// <returns>The latest versions.</returns>
        private static IEnumerable<(string name, string release)> GetLatestVersions(Repository unityRepository)
        {
            return unityRepository
                .GetTags()
                .Select(release => (name: UnityVersionRegex().Match(release).Value, release))
                .GroupBy(version => version.name)
                .Select(version => version.First());
        }
    }
}
