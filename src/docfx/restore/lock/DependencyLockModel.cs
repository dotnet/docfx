// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class DependencyLockModel : DependencyVersion
    {
        public IReadOnlyDictionary<string, DependencyLockModel> Git { get; set; } = new Dictionary<string, DependencyLockModel>();

        public IReadOnlyDictionary<string, DependencyVersion> Downloads { get; set; } = new Dictionary<string, DependencyVersion>();

        public DependencyLockModel()
        {
        }

        public DependencyLockModel(IReadOnlyDictionary<string, DependencyLockModel> gitVersions, IReadOnlyDictionary<string, DependencyVersion> downloads, DependencyVersion version = null)
            : this(gitVersions, downloads, version?.Commit, version?.Hash)
        {
        }

        public DependencyLockModel(IReadOnlyDictionary<string, DependencyLockModel> gitVersions, IReadOnlyDictionary<string, DependencyVersion> downloads, string commit, string hash)
            : base(commit, hash)
        {
            Debug.Assert(gitVersions != null);
            Debug.Assert(downloads != null);

            Git = gitVersions.OrderBy(g => g.Key).ToDictionary(k => k.Key, v => v.Value);
            Downloads = downloads.OrderBy(d => d.Key).ToDictionary(k => k.Key, v => v.Value);
        }
    }
}
