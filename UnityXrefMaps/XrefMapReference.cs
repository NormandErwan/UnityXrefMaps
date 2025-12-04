using YamlDotNet.Serialization;

namespace UnityXrefMaps;

/// <summary>
/// Represents a reference item on a <see cref="XrefMap"/>.
/// </summary>
public sealed class XrefMapReference
{
    [YamlMember(Alias = "uid")]
    public string? Uid { get; set; }

    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "name.vb")]
    public string? NameVb { get; set; }

    [YamlMember(Alias = "href")]
    public string? Href { get; set; }

    [YamlMember(Alias = "commentId")]
    public string? CommentId { get; set; }

    [YamlMember(Alias = "isSpec")]
    public string? IsSpec { get; set; }

    [YamlMember(Alias = "fullName")]
    public string? FullName { get; set; }

    [YamlMember(Alias = "fullName.vb")]
    public string? FullNameVb { get; set; }

    [YamlMember(Alias = "nameWithType")]
    public string? NameWithType { get; set; }

    [YamlMember(Alias = "nameWithType.vb")]
    public string? NameWithTypeVb { get; set; }

    /// <summary>
    /// Gets if this <see cref="XrefMapReference"/> is valid or not.
    /// </summary>
    [YamlIgnore]
    public bool IsValid => !CommentId!.Contains("Overload:");
}
