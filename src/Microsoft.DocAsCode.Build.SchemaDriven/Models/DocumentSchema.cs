// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.IO;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Exceptions;
    using System.Collections.Generic;

    public class DocumentSchema : BaseSchema
    {
        private const string SchemaFileEnding = ".schema.json";

        public Uri SchemaVersion { get; set; }

        public string Version { get; set; }

        public Uri Id { get; set; }

        public string Metadata { get; set; }

        public JsonPointer MetadataReference { get; private set; }

        public JSchema InnerJSchema { get; private set; }

        public JObject InnerJObject { get; private set; }

        public static DocumentSchema Load(StreamReader reader, string title)
        {
            using (var jtr = new JsonTextReader(reader))
            {
                var jObject = JObject.Load(jtr);
                DocumentSchema schema;
                try
                {
                    var jschema = JSchema.Load(jObject.CreateReader());
                    schema = LoadSchema<DocumentSchema>(jschema, new Dictionary<JSchema, BaseSchema>());
                    schema.InnerJSchema = jschema;
                    schema.InnerJObject = jObject;
                    schema.SchemaVersion = jschema.SchemaVersion;
                    schema.Id = jschema.Id;
                    schema.Version = GetValueFromJSchemaExtensionData<string>(jschema, "version");
                    schema.Metadata = GetValueFromJSchemaExtensionData<string>(jschema, "metadata");
                }
                catch (Exception e)
                {
                    throw new InvalidSchemaException($"Not a valid schema: {e.Message}", e);
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

                return schema;
            }
        }

        public static DocumentSchema Load(string schemaPath)
        {
            if (string.IsNullOrEmpty(schemaPath))
            {
                throw new ArgumentNullException(nameof(schemaPath));
            }
            if (!schemaPath.EndsWith(SchemaFileEnding, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidSchemaException($"Schema path {schemaPath} does not end with {SchemaFileEnding}");
            }

            var fileName = Path.GetFileName(schemaPath);
            var name = fileName.Substring(0, fileName.Length - SchemaFileEnding.Length);
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidSchemaException($"Schema path {schemaPath} is invalid");
            }

            using (var fr = new StreamReader(schemaPath))
            {
                return Load(fr, name);
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

            return default(T);
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
    }
}
