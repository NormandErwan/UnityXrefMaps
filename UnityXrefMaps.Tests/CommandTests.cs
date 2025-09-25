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
    public class CommandTests : IAsyncDisposable
    {
        private static readonly string repositoryDirectoryPath = Guid.NewGuid().ToString();
        private static readonly string xrefDirectoryPath = Guid.NewGuid().ToString();
        private static readonly string docFxFilePath = Guid.NewGuid().ToString() + "_config.json";
        private static readonly string docFxFilterFilePath = Guid.NewGuid().ToString() + "_filter_config.yml";

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
        public async Task BuildTest_Success()
        {
            string docFxFileContent = await File.ReadAllTextAsync("docfx.json", TestContext.Current.CancellationToken);

            docFxFileContent = docFxFileContent.Replace("UnityCsReference/", repositoryDirectoryPath + '/');
            docFxFileContent = docFxFileContent.Replace("filterConfig.yml", docFxFilterFilePath);

            await File.WriteAllTextAsync(docFxFilePath, docFxFileContent, TestContext.Current.CancellationToken);

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

            await File.WriteAllTextAsync(docFxFilterFilePath, docFxFilterConfigContent, TestContext.Current.CancellationToken);

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
                repositoryDirectoryPath,
                "--docFxConfigurationFilePath",
                docFxFilePath,
                "--xrefMapsPath",
                $"{xrefDirectoryPath}/{xrefDirectoryName}/{{0}}/{xrefFileName}"
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

            Assert.Equal(2, logRecords.Count(l => l.Message.Equals("XRef map exported.")));

            foreach (string testedVersion in testedVersions)
            {
                string xrefFilePath = $"{xrefDirectoryPath}/{xrefDirectoryName}/{testedVersion}/{xrefFileName}";

                Assert.True(File.Exists(xrefFilePath));

                string xrefFileContent = await File.ReadAllTextAsync(xrefFilePath, TestContext.Current.CancellationToken);

                logger.LogInformation("{FilePath}:\n\n{FileContent}", xrefFilePath, xrefFileContent);
            }

            TestCommand testCommand = new(loggerFactory.CreateLogger<TestCommand>());

            foreach (string testedVersion in testedVersions)
            {
                fakeLogCollector.Clear();

                string[] testArgs = [
                    "--xrefPath",
                    $"{xrefDirectoryPath}/{xrefDirectoryName}/{testedVersion}/{xrefFileName}"
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

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);

            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            File.Delete(docFxFilePath);
            File.Delete(docFxFilterFilePath);

            const int maxRetries = 3;
            const int delay = 500;

            await DeleteDirectoryWithRetries(repositoryDirectoryPath, maxRetries, delay);
            await DeleteDirectoryWithRetries(xrefDirectoryPath, maxRetries, delay);
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
