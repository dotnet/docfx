// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.Tests
{
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "Parser")]
    public class TripleSlashParserUnitTest
    {
        [Trait("Related", "TripleSlashComments")]
        [Fact]
        public void TestTripleSlashParser()
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
        Parital classes <see cref='T:System.AccessViolationException'/><see cref='T:System.AccessViolationException'/>can not cross assemblies, Test <see langword='null'/>

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
    This sample shows how to call the <see cref=""M: Microsoft.DocAsCode.EntityModel.TripleSlashCommentParser.GetExceptions(System.String, Microsoft.DocAsCode.EntityModel.ITripleSlashCommentParserContext)""/> method.
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
            var context = new TripleSlashCommentParserContext
            {
                AddReferenceDelegate = null,
                PreserveRawInlineComments = false,
                Source = new SourceDetail()
                {
                    Path = Path.Combine(inputFolder, "Source.cs"),
                }
            };

            var commentModel = TripleSlashCommentModel.CreateModel(input, SyntaxLanguage.CSharp, context);
            Assert.False(commentModel.IsInheritDoc, nameof(commentModel.IsInheritDoc));

            var summary = commentModel.Summary;
            Assert.Equal(@"
Parital classes <xref href=""System.AccessViolationException"" data-throw-if-not-resolved=""false""></xref><xref href=""System.AccessViolationException"" data-throw-if-not-resolved=""false""></xref>can not cross assemblies, Test <xref uid=""langword_csharp_null"" name=""null"" href=""""></xref>

```
Classes in assemblies are by definition complete.
```
".Replace("\r\n", "\n"), summary);

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
</li><li>item2 in bullet list</li></ul>
".Replace("\r\n", "\n"),
remarks);

            var exceptions = commentModel.Exceptions;
            Assert.Equal(1, exceptions.Count);
            Assert.Equal("System.Xml.XmlException", exceptions[0].Type);
            Assert.Equal(@"This is a sample of exception node. Ref <a href=""http://exception.com"">Exception</a>", exceptions[0].Description);

            var example = commentModel.Examples;
            var expected = new List<string> {
@"
This sample shows how to call the <see cref=""M: Microsoft.DocAsCode.EntityModel.TripleSlashCommentParser.GetExceptions(System.String, Microsoft.DocAsCode.EntityModel.ITripleSlashCommentParserContext)""></see> method.
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
            commentModel = TripleSlashCommentModel.CreateModel(input, SyntaxLanguage.CSharp, context);

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

        [Trait("Related", "TripleSlashComments")]
        [Fact]
        public void InheritDoc()
        {
            const string input = @"
<member name=""M:ClassLibrary1.MyClass.DoThing"">
    <inheritdoc />
</member>";
            var context = new TripleSlashCommentParserContext
            {
                AddReferenceDelegate = null,
                PreserveRawInlineComments = false,
            };

            var commentModel = TripleSlashCommentModel.CreateModel(input, SyntaxLanguage.CSharp, context);
            Assert.True(commentModel.IsInheritDoc);
        }
    }
}
