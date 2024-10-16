// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Glob;

namespace Docfx.Build.Engine;

public sealed class FileMetadata : Dictionary<string, ImmutableArray<FileMetadataItem>>
{
    public string BaseDir { get; }

    public FileMetadata(string baseDir)
    {
        BaseDir = baseDir;
    }

    public FileMetadata(string baseDir, IDictionary<string, ImmutableArray<FileMetadataItem>> dictionary) : base(dictionary)
    {
        BaseDir = baseDir;
    }

    public IEnumerable<GlobMatcher> GetAllGlobs()
    {
        return this.SelectMany(r => r.Value).Select(r => r.Glob).Distinct();
    }
}
