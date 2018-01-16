// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.MarkdownFragments
{
    using System.Collections.Generic;

    using Markdig.Syntax;

    public class MarkdownPropertyModel
    {
        string PropertyName { get; set; }

        Block PropertyNameSource { get; set; }

        List<Block> PropertyValue { get; set; }
    }
}
