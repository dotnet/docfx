// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;
using Docfx.Tests.Common;

using Xunit;

namespace Docfx.Build.Engine.Tests;

[Collection("docfx STA")]
public partial class TemplateManagerUnitTest : TestBase
{
    private readonly string _inputFolder;
    private readonly string _outputFolder;

    public TemplateManagerUnitTest()
    {
        _inputFolder = GetRandomFolder();
        _outputFolder = GetRandomFolder();
    }

    [Trait("Related", "ResourceFinder")]
    [Fact]
    public void TestTemplateManagerWithMultipleThemesShouldWork()
    {
        // If the same resource name exists in the override folder, use the overridden one
        var themes = new List<string> { "tmpl1", "tmpl/tmpl1" };
        var manager = new TemplateManager(null, themes, null);
        var outputFolder = Path.Combine(_outputFolder, "TestTemplateManager_MultipleThemes");
        manager.ProcessTheme(outputFolder, true);
        // 1. Support tmpl1.zip
        var file1 = Path.Combine(outputFolder, "tmpl1.dot.$");
        Assert.True(File.Exists(file1));
        Assert.Equal("Override: This is file with complex filename characters", File.ReadAllText(file1));

        // backslash is also supported
        var file2 = Path.Combine(outputFolder, "sub/file1");
        Assert.True(File.Exists(file2));
        Assert.Equal("Override: This is file inside a subfolder", File.ReadAllText(file2));
    }

    #region Mustache template processor test

    [Trait("Related", "TemplateProcessor")]
    [Trait("Related", "Mustache")]
    [Fact]
    public void TestMustacheTemplateProcessSingleTemplateWithNoScriptShouldWork()
    {
        // 1. Prepare template
        var templateName = "NoScript";
        string template = @"
{{#model}}
name1={{name1}},
name2={{name2}};
{{/model}}
";
        var model = new
        {
            model = new object[]
           {
               new {name1 = "test1"},
               new {name2 = "test2"},
           }
        };
        var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_NoScript.yml");
        var item = new InternalManifestItem
        {
            DocumentType = string.Empty,
            Key = modelFileName,
            FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
            ResourceFile = modelFileName,
            LocalPathFromRoot = modelFileName,
        };
        ProcessTemplate(templateName, null, new[] { item }, model, _outputFolder, Tuple.Create("default.tmpl", template));

        var outputFile = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, null));
        Assert.True(File.Exists(outputFile));
        AssertEqualIgnoreCrlf(@"
name1=test1,
name2=;
name1=,
name2=test2;
", File.ReadAllText(outputFile));
    }

    [Trait("Related", "TemplateProcessor")]
    [Trait("Related", "Mustache")]
    [Fact]
    public void TestMustacheTemplateProcessSingleTemplateWithNoScriptWithPartialShouldWork()
    {
        // 1. Prepare template
        var templateName = "Subfolder/NoScriptWithPartial";
        string template = @"
{{#model}}
{{name}}
{{/model}}
{{>partial1}}
";
        string partial1 = @"partial 1:
{{>partial2}}";
        string partial2 = @"partial 2:
{{#model}}
{{name}}
{{/model}}
";
        var model = new
        {
            model = new[]
           {
               new {name = "test1"},
               new {name = "test2"},
           }
        };
        var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_NoScriptWithPartial.yml");
        var item = new InternalManifestItem
        {
            DocumentType = string.Empty,
            Key = modelFileName,
            FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
            ResourceFile = modelFileName,
            LocalPathFromRoot = modelFileName,
        };
        ProcessTemplate(
            templateName,
            null,
            new[] { item },
            model,
            _outputFolder,
            Tuple.Create("default.tmpl", template),
            Tuple.Create("partial1.tmpl.partial", partial1),
            Tuple.Create("partial2.tmpl.partial", partial2));

        var outputFile = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, null));
        Assert.True(File.Exists(outputFile));
        AssertEqualIgnoreCrlf(@"
test1
test2
partial 1:
partial 2:
test1
test2
", File.ReadAllText(outputFile));
    }

    [Trait("Related", "TemplateProcessor")]
    [Trait("Related", "Mustache")]
    [Fact]
    public void TestMustacheTemplateWithMasterPageShouldWork()
    {
        // 1. Prepare template
        var templateName = "Subfolder/WithMasterPage";
        string template = @"
{{!master('_layout/master.html')}}
{{!master(' _layout/invalid1.html ')}}
{{#model}}
{{name}}
{{/model}}
{{>partial1}}
{{!master( _layout/invalid2.html )}}
";
        string partial1 = @"partial 1:
{{>partial2}}";
        string partial2 = @"partial 2:
{{#model}}
{{name}}
{{/model}}
";

        string master = @"
{{ !include('reference1.html') }}
Hello Master
{{!body}}
Hello Body
{{!body}}
";
        var model = new
        {
            model = new[]
           {
               new {name = "test1"},
               new {name = "test2"},
           }
        };
        var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_WithMasterPage.yml");
        var item = new InternalManifestItem
        {
            DocumentType = string.Empty,
            Key = modelFileName,
            FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
            ResourceFile = modelFileName,
            LocalPathFromRoot = modelFileName,
        };
        ProcessTemplate(
            templateName,
            null,
            new[] { item },
            model,
            _outputFolder,
            Tuple.Create("default.tmpl", template),
            Tuple.Create("_layout/master.html", master),
            Tuple.Create("reference1.html", string.Empty),
            Tuple.Create("partial1.tmpl.partial", partial1),
            Tuple.Create("partial2.tmpl.partial", partial2));

        var outputFile = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, null));
        Assert.True(File.Exists(Path.Combine(_outputFolder, "reference1.html")));
        Assert.True(File.Exists(outputFile));
        AssertEqualIgnoreCrlf(@"
Hello Master

test1
test2
partial 1:
partial 2:
test1
test2
Hello Body

test1
test2
partial 1:
partial 2:
test1
test2
", File.ReadAllText(outputFile));
    }

    [Trait("Related", "TemplateProcessor")]
    [Trait("Related", "Mustache")]
    [Fact]
    public void TestMustacheTemplateProcessInvalidTemplateShouldFail()
    {
        var templateName = "InvalidTemplate.html";
        string inputFolder = null;
        var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_InvalidTemplate.yml");
        var item = new InternalManifestItem
        {
            FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
            DocumentType = string.Empty,
            Key = modelFileName,
            LocalPathFromRoot = modelFileName,
        };
        ProcessTemplate(templateName, inputFolder, new[] { item }, new object(), _outputFolder,
            Tuple.Create("default.invalidtmpl", string.Empty),
            Tuple.Create("default.js", string.Empty),
            Tuple.Create("reference1.html", string.Empty),
            Tuple.Create("reference2.html", string.Empty)
            );
    }

    [Trait("Related", "TemplateProcessor")]
    [Trait("Related", "Mustache")]
    [Fact]
    public void TestMustacheTemplateProcessSingleTemplateWithIncludesShouldWork()
    {
        var templateName = "WithIncludes.html";

        string template = @"
{{ !include('reference1.html') }}
{{ !include('reference2.html') }}
{{#model}}
{{name}}
{{/model}}
";
        string script = @"
exports.transform = function (model){
    model.model.push({name:'test2'});
    return model;
}";

        var model = new
        {
            model = new List<object>
           {
               new {name = "test1"},
           }
        };

        var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_WithIncludes.yml");
        string inputFolder = null;
        var item = new InternalManifestItem
        {
            FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
            DocumentType = string.Empty,
            Key = modelFileName,
            LocalPathFromRoot = modelFileName,
        };
        ProcessTemplate(templateName, inputFolder, new[] { item }, model, _outputFolder,
            Tuple.Create("default.html.tmpl", template),
            Tuple.Create("default.html.js", script),
            Tuple.Create("reference1.html", string.Empty),
            Tuple.Create("reference2.html", string.Empty)
            );
        var outputFilePath = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, "html"));
        Assert.True(File.Exists(outputFilePath));
        Assert.True(File.Exists(Path.Combine(_outputFolder, "reference1.html")));
        Assert.True(File.Exists(Path.Combine(_outputFolder, "reference2.html")));
        AssertEqualIgnoreCrlf(@"
test1
test2
", File.ReadAllText(outputFilePath));
    }

    [Trait("Related", "TemplateProcessor")]
    [Fact]
    public void TestMustacheTemplateProcessSingleTemplateWithRequireScriptShouldWork()
    {
        var templateName = "WithRequireScript.html";

        string template = @"
{{#model}}
{{#result1}}result1 = true{{/result1}}
{{#result2}}result2 = true{{/result2}}
{{#result3}}result3 = true{{/result3}}
{{/model}}
";
        string mainScript = @"
var util = require('./util.js');

exports.transform = function (model){
    var url = 'https://www.microsoft.com';
    model.model.result1 = util.isAbsolutePath(url);
    model.model.result2 = util.isAbsolutePath(url);
    model.model.result3 = util.isAbsolutePath(url);
    return model;
}";
        string utilScript = @"
exports.isAbsolutePath = isAbsolutePath;

function isAbsolutePath(path) {
    return /^(\w+:)?\/\//g.test(path);
}
";

        var model = new
        {
            model = new Dictionary<string, object>()
        };

        var modelFileName = Path.Combine(_inputFolder, "TestTemplateProcessor_WithRequireScript.yml");
        string inputFolder = null;
        var item = new InternalManifestItem
        {
            FileWithoutExtension = Path.ChangeExtension(modelFileName, null),
            DocumentType = string.Empty,
            Key = modelFileName,
            LocalPathFromRoot = modelFileName,
        };
        ProcessTemplate(templateName, inputFolder, new[] { item }, model, _outputFolder,
            Tuple.Create("default.html.tmpl", template),
            Tuple.Create("default.html.js", mainScript),
            Tuple.Create("util.js", utilScript)
            );
        var outputFilePath = Path.Combine(_outputFolder, Path.ChangeExtension(modelFileName, "html"));
        Assert.True(File.Exists(outputFilePath));
        AssertEqualIgnoreCrlf(@"
result1 = true
result2 = true
result3 = true
", File.ReadAllText(outputFilePath));
    }

    [Trait("Related", "TemplateProcessor")]
    [Trait("Related", "Mustache")]
    [Fact]
    public void TestMustacheTemplateProcessTemplateFolderWithDifferentTypeShouldWork()
    {
        var templateName = "TemplateFolder.html";
        string defaultTemplate = @"
default:
{{#model}}
{{name}}
{{/model}}
";
        string conceptualTemplate = @"
conceptual:
{{#model}}
{{name}}
{{/model}}
";
        string script = @"
exports.transform = function (model){
    model.model.push({name:'test2'});
    return model;
}";

        var model = new
        {
            model = new List<object>
           {
               new {name = "test1"},
           }
        };

        string inputFolder = null;
        var item1 = new InternalManifestItem
        {
            FileWithoutExtension = "TestTemplateProcessor_TemplateFolderWithDifferentType1",
            Key = "x.yml",
            DocumentType = "Conceptual",
            LocalPathFromRoot = "TestTemplateProcessor_TemplateFolderWithDifferentType1.md",
        };
        var item2 = new InternalManifestItem
        {
            DocumentType = string.Empty,
            FileWithoutExtension = "TestTemplateProcessor_TemplateFolderWithDifferentType2",
            Key = "y.yml",
            LocalPathFromRoot = "TestTemplateProcessor_TemplateFolderWithDifferentType2.md",
        };
        ProcessTemplate(templateName, inputFolder, new[] { item1, item2 }, model, _outputFolder,
            Tuple.Create("default.html.tmpl", defaultTemplate),
            Tuple.Create("conceptual.md.tmpl", conceptualTemplate),
            Tuple.Create("default.html.js", script),
            Tuple.Create("conceptual.md.js", script)
            );
        var outputFilePath1 = Path.Combine(_outputFolder, "TestTemplateProcessor_TemplateFolderWithDifferentType1.md");
        Assert.True(File.Exists(outputFilePath1));
        AssertEqualIgnoreCrlf(@"
conceptual:
test1
test2
", File.ReadAllText(outputFilePath1));
        var outputFilePath2 = Path.Combine(_outputFolder, "TestTemplateProcessor_TemplateFolderWithDifferentType2.html");
        Assert.True(File.Exists(outputFilePath2));
        AssertEqualIgnoreCrlf(@"
default:
test1
test2
", File.ReadAllText(outputFilePath2));
    }

    [Trait("Related", "TemplateProcessor")]
    [Trait("Related", "Mustache")]
    [Fact]
    public void TestMustacheTemplateWithScriptWithLongStringInModelShouldWork()
    {
        // https://github.com/sebastienros/jint/issues/357

        var templateName = "TemplateFolder.html";
        string defaultTemplate = "{{name}}";
        var name = "this is a looooooooooooooooooooooooooooooooooooog name";
        var longName = string.Concat(Enumerable.Repeat(name, 20000));
        string script = @"
exports.transform = function (model){
    return {
        name: JSON.stringify(model)
    };
}";

        var model = new
        {
            model = new
            {
                name = longName,
            }
        };

        string inputFolder = null;
        var item1 = new InternalManifestItem
        {
            FileWithoutExtension = "TestMustacheTemplateWithScriptWithLongStringInModelShouldWork",
            Key = "x.yml",
            DocumentType = "Conceptual",
            LocalPathFromRoot = "TestMustacheTemplateWithScriptWithLongStringInModelShouldWork.md",
        };
        ProcessTemplate(templateName, inputFolder, new[] { item1 }, model, _outputFolder,
            Tuple.Create("default.html.tmpl", defaultTemplate),
            Tuple.Create("default.html.js", script)
            );
        var outputFilePath1 = Path.Combine(_outputFolder, "TestMustacheTemplateWithScriptWithLongStringInModelShouldWork.html");
        Assert.True(File.Exists(outputFilePath1));
        Assert.Equal($"{{&quot;model&quot;:{{&quot;name&quot;:&quot;{longName}&quot;}},&quot;__global&quot;:{{}}}}", File.ReadAllText(outputFilePath1));
    }

    [Fact]
    public void JsRegexShouldNotShareStatusAmongFunctions()
    {
        // https://github.com/sebastienros/jint/issues/364

        var templateName = "TemplateFolder.html";
        string defaultTemplate = "{{result1}},{{result2}}";
        string script = @"
exports.transform = function (model){
    var url = 'https://www.example.com';
    var result1 = isAbsolutePath(url);
    var result2 = isAbsolutePath(url);

    function isAbsolutePath(path) {
        return /^(\w+:)?\/\//g.test(path);
    }

    return {
        result1: result1,
        result2: result2
    };
}";

        var model = new object();
        var item1 = new InternalManifestItem
        {
            FileWithoutExtension = "file",
            Key = "x.yml",
            DocumentType = "Conceptual",
            LocalPathFromRoot = "file.md",
        };
        ProcessTemplate(templateName, null, new[] { item1 }, model, _outputFolder,
            Tuple.Create("default.html.tmpl", defaultTemplate),
            Tuple.Create("default.html.js", script)
            );
        var outputFilePath1 = Path.Combine(_outputFolder, "file.html");
        Assert.True(File.Exists(outputFilePath1));
        Assert.Equal("True,True", File.ReadAllText(outputFilePath1));
    }

    [Fact]
    public void JsCreateDateShouldNotThrowError()
    {
        var templateName = "TemplateFolder.html";
        string defaultTemplate = "{{date}}";
        string script = @"
exports.transform = function (model){
    return {
        date: new Date(new Date('2019-08-19T05:40:30.4629999Z')).toISOString()
    };
}";

        var model = new object();
        var item1 = new InternalManifestItem
        {
            FileWithoutExtension = "file",
            Key = "x.yml",
            DocumentType = "Conceptual",
            LocalPathFromRoot = "file.md",
        };
        ProcessTemplate(templateName, null, new[] { item1 }, model, _outputFolder,
            Tuple.Create("default.html.tmpl", defaultTemplate),
            Tuple.Create("default.html.js", script)
            );
        var outputFilePath1 = Path.Combine(_outputFolder, "file.html");
        Assert.True(File.Exists(outputFilePath1));
        Assert.Equal("2019-08-19T05:40:30.463Z", File.ReadAllText(outputFilePath1));
    }
    #endregion

    private static void ProcessTemplate(string templateName, string inputFolder, IEnumerable<InternalManifestItem> items, object model, string outputFolder, params Tuple<string, string>[] templateFiles)
    {
        var rootTemplateFolder = "tmpl";
        var templateFolder = Path.Combine(rootTemplateFolder, templateName);
        if (Directory.Exists(templateFolder))
            Directory.Delete(templateFolder, true);
        WriteTemplate(templateFolder, templateFiles);
        var context = new DocumentBuildContext(inputFolder);
        var processor = new TemplateProcessor(new LocalFileResourceReader(templateFolder), context, 4);
        foreach (var item in items)
        {
            if (item.ResourceFile != null)
            {
                var dir = Path.GetDirectoryName(item.ResourceFile);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.Create(item.ResourceFile).Dispose();
            }
            if (string.IsNullOrEmpty(item.InputFolder)) item.InputFolder = Directory.GetCurrentDirectory();
            item.Content = model;
        }
        var settings = new ApplyTemplateSettings(inputFolder, outputFolder);
        EnvironmentContext.SetBaseDirectory(inputFolder);
        EnvironmentContext.SetOutputDirectory(outputFolder);
        try
        {
            processor.CopyTemplateResources(settings);
            processor.Process(items.ToList(), settings);
        }
        finally
        {
            EnvironmentContext.Clean();
        }
    }

    private static void WriteTemplate(string cwd, params Tuple<string, string>[] files)
    {
        foreach (var file in files)
        {
            var filePath = Path.Combine(cwd ?? string.Empty, file.Item1);
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(filePath, file.Item2);
        }
    }

    private static void AssertEqualIgnoreCrlf(string expected, string actual)
    {
        Assert.Equal(expected.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
    }
}
