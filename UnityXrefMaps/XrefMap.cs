using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace UnityXrefMaps;

/// <summary>
/// Represents a xref map file of Unity.
/// </summary>
public sealed partial class XrefMap
{
    private static readonly Deserializer s_deserializer = new();
    private static readonly Serializer s_serializer = new();

    [YamlMember(Alias = "sorted")]
    public bool Sorted { get; set; }

    [YamlMember(Alias = "references")]
    public XrefMapReference[]? References { get; set; }

    /// <summary>
    /// Loads a <see cref="XrefMap"/> from a file.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <returns>The loaded <see cref="XrefMap"/> from <paramref name="filePath"/>.</returns>
    public static async Task<XrefMap> Load(string filePath, CancellationToken cancellationToken = default)
    {
        string xrefMapText = await File.ReadAllTextAsync(filePath, cancellationToken);

        // Remove `0:` strings on the xrefmap that make crash Deserializer
        xrefMapText = ZeroStringsRegex().Replace(xrefMapText, "$1");

        return s_deserializer.Deserialize<XrefMap>(xrefMapText);
    }

    /// <summary>
    /// Fix the <see cref="XrefMapReference.Href"/> of <see cref="References"/> of this <see cref="XrefMap"/>.
    /// </summary>
    /// <param name="apiUrl">The URL of the online API documentation of Unity.</param>
    public void FixHrefs(string apiUrl)
    {
        var fixedReferences = new List<XrefMapReference>();

        foreach (XrefMapReference reference in References!)
        {
            if (!reference.IsValid)
            {
                continue;
            }

            reference.FixHref(apiUrl);
            fixedReferences.Add(reference);
        }

        References = [.. fixedReferences];
    }

    /// <summary>
    /// Saves this <see cref="XrefMap"/> to a file.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    public async Task Save(string filePath, CancellationToken cancellationToken = default)
    {
        string xrefMapText = "### YamlMime:XRefMap\n" + s_serializer.Serialize(this);

        await File.WriteAllTextAsync(filePath, xrefMapText, cancellationToken);
    }

    [GeneratedRegex(@"(\d):")]
    private static partial Regex ZeroStringsRegex();
}
