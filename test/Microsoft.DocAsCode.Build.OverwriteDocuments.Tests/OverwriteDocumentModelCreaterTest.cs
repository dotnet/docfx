// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using Markdig;
    using Markdig.Syntax;

    using Microsoft.DocAsCode.Build.OverwriteDocuments;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Tests.Common;

    using Newtonsoft.Json.Linq;

    using Xunit;

    [Trait("Owner", "jipe")]
    [Trait("EntityType", "OverwriteDocumentModelCreater")]
    public class OverwriteDocumentModelCreaterTest
    {
        private TestLoggerListener _listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter("overwrite_document_model_creater");

        [Fact]
        public void YamlCodeBlockTest()
        {
            var yamlCodeBlockString = @"name: name overwrite
definitions:
- name: Application 1
  properties:
  - name: id
    description: overwrite in yaml block
  - name: displayName
    description: overwrite in yaml block";
            var testYamlCodeBlock = Markdown.Parse($"```\n{yamlCodeBlockString}\n```")[0];
            var actual = JObject.FromObject(OverwriteDocumentModelCreater.ConvertYamlCodeBlock(yamlCodeBlockString, testYamlCodeBlock));
            Assert.Equal("name overwrite", actual["name"].ToString());
            Assert.Equal("Application 1", actual["definitions"][0]["name"].ToString());
            Assert.Equal("id", actual["definitions"][0]["properties"][0]["name"].ToString());
            Assert.Equal("overwrite in yaml block", actual["definitions"][0]["properties"][1]["description"].ToString());
        }

        [Fact]
        public void ContentConvertTest()
        {
            var testBlockList = Markdown.Parse("Test").ToList();

            string[] testOPaths =
            {
                "summary",
                "return/description",
                "return/type",
                "function/parameters[id=\"para1\"]/description",
                "function/parameters[id=\"para1\"]/type",
                "function/parameters[id=\"para2\"]/description",
            };
            var contents = new List<MarkdownPropertyModel>();
            foreach (var item in testOPaths)
            {
                contents.Add(new MarkdownPropertyModel
                {
                    PropertyName = item,
                    PropertyNameSource = Markdown.Parse($"## `{item}`")[0],
                    PropertyValue = testBlockList
                });
            }

            var contentsMetadata = new OverwriteDocumentModelCreater("test.yml.md").ConvertContents(new Dictionary<object, object>(), contents);
            Assert.Equal(3, contentsMetadata.Count);
            Assert.Equal("summary,return,function", ExtractDictionaryKeys(contentsMetadata));
            Assert.Equal(2, ((Dictionary<object, object>)contentsMetadata["return"]).Count);
            Assert.Equal("description,type",
                ExtractDictionaryKeys((Dictionary<object, object>)contentsMetadata["return"]));
            Assert.Single((Dictionary<object, object>)contentsMetadata["function"]);
            Assert.Equal(2,
                ((List<object>)((Dictionary<object, object>)contentsMetadata["function"])["parameters"]).Count);
            Assert.Equal("id,description,type",
                ExtractDictionaryKeys(
                    (Dictionary<object, object>)((List<object>)((Dictionary<object, object>)contentsMetadata["function"])["parameters"])[0]));
            Assert.Equal("id,description",
                ExtractDictionaryKeys(
                    (Dictionary<object, object>)((List<object>)((Dictionary<object, object>)contentsMetadata["function"])["parameters"])[1]));
        }

        [Fact]
        public void DuplicateOPathInMarkdownSectionTest()
        {
            var testOPath = "function/parameters";
            var contents = new List<MarkdownPropertyModel>();

            contents.Add(new MarkdownPropertyModel
            {
                PropertyName = testOPath,
                PropertyNameSource = Markdown.Parse($"## `{testOPath}`")[0],
                PropertyValue = Markdown.Parse("test1").ToList()
            });
            contents.Add(new MarkdownPropertyModel
            {
                PropertyName = testOPath,
                PropertyNameSource = Markdown.Parse($"## `{testOPath}`")[0],
                PropertyValue = Markdown.Parse("test2").ToList()
            });

            Dictionary<string, object> contentsMetadata;
            Logger.RegisterListener(_listener);
            try
            {
                using (new LoggerPhaseScope("overwrite_document_model_creater"))
                {
                    contentsMetadata = new OverwriteDocumentModelCreater("test.yml.md").ConvertContents(new Dictionary<object, object>(), contents);
                }
            }
            finally
            {
                Logger.UnregisterListener(_listener);
            }

            var logs = _listener.Items;
            Assert.Single(logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments));
            Assert.Single(contentsMetadata);
            Assert.Equal("test2",
                ((ParagraphBlock)((MarkdownDocument)((Dictionary<object, object>)contentsMetadata["function"])["parameters"])[0]).Inline.FirstChild.ToString());
        }

        [Fact]
        public void DuplicateOPathsInYamlCodeBlockAndContentsBlockTest()
        {
            // Yaml section
            var yamlCodeBlockString = @"name: name overwrite
definitions:
- name: Application 1
  properties:
  - name: id
    description: overwrite in yaml block
  - name: displayName
    description: overwrite in yaml block";
            var testYamlCodeBlock = Markdown.Parse($"```\n{yamlCodeBlockString}\n```")[0];
            var yamlMetadata = OverwriteDocumentModelCreater.ConvertYamlCodeBlock(yamlCodeBlockString, testYamlCodeBlock);

            // Markdown section
            var testBlockList = Markdown.Parse("Test").ToList();

            string[] testOPaths =
            {
                "summary",
                "definitions[name=\"Application 1\"]/properties[name=\"displayName\"]/description",
                "definitions[name=\"Application 1\"]/properties[name=\"summary\"]/description",
            };
            var contents = new List<MarkdownPropertyModel>();
            foreach (var item in testOPaths)
            {
                contents.Add(new MarkdownPropertyModel
                {
                    PropertyName = item,
                    PropertyNameSource = Markdown.Parse($"## `{item}`")[0],
                    PropertyValue = testBlockList
                });
            }

            Dictionary<string, object> metadata;
            Logger.RegisterListener(_listener);
            try
            {
                using (new LoggerPhaseScope("overwrite_document_model_creater"))
                {
                    metadata = new OverwriteDocumentModelCreater("test.yml.md").ConvertContents(yamlMetadata, contents);
                }
            }
            finally
            {
                Logger.UnregisterListener(_listener);
            }

            var logs = _listener.Items;
            var warningLogs = logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments).ToList();
            Assert.Single(warningLogs);
            Assert.Equal("Two duplicate OPaths `definitions[name=\"Application 1\"]/properties[name=\"displayName\"]/description` in yaml code block and contents block", warningLogs[0].Message);
            Assert.Equal("name overwrite", metadata["name"].ToString());
            Assert.Equal(typeof(MarkdownDocument), metadata["summary"].GetType());
            Assert.Equal(typeof(MarkdownDocument), ((Dictionary<object, object>)((List<object>)((Dictionary<object, object>)((List<object>)metadata["definitions"])[0])["properties"])[1])["description"].GetType());
            Assert.Equal(typeof(MarkdownDocument), ((Dictionary<object, object>)((List<object>)((Dictionary<object, object>)((List<object>)metadata["definitions"])[0])["properties"])[2])["description"].GetType());
        }

        [Fact]
        public void InvalidOPathsTest1()
        {
            var testBlockList = Markdown.Parse("Test").ToList();
            string[] testOPaths =
            {
                "function/parameters/description",
                "function/parameters[id=\"para1\"]/type",
            };
            var contents = new List<MarkdownPropertyModel>();
            foreach (var item in testOPaths)
            {
                contents.Add(new MarkdownPropertyModel
                {
                    PropertyName = item,
                    PropertyNameSource = Markdown.Parse($"## `{item}`")[0],
                    PropertyValue = testBlockList
                });
            }

            var ex = Assert.Throws<MarkdownFragmentsException>(() => new OverwriteDocumentModelCreater("test.yml.md").ConvertContents(new Dictionary<object, object>(), contents));
            Assert.Equal(
                "A(parameters) is not expected to be an array like \"A[c=d]/B\", however it is used as an array in line 0 with `parameters[id=\"para1\"]/...`",
                ex.Message);
            Assert.Equal(0, ex.Position);
        }

        [Fact]
        public void InvalidOPathsTest2()
        {
            var testBlockList = Markdown.Parse("Test").ToList();
            string[] testOPaths =
            {
                "function/parameters[id=\"para1\"]/type",
                "function/parameters/description",
            };
            var contents = new List<MarkdownPropertyModel>();
            foreach (var item in testOPaths)
            {
                contents.Add(new MarkdownPropertyModel
                {
                    PropertyName = item,
                    PropertyNameSource = Markdown.Parse($"## `{item}`")[0],
                    PropertyValue = testBlockList
                });
            }

            var ex = Assert.Throws<MarkdownFragmentsException>(() => new OverwriteDocumentModelCreater("test.yml.md").ConvertContents(new Dictionary<object, object>(), contents));
            Assert.Equal(
                "A(parameters) is not expected to be an object like \"A/B\", however it is used as an object in line 0 with `parameters/...`",
                ex.Message);
            Assert.Equal(0, ex.Position);
        }

        private string ExtractDictionaryKeys(Dictionary<object, object> dict)
        {
            return string.Join(",", dict.Keys.ToArray());
        }

        private string ExtractDictionaryKeys(Dictionary<string, object> dict)
        {
            return string.Join(",", dict.Keys.ToArray());
        }
    }
}
