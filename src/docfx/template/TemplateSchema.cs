// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class TemplateSchema
    {
        public JsonSchema JsonSchema { get; }

        public JsonSchemaValidator JsonSchemaValidator { get; }

        public TemplateSchema(JsonSchema jsonSchema, JsonSchemaValidator jsonSchemaValidator)
        {
            JsonSchema = jsonSchema;
            JsonSchemaValidator = jsonSchemaValidator;
        }
    }
}
