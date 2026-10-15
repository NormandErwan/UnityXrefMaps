using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnityXrefMaps.Tests;

public class XrefMapServiceTests
{
    private readonly XrefMapService _service = new(NullLogger<XrefMapService>.Instance);

    [Fact]
    public async Task Load_ZeroColonAtWordBoundary_IsStripped()
    {
        // `0: value` in a YAML string value causes the parser to treat it as a nested
        // mapping, crashing with a type-mismatch. The regex must strip `0:` before parsing.
        string yaml = """
            sorted: false
            references:
            - uid: SomeType
              name: SomeType
              href: SomeType.html
              commentId: T:SomeType
              nameWithType: 0: orphan
            """;

        string filePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(filePath, yaml);
            XrefMap result = await _service.Load(filePath);
            Assert.NotNull(result);
            Assert.Single(result.References!);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task Load_NonZeroDigitColon_IsPreserved()
    {
        // The old regex `(\d):` stripped the colon from every digit-colon sequence, corrupting
        // legitimate values such as `data1:field` → `data1field` in href or nameWithType fields.
        // Only `0:` at a word boundary should be stripped.
        string yaml = """
            sorted: false
            references:
            - uid: SomeType
              name: SomeType
              href: SomeType.html
              commentId: T:SomeType
              nameWithType: data1:field
            """;

        string filePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(filePath, yaml);
            XrefMap result = await _service.Load(filePath);
            Assert.Equal("data1:field", result.References![0].NameWithType);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
