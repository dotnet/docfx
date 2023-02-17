// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Common;
    using Json.Schema;
    using System.Text.Json.Serialization;

    public class DocumentSchema : BaseSchema
    {
        public string Metadata { get; set; }

        public JsonPointer MetadataReference { get; private set; }

        public SchemaValidator Validator { get; private set; }

        /// <summary>
        /// Overwrites are only allowed when the schema contains "uid" definition
        /// </summary>
        public bool AllowOverwrite { get; private set; }

        public static DocumentSchema Load(string content, string title)
        {
            DocumentSchema schema;
            JSchema jSchema;
            JObject jObject;
            try
            {
                jObject = JObject.Parse(content);
                jSchema = JSchema.Load(jObject.CreateReader());
            }
            catch (Exception e)
            {
                var message = ($"{title} is not a valid schema: {e.Message}");
                Logger.LogError(message, code: ErrorCodes.Build.ViolateSchema);
                throw new InvalidSchemaException(message, e);
            }

            var validator = new SchemaValidator(jObject, jSchema);

            // validate schema here
            validator.ValidateMetaSchema();

            try
            {
                schema = JsonSerializer.Deserialize<DocumentSchema>(
                    content,
                    new JsonSerializerOptions()
                    {
                        AllowTrailingCommas = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = {
                            new JsonStringEnumConverter()
                        }
                    });

                schema.Validator = validator;
            }
            catch (Exception e)
            {
                var message = $"{title} is not a valid schema: {e.Message}";
                Logger.LogError(message, code: ErrorCodes.Build.ViolateSchema);
                throw new InvalidSchemaException(message, e);
            }

            if (string.IsNullOrWhiteSpace(schema.Title))
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    var message = "Title of schema must be specified.";
                    Logger.LogError(message, code: ErrorCodes.Build.ViolateSchema);
                    throw new InvalidSchemaException(message);
                }
                schema.Title = title;
            }

            if (schema.Type != SchemaValueType.Object)
            {
                var message = "Type for the root schema object must be object";
                Logger.LogError(message, code: ErrorCodes.Build.ViolateSchema);
                throw new InvalidSchemaException(message);
            }

            if (!JsonPointer.TryCreate(schema.Metadata, out var pointer))
            {
                var message = $"Metadata's json pointer {schema.Metadata} is invalid.";
                Logger.LogError(message, code: ErrorCodes.Build.ViolateSchema);
                throw new InvalidSchemaException(message);
            }

            var metadataSchema = pointer.FindSchema(schema);
            if (metadataSchema != null && metadataSchema.Type != SchemaValueType.Object)
            {
                throw new InvalidJsonPointerException($"The referenced object is in type: {metadataSchema.Type}, only object can be a metadata reference");
            }

            schema.MetadataReference = pointer;
            schema.AllowOverwrite = CheckOverwriteAbility(schema);

            return schema;
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
