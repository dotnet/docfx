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
        /// The JSON schema applied to each property that matches a regular expression
        /// </summary>
        public Dictionary<string, JsonSchema> PatternProperties { get; set; } = new Dictionary<string, JsonSchema>();

        /// <summary>
        /// An object can have extra keys not defined in properties.
        /// This can be:
        ///     - boolean: allow/disallow additional properties
        ///     - object: the schema for the additional properties
        /// </summary>
        [JsonConverter(typeof(ValueOrObjectConverter))]
        public (bool value, JsonSchema schema) AdditionalProperties { get; set; } = (true, null);

        /// <summary>
        /// The JSON schema that applies to property names.
        /// </summary>
        public JsonSchema PropertyNames { get; set; }

        /// <summary>
        /// The maximum property count that an array can hold.
        /// </summary>
        public int? MaxProperties { get; set; }

        /// <summary>
        /// The minimum item count that an array can hold.
        /// </summary>
        public int? MinProperties { get; set; }

        /// <summary>
        /// The JSON schema that applies to the array items if the current value is array.
        /// </summary>
        public JsonSchema Items { get; set; }

        /// <summary>
        /// Whether an array contains this element.
        /// </summary>
        public JsonSchema Contains { get; set; }

        /// <summary>
        /// Whether each item in array must be unique.
        /// </summary>
        public bool UniqueItems { get; set; }

        /// <summary>
        /// The maximum item count that an array can hold.
        /// </summary>
        public int? MaxItems { get; set; }

        /// <summary>
        /// The minimum item count that an array can hold.
        /// </summary>
        public int? MinItems { get; set; }

        /// <summary>
        /// Current value must be this constant.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public JToken Const { get; set; }

        /// <summary>
        /// An array of valid values for the current value.
        /// </summary>
        public JToken[] Enum { get; set; }

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
        /// The inclusive maximum value of a number.
        /// </summary>
        public double? Maximum { get; set; }

        /// <summary>
        /// The inclusive minimum value of a number.
        /// </summary>
        public double? Minimum { get; set; }

        /// <summary>
        /// The exclusive maximum value of a number.
        /// </summary>
        public double? ExclusiveMaximum { get; set; }

        /// <summary>
        /// The exclusive minimum value of a number.
        /// </summary>
        public double? ExclusiveMinimum { get; set; }

        /// <summary>
        /// The number must be multiple of this value.
        /// </summary>
        public double MultipleOf { get; set; }

        /// <summary>
        /// The regular expression applied to strings
        /// </summary>
        public string Pattern { get; set; }

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
        public string[] Reserved { get; set; } = Array.Empty<string>();

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

        // JSON schema metadata validation extensions
        //-------------------------------------------

        /// <summary>
        /// Properties that are used to realize either logic
        /// </summary>
        public string[][] Either { get; set; } = Array.Empty<string[]>();

        /// <summary>
        /// Properties that are used to realize precludes logic
        /// </summary>
        public string[][] Precludes { get; set; } = Array.Empty<string[]>();

        /// <summary>
        /// Properties that are used to validate date format
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Properties that are used to set maximum time range
        /// </summary>
        public TimeSpan? RelativeMaxDate { get; set; }

        /// <summary>
        /// Properties that are used to set minimum time range
        /// </summary>
        public TimeSpan? RelativeMinDate { get; set; }

        /// <summary>
        /// Properties that are used to indicate the deprecated field
        /// </summary>
        public string ReplacedBy { get; set; }

        /// <summary>
        /// Properties that are used to indicate the value relationship between two fields
        /// Mapping relationship: enumDependencies --> <field-name> --> <dependent-field-name> --> <dependent-field-value> --> <allowed-field-values>
        /// </summary>
        public Dictionary<string, Dictionary<string, Dictionary<JToken, JValue[]>>> EnumDependencies { get; set; } = new Dictionary<string, Dictionary<string, Dictionary<JToken, JValue[]>>>();

        /// <summary>
        /// Properties that are used to validate microsoft alias
        /// </summary>
        public MicrosoftAliasSchema MicrosoftAlias { get; set; }

        // JSON schema metadata validation error extensions
        //-------------------------------------------

        /// <summary>
        /// This field is used to provide overwrite error information and only can be set in root level of schema
        /// </summary>
        public Dictionary<string, Dictionary<string, OverwriteErrorSchema>> OverwriteErrors { get; set; } = new Dictionary<string, Dictionary<string, OverwriteErrorSchema>>();
    }
}
