// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public class GroupInfo
{
    public string Name { get; set; }

    public string Destination { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = [];
}
