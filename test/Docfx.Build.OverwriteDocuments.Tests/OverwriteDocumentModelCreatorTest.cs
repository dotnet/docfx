// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Tests.Common;
using Markdig;
using Markdig.Syntax;
using Newtonsoft.Json.Linq;

namespace Docfx.Build.OverwriteDocuments.Tests;

[TestClass]
public class OverwriteDocumentModelCreatorTest
{
    private readonly TestLoggerListener _listener = new();

    [TestMethod]
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
        var actual = JObject.FromObject(OverwriteDocumentModelCreator.ConvertYamlCodeBlock(yamlCodeBlockString, testYamlCodeBlock));
        Assert.AreEqual("name overwrite", actual["name"].ToString());
        Assert.AreEqual("Application 1", actual["definitions"][0]["name"].ToString());
        Assert.AreEqual("id", actual["definitions"][0]["properties"][0]["name"].ToString());
        Assert.AreEqual("overwrite in yaml block", actual["definitions"][0]["properties"][1]["description"].ToString());
    }

    [TestMethod]
    public void ContentConvertTest()
    {
        var testBlockList = Markdown.Parse("Test").ToList();

        string[] testOPaths =
        [
            "summary",
            "return/description",
            "return/type",
            "function/parameters[id=\"para1\"]/description",
            "function/parameters[id=\"para1\"]/type",
            "function/parameters[id=\"para2\"]/description",
        ];
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

        var contentsMetadata = new OverwriteDocumentModelCreator("test.yml.md").ConvertContents([], contents);
        Assert.AreEqual(3, contentsMetadata.Count);
        Assert.AreEqual("summary,return,function", ExtractDictionaryKeys(contentsMetadata));
        Assert.AreEqual(2, ((Dictionary<object, object>)contentsMetadata["return"]).Count);
        Assert.AreEqual("description,type",
            ExtractDictionaryKeys((Dictionary<object, object>)contentsMetadata["return"]));
        Assert.ContainsSingle((Dictionary<object, object>)contentsMetadata["function"]);
        Assert.AreEqual(2,
            ((List<object>)((Dictionary<object, object>)contentsMetadata["function"])["parameters"]).Count);
        Assert.AreEqual("id,description,type",
            ExtractDictionaryKeys(
                (Dictionary<object, object>)((List<object>)((Dictionary<object, object>)contentsMetadata["function"])["parameters"])[0]));
        Assert.AreEqual("id,description",
            ExtractDictionaryKeys(
                (Dictionary<object, object>)((List<object>)((Dictionary<object, object>)contentsMetadata["function"])["parameters"])[1]));
    }

    [TestMethod]
    public void DuplicateOPathInMarkdownSectionTest()
    {
        var testOPath = "function/parameters";
        var contents = new List<MarkdownPropertyModel>
        {
            new MarkdownPropertyModel
            {
                PropertyName = testOPath,
                PropertyNameSource = Markdown.Parse($"## `{testOPath}`")[0],
                PropertyValue = Markdown.Parse("test1").ToList()
            },
            new MarkdownPropertyModel
            {
                PropertyName = testOPath,
                PropertyNameSource = Markdown.Parse($"## `{testOPath}`")[0],
                PropertyValue = Markdown.Parse("test2").ToList()
            }
        };

        Dictionary<string, object> contentsMetadata;
        Logger.RegisterListener(_listener);
        try
        {
            contentsMetadata = new OverwriteDocumentModelCreator("test.yml.md").ConvertContents([], contents);
        }
        finally
        {
            Logger.UnregisterListener(_listener);
        }

        var logs = _listener.Items;
        Assert.ContainsSingle(logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments));
        Assert.ContainsSingle(contentsMetadata);
        Assert.AreEqual("test2",
            ((ParagraphBlock)((MarkdownDocument)((Dictionary<object, object>)contentsMetadata["function"])["parameters"])[0]).Inline.FirstChild.ToString());
    }

    [TestMethod]
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
        var yamlMetadata = OverwriteDocumentModelCreator.ConvertYamlCodeBlock(yamlCodeBlockString, testYamlCodeBlock);

        // Markdown section
        var testBlockList = Markdown.Parse("Test").ToList();

        string[] testOPaths =
        [
            "summary",
            "definitions[name=\"Application 1\"]/properties[name=\"displayName\"]/description",
            "definitions[name=\"Application 1\"]/properties[name=\"summary\"]/description",
        ];
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
            metadata = new OverwriteDocumentModelCreator("test.yml.md").ConvertContents(yamlMetadata, contents);
        }
        finally
        {
            Logger.UnregisterListener(_listener);
        }

        var logs = _listener.Items;
        var warningLogs = logs.Where(l => l.Code == WarningCodes.Overwrite.InvalidMarkdownFragments).ToList();
        Assert.ContainsSingle(warningLogs);
        Assert.AreEqual("Two duplicate OPaths `definitions[name=\"Application 1\"]/properties[name=\"displayName\"]/description` in yaml code block and contents block", warningLogs[0].Message);
        Assert.AreEqual("name overwrite", metadata["name"].ToString());
        Assert.AreEqual(typeof(MarkdownDocument), metadata["summary"].GetType());
        Assert.AreEqual(typeof(MarkdownDocument), ((Dictionary<object, object>)((List<object>)((Dictionary<object, object>)((List<object>)metadata["definitions"])[0])["properties"])[1])["description"].GetType());
        Assert.AreEqual(typeof(MarkdownDocument), ((Dictionary<object, object>)((List<object>)((Dictionary<object, object>)((List<object>)metadata["definitions"])[0])["properties"])[2])["description"].GetType());
    }

    [TestMethod]
    public void InvalidOPathsTest1()
    {
        var testBlockList = Markdown.Parse("Test").ToList();
        string[] testOPaths =
        [
            "function/parameters/description",
            "function/parameters[id=\"para1\"]/type",
        ];
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

        var ex = Assert.Throws<MarkdownFragmentsException>(() => new OverwriteDocumentModelCreator("test.yml.md").ConvertContents([], contents));
        Assert.AreEqual(
            "A(parameters) is not expected to be an array like \"A[c=d]/B\", however it is used as an array in line 0 with `parameters[id=\"para1\"]/...`",
            ex.Message);
        Assert.AreEqual(0, ex.Position);
    }

    [TestMethod]
    public void InvalidOPathsTest2()
    {
        var testBlockList = Markdown.Parse("Test").ToList();
        string[] testOPaths =
        [
            "function/parameters[id=\"para1\"]/type",
            "function/parameters/description",
        ];
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

        var ex = Assert.Throws<MarkdownFragmentsException>(() => new OverwriteDocumentModelCreator("test.yml.md").ConvertContents([], contents));
        Assert.AreEqual(
            "A(parameters) is not expected to be an object like \"A/B\", however it is used as an object in line 0 with `parameters/...`",
            ex.Message);
        Assert.AreEqual(0, ex.Position);
    }

    private static string ExtractDictionaryKeys(Dictionary<object, object> dict)
    {
        return string.Join(',', dict.Keys.ToArray());
    }

    private static string ExtractDictionaryKeys(Dictionary<string, object> dict)
    {
        return string.Join(',', dict.Keys.ToArray());
    }
}
