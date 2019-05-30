// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class JsonSchema
    {
        // A core subset of JSON schema
        //-------------------------------------------

        /// <summary>
        /// Json schema defnitions
        /// </summary>
        public Dictionary<string, JsonSchema> Definitions { get; } = new Dictionary<string, JsonSchema>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Json schema ref pointer
        /// </summary>
        [JsonProperty("$ref")]
        public string Ref { get; set; }

        /// <summary>
        /// Type of the current value.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public JsonSchemaType[] Type { get; set; }

        /// <summary>
        /// The JSON schema that applies to each property if the currnet value is object.
        /// </summary>
        public Dictionary<string, JsonSchema> Properties { get; } = new Dictionary<string, JsonSchema>();

        /// <summary>
        /// The JSON schema that applies to the array items if the current value is array.
        /// </summary>
        public JsonSchema Items { get; set; }

        /// <summary>
        /// The maximum item count that an array can hold.
        /// </summary>
        public int? MaxItems { get; set; }

        /// <summary>
        /// The minimum item count that an array can hold.
        /// </summary>
        public int? MinItems { get; set; }

        /// <summary>
        /// An array of valid values for the current value.
        /// </summary>
        public JValue[] Enum { get; set; }

        public JsonSchemaStringFormat Format { get; set; }

        /// <summary>
        /// The maximum length of a string.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// The minimum length of a string.
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// An object can have extra keys not defined in properties.
        /// This can be:
        ///     - boolean: allow/disallow additional properties
        ///     - object: the schema for the additional properties
        /// </summary>
        [JsonConverter(typeof(ValueOrObjectConverter))]
        public (bool additionalProperties, JsonSchema additionalPropertyJsonSchema) AdditionalProperties { get; set; } = (true, null);

        /// <summary>
        /// Properties that are required to be present.
        /// </summary>
        public string[] Required { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Properties that are used to indicate the dependencies between fields
        /// </summary>
        public Dictionary<string, string[]> Dependencies { get; set; } = new Dictionary<string, string[]>();

        // JSON schema custom validation extensions
        //-------------------------------------------

        /// <summary>
        /// Alternative name used in output HTML <meta> tag. If not set, the original metadata name is used. Does not have effect in sub schemas.
        /// </summary>
        public string HtmlMetaName { get; set; }

        /// <summary>
        /// Properties that are hidden in output HTML <meta> tag. Does not have effect in sub schemas.
        /// </summary>
        public string[] HtmlMetaHidden { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Properties that are reserved by the system.
        /// </summary>
        public HashSet<string> Reserved { get; set; } = new HashSet<string>();

        // JSON schema custom transform extensions
        //--------------------------------------------

        /// <summary>
        /// Properties that are transformed using specified pipeline like 'markup'
        /// </summary>
        public JsonSchemaContentType ContentType { get; set; }

        /// <summary>
        /// Properties that are built into xref map
        /// </summary>
        public string[] XrefProperties { get; set; } = Array.Empty<string>();
    }
}
