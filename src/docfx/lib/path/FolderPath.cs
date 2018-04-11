// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs
{
    /// <summary>
    /// Represents a normalized relative path to a folder that:
    ///
    ///  - Cannot start with '/'
    ///  - Always end with '/'
    ///
    ///  - All '\' are replaced with '/'
    ///  - Does not contain any consecutive '//'
    ///  - Does not contain any directory indicators including './' and '../'
    /// </summary>
    internal struct FolderPath : IEquatable<FolderPath>
    {
        private readonly string _value;

        public FolderPath(string value)
        {
            throw new NotImplementedException();
        }

        public static implicit operator FolderPath(string value) => new FolderPath(value);

        public static implicit operator string(FolderPath path) => path.ToString();

        public static bool operator ==(FolderPath left, FolderPath right)
            => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        public static bool operator !=(FolderPath left, FolderPath right)
            => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        public bool Equals(FolderPath other)
            => string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj)
            => obj is FolderPath && Equals((FolderPath)obj);

        public override int GetHashCode()
            => StringComparer.OrdinalIgnoreCase.GetHashCode(_value);

        public override string ToString() => _value;
    }
}
