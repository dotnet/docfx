// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using Xunit;
    using Microsoft.DocAsCode.EntityModel.ExtractSearchData;
    public class ExtractSearchDataTest
    {
        [Fact]
        public void TestExtractIndexFromMarkDown()
        {
            // case 1
            var markDownStr = @"# Head1 Title
this is content.";
            var result = ExtractSearchData.ExtractIndexFromMarkdown(markDownStr);
            Assert.Equal("Head1 Title", result.Title);
            Assert.Equal("Head1 Title this content", result.Keywords);

            // case 2
            markDownStr = @" # Head2 Title";
            result = ExtractSearchData.ExtractIndexFromMarkdown(markDownStr);
            Assert.Equal(string.Empty, result.Title);
            Assert.Equal("Head2 Title", result.Keywords);

            // case 3
            markDownStr = @"- yml
Head3 Title
===
this is content.
";
            result = ExtractSearchData.ExtractIndexFromMarkdown(markDownStr);
            Assert.Equal("Head3 Title", result.Title);
            Assert.Equal("yml Head3 Title this content", result.Keywords);

            // case 4
            markDownStr = @"- yml
Head3 Title
===";
            result = ExtractSearchData.ExtractIndexFromMarkdown(markDownStr);
            Assert.Equal("Head3 Title", result.Title);
            Assert.Equal("yml Head3 Title", result.Keywords);
        }

        [Fact]
        public void TestExtractIndexFromYml()
        {
            // case 1
            var ymlStr = @"items:
- uid: IronRuby.Builtins.Glob
  id: Glob
  parent: IronRuby.Builtins
  href: IronRuby.Builtins.Glob.yml
  name: Glob
  fullName: IronRuby.Builtins.Glob
  type: Class";
            var result = ExtractSearchData.ExtractIndexFromYml(ymlStr);
            Assert.Equal("Glob", result.Title);
            Assert.Equal("IronRuby Builtins Glob", result.Keywords);

            // case 2
            ymlStr = @"items:
- uid: IronRuby.Builtins.Glob
  id: Glob
  parent: IronRuby.Builtins
  href: IronRuby.Builtins.Glob.yml
  name: Glob
  fullName: IronRuby.Builtins.Glob
  type: Class
- uid: IronRuby.Builtins.Glob.FnMatch(System.String,System.String,System.Int32)
  id: FnMatch(System.String,System.String,System.Int32)
  parent: IronRuby.Builtins.Glob
  href: IronRuby.Builtins.Glob.yml
  name: FnMatch(String, String, Int32)
  fullName: IronRuby.Builtins.Glob.FnMatch(System.String, System.String, System.Int32)
  type: Method
  description: ";
            result = ExtractSearchData.ExtractIndexFromYml(ymlStr);
            Assert.Equal("Glob FnMatch String String Int32", result.Title);
            Assert.Equal("IronRuby Builtins Glob IronRuby Builtins Glob FnMatch System String System String System Int32", result.Keywords);

            // case 3
            ymlStr = @"";
            result = ExtractSearchData.ExtractIndexFromYml(ymlStr);
            Assert.Equal(null, result);
        }
    }
}