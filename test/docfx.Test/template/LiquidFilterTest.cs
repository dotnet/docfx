// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public class LiquidFilterTest
    {
        private const string DefaultExcludeNodesTemplate = @"{%- assign xpath = ""//div[@class and not(contains(@class,'"" | append: {{context.moniker}} | append: ""'))]"" -%}
                                                             {%- assign filter_content = {{content}} | exclude_nodes: {{xpath}} -%}{{filter_content}}";

        [Theory]
        [InlineData(@"<img src=""image"" data-linktype=""relativepath""></img>", "//*[@data-linktype='relativepath']", "@href|@src", "view", "1", @"<img src=""image?view=1"" data-linktype=""relativepath"">")]
        [InlineData(@"<a invalid=""invalid"" data-linktype=""relativepath"">invalid</a>", "//*[@data-linktype='relativepath']", "@href|@src", "view", "1", @"<a invalid=""invalid"" data-linktype=""relativepath"">invalid</a>")]
        [InlineData(@"<a href=""homepage"" data-linktype=""relativepath"">homepage</a>", "//*[@data-linktype='relativepath']", "@href|@src", "view", "1", @"<a href=""homepage?view=1"" data-linktype=""relativepath"">homepage</a>")]
        [InlineData(@"<a href=""http://www.bing.com"">bing</a>", "//*[@data-linktype='relativepath']", "@href|@src", "view", "1", @"<a href=""http://www.bing.com"">bing</a>")]
        [InlineData(@"<a href=""index"" data-linktype=""relativepath"">index</a>", "//*[@data-linktype='absolutePath']", "@href|@src", "view", "1", @"<a href=""index"" data-linktype=""relativepath"">index</a>")]
        [InlineData(@"<a href=""index?query=value"" data-linktype=""relativepath"">index</a>", "//*[@data-linktype='relativepath']", "@href", "view", "1", @"<a href=""index?query=value&view=1"" data-linktype=""relativepath"">index</a>")]
        [InlineData(@"<a href=""index?query=value#bookmark"" data-linktype=""relativepath"">index</a>", "//*[@data-linktype='relativepath']", "@href", "view", "1", @"<a href=""index?query=value&view=1#bookmark"" data-linktype=""relativepath"">index</a>")]
        [InlineData(@"<a href=""homepage?query=value#"" data-linktype=""relativepath"">homepage</a>", "//*[@data-linktype='relativepath']", "@href", "view", "1", @"<a href=""homepage?query=value&view=1#"" data-linktype=""relativepath"">homepage</a>")]
        [InlineData(@"<a href=""index?query=index%22abc"" data-linktype=""relativepath"">index</a>", "//*[@data-linktype='relativepath']", "@href", "view", "10 sample", @"<a href=""index?query=index%22abc&view=10+sample"" data-linktype=""relativepath"">index</a>")]
        [InlineData(@"<a href=""index?view=11"" data-linktype=""relativepath"">index</a>", "//*[@data-linktype='relativepath']", "@href", "view", "1", @"<a href=""index?view=11"" data-linktype=""relativepath"">index</a>")]
        public void TestAppendQueryString(string html, string nodesXPath, string attributesXPath, string queryKey, string queryValue, string expectedResult)
        {
            Assert.Equal(expectedResult, LiquidFilter.AppendQueryString(html, nodesXPath, attributesXPath, queryKey, queryValue));
        }

        [Theory]
        [InlineData(@"
<div>
    <h2>Constructors summary1</h2>
    <div class=""version1"">
        <p>content1</p>
    </div>
    <div class=""version1 version2"">
        <p>content2</p>
    </div>
    <div class=""version3"">
        <p>content3</p>
    </div>
</div>", "//div[@class and not(contains(@class,'1'))]", @"
<div>
    <h2>Constructors summary1</h2>
    <div class=""version1"">
        <p>content1</p>
    </div>
    <div class=""version1 version2"">
        <p>content2</p>
    </div>
    
</div>")]
        [InlineData(@"
<div>
    <h2>Constructors summary1</h2>
    <div class=""version2"">
        <p>content2</p>
    </div>
    <div class=""version3"">
        <p>content3</p>
    </div>
</div>", "//div[@class and not(contains(@class,'1'))]", @"
< div>
    <h2>Constructors summary1</h2>
    
    
</div>")]
        public void TestExcludeNodes(string html, string nodesXPath, string expectedResult)
        {
            Assert.Equal(
                TestUtility.NormalizeHtml(expectedResult),
                TestUtility.NormalizeHtml(LiquidFilter.ExcludeNodes(html, nodesXPath)));
        }
    }
}
