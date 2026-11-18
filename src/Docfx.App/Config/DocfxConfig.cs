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

    /// <summary>
    /// The assemblies whose APIs carry their assembly in their UID, to disambiguate assemblies that share
    /// namespaces. It lives here rather than inside a `metadata` entry because it applies to the whole
    /// project: an entry mints UIDs for the APIs it references as well as the ones it documents, so all
    /// entries have to agree on which assemblies are qualified.
    /// </summary>
    public AssemblyUidConfig? assemblyUids { get; init; }
}
