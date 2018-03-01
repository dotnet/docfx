// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Build.Engine;

    public class MetadataMergeParameters
    {
        public FileCollection Files { get; set; }
        public string OutputBaseDir { get; set; }
        public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;
        public FileMetadata FileMetadata { get; set; }
        public ImmutableList<string> TocMetadata { get; set; }
    }
}
