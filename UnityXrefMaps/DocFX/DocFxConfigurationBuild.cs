using System.Text.Json.Serialization;

namespace UnityXrefMaps.DocFX;

// https://github.com/dotnet/docfx/blob/main/src/Docfx.App/Config/BuildJsonConfig.cs
public class DocFxConfigurationBuild
{
    [JsonPropertyName("dest")]
    public string? Destination { get; set; }
}
