// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs
{
    /// <summary>
    /// Represents a normalized relative path to a file that:
    ///
    ///  - Cannot start with '/'
    ///  - Cannot end with '/'
    ///
    ///  - All '\' are replaced with '/'
    ///  - Does not contain any consecutive '//'
    ///  - Does not contain any directory indicators including './' and '../'
    /// </summary>
    internal struct FilePath : IEquatable<FilePath>
    {
        private readonly string _value;

        public FilePath(string value)
        {
            throw new NotImplementedException();
        }

        public static implicit operator FilePath(string value) => new FilePath(value);

        public static implicit operator string(FilePath path) => path.ToString();

        public static bool operator ==(FilePath left, FilePath right)
            => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        public static bool operator !=(FilePath left, FilePath right)
            => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        public bool Equals(FilePath other)
            => string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj)
            => obj is FilePath && Equals((FilePath)obj);

        public override int GetHashCode()
            => StringComparer.OrdinalIgnoreCase.GetHashCode(_value);

        public override string ToString() => _value;
    }
}
