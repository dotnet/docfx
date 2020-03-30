// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal sealed class PathStringComparer : IComparer<PathString>
    {
        private enum PathStringComparison
        {
            PathValue = 0,
            PathLengthThenValue = 1,
        }

        private PathStringComparison _pathStringComparison;

        public static readonly PathStringComparer Value = new PathStringComparer(PathStringComparison.PathValue);
        public static readonly PathStringComparer LengthThenValue = new PathStringComparer(PathStringComparison.PathLengthThenValue);

        private PathStringComparer(PathStringComparison pathStringComparison)
        {
            _pathStringComparison = pathStringComparison;
        }

        public int Compare([AllowNull] PathString x, [AllowNull] PathString y)
        {
            var result = 0;
            if (_pathStringComparison == PathStringComparison.PathLengthThenValue)
            {
                result = x.Value.Split('/').Where(v => !string.IsNullOrEmpty(v)).Count() -
                         y.Value.Split('/').Where(v => !string.IsNullOrEmpty(v)).Count();
            }
            if (result == 0)
                result = x.CompareTo(y);
            return result;
        }
    }
}
