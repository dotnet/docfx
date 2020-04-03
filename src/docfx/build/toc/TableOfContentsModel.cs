// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsModel
    {
        public TableOfContentsMetadata Metadata { get; }

        public TableOfContentsNode[] Items { get; }

        public TableOfContentsModel(TableOfContentsNode[] items, TableOfContentsMetadata metadata)
        {
            Items = items;
            Metadata = metadata;
        }
    }
}
