using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Docfx.Dotnet;

enum SymbolUrlKind
{
    Html,
    Markdown,
}

internal static partial class SymbolUrlResolver
{
    public static string? GetSymbolUrl(ISymbol symbol, Compilation compilation, MemberLayout memberLayout, SymbolUrlKind urlKind, HashSet<IAssemblySymbol> allAssemblies)
    {
        return GetDocfxUrl(symbol, memberLayout, urlKind, allAssemblies)
            ?? GetMicrosoftLearnUrl(symbol)
            ?? GetPdbSourceLinkUrl(compilation, symbol);
    }

    internal static string? GetDocfxUrl(ISymbol symbol, MemberLayout memberLayout, SymbolUrlKind urlKind, HashSet<IAssemblySymbol> allAssemblies)
    {
        if (symbol.ContainingAssembly is null || !allAssemblies.Contains(symbol.ContainingAssembly))
            return null;

        var commentId = symbol.GetDocumentationCommentId();
        if (commentId is null)
            return null;

        var parts = commentId.Split(':');
        var type = parts[0];
        var uid = parts[1];
        var ext = urlKind switch
        {
            SymbolUrlKind.Markdown => ".md",
            _ => ".html",
        };

        return type switch
        {
            "!" => null,
            "N" or "T" => $"{uid.Replace('`', '-')}{ext}",
            "M" or "F" or "P" or "E" => memberLayout is MemberLayout.SeparatePages && !symbol.IsEnumMember()
                ? $"{VisitorHelper.FileNameId(VisitorHelper.GetOverloadId(symbol))}{ext}#{Regex.Replace(uid, @"/\W/", "_")}"
                : $"{VisitorHelper.FileNameId(VisitorHelper.GetId(symbol.ContainingType))}{ext}#{Regex.Replace(uid, @"/\W/", "_")}",
            _ => throw new NotSupportedException($"Unknown comment ID format '{type}'"),
        };
    }
}
