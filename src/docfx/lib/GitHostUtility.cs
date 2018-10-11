using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal static class GitHostUtility
    {
        public static bool TryParse(string remote, out GitHost githost)
        {
            githost = GitHost.Unknown;

            if (remote == null)
                return false;

            if (remote.StartsWith("https://github.com", StringComparison.OrdinalIgnoreCase) ||
                remote.StartsWith("http://github.com", StringComparison.OrdinalIgnoreCase))
            {
                githost = GitHost.GitHub;
                return true;
            }

            return false;
        }
    }
}
