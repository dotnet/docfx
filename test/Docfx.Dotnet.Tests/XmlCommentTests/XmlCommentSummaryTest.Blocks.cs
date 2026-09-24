// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentSummaryTest
{
    [Theory]
    [InlineData("    ", "Indented")]
    [InlineData("", "Non-indented")]
    public void Blocks_List_Issue11173(string indent, string description)
    {
        var summary = XmlComment.Parse($"""
            <summary>
            <para>
            {indent}{description} paragraph with a list:
            {indent}<list type="bullet">
            {indent}    <item>see <see cref="T:System.String"/>,</item>
            {indent}    <item>second item.</item>
            {indent}</list>
            </para>
            </summary>
            """).Summary;

        var html = Markdown.ToHtml(summary);

        Assert.Contains("<ul><li>see <xref href=\"System.String\"", html);
        Assert.Contains("<li>second item.</li></ul>", html);
        Assert.DoesNotContain("<pre><code>", html);
    }

    [Theory]
    [InlineData("bullet", "ul")]
    [InlineData("number", "ol")]
    [InlineData("table", "table")]
    public void Blocks_OnlyListIsIndented(string type, string tag)
    {
        var summary = XmlComment.Parse($"""
            <summary>
            <para>
            Unindented text before an indented list:
                <list type="{type}">
                    <item><description>see <see cref="T:System.String"/>.</description></item>
                    <item><description>second item.</description></item>
                </list>
            Following **markdown**.
            </para>
            </summary>
            """).Summary;

        var html = Markdown.ToHtml(summary);

        Assert.Contains($"<{tag}>", html);
        Assert.Contains("<xref href=\"System.String\"", html);
        Assert.Contains("second item.", html);
        Assert.Contains("<p>Following <strong>markdown</strong>.</p>", html);
        Assert.DoesNotContain("<pre><code>", html);
    }

    [Fact]
    public void Blocks_NestedListsWithInlineReferences()
    {
        var summary = XmlComment.Parse("""
            <summary>
            <para>
                See <see cref="T:System.String"/>:
                <list type="bullet">
                    <item>
                        First item with <see cref="T:System.Int32"/>:
                        <list type="number">
                            <item>Nested item.</item>
                        </list>
                    </item>
                    <item>Second item.</item>
                </list>
            </para>
            </summary>
            """).Summary;

        var html = Markdown.ToHtml(summary);

        Assert.Contains("<ul><li>", html);
        Assert.Contains("<ol><li>Nested item.</li></ol>", html);
        Assert.Contains("<li>Second item.</li></ul>", html);
        Assert.Contains("<xref href=\"System.String\"", html);
        Assert.Contains("<xref href=\"System.Int32\"", html);
        Assert.DoesNotContain("<pre><code>", html);
    }

    [Theory]
    [InlineData("<para>{0}</para>")]
    [InlineData("<div>{0}</div>")]
    [InlineData("<example>{0}</example>")]
    [InlineData("<list type=\"bullet\"><item><description>{0}</description></item></list>")]
    [InlineData("<table><tr><td>{0}</td></tr></table>")]
    public void Blocks_NewSeparatorPreservesNestedXml(string container)
    {
        foreach (var rootIndent in new[] { "", "  " })
            foreach (var indent in new[] { "", "  ", "    ", "        ", "\t" })
                foreach (var separator in new[] { "", "\n" })
                {
                    var content = $"\n{rootIndent}{indent}A list:{separator}{rootIndent}{indent}<list type=\"number\"><item>see <see cref=\"T:System.String\"/>.</item></list>\n{rootIndent}";
                    var input = $"<summary>\n{rootIndent}Before **bold**.\n\n{rootIndent}{string.Format(container, content)}\n\n{rootIndent}After **bold**.\n</summary>";

                    var html = Markdown.ToHtml(XmlComment.Parse(input).Summary);

                    Assert.Contains("<ol><li>see <xref href=\"System.String\"", html);
                    Assert.Contains("<p>Before <strong>bold</strong>.</p>", html);
                    Assert.Contains("<p>After <strong>bold</strong>.</p>", html);
                    Assert.DoesNotContain("<pre><code>", html);
                }
    }

    [Theory]
    [InlineData("bullet", "ul")]
    [InlineData("number", "ol")]
    [InlineData("table", "table")]
    public void Blocks_NewSeparatorPreservesFollowingTextIndent(string type, string tag)
    {
        var summary = XmlComment.Parse($$""""
            <summary>
            <para>
                See <see cref="T:System.String"/>:
                <list type="{{type}}"><item><description>Item.</description></item></list>
                IndentedCode();

            Following <see cref="T:System.String"/> and **bold**.

            ```csharp
            if (ready)
                Run();
            ```
            <code>
            if (ready)
            {

                Run();
            }
            </code>
            </para>
            </summary>
            """").Summary;

        var html = Markdown.ToHtml(summary);

        Assert.Contains($"<{tag}>", html);
        Assert.Contains("<pre><code>IndentedCode();\n</code></pre>", html);
        Assert.Contains("and <strong>bold</strong>.</p>", html);
        Assert.Contains("<pre><code class=\"language-csharp\">if (ready)\n    Run();\n</code></pre>", html);
        Assert.Contains("<pre><code class=\"lang-csharp\">if (ready)\n{\n\n    Run();\n}</code></pre>", html);
        Assert.DoesNotContain($"&lt;{tag}&gt;", html);
    }

    [Fact]
    public void Blocks_NewSeparatorOnlyChangesTagIndent()
    {
        ValidateSummary(
            """
            <summary>
            <para>
                A list:
                <list type="bullet"><item>Item.</item></list>
                Code();
            </para>
            </summary>
            """,
            """
            <p>
                A list:

            <ul><li>Item.</li></ul>

                Code();
            </p>
            """);
    }

    [Fact]
    public void Blocks_ExistingBlankLineKeepsIndent()
    {
        ValidateSummary(
            """
            <summary>
            <para>
            Description.

                <list type="bullet"><item>Item.</item></list>
            </para>
            </summary>
            """,
            """
            <p>
            Description.

                <ul><li>Item.</li></ul>
            </p>
            """);
    }

    [Fact]
    public void Blocks_TopLevelMarkdownKeepsIndent()
    {
        ValidateSummary(
            """
            <summary>
            Description.
                <list type="bullet"><item>Item.</item></list>
            </summary>
            """,
            """
            Description.

                <ul><li>Item.</li></ul>
            """);
    }
}
