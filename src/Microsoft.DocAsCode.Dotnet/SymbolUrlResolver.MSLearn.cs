using System.Text;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Microsoft.DocAsCode.Dotnet;

partial class SymbolUrlResolver
{
    private static readonly Lazy<HashSet<string>> s_msLearnAssemblies = new(LoadMSLearnAssemblies);

    public static string? GetMicrosoftLearnUrl(ISymbol symbol)
    {
        if (string.IsNullOrEmpty(symbol.ContainingAssembly?.Name) ||
            !s_msLearnAssemblies.Value.Contains(symbol.ContainingAssembly.Name))
        {
            return null;
        }

        return symbol.GetDocumentationCommentId() is { } commentId ?
            GetMicrosoftLearnUrl(commentId, symbol.IsEnumMember(), symbol.HasOverloads()) : null;
    }

    public static string GetMicrosoftLearnUrl(string commentId, bool isEnumMember, bool hasOverloads)
    {
        const string BaseUrl = "https://learn.microsoft.com/dotnet/api/";

        var parts = commentId.Split(':');
        var type = parts[0];
        var uid = parts[1].ToLowerInvariant();

        return type switch
        {
            "N" or "T" => $"{BaseUrl}{uid.Replace('`', '-')}",
            "F" when isEnumMember => $"{BaseUrl}{GetEnumMemberPathFromUid(uid)}#{GetUrlFragmentFromUid(uid)}",

            "M" or "F" or "P" or "E" => hasOverloads
                ? $"{BaseUrl}{GetMemberPathFromUid(uid)}#{GetUrlFragmentFromUid(uid)}"
                : $"{BaseUrl}{GetMemberPathFromUid(uid)}",

            _ => throw new NotSupportedException($"Unknown comment ID format '{type}"),
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
        // - Transform special charactors:
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
                case char c when (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'):
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

    private static HashSet<string> LoadMSLearnAssemblies()
    {
        var assembly = typeof(SymbolUrlResolver).Assembly;
        var path = $"{assembly.GetName().Name}.Resources.MSLearnAssemblies.txt";
        using var stream = assembly.GetManifestResourceStream(path);
        if (stream is null)
            return new();

        using var reader = new StreamReader(stream);
        var result = new HashSet<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith('/'))
                result.Add(line);
        }
        return result;
    }
}
