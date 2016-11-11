// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility
{
    using System;
    using System.Collections.Generic;

    [Obsolete]
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

            return _stringComparer.GetHashCode(obj);
        }

        private static StringComparer GetStringComparer()
        {
            if (Environment.OSVersion.Platform < PlatformID.Unix)
            {
                return StringComparer.OrdinalIgnoreCase;
            }
            else
            {
                return StringComparer.Ordinal;
            }
        }
    }
}
