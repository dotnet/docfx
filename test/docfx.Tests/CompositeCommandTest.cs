// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;
    using Microsoft.DocAsCode.SubCommands;
    using Microsoft.DocAsCode.Tests.Common;
    using HtmlAgilityPack;

    [Collection("docfx STA")]
    public class CompositeCommandTest : TestBase
    {
        /// <summary>
        /// Use MetadataCommand to generate YAML files from a c# project and a VB project separately
        /// </summary>
        private string _outputFolder;
        private string _projectFolder;

        public CompositeCommandTest()
        {
            _outputFolder = Path.GetFullPath(GetRandomFolder());
            _projectFolder = Path.GetFullPath(GetRandomFolder());
        }

        [Fact]
        [Trait("Related", "docfx#428")]
        [Trait("Language", "CSharp")]
        public void TestCommandFromCSCodeToHtml()
        {
            // Create source file
            var sourceCode = @"
public namespace Hello{
/// <summary>
/// The class &lt; &gt; > description goes here...
/// </summary>
/// <example>
/// Here is some &lt; encoded &gt; example...
/// > [!NOTE]
/// > This is *note*
///
/// <code>
/// var handler = DateTimeHandler();
/// for (var i = 0; i &lt; 10; i++){
///     date = date.AddMonths(1);
/// }
/// </code>
/// </example>
public class HelloWorld(){}}
";
            var sourceFile = Path.Combine(_projectFolder, "test.cs");
            File.WriteAllText(sourceFile, sourceCode);

            var docfxJson = $@"{{
""metadata"": [
    {{
        ""src"": ""test.cs"",
        ""cwd"": ""{_projectFolder.ToNormalizedPath()}"",
        ""dest"": ""{_outputFolder.ToNormalizedPath()}/api""
    }}
],
""build"": {{
    ""content"": {{
        ""files"": ""api/*.yml"",
        ""cwd"": ""../{Path.GetFileName(_outputFolder)}""
    }},
    ""dest"": ""{_outputFolder.ToNormalizedPath()}/site""
}}
}}";
            var docfxJsonFile = Path.Combine(_projectFolder, "docfx.json");
            File.WriteAllText(docfxJsonFile, docfxJson);
            Program.ExecSubCommand(new string[] { docfxJsonFile });
            var filePath = Path.Combine(_outputFolder, "site", "api", "Hello.HelloWorld.html");
            Assert.True(File.Exists(filePath));
            var html = new HtmlDocument();
            html.Load(filePath);
            var summary = html.DocumentNode.SelectSingleNode("//div[contains(@class, 'summary')]/p").InnerHtml;
            Assert.Equal("The class &lt; &gt; &gt; description goes here...", summary.Trim());
            var note = html.DocumentNode.SelectSingleNode("//div[@class='NOTE']").InnerHtml;
            Assert.Equal("<h5>Note</h5><p>This is <em>note</em></p>", note.Trim());
            var code = html.DocumentNode.SelectNodes("//pre/code")[1].InnerHtml;
            Assert.Equal(@"var handler = DateTimeHandler();
for (var i = 0; i &lt; 10; i++){
    date = date.AddMonths(1);
}".Replace("\r\n", "\n"), code);
        }
    }
}