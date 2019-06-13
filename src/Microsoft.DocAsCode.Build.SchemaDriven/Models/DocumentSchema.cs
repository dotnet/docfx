// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Common;

    public class DocumentSchema : BaseSchema
    {
        public Uri SchemaVersion { get; set; }

        public string Version { get; set; }

        public Uri Id { get; set; }

        public string Metadata { get; set; }

        public JsonPointer MetadataReference { get; private set; }

        public SchemaValidator Validator { get; private set; }

        public string Hash { get; private set; }

        /// <summary>
        /// Overwrites are only allowed when the schema contains "uid" definition
        /// </summary>
        public bool AllowOverwrite { get; private set; }

        public static DocumentSchema Load(TextReader reader, string title)
        {
            DocumentSchema schema;
            using (var jtr = new JsonTextReader(reader))
            {
                JSchema jSchema;
                JObject jObject;
                try
                {
                    jObject = JObject.Load(jtr);
                    jSchema = JSchema.Load(jObject.CreateReader());
                }
                catch (Exception e) when (e is JSchemaException || e is JsonException)
                {
                    throw new InvalidSchemaException($"{title} is not a valid schema: {e.Message}", e);
                }

                var validator = new SchemaValidator(jObject, jSchema);

                // validate schema here
                validator.ValidateMetaSchema();

                try
                {
                    schema = LoadSchema<DocumentSchema>(jSchema, new Dictionary<JSchema, BaseSchema>());
                    schema.SchemaVersion = jSchema.SchemaVersion;
                    schema.Id = jSchema.Id;
                    schema.Version = GetValueFromJSchemaExtensionData<string>(jSchema, "version");
                    schema.Metadata = GetValueFromJSchemaExtensionData<string>(jSchema, "metadata");
                    schema.Validator = validator;
                }
                catch (Exception e)
                {
                    throw new InvalidSchemaException($"{title} is not a valid schema: {e.Message}", e);
                }

                if (string.IsNullOrWhiteSpace(schema.Title))
                {
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        throw new InvalidSchemaException($"Title of schema must be specified.");
                    }
                    schema.Title = title;
                }

                if (schema.Type != JSchemaType.Object)
                {
                    throw new InvalidSchemaException("Type for the root schema object must be object");
                }

                if (!JsonPointer.TryCreate(schema.Metadata, out var pointer))
                {
                    throw new InvalidJsonPointerException($"Metadata's json pointer {schema.Metadata} is invalid.");
                }

                var metadataSchema = pointer.FindSchema(schema);
                if (metadataSchema != null && metadataSchema.Type != JSchemaType.Object)
                {
                    throw new InvalidJsonPointerException($"The referenced object is in type: {metadataSchema.Type}, only object can be a metadata reference");
                }

                schema.MetadataReference = pointer;
                schema.AllowOverwrite = CheckOverwriteAbility(schema);
                schema.Hash = JsonUtility.Serialize(jObject).GetMd5String();

                return schema;
            }
        }

        private static T LoadSchema<T>(JSchema schema, Dictionary<JSchema, BaseSchema> cache) where T : BaseSchema, new()
        {
            if (cache.TryGetValue(schema, out var bs))
            {
                return (T)bs;
            }

            bs = new T
            {
                Title = schema.Title,
                Description = schema.Description,
                Type = schema.Type,
                Default = schema.Default,
                ContentType = GetValueFromJSchemaExtensionData<ContentType>(schema, "contentType"),
                Tags = GetValueFromJSchemaExtensionData<List<string>>(schema, "tags"),
                MergeType = GetValueFromJSchemaExtensionData<MergeType>(schema, "mergeType"),
                Reference = GetValueFromJSchemaExtensionData<ReferenceType>(schema, "reference"),
                XrefProperties = GetValueFromJSchemaExtensionData<List<string>>(schema, "xrefProperties"),
            };

            cache[schema] = bs;

            CheckForNotSupportedKeyword(schema.OneOf, nameof(schema.OneOf));
            CheckForNotSupportedKeyword(schema.AllOf, nameof(schema.AllOf));
            CheckForNotSupportedKeyword(schema.AnyOf, nameof(schema.AnyOf));
            CheckForNotSupportedKeyword(schema.AdditionalItems, nameof(schema.AdditionalItems));
            CheckForNotSupportedKeyword(schema.AdditionalProperties, nameof(schema.AdditionalProperties));
            CheckForNotSupportedKeyword(schema.PatternProperties, nameof(schema.PatternProperties));

            if (schema.Properties != null)
            {
                bs.Properties = new Dictionary<string, BaseSchema>();
                foreach (var pair in schema.Properties)
                {
                    bs.Properties[pair.Key] = LoadSchema<BaseSchema>(pair.Value, cache);
                }
            }

            if (schema.Items != null && schema.Items.Count > 0)
            {
                if (schema.Items.Count > 1)
                {
                    throw new SchemaFeatureNotSupportedException("Multiple item definition is not supported in current schema driven document processor");
                }

                bs.Items = LoadSchema<BaseSchema>(schema.Items[0], cache);
            }

            return (T)bs;
        }

        private static T GetValueFromJSchemaExtensionData<T>(JSchema schema, string key)
        {
            if (schema.ExtensionData != null
                && schema.ExtensionData.TryGetValue(key, out var value))
            {
                return value.ToObject<T>();
            }

            return default;
        }

        private static void CheckForNotSupportedKeyword(object keyword, string name)
        {
            if (keyword == null)
            {
                return;
            }

            if (keyword is IList<JSchema> list)
            {
                if (list.Count > 0)
                {
                    throw new SchemaKeywordNotSupportedException(name);
                }
            }
            else if (keyword is IDictionary<string, JSchema> dict)
            {
                if (dict.Count > 0)
                {
                    throw new SchemaKeywordNotSupportedException(name);
                }
            }
            else
            {
                throw new SchemaKeywordNotSupportedException(name);
            }
        }

        private static bool CheckOverwriteAbility(BaseSchema schema)
        {
            return CheckOverwriteAbilityCore(schema, new Dictionary<BaseSchema, bool>());
        }

        private static bool CheckOverwriteAbilityCore(BaseSchema schema, Dictionary<BaseSchema, bool> cache)
        {
            if (schema == null)
            {
                return false;
            }

            if (cache.TryGetValue(schema, out var result))
            {
                return result;
            }

            if (schema.ContentType == ContentType.Uid)
            {
                cache[schema] = result = true;
                return result;
            }

            cache[schema] = result = false;

            if (CheckOverwriteAbilityCore(schema.Items, cache))
            {
                cache[schema] = result = true;
                return result;
            }

            if (schema.Properties != null)
            {
                foreach (var value in schema.Properties.Values)
                {
                    if (CheckOverwriteAbilityCore(value, cache))
                    {
                        cache[schema] = result = true;
                        return result;
                    }
                }
            }

            return result;
        }
    }
}
