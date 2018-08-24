// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class MarkupResult
    {
        public string HtmlTitle = "";

        public bool HasHtml;

        public JObject Metadata;

        public List<Error> Errors = new List<Error>();

        public bool FirstBlockIsInclusionBlock = false;

        public bool HasTitle => !string.IsNullOrEmpty(HtmlTitle);
    }
}
