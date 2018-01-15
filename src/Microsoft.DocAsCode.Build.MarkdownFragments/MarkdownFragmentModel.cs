// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.MarkdownFragments
{
    using System;
    using System.Collections.Generic;

    using Markdig.Syntax;

    [Serializable]
    public class MarkdownFragmentModel
    {
        public string Uid { get; set; }

        public string YamlCodeBlockHeader { get; set; }

        public Dictionary<string, List<Block>> Contents { get; set; }
    }
}
