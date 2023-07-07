// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public class TocInfo
{
    public string TocFileKey { get; }
    public string Homepage { get; set; }

    public TocInfo(string tocFileKey)
    {
        TocFileKey = tocFileKey;
    }
}
