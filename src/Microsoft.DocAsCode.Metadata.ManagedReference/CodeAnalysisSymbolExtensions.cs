namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.CodeAnalysis;

    public static class CodeAnalysisSymbolExtensions
    {
        private const string SystemString = "System";
        private static Regex TemplateParameterRegex = new Regex(@"<.*>", RegexOptions.Compiled);

        public static T GetMember<T>(this Compilation compilation, string qualifiedName) where T : ISymbol
        {
            return (T)compilation.GlobalNamespace.GetMember(qualifiedName);
        }

        public static ISymbol GetMember(this INamespaceOrTypeSymbol container, string qualifiedName)
        {
            var name = TemplateParameterRegex.Replace(qualifiedName, string.Empty);
            var index = name.IndexOf('(');
            if (index != -1)
            {
                name = name.Remove(index);
            }
            var members = GetMembersCore(container, name);
            if (members.Length == 0)
            {
                return null;
            }
            else if (members.Length > 1)
            {
                Debug.Assert(false, "Found multiple members of specified name:\r\n" + string.Join("\r\n", members));
            }

            return members.Single();
        }

        private static ImmutableArray<ISymbol> GetMembersCore(INamespaceOrTypeSymbol container, string name)
        {
            var parts = SplitQualifiedName(name).ToImmutableArray();

            var lastContainer = container;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var nestedContainer = (INamespaceOrTypeSymbol)lastContainer.GetMember(parts[i]);
                if (nestedContainer == null)
                {
                    // If there wasn't a nested namespace or type with that name, assume it's a
                    // member name that includes dots (e.g. explicit interface implementation would contain its interface's name).
                    return lastContainer.GetMembers(string.Join(".", parts.Skip(i)));
                }
                else
                {
                    lastContainer = nestedContainer;
                }
            }

            return lastContainer.GetMembers(parts[parts.Length - 1]);
        }

        private static IEnumerable<string> SplitQualifiedName(
            string pstrName)
        {
            Debug.Assert(pstrName != null);
            do
            {
                var delimiter = -1;
                for (int i = 0; i < pstrName.Length; i++)
                {
                    // If we see consecutive dots, the second is part of the method name
                    // (i.e. ".ctor" or ".cctor").
                    if (pstrName[i] == '.' && delimiter < i - 1)
                    {
                        delimiter = i;
                        break;
                    }
                }

                yield return pstrName.Substring(0, delimiter < 0 ? pstrName.Length : delimiter);
                if (delimiter < 0)
                {
                    pstrName = string.Empty;
                }
                else if (delimiter == 6 && pstrName.StartsWith(SystemString, StringComparison.Ordinal))
                {
                    pstrName = SystemString;
                }
                else
                {
                    pstrName = pstrName.Substring(delimiter + 1);
                }
            } while (pstrName.Length > 0);
        }
    }
}
