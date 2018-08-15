// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    // TODO:
    //   Conceptual model is more than just an html string, it also contain other properties.
    //   We currently bake these other properties into PageModel to make the output flat,
    //   but it makes conceptual kinda special. We may consider lift them outside PageModel.
    internal class Conceptual
    {
        public string Html { get; set; }

        public string Title { get; set; }

        public string HtmlTitle { get; set; }

        public long WordCount { get; set; }
    }
}
