// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Plugins;

public class SaveResult
{
    public string DocumentType { get; set; }
    public string FileWithoutExtension { get; set; }
    public string ResourceFile { get; set; }
    public ImmutableHashSet<string> LinkToUids { get; set; } = [];
    public ImmutableArray<string> LinkToFiles { get; set; } = [];
    public ImmutableArray<XRefSpec> XRefSpecs { get; set; } = [];
    public ImmutableArray<XRefSpec> ExternalXRefSpecs { get; set; } = [];
    public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> UidLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;
    public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> FileLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;
}
