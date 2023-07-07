// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Build.Engine;

namespace Docfx;

internal class MetadataMergeParameters
{
    public FileCollection Files { get; set; }
    public string OutputBaseDir { get; set; }
    public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;
    public FileMetadata FileMetadata { get; set; }
    public ImmutableList<string> TocMetadata { get; set; }
}
