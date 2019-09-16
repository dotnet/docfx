// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public class DependencyGitLock
    {
        public string Url { get; set; }

        public string Branch { get; set; }

        public string Commit { get; set; }
    }
}
