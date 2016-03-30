// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Tests
{
    using System.IO;

    using Newtonsoft.Json.Linq;
    using Xunit;

    using Microsoft.DocAsCode.Build.RestApi.Swagger;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "SwaggerJsonParser")]
    public class SwaggerJsonParserTest
    {
        [Fact]
        public void ParseSimpleSwaggerJsonShouldSucceed()
        {
            var swaggerFile = @"TestData\swagger\simple_swagger2.json";
            var swagger = SwaggerJsonParser.Parse(File.ReadAllText(swaggerFile));

            Assert.Equal(1, swagger.Paths.Count);
            Assert.Equal(1, swagger.Paths["/contacts"].Count);
            var action = swagger.Paths["/contacts"]["get"];
            var parameters = action.Parameters;
            Assert.Equal(1, parameters.Count);
            Assert.Equal("query", parameters[0].Metadata["in"]);
            Assert.Equal(1, action.Responses.Count);
            var response = action.Responses["200"];
            Assert.Equal(1, response.Examples.Count);
            var example = response.Examples["application/json"];
            Assert.NotNull(example);
        }

        [Fact]
        public void ParseSwaggerJsonWithReferenceShouldSucceed()
        {
            var swaggerFile = @"TestData\swagger\ref_swagger2.json";
            var swagger = SwaggerJsonParser.Parse(File.ReadAllText(swaggerFile));

            Assert.Equal(1, swagger.Paths.Count);
            Assert.Equal(1, swagger.Paths["/contacts"].Count);
            var action = swagger.Paths["/contacts"]["patch"];
            var parameters = action.Parameters;
            Assert.Equal(1, parameters.Count);
            Assert.Equal("body", parameters[0].Metadata["in"]);
            var schema = parameters[0].Metadata["schema"] as JObject;
            Assert.NotNull(schema);
            Assert.Equal("Sales", schema["example"]["department"].ToString());

            // Reference object
            var properties = schema["properties"] as JObject;
            Assert.NotNull(properties);
            Assert.Equal(2, properties.Count);
            Assert.Equal("string", properties["objectType"]["type"]);
            Assert.Equal("array", properties["provisioningErrors"]["type"]);
            var refProperty = properties["provisioningErrors"]["items"]["schema"] as JObject;
            Assert.NotNull(refProperty);
            Assert.Equal("string", refProperty["properties"]["errorDetail"]["type"]);

            Assert.Equal(1, action.Responses.Count);
            var response = action.Responses["204"];
            Assert.Equal(1, response.Examples.Count);
            var example = response.Examples["application/json"];
            Assert.NotNull(example);
        }

        [Fact]
        public void ParseSwaggerJsonWithLoopReferenceShouldFail()
        {
            var swaggerFile = @"TestData\swagger\loopref_swagger2.json";
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => SwaggerJsonParser.Parse(File.ReadAllText(swaggerFile)));
        }
    }
}
