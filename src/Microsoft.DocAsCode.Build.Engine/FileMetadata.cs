// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Glob;

    using Newtonsoft.Json;

    public sealed class FileMetadata : Dictionary<string, ImmutableArray<FileMetadataItem>>
    {
        public string BaseDir { get; }

        public FileMetadata(string baseDir) : base()
        {
            BaseDir = baseDir;
        }

        public FileMetadata(string baseDir, IDictionary<string, ImmutableArray<FileMetadataItem>> dictionary) : base(dictionary)
        {
            BaseDir = baseDir;
        }

        public IEnumerable<GlobMatcher> GetAllGlobs()
        {
            return this.SelectMany(r => r.Value).Select(r => r.Glob).Distinct();
        }
    }
}
