// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.MarkdigEngine.Extensions;
using Markdig.Extensions.SmartyPants;

namespace Docfx.MarkdigEngine.Tests;


/// <summary>
/// Unit tests for markdig <see cref="SmartyPantsExtension"/>.
/// </summary>
/// <seealso href="https://github.com/xoofx/markdig/blob/master/src/Markdig.Tests/Specs/SmartyPantsSpecs.md"/>
[TestProperty("Related", "MarkdigExtension")]
[TestClass]
public class SmartyPantsTest
{
    /// <summary>
    /// SmartyPants extension is not enabled by docfx default.
    /// </summary>
    [TestMethod]
    public void SmartyPantsTest_DocfxDefault()
    {
        string content = "This is a \"text 'with\" a another text'";
        string expected = "<p>This is a &quot;text 'with&quot; a another text'</p>";

        TestUtility.VerifyMarkup(content, expected);
    }

    [TestMethod]
    public void SmartyPantsTest_Default()
    {
        string content = "This is a \"text 'with\" a another text'";
        string expected = "<p>This is a &ldquo;text 'with&rdquo; a another text'</p>";

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: ["SmartyPants"]);
    }

    // Currently custom mapping is not works as expected.
    // Because SmartyPantOptions.Mapping is defined as setter-only property. It's not deserialized by default.
    [TestMethod]
    [Ignore("Currently custom mapping is not supported.")]
    public void SmartyPantsTest_Custom()
    {
        var options = new SmartyPantOptions();
        options.Mapping[SmartyPantType.LeftQuote] = "<<";
        options.Mapping[SmartyPantType.RightQuote] = ">>";

        string content = "This is a 'text with' a another text'";
        string expected = "<p>This is a <<text with>> a another text'</p>";

        TestUtility.VerifyMarkup(content, expected, optionalExtensions: [
            new("SmartyPants", JsonSerializer.SerializeToNode(options, MarkdigExtensionSettingConverter.DefaultSerializerOptions))
        ]);
    }
}
