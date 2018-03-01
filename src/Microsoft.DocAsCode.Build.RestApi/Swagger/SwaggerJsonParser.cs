// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger
{
    using System.Threading;

    using Microsoft.DocAsCode.Build.RestApi.Swagger.Internals;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class SwaggerJsonParser
    {
        private static readonly ThreadLocal<JsonSerializer> Serializer = new ThreadLocal<JsonSerializer>(
            () =>
            {
                var jsonSerializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                };
                jsonSerializer.Converters.Add(new SwaggerObjectConverter());
                return jsonSerializer;
            });

        public static SwaggerModel Parse(string swaggerFilePath)
        {
            // Deserialize to internal swagger model
            var builder = new SwaggerJsonBuilder();
            var swagger = builder.Read(swaggerFilePath);

            // Serialize to JToken
            var token = JToken.FromObject(swagger, Serializer.Value);

            // Convert to swagger model
            return token.ToObject<SwaggerModel>(Serializer.Value);
        }
    }
}
