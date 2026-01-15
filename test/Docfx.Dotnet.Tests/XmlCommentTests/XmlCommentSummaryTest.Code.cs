// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentSummaryTest
{
    [Fact]
    public void Code_Block()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            This is a summary with a code block:
            <code>
            var x = 1;
            </code>
            </summary>
            """,
            // Expected Markdown
            """
            This is a summary with a code block:

            <pre><code class="lang-csharp">var x = 1;</code></pre>
            """);
    }

    [Fact]
    public void Code_Block_WithoutNewLine()
    {
        ValidateSummary(
           // Input XML
           """
           <summary>
           Paragraph1<code><![CDATA[
           DELETE /articles/1 HTTP/1.1
           ]]></code>Paragraph2
           </summary>
           """,
           // Expected Markdown
           """
           Paragraph1

           <pre><code class="lang-csharp">DELETE /articles/1 HTTP/1.1</code></pre>

           Paragraph2
           """);
    }

    [Fact]
    public void Code_Inline()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            Paragraph1
            text <c>InlineCode</c> text.
            Paragraph2
            </summary>
            """,
            // Expected Markdown
            """
            Paragraph1
            text <code>InlineCode</code> text.
            Paragraph2
            """);
    }
}
