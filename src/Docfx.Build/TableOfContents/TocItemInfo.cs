// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.Common;
using Docfx.Plugins;

namespace Docfx.Build.TableOfContents;

class TocItemInfo
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
