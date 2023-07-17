// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Dotnet;

internal class MetadataModel
{
    public MetadataItem TocYamlViewModel { get; set; }
    public List<MetadataItem> Members { get; set; }
}
