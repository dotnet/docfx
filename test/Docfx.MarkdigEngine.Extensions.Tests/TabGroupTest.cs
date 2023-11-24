// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.MarkdigEngine.Tests;

public class TabGroupTest
{
    [Fact]
    [Trait("Related", "TabGroup")]
    public void Test_General()
    {
        TestMarkupInGeneral(
            @"Tab group test case
# [title-a](#tab/a)
content-a
# [title-b](#tab/b/c)
content-b
- - -",
            $@"<p sourceFile=""Topic.md"" sourceStartLineNumber=""1"">Tab group test case</p>
<div class=""tabGroup"" id=""tabgroup_1"" sourceFile=""Topic.md"" sourceStartLineNumber=""2"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_1_a"" role=""tab"" aria-controls=""tabpanel_1_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""Topic.md"" sourceStartLineNumber=""2"">title-a</a>
</li>
<li role=""presentation"" aria-hidden=""true"" hidden=""hidden"">
<a href=""#tabpanel_1_b_c"" role=""tab"" aria-controls=""tabpanel_1_b_c"" data-tab=""b"" data-condition=""c"" tabindex=""-1"" sourceFile=""Topic.md"" sourceStartLineNumber=""4"">title-b</a>
</li>
</ul>
<section id=""tabpanel_1_a"" role=""tabpanel"" data-tab=""a"">

<p sourceFile=""Topic.md"" sourceStartLineNumber=""3"">content-a</p>
</section>
<section id=""tabpanel_1_b_c"" role=""tabpanel"" data-tab=""b"" data-condition=""c"" aria-hidden=""true"" hidden=""hidden"">

<p sourceFile=""Topic.md"" sourceStartLineNumber=""5"">content-b</p>
</section>
</div>
"
        );
    }

    [Fact]
    [Trait("Related", "TabGroup")]
    public void Test_TabGroup_Combining()
    {
        TestMarkupInGeneral(
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
            $@"<div class=""tabGroup"" id=""tabgroup_1"" sourceFile=""Topic.md"" sourceStartLineNumber=""1"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_1_a+b"" role=""tab"" aria-controls=""tabpanel_1_a+b"" data-tab=""a+b"" tabindex=""0"" aria-selected=""true"" sourceFile=""Topic.md"" sourceStartLineNumber=""1"">title-a or b</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_1_c"" role=""tab"" aria-controls=""tabpanel_1_c"" data-tab=""c"" tabindex=""-1"" sourceFile=""Topic.md"" sourceStartLineNumber=""3"">title-c</a>
</li>
</ul>
<section id=""tabpanel_1_a+b"" role=""tabpanel"" data-tab=""a+b"">
<p sourceFile=""Topic.md"" sourceStartLineNumber=""2"">content-a or b</p>
</section>
<section id=""tabpanel_1_c"" role=""tabpanel"" data-tab=""c"" aria-hidden=""true"" hidden=""hidden"">
<p sourceFile=""Topic.md"" sourceStartLineNumber=""4"">content-c</p>
</section>
</div>
<div class=""tabGroup"" id=""tabgroup_2"" sourceFile=""Topic.md"" sourceStartLineNumber=""6"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_2_a"" role=""tab"" aria-controls=""tabpanel_2_a"" data-tab=""a"" tabindex=""0"" aria-selected=""true"" sourceFile=""Topic.md"" sourceStartLineNumber=""6"">title-a</a>
</li>
<li role=""presentation"">
<a href=""#tabpanel_2_b+c"" role=""tab"" aria-controls=""tabpanel_2_b+c"" data-tab=""b+c"" tabindex=""-1"" sourceFile=""Topic.md"" sourceStartLineNumber=""8"">title-b or c</a>
</li>
</ul>
<section id=""tabpanel_2_a"" role=""tabpanel"" data-tab=""a"">

<p sourceFile=""Topic.md"" sourceStartLineNumber=""7"">content-a</p>
</section>
<section id=""tabpanel_2_b+c"" role=""tabpanel"" data-tab=""b+c"" aria-hidden=""true"" hidden=""hidden"">

<p sourceFile=""Topic.md"" sourceStartLineNumber=""9"">content-b or c</p>
</section>
</div>
"
        );
    }

    [Fact]
    public void TestTableInTabGroup()
    {
        TestMarkupInGeneral(@"# [title](#tab/id)

a | b
- | -
c | d",
$@"<div class=""tabGroup"" id=""tabgroup_1"" sourceFile=""Topic.md"" sourceStartLineNumber=""1"">
<ul role=""tablist"">
<li role=""presentation"">
<a href=""#tabpanel_1_id"" role=""tab"" aria-controls=""tabpanel_1_id"" data-tab=""id"" tabindex=""0"" aria-selected=""true"" sourceFile=""Topic.md"" sourceStartLineNumber=""1"">title</a>
</li>
</ul>
<section id=""tabpanel_1_id"" role=""tabpanel"" data-tab=""id"">

<table sourceFile=""Topic.md"" sourceStartLineNumber=""3"">
<thead>
<tr sourceFile=""Topic.md"" sourceStartLineNumber=""3"">
<th sourceFile=""Topic.md"" sourceStartLineNumber=""3"">a</th>
<th sourceFile=""Topic.md"" sourceStartLineNumber=""3"">b</th>
</tr>
</thead>
<tbody>
<tr sourceFile=""Topic.md"" sourceStartLineNumber=""5"">
<td sourceFile=""Topic.md"" sourceStartLineNumber=""5"">c</td>
<td sourceFile=""Topic.md"" sourceStartLineNumber=""5"">d</td>
</tr>
</tbody>
</table>
</section>
</div>
");
    }

    private static void TestMarkupInGeneral(string source, string expected)
    {
        TestUtility.VerifyMarkup(source, expected, lineNumber: true, filePath: "Topic.md");
    }
}
