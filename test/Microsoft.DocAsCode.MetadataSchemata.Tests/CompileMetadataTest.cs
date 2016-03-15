namespace Microsoft.DocAsCode.MetadataSchemata.Tests
{
    using System;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;
    using Xunit;

    using Microsoft.DocAsCode.MetadataSchemata;

    public class CompileMetadataTest
    {
        [Fact]
        public void CompileTest()
        {
            var schemaText = @"{
  ""api_scan"": {
    ""type"": ""boolean"",
    ""is_multivalued"": false,
    ""is_queryable"": false,
    ""is_required"": false,
    ""is_visible"": false,
    ""query_name"": null,
    ""display_name"": ""Api Scan"",
    ""choice_set"": null,
    ""description"": ""Indicate whether the depot need to API Scan.""
  },
  ""choice_test"": {
    ""type"": ""string"",
    ""is_multivalued"": true,
    ""is_queryable"": false,
    ""is_required"": false,
    ""is_visible"": false,
    ""query_name"": null,
    ""display_name"": ""Choice test"",
    ""choice_set"": [ ""A"", ""B"" ],
    ""description"": ""Test choice set!""
  },
  ""datetime_test"": {
    ""type"": ""datetime"",
    ""is_multivalued"": false,
    ""is_queryable"": false,
    ""is_required"": false,
    ""is_visible"": false,
    ""query_name"": null,
    ""display_name"": ""DateTime test"",
    ""description"": ""Test date time!""
  }
}";
            var schema = MetadataParser.Load(schemaText);
            var compiler = new MetadataCompiler();
            {
                compiler.Compile(schema, "test", "A.B", "X");
                var type = Type.GetType("A.B.X, test");
                dynamic obj = JsonConvert.DeserializeObject(@"{
    api_scan : true,
    choice_test: [ ""A"", ""B"",""A"" ],
    datetime_test : ""2016-03-14 11:28 AM"",
    unknown: ""what's this?""
}", type);
                Assert.Equal(true, obj.api_scan);
                Assert.Equal(new string[] { "A", "B", "A" }, obj.choice_test);
                Assert.Equal(new DateTime(2016, 3, 14, 11, 28, 00), obj.datetime_test);
                Assert.Equal("what's this?", (string)obj.__Additional["unknown"]);
            }
            {
                compiler.Namer = s => Regex.Replace(s, "(^|_)([a-z])", m => m.Groups[2].Value.ToUpper());
                compiler.Compile(schema, "test_Namer", "A.B", "X");
                var type = Type.GetType("A.B.X, test_Namer");
                dynamic obj = JsonConvert.DeserializeObject(@"{
    api_scan : true,
    choice_test: [ ""A"", ""B"",""A"" ],
    unknown: ""what's this?""
}", type);
                Assert.Equal(true, obj.ApiScan);
                Assert.Equal(new string[] { "A", "B", "A" }, obj.ChoiceTest);
                Assert.Equal("what's this?", (string)obj.__Additional["unknown"]);
            }
        }

        [Fact]
        public void CompileSchemaTest()
        {
            var schema = MetadataParser.GetMetadataSchema();
            Assert.Throws<ArgumentException>(
                () => new MetadataCompiler().Compile(schema, "schema", "A.B", "X"));
        }
    }
}
