// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class SaveResult
    {
        public string DocumentType { get; set; }
        public string FileWithoutExtension { get; set; }
        public string ResourceFile { get; set; }
        public ImmutableHashSet<string> LinkToUids { get; set; } = ImmutableHashSet<string>.Empty;
        public ImmutableArray<string> LinkToFiles { get; set; } = ImmutableArray<string>.Empty;
        [Obsolete("use DocumentBuildContext.TocMap")]
        public ImmutableDictionary<string, HashSet<string>> TocMap { get; set; } = ImmutableDictionary<string, HashSet<string>>.Empty;
        public ImmutableArray<XRefSpec> XRefSpecs { get; set; } = ImmutableArray<XRefSpec>.Empty;
        public ImmutableArray<XRefSpec> ExternalXRefSpecs { get; set; } = ImmutableArray<XRefSpec>.Empty;
        public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> UidLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;
        public ImmutableDictionary<string, ImmutableList<LinkSourceInfo>> FileLinkSources { get; set; } = ImmutableDictionary<string, ImmutableList<LinkSourceInfo>>.Empty;
    }
}
