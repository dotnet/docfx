// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Immutable;

    public struct PathMapping : IEquatable<PathMapping>
    {
        public PathMapping(RelativePath logicalPath, string physicalPath)
        {
            if (logicalPath == null)
            {
                throw new ArgumentNullException(nameof(logicalPath));
            }
            if (physicalPath == null)
            {
                throw new ArgumentNullException(nameof(physicalPath));
            }
            LogicalPath = logicalPath.GetPathFromWorkingFolder();
            PhysicalPath = physicalPath;
            AllowMoveOut = false;
            Properties = ImmutableDictionary<string, string>.Empty;
        }

        public RelativePath LogicalPath { get; }

        public string PhysicalPath { get; }

        public bool IsFolder => LogicalPath.FileName == string.Empty;

        public bool AllowMoveOut { get; set; }

        public ImmutableDictionary<string, string> Properties { get; set; }

        #region Equals & operators

        public override int GetHashCode()
        {
            return LogicalPath.GetHashCode() ^ PhysicalPath.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is PathMapping)
            {
                return Equals((PathMapping)obj);
            }
            return false;
        }

        public bool Equals(PathMapping other)
        {
            return LogicalPath == other.LogicalPath &&
                PhysicalPath == other.PhysicalPath &&
                AllowMoveOut == other.AllowMoveOut &&
                Properties == other.Properties;
        }

        public static bool operator ==(PathMapping left, PathMapping right) =>
            left.Equals(right);

        public static bool operator !=(PathMapping left, PathMapping right) =>
            !left.Equals(right);

        #endregion

    }
}
