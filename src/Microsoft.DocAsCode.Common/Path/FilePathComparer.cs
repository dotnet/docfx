// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;

    public class FilePathComparer
        : IEqualityComparer<string>
    {
        private readonly static StringComparer _stringComparer = GetStringComparer();

        public static readonly FilePathComparer OSPlatformSensitiveComparer = new FilePathComparer();
        public static readonly StringComparer OSPlatformSensitiveStringComparer = GetStringComparer();

        public bool Equals(string x, string y)
        {
            return _stringComparer.Equals(x.ToNormalizedFullPath(), y.ToNormalizedFullPath());
        }

        public int GetHashCode(string obj)
        {
            string path = obj.ToNormalizedFullPath();

            return _stringComparer.GetHashCode(path);
        }

        private static StringComparer GetStringComparer()
        {
            if (PathUtility.IsPathCaseInsensitive())
            {
                return StringComparer.OrdinalIgnoreCase;
            }
            return StringComparer.Ordinal;
        }
    }
}
