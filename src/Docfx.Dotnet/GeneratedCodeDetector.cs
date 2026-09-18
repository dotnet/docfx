// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

#nullable enable

namespace Docfx.Dotnet;

internal static partial class GeneratedCodeDetector
{
    private static readonly Guid CSharpLanguage = new("3f5162f8-07c6-11d3-9053-00c04fa302a1");
    private static readonly Guid VisualBasicLanguage = new("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");

    public static bool IsGeneratedCodeAttribute(AttributeData attribute) =>
        attribute.AttributeClass?.ToDisplayString() == "System.CodeDom.Compiler.GeneratedCodeAttribute";

    public static bool HasGeneratedAttribute(ISymbol symbol) => symbol.GetAttributes().Any(IsGeneratedCodeAttribute);

    public static bool HasGeneratedAttribute(ISymbol symbol, SyntaxReference declaration)
    {
        for (var current = symbol; current is not null; current = current.ContainingType)
        {
            foreach (var attribute in current.GetAttributes().Where(IsGeneratedCodeAttribute))
            {
                if (attribute.ApplicationSyntaxReference is not { } attributeSyntax)
                    continue;

                // A marker on one partial declaration must not hide a member in another declaration.
                if (current.DeclaringSyntaxReferences.Any(parent =>
                    parent.SyntaxTree == declaration.SyntaxTree &&
                    parent.SyntaxTree == attributeSyntax.SyntaxTree &&
                    parent.Span.Contains(declaration.Span) &&
                    AttributeDeclarationSpan(parent).Contains(attributeSyntax.Span)))
                    return true;
            }
        }
        return false;
    }

    private static TextSpan AttributeDeclarationSpan(SyntaxReference declaration) => declaration.GetSyntax() switch
    {
        // Variable symbols point inside a field/event declaration; attributes belong to the enclosing declaration.
        CS.Syntax.VariableDeclaratorSyntax { Parent.Parent: CS.Syntax.BaseFieldDeclarationSyntax field } => field.Span,
        VB.Syntax.ModifiedIdentifierSyntax { Parent.Parent: VB.Syntax.FieldDeclarationSyntax field } => field.Span,
        _ => declaration.Span,
    };

    public static bool HasGeneratedHeader(SyntaxTree tree) =>
        HasGeneratedHeader(tree.GetRoot().GetLeadingTrivia(), tree.Options.Language);

    public static bool HasGeneratedHeader(string text, string language) => language switch
    {
        LanguageNames.CSharp => HasGeneratedHeader(CS.SyntaxFactory.ParseLeadingTrivia(text.TrimStart('\uFEFF')), language),
        LanguageNames.VisualBasic => HasGeneratedHeader(VB.SyntaxFactory.ParseLeadingTrivia(text.TrimStart('\uFEFF')), language),
        _ => false,
    };

    private static bool HasGeneratedHeader(SyntaxTriviaList trivia, string language) =>
        trivia.Any(item => CommentPrefixLength(item.RawKind, language) is > 0 and var length &&
            HeaderMarker().IsMatch(item.ToFullString().AsSpan(length)));

    private static int CommentPrefixLength(int kind, string language) => language switch
    {
        LanguageNames.CSharp => (CS.SyntaxKind)kind is CS.SyntaxKind.SingleLineCommentTrivia
            or CS.SyntaxKind.MultiLineCommentTrivia ? 2 : 0,
        LanguageNames.VisualBasic => (VB.SyntaxKind)kind is VB.SyntaxKind.CommentTrivia ? 1 : 0,
        _ => 0,
    };

    [GeneratedRegex(@"\A[\s*]*<auto-?generated\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HeaderMarker();

    public static bool HasGeneratedEmbeddedHeader(byte[] bytes, Guid language)
    {
        var languageName = language == CSharpLanguage ? LanguageNames.CSharp
            : language == VisualBasicLanguage ? LanguageNames.VisualBasic
            : null;
        if (languageName is null)
            return false;
        if (bytes.Length < sizeof(int))
            throw new InvalidDataException("Embedded source is missing its format header.");

        var size = BinaryPrimitives.ReadInt32LittleEndian(bytes);
        if (size < 0)
            throw new InvalidDataException($"Unsupported embedded-source format: {size}.");

        using var content = new MemoryStream(bytes, sizeof(int), bytes.Length - sizeof(int), writable: false);
        using var expanded = new MemoryStream();
        if (size > 0)
        {
            using var deflate = new DeflateStream(content, CompressionMode.Decompress);
            var buffer = new byte[Math.Min(size, 81920)];
            var remaining = size;
            while (remaining > 0)
            {
                var count = deflate.Read(buffer, 0, Math.Min(buffer.Length, remaining));
                if (count == 0)
                    throw new InvalidDataException("Embedded source is shorter than its declared size.");
                expanded.Write(buffer, 0, count);
                remaining -= count;
            }
            if (deflate.ReadByte() != -1)
                throw new InvalidDataException("Embedded source exceeds its declared size.");
            expanded.Position = 0;
        }

        using var reader = new StreamReader(size == 0 ? content : expanded,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true);
        return HasGeneratedHeader(reader.ReadToEnd(), languageName);
    }
}
