// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class JsonSchemaValidation
    {
        public static List<Error> Validate(JsonSchema schema, JToken token)
        {
            var errors = new List<Error>();
            ValidateCore(schema, token, errors);
            return errors;
        }

        private static void ValidateCore(JsonSchema schema, JToken token, List<Error> errors)
        {
            if (schema.Type != token.Type)
            {
                errors.Add(Errors.ViolateSchema(JsonUtility.GetSourceInfo(token), ""));
                return;
            }

            switch (token)
            {
                case JValue scalar:
                    if (schema.Enum.Count > 0 && !schema.Enum.Contains(scalar))
                    {
                        errors.Add(Errors.ViolateSchema(JsonUtility.GetSourceInfo(token), ""));
                    }
                    break;

                case JArray array:
                    foreach (var item in array)
                    {
                        ValidateCore(schema.Items, token, errors);
                    }
                    break;

                case JObject map:
                    foreach (var (key, value) in map)
                    {
                        if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            ValidateCore(propertySchema, value, errors);
                        }
                    }
                    break;
            }
        }
    }
}
