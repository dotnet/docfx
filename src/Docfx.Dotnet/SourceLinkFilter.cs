// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Docfx.Glob;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Docfx.Dotnet;

internal sealed class SourceLinkFilter
{
    private readonly GlobMatcher[] _exclude;
    private readonly ConcurrentDictionary<SyntaxTree, bool> _generatedTrees = new();

    public bool ExcludeGenerated { get; }

    public SourceLinkFilter(IEnumerable<string>? exclude, bool excludeGenerated = false)
    {
        ExcludeGenerated = excludeGenerated;
        _exclude = exclude?.Select(pattern =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pattern, "sourceLinkExclude");
            // Document paths can contain hidden build directories even when the source file is not hidden.
            return new GlobMatcher(pattern, GlobMatcher.DefaultOptions | GlobMatcherOptions.AllowDotMatch);
        }).ToArray() ?? [];
    }

    public bool IsExcluded(string documentPath) => _exclude.Any(pattern => pattern.Match(documentPath));

    public bool IsExcluded(ISymbol symbol, SyntaxReference declaration) =>
        IsExcluded(declaration.SyntaxTree.FilePath) ||
        (ExcludeGenerated && (GeneratedCodeDetector.HasGeneratedAttribute(symbol, declaration) ||
            _generatedTrees.GetOrAdd(declaration.SyntaxTree, GeneratedCodeDetector.HasGeneratedHeader)));
}
