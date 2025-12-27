using YamlDotNet.Serialization;

namespace UnityXrefMaps;

/// <summary>
/// Represents a xref map file of Unity.
/// </summary>
public sealed class XrefMap
{
    [YamlMember(Alias = "sorted")]
    public bool Sorted { get; set; }

    [YamlMember(Alias = "references")]
    public XrefMapReference[]? References { get; set; }
}
