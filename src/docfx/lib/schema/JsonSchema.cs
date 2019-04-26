// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchema
    {
        // A core subset of JSON schema
        //-------------------------------------------

        /// <summary>
        /// Type of the current value.
        /// </summary>
        public JsonSchemaType Type { get; set; }

        /// <summary>
        /// The JSON schema that applies to each property if the currnet value is object.
        /// </summary>
        public Dictionary<string, JsonSchema> Properties { get; } = new Dictionary<string, JsonSchema>();

        /// <summary>
        /// The JSON schema that applies to the array items if the current value is array.
        /// </summary>
        public JsonSchema Items { get; set; }

        /// <summary>
        /// An array of valid values for the current value.
        /// </summary>
        public List<JValue> Enum { get; set; } = new List<JValue>();

        // JSON schema custom extensions
        //-------------------------------------------

        /// <summary>
        /// Whether this metadata should show in output HTML <meta> tag. The default value is true.
        /// </summary>
        public bool HtmlMetadata { get; set; } = true;

        /// <summary>
        /// Alternative name used in output HTML <meta> tag. If not set, the original metadata name is used.
        /// </summary>
        public string HtmlMetadataName { get; set; }

        /// <summary>
        /// Properties that are reserved by the system.
        /// </summary>
        public string[] Reserved { get; set; } = Array.Empty<string>();
    }
}
