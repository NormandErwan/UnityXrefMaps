using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace UnityXrefMaps.Commands;

internal sealed class TestCommand : Command
{
    public TestCommand(ILoggerFactory loggerFactory) : base("test", $"Check that the links in the {Constants.DefaultXrefMapFileName} file are valid.")
    {
        Option<string> xrefPathOption = new("--xrefPath")
        {
            Description = $"The path to the {Constants.DefaultXrefMapFileName} file.",
            Required = true
        };

        Option<int> maxAttemptsOption = new("--maxAttempts")
        {
            Description = "The maximum number of requests sent for a link when the server answers 429 or 503.",
            DefaultValueFactory = _ => Constants.DefaultMaxAttempts
        };

        maxAttemptsOption.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int>() < 1)
            {
                result.AddError("--maxAttempts must be at least 1.");
            }
        });

        Options.Add(xrefPathOption);
        Options.Add(maxAttemptsOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            bool result = true;

            XrefMapService xrefMapService = new(loggerFactory.CreateLogger<XrefMapService>());

            ILogger logger = loggerFactory.CreateLogger<BuildCommand>();

            string? xrefPath = parseResult.GetValue(xrefPathOption);
            int maxAttempts = parseResult.GetValue(maxAttemptsOption);

            XrefMap xrefMap = await xrefMapService.Load(xrefPath!, cancellationToken);

            foreach (XrefMapReference reference in xrefMap.References!)
            {
                if (!await Utils.TestUriExists(reference.Href, maxAttempts, logger, cancellationToken))
                {
                    result = false;

                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Invalid URL {Href} for {Uid} uid", reference.Href, reference.Uid);
                    }
                }
            }

            return result ? 0 : 1;
        });
    }
}
