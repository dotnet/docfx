// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.YamlSplitter.Models
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Build.OverwriteDocuments;

    public class FragmentFile
    {
        public Dictionary<string, MarkdownFragment> FragmentsByUid { get; set; }

        public string FilePath { get; set; }
    }
}
