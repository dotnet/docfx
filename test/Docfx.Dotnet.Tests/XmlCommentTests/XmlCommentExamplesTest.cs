// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Xunit;

namespace Docfx.Dotnet.Tests;

public class XmlCommentExamplesTest
{
    [Fact]
    public void Examples()
    {
        // Indent of first line should be preserved
        ValidateExamples(
            // Input XML
            """
            <example>
            This sample shows how to call the <see cref="M: Docfx.EntityModel.XmlCommentParser.GetExceptions(System.String, Docfx.EntityModel.XmlCommentParserContext)"/> method.
            <code>
            class TestClass
            {
                static int Main()
                {
                    return GetExceptions(null, null).Count();
                }
            } 
            </code>
            </example>

            <example>
            This is another example
            </example>

            <example>
            Check empty code.
            <code></code>
            </example>

            <example>
            This is an example using source reference.
            <code source="Example.cs" region="Example"/>
            </example>
            """,
            // Expected Markdown
            [
               // Example #1
               """
               This sample shows how to call the <see cref="M: Docfx.EntityModel.XmlCommentParser.GetExceptions(System.String, Docfx.EntityModel.XmlCommentParserContext)"></see> method.

               <pre><code class="lang-csharp">class TestClass
               {
                   static int Main()
                   {
                       return GetExceptions(null, null).Count();
                   }
               }</code></pre>
               """,

               // Example #2
               """
               This is another example
               """,

               // Example #3
               """
               Check empty code.

               <pre><code class="lang-csharp"></code></pre>
               """,

               // Example #4
               """
               This is an example using source reference.

               <pre><code class="lang-cs"></code></pre>
               """,
            ]);
    }

    [Fact]
    public void Examples_WithIndent()
    {
        ValidateExamples(
            // Input XML
            """
            <member>
                <example>
                Paragraph1
                <code>
                class TestClass
                {
                    static int Main()
                    {
                        return 0;
                    }
                } 
                </code>
                </example>
            </member>
            """,
            // Expected Markdown
            [
               """
               Paragraph1

               <pre><code class="lang-csharp">class TestClass
               {
                   static int Main()
                   {
                       return 0;
                   }
               }</code></pre>
               """,
            ]);
    }


    [Fact]
    public void Examples_WithIndent2()
    {
        ValidateExamples(
            // Input XML
            """
            <member>
              <example>
              <code><![CDATA[
              options.UseRelativeLinks = true;
              ]]></code>
              <code><![CDATA[
              {
                "type": "articles",
                "id": "4309",
                "relationships": {
                  "author": {
                    "links": {
                      "self": "/api/shopping/articles/4309/relationships/author",
                      "related": "/api/shopping/articles/4309/author"
                    }
                  }
                }
              }]]></code>
              </example>
            </member>
            """,
            // Expected Markdown
            [
               """
               <pre><code class="lang-csharp">options.UseRelativeLinks = true;</code></pre>
               <pre><code class="lang-csharp">{
                 "type": "articles",
                 "id": "4309",
                 "relationships": {
                   "author": {
                     "links": {
                       "self": "/api/shopping/articles/4309/relationships/author",
                       "related": "/api/shopping/articles/4309/author"
                     }
                   }
                 }
               }</code></pre>
               """,
            ]);
    }

    [Fact]
    public void Examples_WithIndentedCodes()
    {
        ValidateExamples(
            // Input XML
            """
            <member>
              <example>
                <code>aaa</code>
                <code>bbb</code>
              </example>
            </member>
            """,
            // Expected Markdown
            [
               """
               <pre><code class="lang-csharp">aaa</code></pre>
               <pre><code class="lang-csharp">bbb</code></pre>
               """,
            ]);
    }

    private static void ValidateExamples(string input, string[] expected)
    {
        // Act
        var results = XmlComment.Parse(input).Examples;

        // Assert
        results.Should().NotBeNull(); // Failed to get summary from XML input.

        results.Should()
               .BeEquivalentTo(expected, x => x.IgnoringNewlineStyle().WithStrictOrdering());
    }
}
