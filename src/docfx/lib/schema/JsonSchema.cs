// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

[JsonConverter(typeof(JsonSchemaConverter))]
internal class JsonSchema
{
    /// <summary>
    /// `true` is a valid boolean JSON schema. A `true` JSON schema is deserialized into this instance.
    /// </summary>
    public static readonly JsonSchema TrueSchema = new();

    /// <summary>
    /// `false` is a valid boolean JSON schema. A `false` JSON schema is deserialized into this instance.
    /// </summary>
    public static readonly JsonSchema FalseSchema = new();

    /// <summary>
    /// Gets the JsonSchemaResolver to resolve $ref.
    /// </summary>
    [JsonIgnore]
    public JsonSchemaResolver SchemaResolver { get; internal set; } = JsonSchemaResolver.Null;

    // A core subset of JSON schema
    //-------------------------------------------

    /// <summary>
    /// Json schema $ref pointer
    /// </summary>
    [JsonProperty("$ref")]
    public string? Ref { get; init; }

    /// <summary>
    /// Json schema $id
    /// </summary>
    [JsonProperty("$id")]
    public string? Id { get; init; }

    /// <summary>
    /// Type of the current value.
    /// </summary>
    [JsonConverter(typeof(OneOrManyConverter))]
    public JsonSchemaType[]? Type { get; init; }

    /// <summary>
    /// Type of the render type.
    /// </summary>
    public RenderType RenderType { get; init; } = RenderType.Content;

    /// <summary>
    /// The JSON schema that applies to each property if the current value is object.
    /// </summary>
    public Dictionary<string, JsonSchema> Properties { get; } = new Dictionary<string, JsonSchema>();

    /// <summary>
    /// The JSON schema applied to each property that matches a regular expression
    /// </summary>
    public Dictionary<string, JsonSchema> PatternProperties { get; init; } = new Dictionary<string, JsonSchema>();

    /// <summary>
    /// An object can have extra keys not defined in properties.
    /// This can be:
    ///     - boolean: allow/disallow additional properties
    ///     - object: the schema for the additional properties
    /// </summary>
    public JsonSchema? AdditionalProperties { get; init; }

    /// <summary>
    /// The JSON schema that applies to property names.
    /// </summary>
    public JsonSchema? PropertyNames { get; init; }

    /// <summary>
    /// The maximum property count that an array can hold.
    /// </summary>
    public int? MaxProperties { get; init; }

    /// <summary>
    /// The minimum item count that an array can hold.
    /// </summary>
    public int? MinProperties { get; init; }

    /// <summary>
    /// The maximum reference count of current uid
    /// </summary>
    public int? MaxReferenceCount { get; init; }

    /// <summary>
    /// The minimum reference count of current uid
    /// </summary>
    public int? MinReferenceCount { get; init; }

    /// <summary>
    /// The JSON schema that applies to the array items if the current value is array.
    /// </summary>
    [JsonConverter(typeof(UnionTypeConverter))]
    public (JsonSchema? allItems, JsonSchema[]? eachItem) Items { get; init; }

    /// <summary>
    /// The JSON schema that applies to additional items of an array.
    /// </summary>
    public JsonSchema? AdditionalItems { get; init; }

    /// <summary>
    /// Whether an array contains this element.
    /// </summary>
    public JsonSchema? Contains { get; init; }

    /// <summary>
    /// Whether each item in array must be unique.
    /// </summary>
    public bool UniqueItems { get; init; }

    /// <summary>
    /// The maximum item count that an array can hold.
    /// </summary>
    public int? MaxItems { get; init; }

    /// <summary>
    /// The minimum item count that an array can hold.
    /// </summary>
    public int? MinItems { get; init; }

    /// <summary>
    /// Current value must be this constant.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public JToken? Const { get; init; }

    /// <summary>
    /// An array of valid values for the current value.
    /// </summary>
    public JToken[]? Enum { get; init; }

    /// <summary>
    /// The string format for the current value.
    /// </summary>
    public JsonSchemaStringFormat Format { get; init; }

    /// <summary>
    /// The maximum length of a string.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// The minimum length of a string.
    /// </summary>
    public int? MinLength { get; init; }

    /// <summary>
    /// The inclusive maximum value of a number.
    /// </summary>
    public double? Maximum { get; init; }

    /// <summary>
    /// The inclusive minimum value of a number.
    /// </summary>
    public double? Minimum { get; init; }

    /// <summary>
    /// The exclusive maximum value of a number.
    /// </summary>
    public double? ExclusiveMaximum { get; init; }

    /// <summary>
    /// The exclusive minimum value of a number.
    /// </summary>
    public double? ExclusiveMinimum { get; init; }

    /// <summary>
    /// The number must be multiple of this value.
    /// </summary>
    public double MultipleOf { get; init; }

    /// <summary>
    /// The regular expression applied to strings
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Properties that are required to be present.
    /// </summary>
    public string[] Required { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Split from the dependencies in draft 2019-09
    /// </summary>
    [JsonProperty(ItemConverterType = typeof(UnionTypeConverter))]
    public Dictionary<string, (string[] propertyNames, JsonSchema schema)> DependentSchemas { get; } = new();

    /// <summary>
    /// [Obsolete] Properties that are used to indicate the dependencies between fields
    /// </summary>
    [JsonProperty(ItemConverterType = typeof(UnionTypeConverter))]
    public Dictionary<string, (string[] propertyNames, JsonSchema schema)> Dependencies { get; } = new();

    /// <summary>
    /// The given data must be valid against any (one or more) of the given subschemas.
    /// </summary>
    public JsonSchema[] AnyOf { get; init; } = Array.Empty<JsonSchema>();

    /// <summary>
    /// The given data must be valid against exactly all of the given subschemas.
    /// </summary>
    public JsonSchema[] AllOf { get; init; } = Array.Empty<JsonSchema>();

    /// <summary>
    /// The given data must be valid against exactly one of the given subschemas.
    /// </summary>
    public JsonSchema[] OneOf { get; init; } = Array.Empty<JsonSchema>();

    /// <summary>
    /// Allows validation based on outcome of another schema using if/then/else construct.
    /// </summary>
    public JsonSchema? If { get; init; }

    /// <summary>
    /// Allows validation based on outcome of another schema using if/then/else construct.
    /// </summary>
    public JsonSchema? Then { get; init; }

    /// <summary>
    /// Allows validation based on outcome of another schema using if/then/else construct.
    /// </summary>
    public JsonSchema? Else { get; init; }

    /// <summary>
    /// Negates the result of a validation.
    /// </summary>
    public JsonSchema? Not { get; init; }

    // JSON schema custom validation extensions
    //-------------------------------------------

    /// <summary>
    /// the mime type of schema
    /// </summary>
    public string? SchemaType { get; init; }

    /// <summary>
    /// Property indicate the type of xref
    /// </summary>
    public string? XrefType { get; init; }

    /// <summary>
    /// Property indicate which property will fallback to xrefType when SchemaType is null
    /// </summary>
    public string? SchemaTypeProperty { get; init; }

    /// <summary>
    /// Properties that are reserved by the system.
    /// </summary>
    public string[] Reserved { get; init; } = Array.Empty<string>();

    // JSON schema custom transform extensions
    //--------------------------------------------

    /// <summary>
    /// Properties that are transformed using specified pipeline like 'markup'
    /// </summary>
    public JsonSchemaContentType? ContentType { get; init; }

    /// <summary>
    /// Properties that indicate the uid unique scope
    /// </summary>
    public bool UidGlobalUnique { get; init; }

    /// <summary>
    /// Properties that indicate whether the xref need to externally validate
    /// </summary>
    public bool ValidateExternalXrefs { get; init; }

    /// <summary>
    /// Properties that are built into xref map
    /// </summary>
    public HashSet<string> XrefProperties { get; } = new HashSet<string>();

    // JSON schema metadata validation extensions
    //-------------------------------------------

    /// <summary>
    /// Properties that are used to indicate some attitudes are required and the value of them can't be null or white space for string type
    /// </summary>
    public string[] StrictRequired { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Properties that are used to realize either logic
    /// </summary>
    public string[][] Either { get; init; } = Array.Empty<string[]>();

    /// <summary>
    /// Properties that are used to realize precludes logic
    /// </summary>
    public string[][] Precludes { get; init; } = Array.Empty<string[]>();

    /// <summary>
    /// Properties that are used to validate date format
    /// </summary>
    public string? DateFormat { get; init; }

    /// <summary>
    /// Properties that are used to set maximum time range
    /// </summary>
    public TimeSpan? RelativeMaxDate { get; init; }

    /// <summary>
    /// Properties that are used to set minimum time range
    /// </summary>
    public TimeSpan? RelativeMinDate { get; init; }

    /// <summary>
    /// Properties that are used to indicate the deprecated field
    /// </summary>
    public string? ReplacedBy { get; init; }

    /// <summary>
    /// Properties that are used to indicate the value relationship between two fields
    /// </summary>
    public EnumDependenciesSchema? EnumDependencies { get; init; }

    /// <summary>
    /// Properties that are used to validate microsoft alias
    /// </summary>
    public MicrosoftAliasSchema? MicrosoftAlias { get; init; }

    /// <summary>
    /// Properties' value must be unique within the docset
    /// </summary>
    public HashSet<string> DocsetUnique { get; init; } = new HashSet<string>();

    /// <summary>
    /// Whether content fallback is allowed for loc page
    /// </summary>
    public bool ContentFallback { get; init; } = true;

    /// <summary>
    /// Properties that are used to hold min item count that meet condition.
    /// </summary>
    [JsonConverter(typeof(OneOrManyConverter))]
    public ConditionalCheckSchema[] MinItemsWhen { get; init; } = Array.Empty<ConditionalCheckSchema>();

    /// <summary>
    /// Properties that are used to hold max item count that meet condition.
    /// </summary>
    [JsonConverter(typeof(OneOrManyConverter))]
    public ConditionalCheckSchema[] MaxItemsWhen { get; init; } = Array.Empty<ConditionalCheckSchema>();

    // JSON schema metadata validation error extensions
    //-------------------------------------------

    /// <summary>
    /// This field is used to provide additional error information and only can be set in root level of schema
    /// </summary>
    public Dictionary<string, Dictionary<string, CustomRule>> Rules { get; } = new Dictionary<string, Dictionary<string, CustomRule>>();
}
