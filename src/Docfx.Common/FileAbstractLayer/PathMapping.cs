// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.Common;

public struct PathMapping
{
    public PathMapping(RelativePath logicalPath, string physicalPath)
    {
        if (logicalPath == null)
        {
            throw new ArgumentNullException(nameof(logicalPath));
        }
        LogicalPath = logicalPath.GetPathFromWorkingFolder();
        PhysicalPath = physicalPath ?? throw new ArgumentNullException(nameof(physicalPath));
        AllowMoveOut = false;
        Properties = ImmutableDictionary<string, string>.Empty;
    }

    public RelativePath LogicalPath { get; }

    public string PhysicalPath { get; }

    public bool IsFolder => LogicalPath.FileName == string.Empty;

    public bool AllowMoveOut { get; set; }

    public ImmutableDictionary<string, string> Properties { get; set; }
}
