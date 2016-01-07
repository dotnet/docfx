// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MetadataMergers
{
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.EntityModel.Builders;

    public class MetadataMergeParameters
    {
        public FileCollection Files { get; set; }
        public string OutputBaseDir { get; set; }
        public ImmutableDictionary<string, object> Metadata { get; set; } = ImmutableDictionary<string, object>.Empty;
        public FileMetadata FileMetadata { get; set; }
    }
}
