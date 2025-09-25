using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace UnityXrefMaps.Commands;

internal sealed class TestCommand : Command
{
    public TestCommand(ILogger<TestCommand> logger) : base("test", $"Check that the links in the {Constants.DefaultXrefMapFileName} file are valid.")
    {
        Option<string> xrefPathOption = new("--xrefPath")
        {
            Description = $"The path to the {Constants.DefaultXrefMapFileName} file.",
            Required = true
        };

        Options.Add(xrefPathOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            bool result = true;

            string? xrefPath = parseResult.GetValue(xrefPathOption);

            XrefMap xrefMap = await XrefMap.Load(xrefPath!, cancellationToken);

            foreach (XrefMapReference reference in xrefMap.References!)
            {
                if (!await Utils.TestUriExists(reference.Href, logger, cancellationToken))
                {
                    result = false;

                    logger.LogWarning("Invalid URL {Href} for {Uid} uid", reference.Href, reference.Uid);
                }
            }

            return result ? 0 : 1;
        });
    }
}
