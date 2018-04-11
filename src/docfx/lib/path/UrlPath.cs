// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs
{
    /// <summary>
    /// Represents a normalized relative url path that:
    ///
    ///  - Always start with '/'
    ///  - May or may not end with '/'
    ///
    ///  - All '\' are replaced with '/'
    ///  - Does not contain any consecutive '//'
    ///  - Does not contain any directory indicators including './' and '../'
    /// </summary>
    internal struct UrlPath : IEquatable<UrlPath>
    {
        private readonly string _value;

        public UrlPath(string value)
        {
            throw new NotImplementedException();
        }

        public static implicit operator UrlPath(string value) => new UrlPath(value);

        public static implicit operator string(UrlPath path) => path.ToString();

        public static bool operator ==(UrlPath left, UrlPath right)
            => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        public static bool operator !=(UrlPath left, UrlPath right)
            => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        public bool Equals(UrlPath other)
            => string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj)
            => obj is UrlPath && Equals((UrlPath)obj);

        public override int GetHashCode()
            => StringComparer.OrdinalIgnoreCase.GetHashCode(_value);

        public override string ToString() => _value;
    }
}
