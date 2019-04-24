// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchema
    {
        public JTokenType Type { get; set; }

        public Dictionary<string, JsonSchema> Properties { get; } = new Dictionary<string, JsonSchema>();

        public JsonSchema Items { get; set; }

        public List<JValue> Enum { get; set; } = new List<JValue>();

        /// <summary>
        /// Determines whether the property should show in HTML meta tags
        /// </summary>
        public bool IsHtmlMeta { get; set; }

        /// <summary>
        /// Name shown in HTML meta tags
        /// </summary>
        public string HtmlMetaName { get; set; }
    }
}
