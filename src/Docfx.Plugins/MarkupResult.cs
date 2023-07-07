// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Plugins;

public class MarkupResult
{
    public string Html { get; set; }
    public ImmutableDictionary<string, object> YamlHeader { get; set; } = ImmutableDictionary<string, object>.Empty;
    public ImmutableArray<string> LinkToFiles { get; set; } = ImmutableArray<string>.Empty;
    public ImmutableHashSet<string> LinkToUids { get; set; } = ImmutableHashSet<string>.Empty;
    public ImmutableArray<string> Dependency { get; set; } = ImmutableArray<string>.Empty;
    public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> UidLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;
    public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> FileLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;

    public MarkupResult Clone()
    {
        return (MarkupResult)MemberwiseClone();
    }
}
