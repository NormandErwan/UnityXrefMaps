using Meziantou.Extensions.Logging.Xunit.v3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityXrefMaps.Commands;

namespace UnityXrefMaps.Tests
{
    public class CommandTests : IAsyncLifetime, IAsyncDisposable
    {
        private string? _repositoryDirectoryPath;
        private string? _xrefDirectoryPath;
        private string? _docFxFilePath;
        private string? _docFxFilterFilePath;

        private class CustomStringWriter : StringWriter
        {
            private readonly ILogger logger;
            private readonly LogLevel logLevel;

            public CustomStringWriter(ILogger logger, LogLevel logLevel)
            {
                this.logger = logger;
                this.logLevel = logLevel;
            }

            public override void Write(string? value)
            {
                base.Write(value);

                logger.Log(logLevel, "{Message}", value);
            }
        }

        private readonly ITestOutputHelper output;

        public CommandTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task UnityEditor_BuildTest_Success()
        {
            string docFxFileContent = await File.ReadAllTextAsync("docfx.json", TestContext.Current.CancellationToken);

            docFxFileContent = docFxFileContent.Replace("UnityCsReference/", _repositoryDirectoryPath + '/');
            docFxFileContent = docFxFileContent.Replace("filterConfig.yml", _docFxFilterFilePath);

            await File.WriteAllTextAsync(_docFxFilePath!, docFxFileContent, TestContext.Current.CancellationToken);

            string docFxFilterConfigContent = """
### YamlMime:ManagedReference
---
apiRules:
  - include:
      uidRegex: ^UnityEngine\.Vector2$
  - include:
      uidRegex: ^UnityEngine\.Vector3$
  - exclude:
      uidRegex: .*
""";

            await File.WriteAllTextAsync(_docFxFilterFilePath!, docFxFilterConfigContent, TestContext.Current.CancellationToken);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddFakeLogging();
                builder.Services.AddSingleton<ILoggerProvider>(new XUnitLoggerProvider(output, appendScope: false));
            });

            await using ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            FakeLogCollector fakeLogCollector = serviceProvider.GetFakeLogCollector();

            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            ILogger logger = loggerFactory.CreateLogger<CommandTests>();

            InvocationConfiguration invocationConfiguration = new()
            {
                Error = new CustomStringWriter(logger, LogLevel.Error),
                Output = new CustomStringWriter(logger, LogLevel.Information),
            };

            string[] testedVersions = ["6000.0.1f1", "6000.1.1f1"];

            BuildCommand buildCommand = new(loggerFactory.CreateLogger<BuildCommand>());

            string xrefDirectoryName = "test";
            string xrefFileName = "test2.yml";

            string[] buildArgs = [
                "--repositoryPath",
                _repositoryDirectoryPath!,
                "--docFxConfigurationFilePath",
                _docFxFilePath!,
                "--xrefMapsPath",
                $"{_xrefDirectoryPath}/{xrefDirectoryName}/{{0}}/{xrefFileName}",
                "--trimNamespaces",
                "UnityEngine"
            ];

            buildArgs = [.. buildArgs, .. testedVersions.SelectMany(v => new string[] { "--repositoryTags", v })];

            Assert.Equal(
                0,
                await buildCommand
                    .Parse(buildArgs)
                    .InvokeAsync(
                        invocationConfiguration,
                        TestContext.Current.CancellationToken));

            IReadOnlyList<FakeLogRecord> logRecords = fakeLogCollector.GetSnapshot();

            Assert.Equal(2, logRecords.Count(l => l.Message.Contains("XRef map exported.", StringComparison.OrdinalIgnoreCase)));

            foreach (string testedVersion in testedVersions)
            {
                string xrefFilePath = $"{_xrefDirectoryPath}/{xrefDirectoryName}/{testedVersion}/{xrefFileName}";

                Assert.True(File.Exists(xrefFilePath));

                string xrefFileContent = await File.ReadAllTextAsync(xrefFilePath, TestContext.Current.CancellationToken);

                logger.LogInformation("{FilePath}:\n\n{FileContent}", xrefFilePath, xrefFileContent);
            }

            TestCommand testCommand = new(loggerFactory.CreateLogger<TestCommand>());

            foreach (string testedVersion in testedVersions)
            {
                fakeLogCollector.Clear();

                string xrefFilePath = $"{_xrefDirectoryPath}/{xrefDirectoryName}/{testedVersion}/{xrefFileName}";

                XrefMap xrefMap = await XrefMap.Load(xrefFilePath, TestContext.Current.CancellationToken);

                Assert.Equal(3, xrefMap.References!.Length);

                Assert.Equal("UnityEngine", xrefMap.References[0].Uid);
                Assert.Equal("UnityEngine.Vector2", xrefMap.References[1].Uid);
                Assert.Equal("UnityEngine.Vector3", xrefMap.References[2].Uid);

                string[] testArgs = [
                    "--xrefPath",
                    xrefFilePath
                ];

                Assert.Equal(
                    0,
                    await testCommand
                        .Parse(testArgs)
                        .InvokeAsync(
                            invocationConfiguration,
                            TestContext.Current.CancellationToken));

                Assert.Equal(0, fakeLogCollector.Count);
            }
        }

        [Fact]
        public async Task UnityPackage_BuildTest_Success()
        {
            string docFxFileContent = await File.ReadAllTextAsync("docfx.json", TestContext.Current.CancellationToken);

            docFxFileContent = docFxFileContent.Replace("UnityCsReference/Projects/CSharp/*.csproj", "UnityCsReference/InputSystem/**/*.cs");
            docFxFileContent = docFxFileContent.Replace("UnityCsReference/", _repositoryDirectoryPath + '/');
            docFxFileContent = docFxFileContent.Replace("filterConfig.yml", _docFxFilterFilePath);

            await File.WriteAllTextAsync(_docFxFilePath!, docFxFileContent, TestContext.Current.CancellationToken);

            string docFxFilterConfigContent = """
### YamlMime:ManagedReference
---
apiRules:
  - include:
      uidRegex: ^UnityEngine\.InputSystem\.InputSystem$
  - include:
      uidRegex: ^UnityEngine\.InputSystem\.InputActionAsset$
  - exclude:
      uidRegex: .*
""";

            await File.WriteAllTextAsync(_docFxFilterFilePath!, docFxFilterConfigContent, TestContext.Current.CancellationToken);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddFakeLogging();
                builder.Services.AddSingleton<ILoggerProvider>(new XUnitLoggerProvider(output, appendScope: false));
            });

            await using ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            FakeLogCollector fakeLogCollector = serviceProvider.GetFakeLogCollector();

            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            ILogger logger = loggerFactory.CreateLogger<CommandTests>();

            InvocationConfiguration invocationConfiguration = new()
            {
                Error = new CustomStringWriter(logger, LogLevel.Error),
                Output = new CustomStringWriter(logger, LogLevel.Information),
            };

            string[] testedVersions = ["1.14.0", "1.14.2"];

            BuildCommand buildCommand = new(loggerFactory.CreateLogger<BuildCommand>());

            string xrefDirectoryName = "test";
            string xrefFileName = "test2.yml";

            string[] buildArgs = [
                "--repositoryUrl",
                "https://github.com/needle-mirror/com.unity.inputsystem.git",
                "--repositoryPath",
                _repositoryDirectoryPath!,
                "--apiUrl",
                "https://docs.unity3d.com/Packages/com.unity.inputsystem@{0}/api/",
                "--docFxConfigurationFilePath",
                _docFxFilePath!,
                "--xrefMapsPath",
                $"{_xrefDirectoryPath}/{xrefDirectoryName}/{{0}}/{xrefFileName}"
            ];

            buildArgs = [.. buildArgs, .. testedVersions.SelectMany(v => new string[] { "--repositoryTags", v })];

            Assert.Equal(
                0,
                await buildCommand
                    .Parse(buildArgs)
                    .InvokeAsync(
                        invocationConfiguration,
                        TestContext.Current.CancellationToken));

            IReadOnlyList<FakeLogRecord> logRecords = fakeLogCollector.GetSnapshot();

            Assert.Equal(2, logRecords.Count(l => l.Message.Contains("XRef map exported.", StringComparison.OrdinalIgnoreCase)));

            foreach (string testedVersion in testedVersions)
            {
                string xrefFilePath = $"{_xrefDirectoryPath}/{xrefDirectoryName}/{testedVersion}/{xrefFileName}";

                Assert.True(File.Exists(xrefFilePath));

                string xrefFileContent = await File.ReadAllTextAsync(xrefFilePath, TestContext.Current.CancellationToken);

                logger.LogInformation("{FilePath}:\n\n{FileContent}", xrefFilePath, xrefFileContent);
            }

            TestCommand testCommand = new(loggerFactory.CreateLogger<TestCommand>());

            foreach (string testedVersion in testedVersions)
            {
                fakeLogCollector.Clear();

                string xrefFilePath = $"{_xrefDirectoryPath}/{xrefDirectoryName}/{testedVersion}/{xrefFileName}";

                XrefMap xrefMap = await XrefMap.Load(xrefFilePath, TestContext.Current.CancellationToken);

                Assert.Equal(3, xrefMap.References!.Length);

                Assert.Equal("UnityEngine.InputSystem", xrefMap.References[0].Uid);
                Assert.Equal("UnityEngine.InputSystem.InputActionAsset", xrefMap.References[1].Uid);
                Assert.Equal("UnityEngine.InputSystem.InputSystem", xrefMap.References[2].Uid);

                string[] testArgs = [
                    "--xrefPath",
                    xrefFilePath
                ];

                Assert.Equal(
                    0,
                    await testCommand
                        .Parse(testArgs)
                        .InvokeAsync(
                            invocationConfiguration,
                            TestContext.Current.CancellationToken));

                Assert.Equal(0, fakeLogCollector.Count);
            }
        }

        // https://stackoverflow.com/a/1702920
        private static void DeleteDirectory(string targetDir)
        {
            File.SetAttributes(targetDir, FileAttributes.Normal);

            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(targetDir, false);
        }

        public ValueTask InitializeAsync()
        {
            _repositoryDirectoryPath = Guid.NewGuid().ToString();
            _xrefDirectoryPath = Guid.NewGuid().ToString();
            _docFxFilePath = Guid.NewGuid().ToString() + "_config.json";
            _docFxFilterFilePath = Guid.NewGuid().ToString() + "_filter_config.yml";

            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);

            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            File.Delete(_docFxFilePath!);
            File.Delete(_docFxFilterFilePath!);

            const int maxRetries = 3;
            const int delay = 500;

            await DeleteDirectoryWithRetries(_repositoryDirectoryPath!, maxRetries, delay);
            await DeleteDirectoryWithRetries(_xrefDirectoryPath!, maxRetries, delay);
        }

        private async Task DeleteDirectoryWithRetries(string directoryPath, int maxRetries, int delay)
        {
            int retries = 0;

            while (retries < maxRetries)
            {
                try
                {
                    output.WriteLine($"Trying to delete directory: {directoryPath}");

                    DeleteDirectory(directoryPath);

                    output.WriteLine($"Directory deleted: {directoryPath}");

                    break;
                }
                catch (Exception e)
                {
                    output.WriteLine($"Error deleting directory: {directoryPath}. Error: {e}");

                    retries++;

                    if (retries < maxRetries)
                    {
                        output.WriteLine($"Retrying in {delay}ms...");

                        await Task.Delay(delay);
                    }
                    else
                    {
                        output.WriteLine("Retry limit reached.");
                    }
                }
            }
        }
    }
}
