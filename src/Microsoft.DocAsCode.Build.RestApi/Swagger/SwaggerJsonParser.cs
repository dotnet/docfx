// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Swagger
{
    using System.IO;
    using System.Threading;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Microsoft.DocAsCode.Build.RestApi.Swagger.Internals;

    internal class SwaggerJsonParser
    {
        private static readonly ThreadLocal<JsonSerializer> _serializer = new ThreadLocal<JsonSerializer>(
            () =>
            {
                var jsonSerializer = new JsonSerializer();
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                jsonSerializer.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
                jsonSerializer.Converters.Add(new SwaggerObjectConverter());
                return jsonSerializer;
            });

        public static SwaggerModel Parse(string json)
        {
            using (JsonReader reader = new JsonTextReader(new StringReader(json)))
            {
                // Deserialize to internal swagger model
                var builder = new SwaggerJsonBuilder();
                var swagger = builder.Read(reader);

                // Serialze to JToken
                var token = JToken.FromObject(swagger, _serializer.Value);

                // Convert to swagger model
                return token.ToObject<SwaggerModel>(_serializer.Value);
            }
        }
    }
}
