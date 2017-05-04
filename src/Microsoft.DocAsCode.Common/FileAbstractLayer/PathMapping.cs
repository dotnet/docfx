// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Immutable;

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
}
