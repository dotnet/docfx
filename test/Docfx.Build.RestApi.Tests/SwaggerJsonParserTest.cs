// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.RestApi.Swagger;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Docfx.Build.RestApi.Tests;

[TestClass]
public class SwaggerJsonParserTest
{
    [TestMethod]
    public void ParseSimpleSwaggerJsonShouldSucceed()
    {
        var swaggerFile = "TestData/swagger/simple_swagger2.json";
        var swagger = SwaggerJsonParser.Parse(swaggerFile);

        Assert.ContainsSingle(swagger.Paths.Values);
        var actionJObject = swagger.Paths["/contacts"].Metadata["get"] as JObject;
        Assert.IsNotNull(actionJObject);
        var action = actionJObject.ToObject<OperationObject>();
        var parameters = action.Parameters;
        Assert.ContainsSingle(parameters);
        Assert.AreEqual("query", parameters[0].Metadata["in"]);
        Assert.ContainsSingle(action.Responses);
        var response = action.Responses["200"];
        Assert.ContainsSingle(response.Examples);
        var example = response.Examples["application/json"];
        Assert.IsNotNull(example);
    }

    [TestMethod]
    public void ParseSwaggerJsonWithReferenceShouldSucceed()
    {
        var swaggerFile = "TestData/swagger/ref_swagger2.json";
        var swagger = SwaggerJsonParser.Parse(swaggerFile);

        Assert.ContainsSingle(swagger.Paths);
        Assert.ContainsSingle(swagger.Paths["/contacts"].Metadata);
        var actionJObject = swagger.Paths["/contacts"].Metadata["patch"] as JObject;
        Assert.IsNotNull(actionJObject);
        var action = actionJObject.ToObject<OperationObject>();
        var parameters = action.Parameters;
        var schema = parameters[0].Metadata["schema"] as JObject;
        Assert.IsNotNull(schema);
        Assert.AreEqual("Sales", schema["example"]["department"].ToString());

        // Reference object
        var properties = schema["properties"] as JObject;
        Assert.IsNotNull(properties);
        Assert.AreEqual(2, properties.Count);
        Assert.AreEqual("string", properties["objectType"]["type"]);
        Assert.AreEqual("array", properties["provisioningErrors"]["type"]);
        var refProperty = properties["provisioningErrors"]["items"]["schema"] as JObject;
        Assert.IsNotNull(refProperty);
        Assert.AreEqual("string", refProperty["properties"]["errorDetail"]["type"]);

        schema = parameters[1].Metadata["schema"] as JObject;
        properties = schema["properties"] as JObject;
        var message = properties["message"];
        Assert.AreEqual("A message describing the error, intended to be suitable for display in a user interface.", message["description"]);

        Assert.ContainsSingle(action.Responses);
        var response = action.Responses["204"];
        Assert.ContainsSingle(response.Examples);
        var example = response.Examples["application/json"];
        Assert.IsNotNull(example);
    }

    [TestMethod]
    public void ParseSwaggerJsonWithTagShouldSucceed()
    {
        const string swaggerFile = "TestData/swagger/tag_swagger2.json";
        var swagger = SwaggerJsonParser.Parse(swaggerFile);

        Assert.AreEqual(3, swagger.Tags.Count);
        var tag0 = swagger.Tags[0];
        Assert.AreEqual("contact", tag0.Name);
        Assert.AreEqual("Everything about the **contacts**", tag0.Description);
        Assert.AreEqual("contact-bookmark", tag0.BookmarkId);
        Assert.ContainsSingle(tag0.Metadata);
        var externalDocs = (JObject)tag0.Metadata["externalDocs"];
        Assert.IsNotNull(externalDocs);
        Assert.AreEqual("Find out more", externalDocs["description"]);
        Assert.AreEqual("http://swagger.io", externalDocs["url"]);
    }

    [TestMethod]
    public void ParseSwaggerJsonWithPathParametersShouldSucceed()
    {
        const string swaggerFile = "TestData/swagger/pathParameters_swagger2.json";
        var swagger = SwaggerJsonParser.Parse(swaggerFile);

        Assert.ContainsSingle(swagger.Paths.Values);
        var parameters = swagger.Paths["/contacts"].Parameters;
        Assert.AreEqual(2, parameters.Count);

        // $ref parameter
        Assert.AreEqual("api-version", parameters[0].Name);
        Assert.AreEqual(false, parameters[0].Metadata["required"]);
        Assert.AreEqual("api version description", parameters[0].Description);

        // self defined parameter
        Assert.AreEqual("subscriptionId", parameters[1].Name);
        Assert.AreEqual(true, parameters[1].Metadata["required"]);
        Assert.AreEqual("subscription id", parameters[1].Description);
    }

    [TestMethod]
    public void ParseSwaggerJsonWithLoopReferenceShouldSucceed()
    {
        const string swaggerFile = "TestData/swagger/loopref_swagger2.json";
        var swagger = SwaggerJsonParser.Parse(swaggerFile);

        Assert.ContainsSingle(swagger.Paths.Values);
        var actionJObject = swagger.Paths["/contacts"].Metadata["patch"] as JObject;
        Assert.IsNotNull(actionJObject);
        var action = actionJObject.ToObject<OperationObject>();
        var schemaJObject = (JObject)action.Parameters[0].Metadata["schema"];
        var schemaObj = schemaJObject.ToString(Formatting.Indented);
        Assert.AreEqual(@"{
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

    [TestMethod]
    public void ParseSwaggerJsonWithExternalLoopReferenceShouldSucceed()
    {
        const string swaggerFile = "TestData/swagger/externalLoopRef_A.json";
        var swagger = SwaggerJsonParser.Parse(swaggerFile);

        Assert.ContainsSingle(swagger.Paths.Values);
        var actionJObject = swagger.Paths["/contacts"].Metadata["patch"] as JObject;
        Assert.IsNotNull(actionJObject);
        var action = actionJObject.ToObject<OperationObject>();
        var schemaJObject = (JObject)action.Parameters[0].Metadata["schema"];
        var schemaObj = schemaJObject.ToString(Formatting.Indented);
        Assert.AreEqual(@"{
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
    [TestMethod]
    public void ParseKeyWordSwaggerJsonShouldSucceed()
    {
        var swaggerFile = "TestData/swagger/resolveKeywordWithRefInside.json";
        var swagger = SwaggerJsonParser.Parse(swaggerFile);

        ///test x-ms-examples: unresolved.
        var xmsexamples = swagger.Metadata["x-ms-examples"] as JObject;
        Assert.IsNotNull(xmsexamples["$ref"]);

        var get = swagger.Paths["/{resourceUri}/providers/microsoft.insights/metrics"].Metadata["get"] as JObject;
        var responses = get["responses"] as JObject;

        ///test responses/../examples: unresolved.
        var response200 = responses["200"] as JObject;
        var examplesOfResponse200 = response200["examples"] as JObject;
        Assert.IsNotNull(examplesOfResponse200["$ref"]);

        ///test responses/examples: resolved.
        var examplesOfResponse = responses["examples"] as JObject;
        Assert.IsNull(examplesOfResponse["$ref"]);

        ///test parameters/examples: resolved.
        var parameters = get["parameters"] as JObject;
        var examplesOfParameters = parameters["examples"] as JObject;
        Assert.IsNull(examplesOfParameters["$ref"]);

        ///test definitions/../example: unresolved.
        var definitions = swagger.Definitions as JObject;
        var tag = definitions["Tag"] as JObject;
        var examplesOfTag = tag["example"] as JObject;
        Assert.IsNotNull(examplesOfTag["$ref"]);

        ///test definitions/example: resolved.
        var examplesOfDefinitions = definitions["example"] as JObject;
        Assert.IsNull(examplesOfDefinitions["$ref"]);

        ///test properties/../example: unresolved.
        var propertiesOfTag = tag["properties"] as JObject;
        var unresolvedOfTag = propertiesOfTag["unresolved"] as JObject;
        var examplesOfUnresolved = unresolvedOfTag["example"] as JObject;
        Assert.IsNotNull(examplesOfUnresolved["$ref"]);

        ///test properties/example: resolved.
        var examplesOfResolved = propertiesOfTag["example"] as JObject;
        Assert.IsNull(examplesOfResolved["$ref"]);
    }
}
