// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    public class ResolverContext
    {
        public bool PreserveRawInlineComments { get; set; }

        public Dictionary<string, ReferenceItem> References { get; set; }

        public Dictionary<string, MetadataItem> Members { get; set; }
    }
}
