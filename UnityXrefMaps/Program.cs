using System.CommandLine;
using Microsoft.Extensions.Logging;
using UnityXrefMaps.Commands;

ILoggerFactory factory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

RootCommand rootCommand = new BuildCommand(factory.CreateLogger<BuildCommand>());
rootCommand.Subcommands.Add(new TestCommand(factory.CreateLogger<TestCommand>()));

await rootCommand.Parse(args).InvokeAsync();
