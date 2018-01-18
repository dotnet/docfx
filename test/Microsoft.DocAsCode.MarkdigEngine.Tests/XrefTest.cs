// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Tests
{
    using Xunit;

    public class XrefTest
    {
        [Fact]
        public void XrefTestGeneral()
        {
            //arange
            var content = @"<xref:Microsoft.Build.Tasks>
@Microsoft.Build.Tasks
""@""a[test](link)
@hehe
@""Microsoft.Build.Tasks?text=Tasks""
[link_text](xref:Microsoft.Build.Tasks)
<xref:Microsoft.Build.Tasks#Anchor_1>
<xref href=""Microsoft.Build.Tasks?alt=ImmutableArray""/>
<xref:""Microsoft.Build.Tasks?alt=ImmutableArray"">
<a href=""xref:Microsoft.Build.Tasks?displayProperty=fullName""/>
";
            // act
            var marked = TestUtility.MarkupWithoutSourceInfo(content, "Topic.md");

            // assert
            var expected = @"<p><xref href=""Microsoft.Build.Tasks"" data-throw-if-not-resolved=""True""></xref>
@Microsoft.Build.Tasks
&quot;@&quot;a<a href=""link"">test</a>
@hehe
<xref href=""Microsoft.Build.Tasks?text=Tasks"" data-throw-if-not-resolved=""False"" data-raw-source=""@&quot;Microsoft.Build.Tasks?text=Tasks&quot;""></xref>
<a href=""xref:Microsoft.Build.Tasks"">link_text</a>
<xref href=""Microsoft.Build.Tasks#Anchor_1"" data-throw-if-not-resolved=""True""></xref>
<xref href=""Microsoft.Build.Tasks?alt=ImmutableArray""/>
<xref href=""Microsoft.Build.Tasks?alt=ImmutableArray"" data-throw-if-not-resolved=""True""></xref>
<a href=""xref:Microsoft.Build.Tasks?displayProperty=fullName""/></p>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }
    }
}
