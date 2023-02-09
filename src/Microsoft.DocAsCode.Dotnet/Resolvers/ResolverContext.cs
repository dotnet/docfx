// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dotnet
{
    using System.Collections.Generic;

    internal class ResolverContext
    {
        public bool PreserveRawInlineComments { get; set; }

        public Dictionary<string, ReferenceItem> References { get; set; }

        public Dictionary<string, MetadataItem> Members { get; set; }
    }
}
