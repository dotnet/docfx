// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Exceptions;

    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;

    public class SchemaValidator
    {
        private static readonly Uri SupportedMetaSchemaUri = new Uri("https://dotnet.github.io/docfx/schemas/v1.0/schema.json#");
        private readonly JSchema _jSchema;
        private readonly object _schemaObject;
        private readonly SchemaValidateService _validateService = SchemaValidateService.Instance;

        public SchemaValidator(JObject schemaObj, JSchema schema)
        {
            _schemaObject = schemaObj;
            _jSchema = schema;
        }

        public void Validate(object obj)
        {
            _validateService.Validate(obj, _jSchema);
        }

        public void ValidateMetaSchema()
        {
            if (!ValidateSchemaUrl(_jSchema.SchemaVersion))
            {
                throw new InvalidSchemaException($"Schema {_jSchema.SchemaVersion} is not supported. Current supported schemas are: {SupportedMetaSchemaUri.OriginalString}.");
            }
            using (var stream = typeof(DocumentSchema).Assembly.GetManifestResourceStream("Microsoft.DocAsCode.Build.SchemaDriven.schemas.v1._0.schema.json"))
            using (var sr = new StreamReader(stream))
            {
                var metaSchema = JSchema.Parse(sr.ReadToEnd());
                _validateService.Validate(_schemaObject, metaSchema);
            }
        }

        private static bool ValidateSchemaUrl(Uri uri)
        {
            return uri.Host == SupportedMetaSchemaUri.Host
                && uri.LocalPath == SupportedMetaSchemaUri.LocalPath
                && (string.IsNullOrEmpty(uri.Fragment) || uri.Fragment == "#");
        }
    }
}
