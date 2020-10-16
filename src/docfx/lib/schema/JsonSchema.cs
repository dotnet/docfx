// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    [JsonConverter(typeof(JsonSchemaConverter))]
    internal class JsonSchema
    {
        /// <summary>
        /// `true` is a valid boolean JSON schema. A `true` JSON schema is deserialized into this instance.
        /// </summary>
        public static readonly JsonSchema TrueSchema = new JsonSchema();

        /// <summary>
        /// `false` is a valid boolean JSON schema. A `false` JSON schema is deserialized into this instance.
        /// </summary>
        public static readonly JsonSchema FalseSchema = new JsonSchema();

        // A core subset of JSON schema
        //-------------------------------------------

        /// <summary>
        /// Json schema definitions
        /// </summary>
        public Dictionary<string, JsonSchema> Definitions { get; } = new Dictionary<string, JsonSchema>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Json schema ref pointer
        /// </summary>
        [JsonProperty("$ref")]
        public string? Ref { get; set; }

        /// <summary>
        /// Type of the current value.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public JsonSchemaType[]? Type { get; set; }

        /// <summary>
        /// Type of the render type.
        /// </summary>
        public RenderType RenderType { get; set; } = RenderType.Content;

        /// <summary>
        /// The JSON schema that applies to each property if the current value is object.
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
        public JsonSchema? AdditionalProperties { get; set; }

        /// <summary>
        /// The JSON schema that applies to property names.
        /// </summary>
        public JsonSchema? PropertyNames { get; set; }

        /// <summary>
        /// The maximum property count that an array can hold.
        /// </summary>
        public int? MaxProperties { get; set; }

        /// <summary>
        /// The minimum item count that an array can hold.
        /// </summary>
        public int? MinProperties { get; set; }

        /// <summary>
        /// The maximum reference count of current uid
        /// </summary>
        public int? MaxReferenceCount { get; set; }

        /// <summary>
        /// The minimum reference count of current uid
        /// </summary>
        public int? MinReferenceCount { get; set; }

        /// <summary>
        /// The JSON schema that applies to the array items if the current value is array.
        /// </summary>
        [JsonConverter(typeof(UnionTypeConverter))]
        public (JsonSchema? schema, JsonSchema[]? schemas) Items { get; set; }

        /// <summary>
        /// The JSON schema that applies to additional items of an array.
        /// </summary>
        public JsonSchema? AdditionalItems { get; set; }

        /// <summary>
        /// Whether an array contains this element.
        /// </summary>
        public JsonSchema? Contains { get; set; }

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
        public JToken? Const { get; set; }

        /// <summary>
        /// An array of valid values for the current value.
        /// </summary>
        public JToken[]? Enum { get; set; }

        /// <summary>
        /// The string format for the current value.
        /// </summary>
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
        public string? Pattern { get; set; }

        /// <summary>
        /// Properties that are required to be present.
        /// </summary>
        public string[] Required { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Properties that are used to indicate the dependencies between fields
        /// </summary>
        [JsonProperty(ItemConverterType = typeof(UnionTypeConverter))]
        public Dictionary<string, (string[] propertyNames, JsonSchema schema)> Dependencies { get; }
         = new Dictionary<string, (string[] propertyNames, JsonSchema schema)>();

        // JSON schema custom validation extensions
        //-------------------------------------------

        /// <summary>
        /// the mime type of schema
        /// </summary>
        public string? SchemaType { get; set; }

        /// <summary>
        /// Property indicate the type of xref
        /// </summary>
        public string? XrefType { get; set; }

        /// <summary>
        /// Property indicate which property will fallback to xrefType when SchemaType is null
        /// </summary>
        public string? SchemaTypeProperty { get; set; }

        /// <summary>
        /// Alternative name used in output HTML <meta> tag. If not set, the original metadata name is used. Does not have effect in sub schemas.
        /// </summary>
        public string? HtmlMetaName { get; set; }

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
        public JsonSchemaContentType? ContentType { get; set; }

        /// <summary>
        /// Properties that indicate the uid unique scope
        /// </summary>
        public bool UidGlobalUnique { get; set; }

        /// <summary>
        /// Properties that indicate whether the xref need to externally validate
        /// </summary>
        public bool ValidateExternalXrefs { get; set; }

        /// <summary>
        /// Properties that are built into xref map
        /// </summary>
        public HashSet<string> XrefProperties { get; } = new HashSet<string>();

        // JSON schema metadata validation extensions
        //-------------------------------------------

        /// <summary>
        /// Properties that are used to indicate some attitudes are required and the value of them can't be null or white space for string type
        /// </summary>
        public string[] StrictRequired { get; set; } = Array.Empty<string>();

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
        public string? DateFormat { get; set; }

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
        public string? ReplacedBy { get; set; }

        /// <summary>
        /// Properties that are used to indicate the value relationship between two fields
        /// </summary>
        public EnumDependenciesSchema? EnumDependencies { get; set; }

        /// <summary>
        /// Properties that are used to validate microsoft alias
        /// </summary>
        public MicrosoftAliasSchema? MicrosoftAlias { get; set; }

        /// <summary>
        /// Properties' value must be unique within the docset
        /// </summary>
        public HashSet<string> DocsetUnique { get; set; } = new HashSet<string>();

        /// <summary>
        /// Whether content fallback is allowed for loc page
        /// </summary>
        public bool ContentFallback { get; set; } = true;

        /// <summary>
        /// Properties that are used to hold min item count that meet condition.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public ConditionalCheckSchema[] MinItemsWhen { get; private set; } = Array.Empty<ConditionalCheckSchema>();

        /// <summary>
        /// Properties that are used to hold max item count that meet condition.
        /// </summary>
        [JsonConverter(typeof(OneOrManyConverter))]
        public ConditionalCheckSchema[] MaxItemsWhen { get; private set; } = Array.Empty<ConditionalCheckSchema>();

        // JSON schema metadata validation error extensions
        //-------------------------------------------

        /// <summary>
        /// This field is used to provide additional error information and only can be set in root level of schema
        /// </summary>
        public Dictionary<string, Dictionary<string, CustomRule>> Rules { get; } = new Dictionary<string, Dictionary<string, CustomRule>>();
    }
}
