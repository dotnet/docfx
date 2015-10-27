// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class SaveResult
    {
        public string DocumentType { get; set; }
        public string ModelFile { get; set; }
        public string ResourceFile { get; set; }
        public ImmutableArray<string> LinkToUids { get; set; } = ImmutableArray<string>.Empty;
        public ImmutableArray<string> LinkToFiles { get; set; } = ImmutableArray<string>.Empty;
        public ImmutableDictionary<string, HashSet<string>> TocMap { get; set; } = ImmutableDictionary<string, HashSet<string>>.Empty;
        public ImmutableArray<XRefSpec> XRefSpecs { get; set; } = ImmutableArray<XRefSpec>.Empty;
    }
}
