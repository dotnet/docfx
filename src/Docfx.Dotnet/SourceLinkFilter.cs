// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Glob;

#nullable enable

namespace Docfx.Dotnet;

internal sealed class SourceLinkFilter
{
    private readonly GlobMatcher[] _exclude;

    public SourceLinkFilter(IEnumerable<string>? exclude)
    {
        _exclude = exclude?.Select(pattern =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pattern, "sourceLinkExclude");
            // Document paths can contain hidden build directories even when the source file is not hidden.
            return new GlobMatcher(pattern, GlobMatcher.DefaultOptions | GlobMatcherOptions.AllowDotMatch);
        }).ToArray() ?? [];
    }

    public bool IsExcluded(string documentPath) => _exclude.Any(pattern => pattern.Match(documentPath));
}
