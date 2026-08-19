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


    [Fact]
    public void Code_HtmlTagExistBefore()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            paragraph1
            <para>paragraph2</para>
            <code><![CDATA[
            public class Sample
            {
                line1
            
                line2
            }]]></code>
            <code><![CDATA[
            public class Sample2
            {
                line1
            
                line2
            }]]></code>
            </summary>
            """,
            // Expected Markdown
            """
            paragraph1

            <p>paragraph2</p>

            <pre><code class="lang-csharp">public class Sample
            {
                line1
           
                line2
            }</code></pre>
            <pre><code class="lang-csharp">public class Sample2
            {
                line1
            
                line2
            }</code></pre>
            """);
    }

    [Fact]
    public void Code_ParentHtmlTagExist()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            <para>Paragraph1</para>
            <div>
              <code><![CDATA[
              public class Sample
              {
                  line1
              
                  line2
              }]]></code>
            </div>
            <para>Paragraph2</para>
            </summary>
            """,
            // Expected Markdown
            """
            <p>Paragraph1</p>
            <div>

              <pre><code class="lang-csharp">public class Sample
            {
                line1
           
                line2
            }</code></pre>

            </div>
            <p>Paragraph2</p>
            """);
    }

    [Fact]
    public void Code_MultipleBlockWithoutNewLine()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            Paragraph1
            <code>var x = 1;</code><code>var x = 2;</code>
            Paragraph2
            </summary>
            """,
            // Expected Markdown
            """
            Paragraph1

            <pre><code class="lang-csharp">var x = 1;</code></pre><pre><code class="lang-csharp">var x = 2;</code></pre>

            Paragraph2
            """);
    }

    [Fact]
    public void Code_StartsWithSameLine()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
              <example>
              Paragraph: <code><![CDATA[
                Code]]></code>
              </example>
            </summary>
            """,
            // Expected Markdown
            """
            <example>
            Paragraph: 

            <pre><code class="lang-csharp">Code</code></pre>

            </example>
            """);
    }

    [Fact]
    public void Code_SingleLineWithParagraph()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
              <example>
              Paragraph1<code>Code</code>
              Paragraph2
              </example>
            </summary>
            """,
            // Expected Markdown
            """
            <example>
            Paragraph1

            <pre><code class="lang-csharp">Code</code></pre>

            Paragraph2
            </example>
            """);
    }

    [Fact]
    public void Code_SingleLineWithMultipleParagraphs()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
              <example>
              aaa<p>bbb</p>ccc<code>Code</code>ddd<p>eee</p>fff
              </example>
            </summary>
            """,
            // Expected Markdown
            """
            <example>
            aaa

            <p>bbb</p>

            ccc

            <pre><code class="lang-csharp">Code</code></pre>

            ddd
            
            <p>eee</p>
            
            fff
            </example>
            """);
    }

    [Fact]
    public void Code_Indented()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
              <example>
              Paragraph
              <code>
              Code
              </code>
              </example>
            </summary>
            """,
            // Expected Markdown
            """
            <example>
            Paragraph

            <pre><code class="lang-csharp">Code</code></pre>

            </example>
            """);
    }
}
