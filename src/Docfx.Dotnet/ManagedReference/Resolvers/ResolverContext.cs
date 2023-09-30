// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Dotnet;

internal class ResolverContext
{
    public Dictionary<string, ReferenceItem> References { get; set; }

    public Dictionary<string, MetadataItem> Members { get; set; }
}
