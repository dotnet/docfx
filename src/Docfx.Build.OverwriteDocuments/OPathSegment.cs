// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.OverwriteDocuments;

public class OPathSegment
{
    public string SegmentName { get; set; }

    public string Key { get; set; }

    public string Value { get; set; }

    public string OriginalSegmentString { get; set; }
}
