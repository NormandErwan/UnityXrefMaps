using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityXrefMaps
{
    internal static partial class XRefHrefFixer
    {
        public static string Fix(string apiUrl, XrefMapReference xrefMapReference, IEnumerable<string> hrefNamespacesToTrim, bool isPackage)
        {
            string[] parts = xrefMapReference.CommentId!.Split(':');
            string type = parts[0];
            string uid = parts[1];
            string name = xrefMapReference.Name!;

            if (isPackage)
            {
                return FixPackage(apiUrl, name, type, uid);
            }
            else
            {
                return FixUnity(apiUrl, type, uid, hrefNamespacesToTrim);
            }
        }

        // https://docs.unity3d.com/Packages/com.unity.package-manager-doctools@3.14/manual/index.html
        // https://github.com/dotnet/docfx/blob/main/src/Docfx.Dotnet/SymbolUrlResolver.cs#L37
        private static string FixPackage(string apiUrl, string name, string type, string uid)
        {
            string classFullName;

            switch (type)
            {
                case "N":
                case "T":
                    string _namespace = uid;

                    return $"{apiUrl}{_namespace}.html";
                case "M":
                    Match methodNameMatch = MethodNameRegex().Match(name);

                    string methodName = name.Substring(0, methodNameMatch.Groups[1].Value.Length);

                    classFullName = uid.Substring(0, uid.IndexOf(methodName) - 1);

                    return $"{apiUrl}{classFullName}.html#{NonWordCharRegex().Replace(uid, "_")}";
                case "P":
                case "F":
                default:
                    classFullName = uid.Substring(0, uid.IndexOf(name) - 1);

                    return $"{apiUrl}{classFullName}.html#{NonWordCharRegex().Replace(uid, "_")}";
            }
        }

        private static string FixUnity(string apiUrl, string type, string uid, IEnumerable<string> hrefNamespacesToTrim)
        {
            string href;

            // Namespaces point to documentation index
            if (type.Equals("N"))
            {
                href = "index";
            }
            else
            {
                href = uid;

                foreach (string hrefNamespaceToTrim in hrefNamespacesToTrim)
                {
                    href = href.Replace(hrefNamespaceToTrim + ".", string.Empty);
                }

                // Fix href of constructors
                href = href.Replace(".#ctor", "-ctor");

                // Fix href of generics
                href = GenericHrefRegex().Replace(href, string.Empty);
                href = href.Replace("`", "_");

                // Fix href of methods
                href = MethodHrefPointerRegex().Replace(href, string.Empty);
                href = MethodHrefRegex().Replace(href, string.Empty);

                // Fix href of operator
                if (type.Equals("M") && uid.Contains(".op_"))
                {
                    href = href.Replace(".op_", ".operator_");

                    href = href.Replace(".operator_Subtraction", ".operator_subtract");
                    href = href.Replace(".operator_Multiply", ".operator_multiply");
                    href = href.Replace(".operator_Division", ".operator_divide");
                    href = href.Replace(".operator_Addition", ".operator_add");
                    href = href.Replace(".operator_Equality", ".operator_eq");
                    href = href.Replace(".operator_Implicit~", ".operator_");
                }

                // Fix href of properties
                if (type.Equals("F") || type.Equals("M") || type.Equals("P"))
                {
                    href = PropertyHrefRegex().Replace(href, "-$1");
                }
            }

            return apiUrl + href + ".html";
        }

        [GeneratedRegex(@"`{2}\d")]
        private static partial Regex GenericHrefRegex();

        [GeneratedRegex(@"\*$")]
        private static partial Regex MethodHrefPointerRegex();

        [GeneratedRegex(@"\(.*\)")]
        private static partial Regex MethodHrefRegex();

        [GeneratedRegex(@"\.([a-z].*)$")]
        private static partial Regex PropertyHrefRegex();

        [GeneratedRegex(@"\W")]
        private static partial Regex NonWordCharRegex();

        [GeneratedRegex(@"([^<>]*).*\(")]
        private static partial Regex MethodNameRegex();
    }
}
