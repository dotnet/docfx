// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class DependencyGitLock
    {
        public string Commit { get; set; }

        public IReadOnlyDictionary<string, DependencyGitLock> Git { get; set; } = new Dictionary<string, DependencyGitLock>();
    }
}
