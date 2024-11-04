// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Docfx.Dotnet;

partial class SymbolUrlResolver
{
    public static string? GetMicrosoftLearnUrl(ISymbol symbol)
    {
        if (string.IsNullOrEmpty(symbol.ContainingAssembly?.Name))
            return null;

        if (symbol.GetDocumentationCommentId() is not { } commentId)
            return null;

        foreach (var (baseUrl, assemblyNames) in s_msLearnAssemblies.Value)
        {
            if (assemblyNames.Contains(symbol.ContainingAssembly.Name))
                return GetMicrosoftLearnUrl(commentId, symbol.IsEnumMember(), symbol.HasOverloads(), baseUrl);
        }

        return null;
    }

    public static string? GetMicrosoftLearnUrl(string commentId, bool isEnumMember, bool hasOverloads, string baseUrl = "https://learn.microsoft.com/dotnet/api/")
    {
        var parts = commentId.Split(':');
        var type = parts[0];
        var uid = parts[1].ToLowerInvariant();

        return type switch
        {
            "!" => null,
            "N" or "T" => $"{baseUrl}{uid.Replace('`', '-')}",
            "F" when isEnumMember => $"{baseUrl}{GetEnumMemberPathFromUid(uid)}#{GetUrlFragmentFromUid(uid)}",

            "M" or "F" or "P" or "E" => hasOverloads
                ? $"{baseUrl}{GetMemberPathFromUid(uid)}#{GetUrlFragmentFromUid(uid)}"
                : $"{baseUrl}{GetMemberPathFromUid(uid)}",

            _ => throw new NotSupportedException($"Unknown comment ID format '{type}'"),
        };

        static string GetEnumMemberPathFromUid(string uid)
        {
            var i = uid.LastIndexOf('.');
            return i >= 0 ? uid.Substring(0, i) : uid;
        }

        static string GetMemberPathFromUid(string uid)
        {
            var span = uid.AsSpan();
            var result = new StringBuilder(uid.Length);

            // Ignore parameters
            var parametersIndex = span.IndexOf('(');
            span = parametersIndex >= 0 ? span[..parametersIndex] : span;

            // Ignore method generic type parameters but keep type generic type parameters
            var methodNameIndex = span.LastIndexOf('.');
            var methodGenericTypeParametersIndex = span[methodNameIndex..].IndexOf('`');
            if (methodGenericTypeParametersIndex >= 0)
            {
                span = span[..(methodNameIndex + methodGenericTypeParametersIndex)];
            }

            foreach (var c in span)
            {
                result.Append(c is '#' or '{' or '}' or '`' or '@' ? '-' : c);
            }

            return result.ToString();
        }
    }

    internal static string GetUrlFragmentFromUid(string uid)
    {
        // Prettify URL fragment from UID:
        //
        // - Transform to lower case
        // - Transform special characters:
        //   - Map <> [] {} to () () (())
        //   - Keep a-z, 0-9, ()*@
        //   - Drop "'%^\
        //   - Replace anything else with dash
        // - Remove adjacent, leading or trailing dash
        var sb = new StringBuilder();
        for (var i = 0; i < uid.Length; i++)
        {
            var ch = char.ToLowerInvariant(uid[i]);
            switch (ch)
            {
                case '"' or '\'' or '%' or '^' or '\\':
                    continue;
                case '<' or '[':
                    sb.Append('(');
                    break;
                case '>' or ']':
                    sb.Append(')');
                    break;
                case '{':
                    sb.Append("((");
                    break;
                case '}':
                    sb.Append("))");
                    break;
                case char and (>= 'a' and <= 'z' or >= '0' and <= '9'):
                case '(' or ')' or '*' or '@':
                    sb.Append(ch);
                    break;
                default:
                    if (sb.Length == 0 || sb[^1] == '-')
                    {
                        continue;
                    }
                    sb.Append('-');
                    break;
            }
        }

        if (sb[^1] == '-')
        {
            for (var i = sb.Length - 1; i >= 0; i--)
            {
                if (sb[i] == '-')
                {
                    sb.Remove(i, 1);
                }
                else
                {
                    break;
                }
            }
        }

        return sb.ToString();
    }
}
