// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Tests.Common;

    using HtmlAgilityPack;
    using Xunit;

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
            var sourceFile = Path.Combine(_projectFolder, "src", "test.cs");
            CreateFile(sourceFile, sourceCode, "src");

            var docfxJson = $@"{{
""metadata"": [
    {{
        ""src"": ""src/test.cs"",
        ""dest"": ""api""
    }}
],
""build"": {{
    ""content"": {{
        ""files"": ""api/*.yml""
    }},
    ""dest"": ""{_outputFolder.ToNormalizedPath()}/site"",
    ""sitemap"":{{
        ""baseUrl"": ""https://dotnet.github.io/docfx"",
        ""priority"": 0.1,
        ""changefreq"": ""monthly"",
        ""fileOptions"":{{
            ""**.yml"": {{
                ""priority"": 0.3,
                ""lastmod"": ""1999-01-01""
            }},
            ""**/Hello.yml"": {{
                ""baseUrl"": ""https://dotnet.github.io/docfx/1"",
                ""priority"": 0.8,
                ""changefreq"": ""Daily""
            }}
        }}
    }}
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
            var sitemap = Path.Combine(_outputFolder, "site", "sitemap.xml");
            Assert.True(File.Exists(sitemap));

            XDocument xdoc = XDocument.Load(sitemap);

                var documentElement = xdoc.Elements().FirstOrDefault();
            Assert.NotNull(documentElement);
            var ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            Assert.Equal(ns, documentElement.GetDefaultNamespace());
            var elements = documentElement.Elements().ToList();
            Assert.Equal(2, elements.Count);
            Assert.Equal("0.3", elements[0].Element(XName.Get("priority", ns)).Value);
            Assert.Equal("monthly", elements[0].Element(XName.Get("changefreq", ns)).Value);
            Assert.Equal("https://dotnet.github.io/docfx/api/Hello.HelloWorld.html", elements[0].Element(XName.Get("loc", ns)).Value);
            Assert.Equal(new DateTime(1999, 01, 01).ToString("yyyy-MM-ddThh:mm:ssK"), elements[0].Element(XName.Get("lastmod", ns)).Value);

            Assert.Equal("0.8", elements[1].Element(XName.Get("priority", ns)).Value);
            Assert.Equal("daily", elements[1].Element(XName.Get("changefreq", ns)).Value);
            Assert.Equal("https://dotnet.github.io/docfx/1/api/Hello.html", elements[1].Element(XName.Get("loc", ns)).Value);
        }
    }
}