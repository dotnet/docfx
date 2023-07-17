// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Plugins;

public class TreeItemRestructure
{
    public string Key { get; set; }

    public TreeItemKeyType TypeOfKey { get; set; }

    public TreeItemActionType ActionType { get; set; }

    public IImmutableList<TreeItem> RestructuredItems { get; set; }

    /// <summary>
    /// Specifies which files trigger the restructure
    /// </summary>
    public IImmutableList<FileAndType> SourceFiles { get; set; }
}
