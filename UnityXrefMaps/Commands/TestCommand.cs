using System.CommandLine;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace UnityXrefMaps.Commands;

internal sealed class TestCommand : Command
{
    private readonly HttpClient? _httpClient;

    public TestCommand(ILoggerFactory loggerFactory, HttpClient? httpClient = null) : base("test", $"Check that the links in the {Constants.DefaultXrefMapFileName} file are valid.")
    {
        _httpClient = httpClient;

        Option<string> xrefPathOption = new("--xrefPath")
        {
            Description = $"The path to the {Constants.DefaultXrefMapFileName} file.",
            Required = true
        };

        Options.Add(xrefPathOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            bool result = true;

            XrefMapService xrefMapService = new(loggerFactory.CreateLogger<XrefMapService>());

            ILogger logger = loggerFactory.CreateLogger<BuildCommand>();

            string? xrefPath = parseResult.GetValue(xrefPathOption);

            XrefMap xrefMap = await xrefMapService.Load(xrefPath!, cancellationToken);

            foreach (XrefMapReference reference in xrefMap.References!)
            {
                if (!await Utils.TestUriExists(reference.Href, logger, cancellationToken, _httpClient))
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
