// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.RestApi.Swagger.Internals;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi.Swagger;

public class SwaggerJsonParser
{
    private static readonly ThreadLocal<JsonSerializer> Serializer = new(
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
