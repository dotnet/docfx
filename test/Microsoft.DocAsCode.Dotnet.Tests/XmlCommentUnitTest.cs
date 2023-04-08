// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.DocAsCode.DataContracts.Common;

using Xunit;

namespace Microsoft.DocAsCode.Dotnet.Tests;

public class XmlCommentUnitTest
{
    private static void Verify(string comment, string summary)
    {
        Assert.Equal(
            summary,
            XmlComment.Parse($"<summary>{comment}</summary>").Summary,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public static void SeeLangword()
    {
        Verify("<see langword=\"if\" />", "<a href=\"https://learn.microsoft.com/dotnet/csharp/language-reference/statements/selection-statements#the-if-statement\">if</a>");
        Verify("<see langword=\"undefined-langword\" />", "<c>undefined-langword</c>");
    }

    [Fact]
    public static void Issue8122()
    {
        var comment = XmlComment.Parse("<seealso href=\"#\">Foo's</seealso>");
        Assert.Equal("Foo's", comment.SeeAlsos[0].AltText);
    }

    [Fact]
    public static void Issue4165()
    {
        var comment = XmlComment.Parse(
            """
            <param name="args">arg1</param>
            <param name="args">arg2</param>
            """);
        Assert.Equal("arg1", comment.Parameters["args"]);
    }

    [Fact]
    public static void BasicCodeBlock()
    {
        var comment = XmlComment.Parse(
            """
            <remarks>
                <code>
                    public int Main(string[] args)
                    {
                        Console.HelloWorld();
                    }
                </code>
            </remarks>
            """);

        Assert.Equal(
            """
            <pre><code class="lang-csharp">public int Main(string[] args)
            {
                Console.HelloWorld();
            }</code></pre>
            """,
            comment.Remarks,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public static void ExternalCodeBlockCSharp()
    {
        var example = """
            using System;

            namespace Example
            {
            #region Example
                static class Program
                {
                    public int Main(string[] args)
                    {
                        Console.HelloWorld();
                    }
                }
            #endregion
            }
            """;

        var comment = XmlComment.Parse(
            """
            <remarks>
                <code source="Example.cs" region="Example" />
            </remarks>
            """,
            new() { ResolveCode = _ => example });

        Assert.Equal(
            """
            <pre><code class="lang-cs">static class Program
            {
                public int Main(string[] args)
                {
                    Console.HelloWorld();
                }
            }</code></pre>
            """,
            comment.Remarks,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ExternalCodeBlockXaml()
    {
        var example = """
            <UserControl x:Class="Examples">
                            <UserControl.Resources>

                            <!-- <Example> -->
                            <Grid>
                              <TextBlock Text="Hello World" />
                            </Grid>
                            <!-- </Example> -->
            </UserControl>
            """;

        var input = """
            <member name='T:TestClass1.Partial1'>
                <example>
                This is an example using source reference in a xaml file.
                <code source='Example.xaml' region='Example'/>
                </example>
            </member>
            """;

        var commentModel = XmlComment.Parse(input, new() { ResolveCode = _ => example });

        Assert.Equal(
            """
            This is an example using source reference in a xaml file.
            <pre><code class="lang-xaml">&lt;Grid>
              &lt;TextBlock Text=&quot;Hello World&quot; />
            &lt;/Grid></code></pre>
            """,
            commentModel.Examples.Single(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public static void MarkdownCodeBlock()
    {
        var comment = XmlComment.Parse(
            """
            <summary>
                public int Main(string[] args)
                {
                    Console.HelloWorld();
                }
            </summary>
            <remarks>
            For example:

                public int Main(string[] args)
                {
                    Console.HelloWorld();
                }
            </remarks>
            """);

        Assert.Equal("""
            public int Main(string[] args)
            {
                Console.HelloWorld();
            }
            """, comment.Summary, ignoreLineEndingDifferences: true);

        Assert.Equal("""
            For example:

                public int Main(string[] args)
                {
                    Console.HelloWorld();
                }
            """, comment.Remarks, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TestXmlCommentParser()
    {
        var input = """
            <member name='T:TestClass1.Partial1'>
                <summary>
                    Partial classes <see cref='T:System.AccessViolationException'/><see cref='T:System.AccessViolationException'/>can not cross assemblies, Test <see langword='null'/>

                    ```
                    Classes in assemblies are by definition complete.
                    ```
                </summary>
                <remarks>
                <see href="https://example.org"/>
                <see href="https://example.org">example</see>
                <para>This is <paramref name='ref'/> <paramref />a sample of exception node</para>
                <list type='bullet'>
                    <item>
                        <description>
                            <code language = 'c#'>
                            public class XmlElement
                                : XmlLinkedNode
                            </code>
                            <list type='number'>
                                <item>
                                    <description>
                                        word inside list->listItem->list->listItem->para.>
                                        the second line.
                                    </description>
                                </item>
                                <item>
                                    <description>item2 in numbered list</description>
                                </item>
                            </list>
                        </description>
                    </item>
                    <item>
                        <description>item2 in bullet list</description>
                    </item>
                    <item>
                        loose text <i>not</i> wrapped in description
                    </item>
                </list>
                </remarks>
                <returns>Task<see cref='T:System.AccessViolationException'/> returns</returns>

                    <param name='input'>This is <see cref='T:System.AccessViolationException'/>the input</param>

                    <param name = 'output' > This is the output </param >
                    <exception cref='T:System.Xml.XmlException'>This is a sample of exception node. Ref <see href="http://exception.com">Exception</see></exception>
                    <exception cref='System.Xml.XmlException'>This is a sample of exception node with invalid cref</exception>
                    <exception cref=''>This is a sample of invalid exception node</exception>
                    <exception >This is a sample of another invalid exception node</exception>

                <example>
                This sample shows how to call the <see cref="M: Microsoft.DocAsCode.EntityModel.XmlCommentParser.GetExceptions(System.String, Microsoft.DocAsCode.EntityModel.XmlCommentParserContext)"/> method.
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
                <code source='Example.cs' region='Example'/>
                </example>
                <see cref="T:Microsoft.DocAsCode.EntityModel.SpecIdHelper"/>
                <see cref="T:System.Diagnostics.SourceSwitch"/>
                <see cref="Overload:System.String.Compare"/>
                <see href="http://exception.com">Global See section</see>
                <see href="http://exception.com"/>
                <seealso cref="T:System.IO.WaitForChangedResult"/>
                <seealso cref="!:http://google.com">ABCS</seealso>
                <seealso href="http://www.bing.com">Hello Bing</seealso>
                <seealso href="http://www.bing.com"/>
            </member>
            """;

        var commentModel = XmlComment.Parse(input);

        var summary = commentModel.Summary;
        Assert.Equal("""
            Partial classes <xref href="System.AccessViolationException" data-throw-if-not-resolved="false"></xref><xref href="System.AccessViolationException" data-throw-if-not-resolved="false"></xref>can not cross assemblies, Test <a href="https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null">null</a>

            ```
            Classes in assemblies are by definition complete.
            ```
            """, summary, ignoreLineEndingDifferences: true);

        var returns = commentModel.Returns;
        Assert.Equal("Task<xref href=\"System.AccessViolationException\" data-throw-if-not-resolved=\"false\"></xref> returns", returns);

        var paramInput = commentModel.Parameters["input"];
        Assert.Equal("This is <xref href=\"System.AccessViolationException\" data-throw-if-not-resolved=\"false\"></xref>the input", paramInput);

        var remarks = commentModel.Remarks;
        Assert.Equal("""
            <a href="https://example.org">https://example.org</a>
            <a href="https://example.org">example</a>
            <p>This is <c class="paramref">ref</c> a sample of exception node</p>
            <ul><li>
                        <pre><code class="lang-csharp">public class XmlElement
                            : XmlLinkedNode</code></pre>
                        <ol><li>
                                    word inside list->listItem->list->listItem->para.>
                                    the second line.
                                </li><li>item2 in numbered list</li></ol>
                    </li><li>item2 in bullet list</li><li>
                    loose text <i>not</i> wrapped in description
                </li></ul>
            """, remarks, ignoreLineEndingDifferences: true);

        var exceptions = commentModel.Exceptions;
        Assert.Single(exceptions);
        Assert.Equal("System.Xml.XmlException", exceptions[0].Type);
        Assert.Equal(@"This is a sample of exception node. Ref <a href=""http://exception.com"">Exception</a>", exceptions[0].Description);

        Assert.Collection(
            commentModel.Examples,
            e => Assert.Equal(
                """
                This sample shows how to call the <see cref="M: Microsoft.DocAsCode.EntityModel.XmlCommentParser.GetExceptions(System.String, Microsoft.DocAsCode.EntityModel.XmlCommentParserContext)"></see> method.
                <pre><code class="lang-csharp">class TestClass
                 {
                     static int Main()
                     {
                         return GetExceptions(null, null).Count();
                     }
                 }</code></pre>
                """, e, ignoreLineEndingDifferences: true),
            e => Assert.Equal(
                """
                This is another example
                """, e, ignoreLineEndingDifferences: true),
            e => Assert.Equal(
                """
                Check empty code.
                <pre><code class="lang-csharp"></code></pre>
                """, e, ignoreLineEndingDifferences: true),
            e => Assert.Equal(
                """
                This is an example using source reference.
                <pre><code class="lang-cs"></code></pre>
                """, e, ignoreLineEndingDifferences: true)
            );

        commentModel = XmlComment.Parse(input);

        var seeAlsos = commentModel.SeeAlsos;
        Assert.Equal(3, seeAlsos.Count);
        Assert.Equal("System.IO.WaitForChangedResult", seeAlsos[0].LinkId);
        Assert.Null(seeAlsos[0].AltText);
        Assert.Equal("http://www.bing.com", seeAlsos[1].LinkId);
        Assert.Equal("Hello Bing", seeAlsos[1].AltText);
        Assert.Equal("http://www.bing.com", seeAlsos[2].AltText);
        Assert.Equal("http://www.bing.com", seeAlsos[2].LinkId);
    }

    [Fact]
    public void SeeAltText()
    {
        string input = """

            <member name='T:TestClass1.Partial1'>
                <summary>
                    Class summary <see cref='T:System.AccessViolationException'>Exception type</see>
                </summary>
                <remarks>
                See <see cref='T:System.Int'>Integer</see>.
                </remarks>
                <returns>Returns an <see cref='T:System.AccessViolationException'>Exception</see>.</returns>

                    <param name='input'>This is an <see cref='T:System.AccessViolationException'>Exception</see>.</param>
            </member>
            """;

        var commentModel = XmlComment.Parse(input);

        var summary = commentModel.Summary;
        Assert.Equal("""
            Class summary <xref href="System.AccessViolationException?text=Exception+type" data-throw-if-not-resolved="false"></xref>
            """, summary, ignoreLineEndingDifferences: true);

        var returns = commentModel.Returns;
        Assert.Equal("Returns an <xref href=\"System.AccessViolationException?text=Exception\" data-throw-if-not-resolved=\"false\"></xref>.", returns);

        var paramInput = commentModel.Parameters["input"];
        Assert.Equal("This is an <xref href=\"System.AccessViolationException?text=Exception\" data-throw-if-not-resolved=\"false\"></xref>.", paramInput);

        var remarks = commentModel.Remarks;
        Assert.Equal("""
            See <xref href="System.Int?text=Integer" data-throw-if-not-resolved="false"></xref>.
            """, remarks, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void ParseXmlCommentWithoutRootNode()
    {
        var input = @"<summary>A</summary>";
        var commentModel = XmlComment.Parse(input, new XmlCommentParserContext());
        Assert.Equal("A", commentModel.Summary);
    }
}
