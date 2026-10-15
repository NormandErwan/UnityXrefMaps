using System.Text.Json.Serialization;

namespace UnityXrefMaps.DocFX
{
    [JsonSerializable(typeof(DocFxConfiguration))]
    internal partial class DocFxJsonSerializerContext : JsonSerializerContext
    {
    }
}
