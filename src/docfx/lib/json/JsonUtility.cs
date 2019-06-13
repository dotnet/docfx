// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Docs.Build
{
    internal static class JsonUtility
    {
        private static readonly ConcurrentDictionary<Type, JsonSchemaValidator> s_jsonSchemaValidator = new ConcurrentDictionary<Type, JsonSchemaValidator>();
        private static readonly NamingStrategy s_namingStrategy = new CamelCaseNamingStrategy();
        private static readonly JsonConverter[] s_jsonConverters =
        {
            new StringEnumConverter { NamingStrategy = s_namingStrategy },
            new SourceInfoJsonConverter { },
            new JTokenJsonConverter { },
        };

        private static readonly JsonContractResolver s_contractResolver = new JsonContractResolver { NamingStrategy = s_namingStrategy };

        private static readonly JsonSerializer s_serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = s_jsonConverters,
            ContractResolver = s_contractResolver,
            Error = (a, b) => { b.ErrorContext.Handled = true; },
        });

        private static readonly JsonSerializer s_indentSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            Converters = s_jsonConverters,
            ContractResolver = s_contractResolver,
            Error = (a, b) => { b.ErrorContext.Handled = true; },
        });

        private static readonly JsonSerializer s_serializerWithoutErrorHandling = JsonSerializer.Create(new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            Converters = s_jsonConverters,
            ContractResolver = s_contractResolver,
        });

        internal static JsonSerializer Serializer => s_serializerWithoutErrorHandling;

        /// <summary>
        /// Fast pass to read MIME from $schema attribute.
        /// </summary>
        public static SourceInfo<string> ReadMime(TextReader reader, string file)
        {
            var schema = ReadSchema(reader, file);
            if (schema?.Value is null)
                return schema;

            // TODO: be more strict
            var mime = schema.Value.Split('/').LastOrDefault();
            if (mime != null)
            {
                schema.Value = Path.GetFileNameWithoutExtension(schema);
            }
            else
            {
                schema.Value = null;
            }
            return schema;
        }

        public static IEnumerable<string> GetPropertyNames(Type type)
        {
            return ((JsonObjectContract)s_serializer.ContractResolver.ResolveContract(type)).Properties.Select(prop => prop.PropertyName);
        }

        /// <summary>
        /// Serialize an object to TextWriter
        /// </summary>
        public static void Serialize(TextWriter writer, object graph, bool indent = false)
        {
            var serializer = indent ? s_indentSerializer : s_serializer;
            serializer.Serialize(writer, graph);
        }

        /// <summary>
        /// Serialize an object to string
        /// </summary>
        public static string Serialize(object graph, bool indent = false)
        {
            using (StringWriter writer = new StringWriter())
            {
                Serialize(writer, graph, indent);
                return writer.ToString();
            }
        }

        /// <summary>
        /// De-serialize a data string, which is not user input, to an object
        /// schema validation errors will be ignored, syntax errors and type mismatching will be thrown
        /// </summary>
        public static T Deserialize<T>(string json, string file)
        {
            using (var stringReader = new StringReader(json))
            using (var reader = new JsonTextReader(stringReader))
            {
                try
                {
                    return s_serializerWithoutErrorHandling.Deserialize<T>(reader);
                }
                catch (JsonReaderException ex)
                {
                    throw ToError(ex, file).ToException(ex);
                }
                catch (JsonSerializationException ex)
                {
                    throw ToError(ex, file).ToException(ex);
                }
            }
        }

        /// <summary>
        /// Converts a strongly typed C# object to weakly typed json object using the default serialization settings.
        /// </summary>
        public static JObject ToJObject(object model)
        {
            return JObject.FromObject(model, s_serializer);
        }

        /// <summary>
        /// Creates an instance of the specified .NET type from the JToken with schema validation
        /// </summary>
        public static (List<Error> errors, T value) ToObject<T>(JToken token)
        {
            var (errors, obj) = ToObject(token, typeof(T));
            return (errors, (T)obj);
        }

        public static (List<Error> errors, object value) ToObject(
            JToken token,
            Type type)
        {
            var validator = GetJsonSchemaValidation(type);
            var errors = validator.Validate(token);

            return (errors, s_serializer.Deserialize(new JTokenReader(token), type));
        }

        /// <summary>
        /// Deserialize from JSON file, get from or add to cache
        /// </summary>
        public static (List<Error>, JToken) Parse(Document file, Context context) => context.Cache.LoadJsonFile(file);

        /// <summary>
        /// Parse a string to JToken.
        /// Validate null value during the process.
        /// </summary>
        public static (List<Error> errors, JToken value) Parse(string json, string file)
        {
            try
            {
                using (var stringReader = new StringReader(json))
                using (var reader = new JsonTextReader(stringReader) { DateParseHandling = DateParseHandling.None })
                {
                    return SetSourceInfo(JToken.ReadFrom(reader), file).RemoveNulls();
                }
            }
            catch (JsonReaderException ex)
            {
                throw ToError(ex, file).ToException(ex);
            }
        }

        public static void Merge(JObject container, JObject overwrite)
        {
            if (overwrite is null)
                return;

            foreach (var property in overwrite.Properties())
            {
                var key = property.Name;
                var value = property.Value;

                if (container[key] is JObject containerObj && value is JObject overwriteObj)
                {
                    Merge(containerObj, overwriteObj);
                }
                else
                {
                    container[key] = DeepClone(value);
                    SetSourceInfo(container.Property(key), property.Annotation<SourceInfo>());
                }
            }
        }

        public static JToken DeepClone(JToken token)
        {
            if (token is JValue v)
            {
                return SetSourceInfo(new JValue(v), token.Annotation<SourceInfo>());
            }

            if (token is JObject obj)
            {
                var result = new JObject();
                foreach (var prop in obj.Properties())
                {
                    result[prop.Name] = DeepClone(prop.Value);
                    SetSourceInfo(result.Property(prop.Name), prop.Annotation<SourceInfo>());
                }
                return SetSourceInfo(result, token.Annotation<SourceInfo>());
            }

            if (token is JArray array)
            {
                var result = new JArray();
                foreach (var item in array)
                {
                    result.Add(DeepClone(item));
                }
                return SetSourceInfo(result, token.Annotation<SourceInfo>());
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Trims all string values
        /// </summary>
        public static void TrimStringValues(JToken token)
        {
            switch (token)
            {
                case JValue scalar when scalar.Value is string str:
                    scalar.Value = str.Trim();
                    break;

                case JArray array:
                    foreach (var item in array)
                    {
                        TrimStringValues(item);
                    }
                    break;

                case JObject map:
                    foreach (var (key, value) in map)
                    {
                        TrimStringValues(value);
                    }
                    break;
            }
        }

        /// <summary>
        /// Report warnings for null values inside arrays and remove nulls inside arrays.
        /// </summary>
        public static (List<Error>, JToken) RemoveNulls(this JToken root)
        {
            var errors = new List<Error>();
            var nullArrayNodes = new List<(JToken, string)>();

            RemoveNullsCore(root, null);

            foreach (var (node, name) in nullArrayNodes)
            {
                errors.Add(Errors.NullArrayValue(GetSourceInfo(node), name));
                node.Remove();
            }

            // treat null JToken as empty JObject since it is from user input
            return (errors, IsNullOrUndefined(root) ? new JObject() : root);

            void RemoveNullsCore(JToken token, string name)
            {
                if (token is JArray array)
                {
                    foreach (var item in array)
                    {
                        if (item.IsNullOrUndefined())
                        {
                            nullArrayNodes.Add((item, name ?? item.Path));
                        }
                        else
                        {
                            RemoveNullsCore(item, name);
                        }
                    }
                }
                else if (token is JObject obj)
                {
                    foreach (var prop in obj.Properties())
                    {
                        if (!prop.Value.IsNullOrUndefined())
                        {
                            RemoveNullsCore(prop.Value, prop.Name);
                        }
                    }
                }
            }
        }

        public static bool TryGetValue<T>(this JObject obj, string key, out T value) where T : JToken
        {
            value = null;
            if (obj is null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (obj.TryGetValue(key, out var valueToken) && valueToken is T valueT)
            {
                value = valueT;
                return true;
            }

            return false;
        }

        public static SourceInfo GetSourceInfo(JToken token)
        {
            return token.Annotation<SourceInfo>();
        }

        internal static JsonSchema GenerateJsonSchema(Type type, JsonSchema rootJsonSchema = null)
        {
            var jsonSchema = new JsonSchema();
            rootJsonSchema = rootJsonSchema ?? jsonSchema;

            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(SourceInfo<>) || type.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                type = type.GetGenericArguments()[0];
            }

            var jsonContract = s_contractResolver.ResolveContract(type);

            if (type == typeof(object))
            {
                jsonSchema.Type = new[] { JsonSchemaType.Array, JsonSchemaType.Boolean, JsonSchemaType.Integer, JsonSchemaType.Number, JsonSchemaType.Null, JsonSchemaType.Object, JsonSchemaType.String };
                return jsonSchema;
            }

            if (jsonContract is JsonObjectContract jsonObjectContract)
            {
                if (!rootJsonSchema.Definitions.TryGetValue(type.Name, out _))
                {
                    rootJsonSchema.Definitions[type.Name] = new JsonSchema
                    {
                        Type = new[] { JsonSchemaType.Object },
                    };

                    foreach (var prop in jsonObjectContract.Properties)
                    {
                        if (prop.PropertyName == "valueEnum")
                        {
                            Console.WriteLine("");
                        }
                        rootJsonSchema.Definitions[type.Name].Properties.Add(prop.PropertyName, GenerateJsonSchema(prop.PropertyType, rootJsonSchema));
                    }
                }

                jsonSchema.Ref = $"#/definitions/{type.Name}";
            }

            if (jsonContract is JsonDictionaryContract jsonDictionaryContract)
            {
                jsonSchema.Type = new[] { JsonSchemaType.Object };
                jsonSchema.AdditionalProperties = (true, GenerateJsonSchema(jsonDictionaryContract.DictionaryKeyType, rootJsonSchema));
            }

            if (jsonContract is JsonArrayContract jsonArrayContract)
            {
                jsonSchema.Items = GenerateJsonSchema(jsonArrayContract.CollectionItemType, rootJsonSchema);
                jsonSchema.Type = new[] { JsonSchemaType.Array };
            }

            if (jsonContract is JsonPrimitiveContract jsonPrimitiveContract)
            {
                jsonSchema.Type = new[] { GetJsonSchemaTypeFromPrimitiveContract(jsonPrimitiveContract) };
                if (jsonPrimitiveContract.UnderlyingType.IsEnum)
                {
                    jsonSchema.Enum = jsonPrimitiveContract.UnderlyingType.GetEnumNames().Select(en => new JValue(en)).ToArray();
                }
            }

            return jsonSchema;

            JsonSchemaType GetJsonSchemaTypeFromPrimitiveContract(JsonPrimitiveContract contract)
            {
                if (contract.UnderlyingType == typeof(string))
                {
                    return JsonSchemaType.String;
                }

                if (contract.UnderlyingType == typeof(double))
                {
                    return JsonSchemaType.Number;
                }

                if (contract.UnderlyingType == typeof(bool))
                {
                    return JsonSchemaType.Boolean;
                }

                if (contract.UnderlyingType == typeof(int))
                {
                    return JsonSchemaType.Integer;
                }

                if (contract.UnderlyingType.IsEnum)
                {
                    return JsonSchemaType.String;
                }

                return JsonSchemaType.None;
            }
        }

        internal static JToken SetSourceInfo(JToken token, SourceInfo source)
        {
            token.RemoveAnnotations<SourceInfo>();
            if (source != null)
            {
                token.AddAnnotation(source);
            }
            return token;
        }

        internal static void SkipToken(JsonReader reader)
        {
            var currentDepth = reader.Depth;
            reader.Skip();
            while (reader.Depth > currentDepth)
            {
                if (!reader.Read())
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Fast pass to read the value of $schema specified in JSON.
        /// $schema must be the first attribute in the root object.
        /// Assume input is a valid JSON. Bad input will be processed through Json.NET
        /// </summary>
        private static SourceInfo<string> ReadSchema(TextReader reader, string file)
        {
            try
            {
                var json = new JsonTextReader(reader);

                if (json.Read() && json.TokenType == JsonToken.StartObject)
                {
                    if (json.Read() && json.TokenType == JsonToken.PropertyName && json.Value is string str && str == "$schema")
                    {
                        if (json.Read() && json.Value is string schema)
                        {
                            var lineInfo = (IJsonLineInfo)json;
                            return new SourceInfo<string>(schema, new SourceInfo(file, lineInfo.LineNumber, lineInfo.LinePosition));
                        }
                    }
                }
                return new SourceInfo<string>(null, new SourceInfo(file, 1, 1));
            }
            catch (JsonReaderException)
            {
                return null;
            }
        }

        private static JsonSchemaValidator GetJsonSchemaValidation(Type type)
        {
            return s_jsonSchemaValidator.GetOrAdd(type, new JsonSchemaValidator(GenerateJsonSchema(type)));
        }

        private static bool IsNullOrUndefined(this JToken token)
        {
            return
                (token is null) ||
                (token.Type == JTokenType.Null) ||
                (token.Type == JTokenType.Undefined);
        }

        private static JToken SetSourceInfo(JToken token, string file)
        {
            var lineInfo = (IJsonLineInfo)token;
            SetSourceInfo(token, new SourceInfo(file, lineInfo.LineNumber, lineInfo.LinePosition));

            switch (token)
            {
                case JProperty prop:
                    SetSourceInfo(prop.Value, file);
                    break;

                case JArray arr:
                    foreach (var item in arr)
                    {
                        SetSourceInfo(item, file);
                    }
                    break;

                case JObject obj:
                    foreach (var prop in obj.Properties())
                    {
                        SetSourceInfo(prop, file);
                    }
                    break;
            }

            return token;
        }

        private static Error ToError(Exception ex, string file)
        {
            var (message, line, column) = ParseException(ex);

            return Errors.JsonSyntaxError(new SourceInfo(file, line, column), message);
        }

        private static (string message, int line, int column) ParseException(Exception ex)
        {
            // TODO: Json.NET type conversion error message is developer friendly but not writer friendly.
            var match = Regex.Match(ex.Message, "^([\\s\\S]*)\\sPath '(.*)', line (\\d+), position (\\d+).$");
            if (match.Success)
            {
                return (RewriteErrorMessage(match.Groups[1].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value));
            }

            match = Regex.Match(ex.Message, "^([\\s\\S]*)\\sPath '(.*)'.$");

            return (RewriteErrorMessage(match.Success ? match.Groups[1].Value : ex.Message), 0, 0);
        }

        private static string RewriteErrorMessage(string message)
        {
            if (message.StartsWith("Error reading string. Unexpected token"))
            {
                return "Expected type String, please input String or type compatible with String.";
            }
            return message;
        }
    }
}
