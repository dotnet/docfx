// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.TableOfContents;

internal sealed class TocItemInfo
{
    public TocItemViewModel Content { get; set; }
    public FileAndType File { get; }
    public bool IsResolved { get; set; }
    public bool IsReferenceToc { get; set; }

    public TocItemInfo(FileAndType file, TocItemViewModel item)
    {
        Content = item;
        File = file;
        IsResolved = false;
        IsReferenceToc = false;
    }
}
