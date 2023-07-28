// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public class FolderRedirectionRule
{
    public FolderRedirectionRule(string from, string to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        From = from;
        To = to;
    }

    public string From { get; set; }

    public string To { get; set; }
}
