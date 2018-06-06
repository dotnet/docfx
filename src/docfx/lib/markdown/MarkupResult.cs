// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal struct MarkupResult
    {
        public string Title;

        public bool HasHtml;

        public JObject Metadata;

        public List<DocfxException> Errors;
    }
}
