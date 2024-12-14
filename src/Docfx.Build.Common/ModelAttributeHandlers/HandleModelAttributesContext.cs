// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Common;

public class HandleModelAttributesContext
{
    internal int NestedLevel { get; set; } = 0;
    public IHostService Host { get; set; }
    public bool SkipMarkup { get; set; }
    public bool EnableContentPlaceholder { get; set; }
    public string PlaceholderContent { get; set; }
    public bool ContainsPlaceholder { get; set; }
    public HashSet<string> Dependency { get; set; } = [];
    public FileAndType FileAndType { get; set; }
    public HashSet<string> LinkToFiles { get; set; } = new(FilePathComparer.OSPlatformSensitiveStringComparer);
    public HashSet<string> LinkToUids { get; set; } = [];
    public List<UidDefinition> Uids { get; set; } = [];
    public Dictionary<string, List<LinkSourceInfo>> UidLinkSources { get; set; } = [];
    public Dictionary<string, List<LinkSourceInfo>> FileLinkSources { get; set; } = [];
}
