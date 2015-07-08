namespace Microsoft.DocAsCode.BackEnd.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using EntityModel;
    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "Parser")]
    public class ParsersUnitTest
    {
        [Trait("Related", "TripleSlashComments")]
        [Fact]
        public void TestTripleSlashParser()
        {
            string input = @"
      <member name='T:TestClass1.Partial1'>

          <summary>Parital classes <see cref='T:System.AccessViolationException'/><see cref='T:System.AccessViolationException'/>can not cross assemblies, ```Classes in assemblies are by definition complete.```
          </summary>
    <remarks>
    <para>This is a sample of exception node</para>
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

            var summary = TripleSlashCommentParser.GetSummary(input, context);
            Assert.Equal("Parital classes @'System.AccessViolationException'@'System.AccessViolationException'can not cross assemblies, ```Classes in assemblies are by definition complete.```", summary);

            var returns = TripleSlashCommentParser.GetReturns(input, context);
            Assert.Equal("Task@'System.AccessViolationException' returns", returns);

            var paramInput = TripleSlashCommentParser.GetParam(input, "input", context);
            Assert.Equal("This is @'System.AccessViolationException'the input", paramInput);

            var invalidParam = TripleSlashCommentParser.GetParam(input, "invalid", context);
            Assert.Null(invalidParam);

            var remarks = TripleSlashCommentParser.GetRemarks(input, context);
            Assert.Equal("<para>This is a sample of exception node</para>", remarks);

            var exceptions = TripleSlashCommentParser.GetExceptions(input, context);
            Assert.Equal(1, exceptions.Count);
            Assert.Equal("System.Xml.XmlException", exceptions[0].Type);
            Assert.Equal("This is a sample of exception node", exceptions[0].Description);

            var sees = TripleSlashCommentParser.GetSees(input, context);
            Assert.Equal(2, sees.Count);
            Assert.Equal("Microsoft.DocAsCode.EntityModel.SpecIdHelper", sees[0].Type);
            Assert.Null(sees[0].Description);

            var seeAlsos = TripleSlashCommentParser.GetSeeAlsos(input, context);
            Assert.Equal(1, seeAlsos.Count);
            Assert.Equal("System.IO.WaitForChangedResult", seeAlsos[0].Type);
            Assert.Null(seeAlsos[0].Description);

            var example = TripleSlashCommentParser.GetExample(input, context);
            Assert.Equal(@"This sample shows how to call the <see cref=""M: Microsoft.DocAsCode.EntityModel.TripleSlashCommentParser.GetExceptions(System.String, Microsoft.DocAsCode.EntityModel.ITripleSlashCommentParserContext)"" /> method.
<code>
class TestClass
{
static int Main()
{
return GetExceptions(null, null).Count();
}
}
</code>", example);
        }

        [Trait("Related", "YamlHeader")]
        [Fact]
        public void TestYamlHeaderParser()
        {
            // spaces are allowed
            string input = @"
                            ---      
                             uid: abc
                            ---
                            ";
            var yamlHeaders = YamlHeaderParser.Select(input);
            Assert.Equal(1, yamlHeaders.Count());
            Assert.Equal("abc", yamlHeaders[0].Id);

            // --- should be prefixed by a new line
            input = @"---      
                             uid: abc
                            ---
                            ";
            yamlHeaders = YamlHeaderParser.Select(input);
            Assert.Null(yamlHeaders);

            // --- should be start with uid
            input = @"
                            ---      
                             id: abc
                            ---
                            ";
            yamlHeaders = YamlHeaderParser.Select(input);
            Assert.Null(yamlHeaders);
        }

        [Trait("Related", "CodeSnippet")]
        [Fact]
        public void TestCodeSnippetParser()
        {
            // spaces are allowed
            string input = @" {{'relativePath'[1-2] }}";
            var codeSnippet = CodeSnippetParser.Select(input);
            Assert.Equal(1, codeSnippet.Count());
            Assert.Equal("relativePath", codeSnippet[0].Path);

            input = @" {{'<code_source_file_path>'[1-2] }}";
            codeSnippet = CodeSnippetParser.Select(input);
            Assert.Equal(0, codeSnippet.Count());
        }

        [Trait("Related", "Link")]
        [Fact]
        public void TestLinkParser()
        {
            Dictionary<string, string> index = new Dictionary<string, string>
            {
                {"link", "href" },
            };
            string input = "a@'link'@invalid";
            string output = LinkParser.ResolveText((s) =>
            {
                string item;
                if (index.TryGetValue(s, out item)) { return item; }
                return null;
            }, input, s => s);
            Assert.Equal("ahref@invalid", output);
            input = @"a@link @'link' @""link""";
            output = LinkParser.ResolveText((s) =>
            {
                string item;
                if (index.TryGetValue(s, out item)) { return item; }
                return null;
            }, input, s => "[link](" + s + ")");
            Assert.Equal("a[link](href) [link](href) [link](href)", output);
        }
    }
}
