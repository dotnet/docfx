// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Tests
{
    using Microsoft.DocAsCode.Build.RestApi.Swagger;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "SwaggerJsonParser")]
    public class SwaggerJsonParserTest
    {
        [Fact]
        public void ParseSimpleSwaggerJsonShouldSucceed()
        {
            var swaggerFile = @"TestData\swagger\simple_swagger2.json";
            var swagger = SwaggerJsonParser.Parse(swaggerFile);

            Assert.Equal(1, swagger.Paths.Values.Count);
            var actionJObject = swagger.Paths["/contacts"].Metadata["get"] as JObject;
            Assert.NotNull(actionJObject);
            var action = actionJObject.ToObject<OperationObject>();
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
            var swagger = SwaggerJsonParser.Parse(swaggerFile);

            Assert.Equal(1, swagger.Paths.Count);
            Assert.Equal(1, swagger.Paths["/contacts"].Metadata.Count);
            var actionJObject = swagger.Paths["/contacts"].Metadata["patch"] as JObject;
            Assert.NotNull(actionJObject);
            var action = actionJObject.ToObject<OperationObject>();
            var parameters = action.Parameters;
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

            schema = parameters[1].Metadata["schema"] as JObject;
            properties = schema["properties"] as JObject;
            var message = properties["message"];
            Assert.Equal("A message describing the error, intended to be suitable for display in a user interface.", message["description"]);

            Assert.Equal(1, action.Responses.Count);
            var response = action.Responses["204"];
            Assert.Equal(1, response.Examples.Count);
            var example = response.Examples["application/json"];
            Assert.NotNull(example);
        }

        [Fact]
        public void ParseSwaggerJsonWithTagShouldSucceed()
        {
            const string swaggerFile = @"TestData\swagger\tag_swagger2.json";
            var swagger = SwaggerJsonParser.Parse(swaggerFile);

            Assert.Equal(3, swagger.Tags.Count);
            var tag0 = swagger.Tags[0];
            Assert.Equal("contact", tag0.Name);
            Assert.Equal("Everything about the **contacts**", tag0.Description);
            Assert.Equal("contact-bookmark", tag0.BookmarkId);
            Assert.Equal(1, tag0.Metadata.Count);
            var externalDocs = (JObject)tag0.Metadata["externalDocs"];
            Assert.NotNull(externalDocs);
            Assert.Equal("Find out more", externalDocs["description"]);
            Assert.Equal("http://swagger.io", externalDocs["url"]);
        }


        [Fact]
        public void ParseSwaggerJsonWithPathParametersShouldSucceed()
        {
            const string swaggerFile = @"TestData\swagger\pathParameters_swagger2.json";
            var swagger = SwaggerJsonParser.Parse(swaggerFile);

            Assert.Equal(1, swagger.Paths.Values.Count);
            var parameters = swagger.Paths["/contacts"].Parameters;
            Assert.Equal(2, parameters.Count);

            // $ref parameter
            Assert.Equal("api-version", parameters[0].Name);
            Assert.Equal(false, parameters[0].Metadata["required"]);
            Assert.Equal("api version description", parameters[0].Description);

            // self defined parameter
            Assert.Equal("subscriptionId", parameters[1].Name);
            Assert.Equal(true, parameters[1].Metadata["required"]);
            Assert.Equal("subscription id", parameters[1].Description);
        }

        [Fact]
        public void ParseSwaggerJsonWithLoopReferenceShouldSucceed()
        {
            const string swaggerFile = @"TestData\swagger\loopref_swagger2.json";
            var swagger = SwaggerJsonParser.Parse(swaggerFile);

            Assert.Equal(1, swagger.Paths.Values.Count);
            var actionJObject = swagger.Paths["/contacts"].Metadata["patch"] as JObject;
            Assert.NotNull(actionJObject);
            var action = actionJObject.ToObject<OperationObject>();
            var schemaJObject = (JObject)action.Parameters[0].Metadata["schema"];
            var schemaObj = schemaJObject.ToString(Formatting.Indented);
            Assert.Equal(@"{
  ""properties"": {
    ""provisioningErrors"": {
      ""type"": ""array"",
      ""items"": {
        ""properties"": {
          ""errorDetail"": {
            ""type"": ""array"",
            ""items"": {
              ""x-internal-loop-ref-name"": ""contact"",
              ""x-internal-loop-token"": {}
            }
          }
        },
        ""x-internal-ref-name"": ""ProvisioningError""
      },
      ""readOnly"": true
    }
  },
  ""x-internal-ref-name"": ""contact"",
  ""example"": {
    ""department"": ""Sales"",
    ""jobTitle"": ""Sales Rep""
  }
}".Replace("\r\n", "\n"), schemaObj.Replace("\r\n", "\n"));
        }

        [Fact]
        public void ParseSwaggerJsonWithExternalLoopReferenceShouldSucceed()
        {
            const string swaggerFile = @"TestData\swagger\externalLoopRef_A.json";
            var swagger = SwaggerJsonParser.Parse(swaggerFile);

            Assert.Equal(1, swagger.Paths.Values.Count);
            var actionJObject = swagger.Paths["/contacts"].Metadata["patch"] as JObject;
            Assert.NotNull(actionJObject);
            var action = actionJObject.ToObject<OperationObject>();
            var schemaJObject = (JObject)action.Parameters[0].Metadata["schema"];
            var schemaObj = schemaJObject.ToString(Formatting.Indented);
            Assert.Equal(@"{
  ""properties"": {
    ""provisioningErrors"": {
      ""type"": ""array"",
      ""items"": {
        ""properties"": {
          ""errorDetail"": {
            ""type"": ""array"",
            ""items"": {
              ""x-internal-loop-ref-name"": ""contact"",
              ""x-internal-loop-token"": {}
            }
          }
        },
        ""x-internal-ref-name"": ""Provision%ing|Error""
      },
      ""readOnly"": true
    }
  },
  ""x-internal-ref-name"": ""contact"",
  ""example"": {
    ""department"": ""Sales"",
    ""jobTitle"": ""Sales Rep""
  }
}".Replace("\r\n", "\n"), schemaObj.Replace("\r\n", "\n"));
        }
    }
}
