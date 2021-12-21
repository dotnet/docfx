// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.MarkdigExtensions.Tests;

public class NolocTest
{
    [Fact]
    [Trait("Related", "Noloc")]
    public void NolocTest_General()
    {
        // Normal syntax
        TestUtility.VerifyMarkup(
            "使用 :::no-loc text=\"Find\"::: 方法.",
            "<p>使用 <span class=\"no-loc\" dir=\"ltr\" lang=\"en-us\">Find</span> 方法.</p>");

        // Escape syntax
        TestUtility.VerifyMarkup(
            "使用 :::no-loc text=\"Find a \\\"Quotation\\\"\"::: 方法.",
            "<p>使用 <span class=\"no-loc\" dir=\"ltr\" lang=\"en-us\">Find a &quot;Quotation&quot;</span> 方法.</p>\n");

        // Markdown in noloc
        TestUtility.VerifyMarkup(
            @":::no-loc text=""*Hello*"":::",
            @"<p><span class=""no-loc"" dir=""ltr"" lang=""en-us"">*Hello*</span></p>");
    }

    [Fact]
    [Trait("Related", "Noloc")]
    public void NolocTest_Invalid()
    {
        // MultipleLines
        TestUtility.VerifyMarkup(
            @":::no-loc text=""I am crossing\
a line"":::",
            @"<p>:::no-loc text=&quot;I am crossing<br />a line&quot;:::</p>");

        // Spaces not exactly match
        TestUtility.VerifyMarkup(
            @"::: no-loc text=""test"" :::",
            @"<p>::: no-loc text=&quot;test&quot; :::</p>");

        // Case sensitive
        TestUtility.VerifyMarkup(
            @":::No-loc text=""test"":::", @"<p>:::No-loc text=&quot;test&quot;:::</p>");
    }
}
