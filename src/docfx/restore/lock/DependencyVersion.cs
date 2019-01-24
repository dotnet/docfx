// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class DependencyVersion
    {
        public string Hash { get; set; }

        public string Commit { get; set; }

        public DependencyVersion(string commit = null, string hash = null)
        {
            Commit = commit;
            Hash = hash;
        }
    }
}
