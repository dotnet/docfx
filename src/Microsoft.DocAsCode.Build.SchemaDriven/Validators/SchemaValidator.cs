// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json.Schema;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;

    public class SchemaValidator
    {
        private static readonly Uri SupportedMetaSchemaUri = new Uri("https://dotnet.github.io/docfx/schemas/v1.0/schema.json#");
        public static void Validate(DocumentSchema schema)
        {
            if (!ValidateSchemaUrl(schema.Schema))
            {
                throw new InvalidSchemaException($"Schema {schema.Schema} is not supported. Current supported schemas are: {SupportedMetaSchemaUri.OriginalString}.");
            }

            using (var stream = typeof(SchemaValidator).Assembly.GetManifestResourceStream("Microsoft.DocAsCode.Build.SchemaDriven.schemas.v1._0.schema.json"))
            using (var sr = new StreamReader(stream))
            {
                var metaSchema = JSchema.Parse(sr.ReadToEnd());
                var o = schema.ToJObject();
                var isValid = o.IsValid(metaSchema, out IList<string> errors);
                if (!isValid)
                {
                    throw new InvalidSchemaException($"Schema {schema.Title} is not a valid one according to {SupportedMetaSchemaUri.OriginalString}: \n{errors.ToDelimitedString("\n")}");
                }
            }
        }

        public static bool ValidateSchemaUrl(string url)
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
