// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;
    using Newtonsoft.Json.Serialization;

    using Microsoft.DocAsCode.Exceptions;

    public class DocumentSchema : BaseSchema
    {
        private const string SchemaFileEnding = ".schema.json";
        public static readonly ThreadLocal<JsonSerializer> DefaultSerializer = new ThreadLocal<JsonSerializer>(
               () => new JsonSerializer
               {
                   NullValueHandling = NullValueHandling.Ignore,
                   ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                   ContractResolver = new CamelCasePropertyNamesContractResolver(),
                   Converters =
                   {
                       new StringEnumConverter { CamelCaseText = true },
                   },
               });

        [JsonProperty("$schema")]
        public string Schema { get; set; }

        public string Version { get; set; }

        public string Id { get; set; }

        public string Metadata { get; set; }

        [JsonIgnore]
        public JsonPointer MetadataReference { get; set; }

        public JObject ToJObject()
        {
            return JObject.FromObject(this, DefaultSerializer.Value);
        }

        public static DocumentSchema Load(TextReader reader, string title)
        {
            using (var json = new JsonTextReader(reader))
            {
                DocumentSchema schema;
                try
                {
                    schema = DefaultSerializer.Value.Deserialize<DocumentSchema>(json);
                }
                catch (Exception e)
                {
                    throw new InvalidSchemaException($"Not a valid schema: {e.Message}", e);
                }

                SchemaValidator.Validate(schema);

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
    }
}
