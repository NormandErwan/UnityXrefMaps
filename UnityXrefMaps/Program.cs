using System.CommandLine;
using Microsoft.Extensions.Logging;
using UnityXrefMaps.Commands;

ILoggerFactory factory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

RootCommand rootCommand = new BuildCommand(factory);
rootCommand.Subcommands.Add(new TestCommand(factory));

return await rootCommand.Parse(args).InvokeAsync();
