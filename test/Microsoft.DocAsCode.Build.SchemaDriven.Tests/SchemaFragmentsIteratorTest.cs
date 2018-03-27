// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Tests
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Build.OverwriteDocuments;

    using Xunit;
    using YamlDotNet.RepresentationModel;

    [Trait("Owner", "renzeyu")]
    [Trait("EntityType", "SchemaDrivenProcessorTest")]
    public class SchemaFragmentsIteratorTest
    {
        [Fact]
        public void TestSchemaFragmentsIterator()
        {
            // arrage
            var counter = new UidPropertyCounter();
            var iterator = new SchemaFragmentsIterator(counter);
            DocumentSchema schema;
            using (var sr = new StreamReader("TestData/schemas/rest.mixed.schema.json"))
            {
                schema = DocumentSchema.Load(sr, "rest.mixed");
            }
            var yamlStream = new YamlStream();
            using (var sr = new StreamReader("TestData/inputs/Suppressions.yml"))
            {
                yamlStream.Load(sr);
            }

            // act
            iterator.Traverse(yamlStream.Documents[0].RootNode, new Dictionary<string, MarkdownFragment>(), schema);

            // assert
            Assert.Single(counter.ExistingUids);
            Assert.Equal("management.azure.com.advisor.suppressions", counter.ExistingUids[0]);
            Assert.Single(counter.ExistingMarkdownProperties);
            Assert.Equal("definitions[name=\"Application 1\"]/properties[name=\"id\"]/description", counter.ExistingMarkdownProperties[0]);
            Assert.Equal(6, counter.MissingMarkdownProperties.Count);
        }

        private class UidPropertyCounter : ISchemaFragmentsHandler
        {
            public List<string> ExistingMarkdownProperties { get; private set; } = new List<string>();

            public List<string> MissingMarkdownProperties { get; private set; } = new List<string>();

            public List<string> ExistingUids { get; private set; } = new List<string>();

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
}
