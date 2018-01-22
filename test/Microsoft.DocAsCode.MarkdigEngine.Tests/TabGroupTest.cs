// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;

    using MarkdigEngine.Extensions;

    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    
    public class TabGroupTest
    {
        [Fact]
        [Trait("Related", "TabGroup")]
        public void Test_General()
        {
            var groupId = "w61hnTEDJ7";
            TestDfmInGeneral(
                @"Tab group test case
# [title-a](#tab/a)
content-a
# [title-b](#tab/b/c)
content-b
- - -",
                $@"<p sourceFile=""Topic.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""1"">Tab group test case</p>
<div class=""tabGroup"" id=""tabgroup_{groupId}"" sourceFile=""Topic.md"" sourceStartLineNumber=""2"" sourceEndLineNumber=""6"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_a"" role=""tab"" aria-controls=""tabpanel_{groupId}_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""Topic.md"" sourceStartLineNumber=""2"" sourceEndLineNumber=""2"">title-a</a>
</li>
<li role=""presentation"" aria-hidden=""true"" hidden=""hidden"">
<a href=""#tabpanel_{groupId}_b_c"" role=""tab"" aria-controls=""tabpanel_{groupId}_b_c"" data-tab=""b"" data-condition=""c"" tabindex=""-1"" sourceFile=""Topic.md"" sourceStartLineNumber=""4"" sourceEndLineNumber=""4"">title-b</a>
</li>
</ul>
<section id=""tabpanel_{groupId}_a"" role=""tabpanel"" data-tab=""a"">

<p sourceFile=""Topic.md"" sourceStartLineNumber=""3"" sourceEndLineNumber=""3"">content-a</p>
</section>
<section id=""tabpanel_{groupId}_b_c"" role=""tabpanel"" data-tab=""b"" data-condition=""c"" aria-hidden=""true"" hidden=""hidden"">

<p sourceFile=""Topic.md"" sourceStartLineNumber=""5"" sourceEndLineNumber=""5"">content-b</p>
</section>
</div>
"
            );
        }

        [Fact]
        [Trait("Related", "TabGroup")]
        public void Test_TabGroup_Combining()
        {
            var groupId = "w61hnTEDJ7";
            TestDfmInGeneral(
                @"# [title-a or b](#tab/a+b)
content-a or b
# [title-c](#tab/c)
content-c
- - -
# [title-a](#tab/a)
content-a
# [title-b or c](#tab/b+c)
content-b or c
- - -",
                $@"<div class=""tabGroup"" id=""tabgroup_{groupId}"" sourceFile=""Topic.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""5"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_a+b"" role=""tab"" aria-controls=""tabpanel_{groupId}_a+b"" data-tab=""a+b"" tabindex=""0"" aria-selected=""true"" sourceFile=""Topic.md"" sourceStartLineNumber=""1"" sourceEndLineNumber=""1"">title-a or b</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_{groupId}_c"" role=""tab"" aria-controls=""tabpanel_{groupId}_c"" data-tab=""c"" tabindex=""-1"" sourceFile=""Topic.md"" sourceStartLineNumber=""3"" sourceEndLineNumber=""3"">title-c</a>
</li>
</ul>
<section id=""tabpanel_{groupId}_a+b"" role=""tabpanel"" data-tab=""a+b"">
<p sourceFile=""Topic.md"" sourceStartLineNumber=""2"" sourceEndLineNumber=""2"">content-a or b</p>
</section>
<section id=""tabpanel_{groupId}_c"" role=""tabpanel"" data-tab=""c"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""Topic.md"" sourceStartLineNumber=""4"" sourceEndLineNumber=""4"">content-c</p>
</section>
</div>
<div class=""tabGroup"" id=""tabgroup_{groupId}-1"" sourceFile=""Topic.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""10"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_{groupId}-1_a"" role=""tab"" aria-controls=""tabpanel_{groupId}-1_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""Topic.md"" sourceStartLineNumber=""6"" sourceEndLineNumber=""6"">title-a</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_{groupId}-1_b+c"" role=""tab"" aria-controls=""tabpanel_{groupId}-1_b+c"" data-tab=""b+c"" tabindex=""-1"" sourceFile=""Topic.md"" sourceStartLineNumber=""8"" sourceEndLineNumber=""8"">title-b or c</a>
</li>
</ul>
<section id=""tabpanel_{groupId}-1_a"" role=""tabpanel"" data-tab=""a"">

<p sourceFile=""Topic.md"" sourceStartLineNumber=""7"" sourceEndLineNumber=""7"">content-a</p>
</section>
<section id=""tabpanel_{groupId}-1_b+c"" role=""tabpanel"" data-tab=""b+c"" aria-hidden=""true"" hidden=""hidden"">

<p sourceFile=""Topic.md"" sourceStartLineNumber=""9"" sourceEndLineNumber=""9"">content-b or c</p>
</section>
</div>
"
            );
        }

        private static void TestDfmInGeneral(string source, string expected)
        {
            var result = SimpleMarkup(source).Html;
            Assert.Equal(expected.Replace("\r\n", "\n"), result);
        }

        private static MarkupResult SimpleMarkup(string source)
        {
            var parameter = new MarkdownServiceParameters
            {
                BasePath = ".",
                Extensions = new Dictionary<string, object>
                {
                    { LineNumberExtension.EnableSourceInfo, true }
                }
            };
            var service = new MarkdigMarkdownService(parameter);
            return service.Markup(source, "Topic.md");
        }
    }
}
