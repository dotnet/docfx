// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentSummaryTest
{
    [Fact]
    public void Indent_NoIndent()
    {
        // Basic sumamry content
        ValidateSummary(
            // Input XML
            """
            <summary>
            First line
            Second line
            </summary>
            """,
            // Expected Markdown
            """
            First line
            Second line
            """);
    }

    [Fact]
    public void Indent_SharedIndent_Removed()
    {
        // Shared indent should be removed.
        ValidateSummary(
            // Input XML
            """
            <summary>
            First line
              Second line
            </summary>
            """,
            // Expected Markdown
            """
            First line
              Second line
            """);
    }

    [Fact]
    public void Indent_FirstLine()
    {
        // Indent of first line should be preserved
        ValidateSummary(
            // Input XML
            """
            <summary>
              First line
            Second line
            </summary>
            """,
            // Expected Markdown
            """
              First line
            Second line
            """);
    }

    [Fact]
    public void Indent_SecondLine()
    {
        // Indent of second line should be preserved
        ValidateSummary(
            // Input XML
            """
            <summary>
            First line
              Second line
            </summary>
            """,
            // Expected Markdown
            """
            First line
              Second line
            """);
    }


    [Fact]
    public void Indent_BetweenInlineTags()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            <p>Paragraph1</p>   <p>Paragraph2</p>
            </summary>
            """,
            // Expected Markdown
            """
            <p>Paragraph1</p>   <p>Paragraph2</p>
            """);
    }


    [Fact]
    public void Indent_MarkdownLineBreak1()
    {
        // Indent of before new line is preserved.
        ValidateSummary(
            // Input XML
            """
            <summary>
            First line  
            Second line
            </summary>
            """,
            // Expected Markdown
            """
            First line  
            Second line
            """);
    }

    [Fact]
    public void Indent_MarkdownLineBreak2()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            First line\
            Second line
            </summary>
            """,
            // Expected Markdown
            """
            First line\
            Second line
            """);
    }

    /// <summary>
    /// Tests for XML that are returned by roslyn.
    /// </summary>
    [Fact]
    public void Indent_Code()
    {
        ValidateSummary(
            // Input XML
            """
            <member name="M:BuildFromProject.Class1.Issue1651">
                <summary>
                Summary
                <list type="bullet">
                    <item><term>1</term><description>ListItem</description></item>
                </list>
                </summary>
            </member>
            """,
            // Expected Markdown
            """
            Summary

            <ul><li><span class="term">1</span>ListItem</li></ul>
            """);
    }
}
