using System.Text.Json.Serialization;

namespace UnityXrefMaps.DocFX;

// https://github.com/dotnet/docfx/blob/main/src/Docfx.App/Config/DocfxConfig.cs
public class DocFxConfiguration
{
    [JsonPropertyName("build")]
    public DocFxConfigurationBuild? Build { get; set; }
}
