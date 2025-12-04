using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace UnityXrefMaps
{
    internal sealed partial class XrefMapService
    {
        private readonly Deserializer _deserializer = new();
        private readonly Serializer _serializer = new();

        private readonly ILogger<XrefMapService> _logger;

        public XrefMapService(ILogger<XrefMapService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Loads a <see cref="XrefMap"/> from a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <returns>The loaded <see cref="XrefMap"/> from <paramref name="filePath"/>.</returns>
        public async Task<XrefMap> Load(string filePath, CancellationToken cancellationToken = default)
        {
            string xrefMapText = await File.ReadAllTextAsync(filePath, cancellationToken);

            // Remove `0:` strings on the xrefmap that make crash Deserializer
            xrefMapText = ZeroStringsRegex().Replace(xrefMapText, "$1");

            return _deserializer.Deserialize<XrefMap>(xrefMapText);
        }

        /// <summary>
        /// Fix the <see cref="XrefMapReference.Href"/> of <see cref="References"/> of this <see cref="XrefMap"/>.
        /// </summary>
        /// <param name="apiUrl">The URL of the online API documentation of Unity.</param>
        public IEnumerable<XrefMapReference> Process(string apiUrl, IEnumerable<XrefMapReference> references, IEnumerable<string> hrefNamespacesToTrim, bool isPackage)
        {
            List<XrefMapReference> fixedReferences = [];

            foreach (XrefMapReference reference in references)
            {
                if (!reference.IsValid)
                {
                    continue;
                }

                try
                {
                    reference.Href = XRefHrefFixer.Fix(apiUrl, reference, hrefNamespacesToTrim, isPackage);

                    fixedReferences.Add(reference);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Error fixing href: {Uid}", reference.Uid);
                }
            }

            return fixedReferences;
        }

        /// <summary>
        /// Saves this <see cref="XrefMap"/> to a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        public async Task Save(string filePath, XrefMap xrefMap, CancellationToken cancellationToken = default)
        {
            string xrefMapText = "### YamlMime:XRefMap\n" + _serializer.Serialize(xrefMap);

            await File.WriteAllTextAsync(filePath, xrefMapText, cancellationToken);
        }

        [GeneratedRegex(@"(\d):")]
        private static partial Regex ZeroStringsRegex();
    }
}
