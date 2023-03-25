// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.DocAsCode.DataContracts.Common;
using Microsoft.DocAsCode.DataContracts.ManagedReference;

using Xunit;

namespace Microsoft.DocAsCode.Dotnet.Tests;

public class XmlCommentUnitTest
{
    private static void Verify(string comment, string summary)
    {
        Assert.Equal(
            summary,
            XmlComment.Parse($"<summary>{comment}</summary>", new()).Summary,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public static void SeeLangword()
    {
        Verify("<see langword=\"if\" />", "<a href=\"https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/selection-statements#the-if-statement\">if</a>");
        Verify("<see langword=\"undefined-langword\" />", "<c>undefined-langword</c>");
    }

    [Fact]
    public static void Issue8122()
    {
        var comment = XmlComment.Parse("<seealso href=\"#\">Foo's</seealso>", new());
        Assert.Equal("Foo's", comment.SeeAlsos[0].AltText);
    }

    [Fact]
    public void TestXmlCommentParser()
    {
        string inputFolder = Path.GetRandomFileName();
        Directory.CreateDirectory(inputFolder);
        File.WriteAllText(Path.Combine(inputFolder, "Example.cs"), @"
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
");
        string input = @"
<member name='T:TestClass1.Partial1'>
    <summary>
        Partial classes <see cref='T:System.AccessViolationException'/><see cref='T:System.AccessViolationException'/>can not cross assemblies, Test <see langword='null'/>

        ```
        Classes in assemblies are by definition complete.
        ```
    </summary>
    <remarks>
    <see href=""https://example.org""/>
    <see href=""https://example.org"">example</see>
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
        <exception cref='T:System.Xml.XmlException'>This is a sample of exception node. Ref <see href=""http://exception.com"">Exception</see></exception>
        <exception cref='System.Xml.XmlException'>This is a sample of exception node with invalid cref</exception>
        <exception cref=''>This is a sample of invalid exception node</exception>
        <exception >This is a sample of another invalid exception node</exception>

    <example>
    This sample shows how to call the <see cref=""M: Microsoft.DocAsCode.EntityModel.XmlCommentParser.GetExceptions(System.String, Microsoft.DocAsCode.EntityModel.XmlCommentParserContext)""/> method.
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
    <see cref=""T:Microsoft.DocAsCode.EntityModel.SpecIdHelper""/>
    <see cref=""T:System.Diagnostics.SourceSwitch""/>
    <see cref=""Overload:System.String.Compare""/>
    <see href=""http://exception.com"">Global See section</see>
    <see href=""http://exception.com""/>
    <seealso cref=""T:System.IO.WaitForChangedResult""/>
    <seealso cref=""!:http://google.com"">ABCS</seealso>
    <seealso href=""http://www.bing.com"">Hello Bing</seealso>
    <seealso href=""http://www.bing.com""/>
</member>";
        var context = new XmlCommentParserContext
        {
            AddReferenceDelegate = null,
            PreserveRawInlineComments = false,
            Source = new SourceDetail
            {
                Path = Path.Combine(inputFolder, "Source.cs"),
            }
        };

        var commentModel = XmlComment.Parse(input, context);
        Assert.True(commentModel.InheritDoc == null, nameof(commentModel.InheritDoc));

        var summary = commentModel.Summary;
        Assert.Equal("""

            Partial classes <xref href="System.AccessViolationException" data-throw-if-not-resolved="false"></xref><xref href="System.AccessViolationException" data-throw-if-not-resolved="false"></xref>can not cross assemblies, Test <a href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null">null</a>

            ```
            Classes in assemblies are by definition complete.
            ```

            """.Replace("\r\n", "\n"), summary);

        var returns = commentModel.Returns;
        Assert.Equal("Task<xref href=\"System.AccessViolationException\" data-throw-if-not-resolved=\"false\"></xref> returns", returns);

        var paramInput = commentModel.Parameters["input"];
        Assert.Equal("This is <xref href=\"System.AccessViolationException\" data-throw-if-not-resolved=\"false\"></xref>the input", paramInput);

        var remarks = commentModel.Remarks;
        Assert.Equal(@"
<a href=""https://example.org"">https://example.org</a>
<a href=""https://example.org"">example</a>
<p>This is <code data-dev-comment-type=""paramref"" class=""paramref"">ref</code> a sample of exception node</p>
<ul><li>
<pre><code class=""lang-c#"">public class XmlElement
    : XmlLinkedNode</code></pre>
<ol><li>
            word inside list->listItem->list->listItem->para.>
            the second line.
</li><li>item2 in numbered list</li></ol>
</li><li>item2 in bullet list</li><li>
loose text <em>not</em> wrapped in description
</li></ul>
".Replace("\r\n", "\n"),
remarks);

        var exceptions = commentModel.Exceptions;
        Assert.Single(exceptions);
        Assert.Equal("System.Xml.XmlException", exceptions[0].Type);
        Assert.Equal(@"This is a sample of exception node. Ref <a href=""http://exception.com"">Exception</a>", exceptions[0].Description);

        var example = commentModel.Examples;
        var expected = new List<string> {
@"
This sample shows how to call the <see cref=""M: Microsoft.DocAsCode.EntityModel.XmlCommentParser.GetExceptions(System.String, Microsoft.DocAsCode.EntityModel.XmlCommentParserContext)""></see> method.
<pre><code>class TestClass
{
    static int Main()
    {
        return GetExceptions(null, null).Count();
    }
} </code></pre>
".Replace("\r\n", "\n"),
@"
This is another example
".Replace("\r\n", "\n"),
@"
Check empty code.
<pre><code></code></pre>
".Replace("\r\n", "\n"),
@"
This is an example using source reference.
<pre><code source=""Example.cs"" region=""Example"">    static class Program
{
    public int Main(string[] args)
    {
        Console.HelloWorld();
    }
}</code></pre>
".Replace("\r\n", "\n")};
        Assert.Equal(expected, example);

        context.PreserveRawInlineComments = true;
        commentModel = XmlComment.Parse(input, context);

        var sees = commentModel.Sees;
        Assert.Equal(5, sees.Count);
        Assert.Equal("Microsoft.DocAsCode.EntityModel.SpecIdHelper", sees[0].LinkId);
        Assert.Null(sees[0].AltText);
        Assert.Equal("System.String.Compare*", sees[2].LinkId);
        Assert.Null(sees[1].AltText);
        Assert.Equal("http://exception.com", sees[3].LinkId);
        Assert.Equal("Global See section", sees[3].AltText);
        Assert.Equal("http://exception.com", sees[4].AltText);
        Assert.Equal("http://exception.com", sees[4].LinkId);

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
        string inputFolder = Path.GetRandomFileName();
        Directory.CreateDirectory(inputFolder);
        string input = @"
<member name='T:TestClass1.Partial1'>
    <summary>
        Class summary <see cref='T:System.AccessViolationException'>Exception type</see>
    </summary>
    <remarks>
    See <see cref='T:System.Int'>Integer</see>.
    </remarks>
    <returns>Returns an <see cref='T:System.AccessViolationException'>Exception</see>.</returns>

        <param name='input'>This is an <see cref='T:System.AccessViolationException'>Exception</see>.</param>
</member>";
        var context = new XmlCommentParserContext
        {
            AddReferenceDelegate = null,
            PreserveRawInlineComments = false,
            Source = new SourceDetail
            {
                Path = Path.Combine(inputFolder, "Source.cs"),
            }
        };

        var commentModel = XmlComment.Parse(input, context);
        Assert.True(commentModel.InheritDoc == null, nameof(commentModel.InheritDoc));

        var summary = commentModel.Summary;
        Assert.Equal("\nClass summary <xref href=\"System.AccessViolationException?text=Exception+type\" data-throw-if-not-resolved=\"false\"></xref>\n", summary);

        var returns = commentModel.Returns;
        Assert.Equal("Returns an <xref href=\"System.AccessViolationException?text=Exception\" data-throw-if-not-resolved=\"false\"></xref>.", returns);

        var paramInput = commentModel.Parameters["input"];
        Assert.Equal("This is an <xref href=\"System.AccessViolationException?text=Exception\" data-throw-if-not-resolved=\"false\"></xref>.", paramInput);

        var remarks = commentModel.Remarks;
        Assert.Equal("\nSee <xref href=\"System.Int?text=Integer\" data-throw-if-not-resolved=\"false\"></xref>.\n", remarks);
    }

    [Fact]
    public void InheritDoc()
    {
        const string input = @"
<member name=""M:ClassLibrary1.MyClass.DoThing"">
    <inheritdoc />
</member>";
        var context = new XmlCommentParserContext
        {
            AddReferenceDelegate = null,
            PreserveRawInlineComments = false,
        };

        var commentModel = XmlComment.Parse(input, context);
        Assert.True(commentModel.InheritDoc != null, nameof(commentModel.InheritDoc));
    }

    [Fact]
    public void InheritDocWithCref()
    {
        const string input = @"
<member name=""M:ClassLibrary1.MyClass.DoThing"">
    <inheritdoc cref=""M:ClassLibrary1.MyClass.DoThing""/>
</member>";
        var context = new XmlCommentParserContext
        {
            AddReferenceDelegate = null,
            PreserveRawInlineComments = false,
        };

        var commentModel = XmlComment.Parse(input, context);
        Assert.Equal("ClassLibrary1.MyClass.DoThing", commentModel.InheritDoc);
    }

    [Fact]
    public void TestXmlCommentParserForXamlSource()
    {
        string inputFolder = Path.GetRandomFileName();
        Directory.CreateDirectory(inputFolder);
        var expectedExampleContent = @"                <Grid>
                  <TextBlock Text=""Hello World"" />
                </Grid>";

        File.WriteAllText(Path.Combine(inputFolder, "Example.xaml"), $@"
<UserControl x:Class=""Examples""
            xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
            xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
            xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
            mc: Ignorable = ""d""
            d:DesignHeight=""300"" d:DesignWidth=""300"" >
                <UserControl.Resources>

                <!-- <Example> -->
{expectedExampleContent}
                <!-- </Example> -->
");
        string input = @"
<member name='T:TestClass1.Partial1'>
    <summary>
    </summary>
    <returns>Something</returns>

    <example>
    This is an example using source reference in a xaml file.
    <code source='Example.xaml' region='Example'/>
    </example>
</member>";
        var context = new XmlCommentParserContext
        {
            AddReferenceDelegate = null,
            PreserveRawInlineComments = false,
            Source = new SourceDetail
            {
                Path = Path.Combine(inputFolder, "Source.cs"),
            }
        };

        var commentModel = XmlComment.Parse(input, context);
        
        // using xml to get rid of escaped tags
        var example = commentModel.Examples.Single();
        var doc = XDocument.Parse($"<root>{example}</root>");
        var codeNode = doc.Descendants("code").Single();
        var actual = NormalizeWhitespace(codeNode.Value);
        var expected = NormalizeWhitespace(expectedExampleContent.Replace("\r\n", "\n"));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ParseXmlCommentWithoutRootNode()
    {
        var input = @"<summary>A</summary>";
        var commentModel = XmlComment.Parse(input, new XmlCommentParserContext());
        Assert.Equal("A", commentModel.Summary);
    }

    /// <summary>
    /// Normalizes multiple whitespaces into 1 single whitespace to allow ignoring of insignificant whitespaces.
    /// </summary>
    private string NormalizeWhitespace(String s)
    {
        var regex = new Regex(@"(?<= ) +");
        return regex.Replace(s, String.Empty);
    }
}
