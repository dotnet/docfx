// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Dotnet.Tests;

public partial class XmlCommentSummaryTest
{
    [Fact]
    public void ExampleWithParagraph()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            Paragraph1
            <example>
              <code class="lang-csharp">code content</code>
            </example>
            Paragraph2
            </summary>
            """,
            // Expected Markdown
            """
            Paragraph1

            <example>

              <pre><code class="lang-csharp">code content</code></pre>

            </example>

            Paragraph2
            """);
    }

    [Fact]
    public void ContainsXmlEscapeChars()
    {
        // ['<' '>' '%'] chars are not unescaped. These chars need to be escaped before converting markdown. 
        ValidateSummary(
            // Input XML
            """
            <summary>
            &quot;&apos;&lt;&gt;&amp;
            </summary>
            """,
            // Expected Markdown
            """
            "'&lt;&gt;&amp;
            """);
    }

    [Fact]
    public void ContainsBr()
    {
        ValidateSummary(
            // Input XML
            """
            <summary> 
            Used to provide asynchronous lifetime functionality.Currently supported:<br />
            - Test classes<br />
            - Classes used in <see cref = "IClassFixture{TFixture}" /><br />
            - Classes used in <see cref = "ICollectionFixture{TFixture}" />.<br />
            - Classes used in <c>[assembly: <see cref="AssemblyFixtureAttribute"/>()]</c>.
            </summary>
            """,
            // Expected Markdown
            """
            Used to provide asynchronous lifetime functionality.Currently supported:<br />
            - Test classes<br />
            - Classes used in <see cref="IClassFixture{TFixture}"></see><br />
            - Classes used in <see cref="ICollectionFixture{TFixture}"></see>.<br />
            - Classes used in <code>[assembly: <see cref="AssemblyFixtureAttribute"></see>()]</code>.
            """);
    }

    [Fact]
    public void ContainsEmptyLine_BeforeMarkdownBlock()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            <code>
            CodeBlock1
            </code>

            ```csharp
            CodeBlock2
            ```
            </summary>
            ```
            """,
            // Expected Markdown
            """
            <pre><code class="lang-csharp">CodeBlock1</code></pre>

            ```csharp
            CodeBlock2
            ```
            """);
    }

    [Fact]
    public void ContainsEmptyLine_AfterMarkdownBlock()
    {
        ValidateSummary(
            // Input XML
            """
            <summary>
            ```csharp
            CodeBlock1
            ```

            <code>
            CodeBlock2
            </code>
            </summary>
            """,
            // Expected Markdown
            """
            ```csharp
            CodeBlock1
            ```

            <pre><code class="lang-csharp">CodeBlock2</code></pre>
            """);
    }
}
