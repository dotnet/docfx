namespace Microsoft.DocAsCode.Metadata.Tests
{
    using Microsoft.DocAsCode.Metadata;

    using Xunit;

    public class MetadataValidationTest
    {
        [Fact]
        public void TestValidation()
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
  }
}";
            var ss = MetadataParser.GetMetadataSchema();
            Assert.True(ss.ValidateMetadata(schemaText).IsSuccess);
            var schema = MetadataParser.Load(schemaText);
            var vrs = schema.ValidateMetadata(@"{
    api_scan: true,
    choice_test: [ ""A"", ""B"" ],
    unknown_good1: ""1"",
    unknown_good2: 2
}");
            Assert.True(vrs.IsSuccess);

            vrs = schema.ValidateMetadata(@"{
    ""api_scan"": 1,
    ""unknown-bad1"": ""1"",
    ""unknown_bad2"": {},
    ""unknown_bad3"": [""1"",2],
    choice_test: [ ""A"", ""Bad!"" ],
}");
            Assert.False(vrs.IsSuccess);
            Assert.Equal(5, vrs.Items.Count);
            Assert.Equal(ValidationErrorCodes.WellknownMetadata.UnexpectedType, vrs.Items[0].Code);
            Assert.Equal("api_scan", vrs.Items[0].Path);
            Assert.Equal(ValidationErrorCodes.UnknownMetadata.BadNaming, vrs.Items[1].Code);
            Assert.Equal("unknown-bad1", vrs.Items[1].Path);
            Assert.Equal(ValidationErrorCodes.UnknownMetadata.UnexpectedType, vrs.Items[2].Code);
            Assert.Equal("unknown_bad2", vrs.Items[2].Path);
            Assert.Equal(ValidationErrorCodes.UnknownMetadata.UnexpectedType, vrs.Items[3].Code);
            Assert.Equal("unknown_bad3[1]", vrs.Items[3].Path);
            Assert.Equal(ValidationErrorCodes.WellknownMetadata.UndefinedValue, vrs.Items[4].Code);
            Assert.Equal("choice_test[1]", vrs.Items[4].Path);
        }
    }
}
