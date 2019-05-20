// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class JsonSchemaTransform
    {
        public static (List<Error> errors, JToken token) Transform(JsonSchema schema, JToken token)
        {
            var errors = new List<Error>();

            var transformedToken = token.DeepClone();
            Transform(schema, transformedToken, errors);
            return (errors, transformedToken);
        }

        private static void Transform(JsonSchema schema, JToken token, List<Error> errors)
        {
            switch (token)
            {
                case JValue scalar:
                    token.Replace(TransformScalar(schema, scalar, errors));
                    break;
                case JArray array:
                    foreach (var a in array)
                    {
                        Transform(schema, a, errors);
                    }
                    break;
                case JObject obj:
                    foreach (var (key, value) in obj)
                    {
                        if (schema.Properties.TryGetValue(key, out var propertySchema))
                        {
                            Transform(propertySchema, value, errors);
                        }
                    }
                    break;
            }
        }

        private static JValue TransformScalar(JsonSchema schema, JValue value, List<Error> errors)
        {
            return value;
        }
    }
}
