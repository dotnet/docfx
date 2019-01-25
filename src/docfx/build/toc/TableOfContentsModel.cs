// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Docs.Build
{
    public sealed class TableOfContentsModel
    {
        public TableOfContentsMetadata Metadata { get; set; } = new TableOfContentsMetadata();

        [MinLength(1)]
        public List<TableOfContentsItem> Items { get; set; } = new List<TableOfContentsItem>();
    }
}
