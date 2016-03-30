// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common.Tests
{
    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "Parser")]
    public class YamlHeaderParserUnitTest
    {
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
            Assert.Equal(1, yamlHeaders.Count);
            Assert.Equal("abc", yamlHeaders[0].Id);

            // --- Should also work
            input = @"---      
                             uid: abc
                            ---
                            ";
            yamlHeaders = YamlHeaderParser.Select(input);
            Assert.Equal(1, yamlHeaders.Count);
            Assert.Equal("abc", yamlHeaders[0].Id);

            // --- should be start with uid
            input = @"
                            ---      
                             id: abc
                            ---
                            ";
            yamlHeaders = YamlHeaderParser.Select(input);
            Assert.Equal(0, yamlHeaders.Count);
        }
    }
}
