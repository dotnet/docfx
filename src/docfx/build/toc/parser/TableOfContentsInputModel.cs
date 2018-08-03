// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal sealed class TableOfContentsInputModel
    {
        public JObject Metadata { get; set; }

        [MinLength(1)]
        public List<TableOfContentsInputItem> Items { get; set; } = new List<TableOfContentsInputItem>();
    }
}
