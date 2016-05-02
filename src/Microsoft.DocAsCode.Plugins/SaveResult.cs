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

        /// <summary>
        /// Model file should not be in-use, will remove in v1.9
        /// </summary>
        [Obsolete]
        public string ModelFile { get; set; }
        public string FileWithoutExtension { get; set; }
        public string ResourceFile { get; set; }
        public ImmutableHashSet<string> LinkToUids { get; set; } = ImmutableHashSet<string>.Empty;
        public ImmutableArray<string> LinkToFiles { get; set; } = ImmutableArray<string>.Empty;
        public ImmutableDictionary<string, HashSet<string>> TocMap { get; set; } = ImmutableDictionary<string, HashSet<string>>.Empty;
        public ImmutableArray<XRefSpec> XRefSpecs { get; set; } = ImmutableArray<XRefSpec>.Empty;
        public ImmutableArray<XRefSpec> ExternalXRefSpecs { get; set; } = ImmutableArray<XRefSpec>.Empty;
    }
}
