// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using System;
    using System.Collections.Generic;

    using Markdig.Syntax;

    [Serializable]
    public class MarkdownPropertyModel
    {
        public string PropertyName { get; set; }

        public Block PropertyNameSource { get; set; }

        public List<Block> PropertyValue { get; set; }
    }
}
