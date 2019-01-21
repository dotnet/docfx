// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class DependencyLock : DependencyVersion
    {
        public Dictionary<string, DependencyLock> Git { get; set; } = new Dictionary<string, DependencyLock>();

        public Dictionary<string, DependencyVersion> Downloads { get; set; } = new Dictionary<string, DependencyVersion>();

        public DependencyLock GetGitLock(string href, string branch)
        {
            if (Git.TryGetValue($"{href}#{branch}", out var dependencyLock))
            {
                return dependencyLock;
            }

            if (branch == "master" && Git.TryGetValue($"{href}", out dependencyLock))
            {
                return dependencyLock;
            }

            return null;
        }

        public bool ContainsGitLock(string href)
        {
            return Git.ContainsKey(href) || Git.Keys.Any(g => g.StartsWith($"{href}#"));
        }
    }
}
