// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "Parser")]
    public class TripleSlashParserUnitTest
    {
        [Trait("Related", "TripleSlashComments")]
        [Fact]
        public void TestTripleSlashParser()
        {
            string input = @"
<member name='T:TestClass1.Partial1'>
    <summary>
        Parital classes <see cref='T:System.AccessViolationException'/><see cref='T:System.AccessViolationException'/>can not cross assemblies,
    

        ```
        Classes in assemblies are by definition complete.
        ```
    </summary>
    <remarks>
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
                            word inside list->listItem->list->listItem->para.
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
        <exception cref='T:System.Xml.XmlException'>This is a sample of exception node</exception>
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
    <see cref=""T:Microsoft.DocAsCode.EntityModel.SpecIdHelper""/>
    <see cref=""T:System.Diagnostics.SourceSwitch""/>
    <seealso cref=""T:System.IO.WaitForChangedResult""/>
    <seealso cref=""!:http://google.com"">ABCS</seealso>

</member>";
            var context = new TripleSlashCommentParserContext
            {
                AddReferenceDelegate = null,
                Normalize = true,
                PreserveRawInlineComments = false,
            };


            var commentModel = TripleSlashCommentModel.CreateModel(input, context);

            var summary = commentModel.Summary;
            Assert.Equal(@"
    Parital classes <xref href=""System.AccessViolationException"" data-throw-if-not-resolved=""false""></xref><xref href=""System.AccessViolationException"" data-throw-if-not-resolved=""false""></xref>can not cross assemblies,


    ```
    Classes in assemblies are by definition complete.
    ```
", summary);

            var returns = commentModel.Returns;
            Assert.Equal("Task<xref href=\"System.AccessViolationException\" data-throw-if-not-resolved=\"false\"></xref> returns", returns);

            var paramInput = commentModel.Parameters["input"];
            Assert.Equal("This is <xref href=\"System.AccessViolationException\" data-throw-if-not-resolved=\"false\"></xref>the input", paramInput);

            var remarks = commentModel.Remarks;
            Assert.Equal(@"
<p>This is <em>ref</em> a sample of exception node</p>
<ul><li>
            <pre><code class=""c#"">
            public class XmlElement
                : XmlLinkedNode
            </code></pre>
            <ol><li>
                        word inside list->listItem->list->listItem->para.
                    </li><li>item2 in numbered list</li></ol>
        </li><li>item2 in bullet list</li></ul>
", remarks);

            var exceptions = commentModel.Exceptions;
            Assert.Equal(1, exceptions.Count);
            Assert.Equal("System.Xml.XmlException", exceptions[0].Type);
            Assert.Equal("This is a sample of exception node", exceptions[0].Description);

            var example = commentModel.Examples;
            var expected = new List<string> {
@"
This sample shows how to call the <see cref=""M: Microsoft.DocAsCode.EntityModel.TripleSlashCommentParser.GetExceptions(System.String, Microsoft.DocAsCode.EntityModel.ITripleSlashCommentParserContext)""></see> method.
<pre><code>
class TestClass
{
    static int Main()
    {
        return GetExceptions(null, null).Count();
    }
} 
</code></pre> 
",
@"
This is another example
"};
            Assert.Equal(expected, example);

            context.PreserveRawInlineComments = true;
            commentModel = TripleSlashCommentModel.CreateModel(input, context);

            var sees = commentModel.Sees;
            Assert.Equal(2, sees.Count);
            Assert.Equal("Microsoft.DocAsCode.EntityModel.SpecIdHelper", sees[0].Type);
            Assert.Null(sees[0].Description);

            var seeAlsos = commentModel.SeeAlsos;
            Assert.Equal(1, seeAlsos.Count);
            Assert.Equal("System.IO.WaitForChangedResult", seeAlsos[0].Type);
            Assert.Null(seeAlsos[0].Description);
        }
    }
}
