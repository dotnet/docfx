// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Plugins;

public class SaveResult
{
    public string DocumentType { get; set; }
    public string FileWithoutExtension { get; set; }
    public string ResourceFile { get; set; }
    public ImmutableHashSet<string> LinkToUids { get; set; } = ImmutableHashSet<string>.Empty;
    public ImmutableArray<string> LinkToFiles { get; set; } = ImmutableArray<string>.Empty;
    [Obsolete("use DocumentBuildContext.TocMap")]
    public ImmutableDictionary<string, HashSet<string>> TocMap { get; set; } = ImmutableDictionary<string, HashSet<string>>.Empty;
    public ImmutableArray<XRefSpec> XRefSpecs { get; set; } = ImmutableArray<XRefSpec>.Empty;
    public ImmutableArray<XRefSpec> ExternalXRefSpecs { get; set; } = ImmutableArray<XRefSpec>.Empty;
    public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> UidLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;
    public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> FileLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;
}
