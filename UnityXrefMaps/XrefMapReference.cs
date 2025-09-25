using System.Collections.Generic;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace UnityXrefMaps;

/// <summary>
/// Represents a reference item on a <see cref="XrefMap"/>.
/// </summary>
public sealed partial class XrefMapReference
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

    /// <summary>
    /// Sets <see cref="Href"/> to link to the online API documentation of Unity.
    /// </summary>
    /// <param name="apiUrl">The URL of the online API documentation of Unity.</param>
    public void FixHref(string apiUrl, IEnumerable<string> hrefNamespacesToTrim)
    {
        // Namespaces point to documentation index
        if (CommentId!.StartsWith("N:"))
        {
            Href = "index";
        }
        else
        {
            Href = Uid;

            foreach (string hrefNamespaceToTrim in hrefNamespacesToTrim)
            {
                Href = Href!.Replace(hrefNamespaceToTrim + ".", string.Empty);
            }

            // Fix href of constructors
            Href = Href!.Replace(".#ctor", "-ctor");

            // Fix href of generics
            Href = GenericHrefRegex().Replace(Href!, string.Empty);
            Href = Href.Replace("`", "_");

            // Fix href of methods
            Href = MethodHrefPointerRegex().Replace(Href, string.Empty);
            Href = MethodHrefRegex().Replace(Href, string.Empty);

            // Fix href of operator
            if (CommentId.StartsWith("M:") && CommentId.Contains(".op_"))
            {
                Href = Href.Replace(".op_", ".operator_");

                Href = Href.Replace(".operator_Subtraction", ".operator_subtract");
                Href = Href.Replace(".operator_Multiply", ".operator_multiply");
                Href = Href.Replace(".operator_Division", ".operator_divide");
                Href = Href.Replace(".operator_Addition", ".operator_add");
                Href = Href.Replace(".operator_Equality", ".operator_eq");
                Href = Href.Replace(".operator_Implicit~", ".operator_");
            }

            // Fix href of properties
            if (CommentId.StartsWith("F:") || CommentId.StartsWith("M:") || CommentId.StartsWith("P:"))
            {
                Href = PropertyHrefRegex().Replace(Href, "-$1");
            }
        }

        Href = apiUrl + Href + ".html";
    }

    [GeneratedRegex(@"`{2}\d")]
    private static partial Regex GenericHrefRegex();

    [GeneratedRegex(@"\*$")]
    private static partial Regex MethodHrefPointerRegex();

    [GeneratedRegex(@"\(.*\)")]
    private static partial Regex MethodHrefRegex();

    [GeneratedRegex(@"\.([a-z].*)$")]
    private static partial Regex PropertyHrefRegex();
}
