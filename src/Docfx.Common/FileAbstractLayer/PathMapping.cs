// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Common;

public readonly struct PathMapping
{
    public PathMapping(RelativePath logicalPath, string physicalPath)
    {
        ArgumentNullException.ThrowIfNull(logicalPath);
        ArgumentNullException.ThrowIfNull(physicalPath);

        LogicalPath = logicalPath.GetPathFromWorkingFolder();
        PhysicalPath = physicalPath;
    }

    public RelativePath LogicalPath { get; }

    public string PhysicalPath { get; }
}
