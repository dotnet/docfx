// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.OverwriteDocuments;

using Xunit;
using YamlDotNet.RepresentationModel;

namespace Docfx.Build.SchemaDriven.Tests;

public class SchemaFragmentsIteratorTest
{
    [Fact]
    public void TestSchemaFragmentsIterator()
    {
        // arrange
        var counter = new UidPropertyCounter();
        var iterator = new SchemaFragmentsIterator(counter);
        var schema = DocumentSchema.Load(File.ReadAllText("TestData/schemas/rest.mixed.schema.json"), "rest.mixed");
        var yamlStream = new YamlStream();
        using (var sr = new StreamReader("TestData/inputs/Suppressions.yml"))
        {
            yamlStream.Load(sr);
        }

        // act
        iterator.Traverse(yamlStream.Documents[0].RootNode, [], schema);

        // assert
        Assert.Single(counter.ExistingUids);
        Assert.Equal("management.azure.com.advisor.suppressions", counter.ExistingUids[0]);
        Assert.Single(counter.ExistingMarkdownProperties);
        Assert.Equal("definitions[name=\"Application 1\"]/properties[name=\"id\"]/description", counter.ExistingMarkdownProperties[0]);
        Assert.Equal(6, counter.MissingMarkdownProperties.Count);
    }

    private class UidPropertyCounter : ISchemaFragmentsHandler
    {
        public List<string> ExistingMarkdownProperties { get; } = [];

        public List<string> MissingMarkdownProperties { get; } = [];

        public List<string> ExistingUids { get; } = [];

        public void HandleUid(string uidKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid)
        {
            ExistingUids.Add(node.Children[uidKey].ToString());
        }

        public void HandleProperty(string propertyKey, YamlMappingNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string oPathPrefix, string uid)
        {
            if (schema.Properties[propertyKey].ContentType != ContentType.Markdown)
            {
                return;
            }

            if (node.Children.ContainsKey(propertyKey))
            {
                ExistingMarkdownProperties.Add($"{oPathPrefix}{propertyKey}");
            }
            else
            {
                MissingMarkdownProperties.Add($"{oPathPrefix}{propertyKey}");
            }
        }
    }
}
