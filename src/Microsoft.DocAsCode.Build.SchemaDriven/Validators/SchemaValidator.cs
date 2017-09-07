// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;

    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;

    public class SchemaValidator
    {
        private static readonly Uri SupportedMetaSchemaUri = new Uri("https://dotnet.github.io/docfx/schemas/v1.0/schema.json#");
        private readonly DocumentSchema _schema;
        private readonly JObject _schemaObject;
        private readonly JSchema _jSchema;

        public SchemaValidator(DocumentSchema schema)
        {
            _schema = schema;
            _schemaObject = schema.ToJObject();
            Validate(schema, _schemaObject);
            _jSchema = JSchema.Load(_schemaObject.CreateReader());
        }

        public void Validate(object obj)
        {
            var errors = new List<string>();

            ValidateObject(obj, (sender, args) => errors.Add(args.Message));

            if (errors.Count > 0)
            {
                throw new InvalidSchemaException($"Validation against {SupportedMetaSchemaUri.OriginalString} failed: \n{errors.ToDelimitedString("\n")}");
            }
        }

        private void ValidateObject(object obj, SchemaValidationEventHandler validationEventHandler)
        {
            using (var reader = new JSchemaValidatingReader(new ObjectJsonReader(obj)))
            {
                reader.Schema = _jSchema;
                if (validationEventHandler != null)
                {
                    reader.ValidationEventHandler += validationEventHandler;
                }
                while (reader.Read())
                {
                }
            }
        }

        private static void Validate(DocumentSchema schema, JObject obj)
        {
            if (!ValidateSchemaUrl(schema.Schema))
            {
                throw new InvalidSchemaException($"Schema {schema.Schema} is not supported. Current supported schemas are: {SupportedMetaSchemaUri.OriginalString}.");
            }

            using (var stream = typeof(SchemaValidator).Assembly.GetManifestResourceStream("Microsoft.DocAsCode.Build.SchemaDriven.schemas.v1._0.schema.json"))
            using (var sr = new StreamReader(stream))
            {
                var metaSchema = JSchema.Parse(sr.ReadToEnd());
                var isValid = obj.IsValid(metaSchema, out IList<string> errors);
                if (!isValid)
                {
                    throw new InvalidSchemaException($"Schema {schema.Title} is not a valid one according to {SupportedMetaSchemaUri.OriginalString}: \n{errors.ToDelimitedString("\n")}");
                }
            }
        }

        private static bool ValidateSchemaUrl(string url)
        {
            if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                return false;
            }
            
            return uri.Host == SupportedMetaSchemaUri.Host
                && uri.LocalPath == SupportedMetaSchemaUri.LocalPath
                && (string.IsNullOrEmpty(uri.Fragment) || uri.Fragment == "#");
        }
    }
}
