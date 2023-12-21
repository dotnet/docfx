// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Docfx.Common;

namespace Docfx;

class DocfxConfig
{
    public MetadataJsonConfig? metadata { get; init; }

    public MergeJsonConfig? merge { get; set; }

    public BuildJsonConfig? build { get; init; }

    public Dictionary<string, LogLevel>? rules { get; init; }
}
