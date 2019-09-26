// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Xunit;

    public class XrefTest
    {
        [Fact]
        public void XrefTestGeneral()
        {
            //arange
            var content = @"<xref:Microsoft.Build.Tasks>
\<xref:Microsoft.Build.Tasks>
\\<xref:Microsoft.Build.Tasks>
@Microsoft.Build.Tasks
""@""a[test](link)
@hehe

![img @hehe a](src ""title @hehe b"")
![img @hehe a
![img @hehe a](src ""title @hehe b""
![this **img @hehe a** haha](src ""title @hehe b"")

@""Microsoft.Build.Tasks?text=Tasks""
[link_text](xref:Microsoft.Build.Tasks)
<xref:Microsoft.Build.Tasks#Anchor_1>
<xref href=""Microsoft.Build.Tasks?alt=ImmutableArray""/>
<xref:""Microsoft.Build.Tasks?alt=ImmutableArray"">
<a href=""xref:Microsoft.Build.Tasks?displayProperty=fullName""/>
";
            // assert
            var expected = @"<p><xref href=""Microsoft.Build.Tasks"" data-throw-if-not-resolved=""True"" data-raw-source=""&lt;xref:Microsoft.Build.Tasks&gt;""></xref>
&lt;xref:Microsoft.Build.Tasks&gt;
\<xref href=""Microsoft.Build.Tasks"" data-throw-if-not-resolved=""True"" data-raw-source=""&lt;xref:Microsoft.Build.Tasks&gt;""></xref>
<xref href=""Microsoft.Build.Tasks"" data-throw-if-not-resolved=""False"" data-raw-source=""@Microsoft.Build.Tasks""></xref>
&quot;@&quot;a<a href=""link"">test</a>
<xref href=""hehe"" data-throw-if-not-resolved=""False"" data-raw-source=""@hehe""></xref></p>
<p><img src=""src"" alt=""img @hehe a"" title=""title @hehe b"" />
![img <xref href=""hehe"" data-throw-if-not-resolved=""False"" data-raw-source=""@hehe""></xref> a
![img <xref href=""hehe"" data-throw-if-not-resolved=""False"" data-raw-source=""@hehe""></xref> a](src &quot;title <xref href=""hehe"" data-throw-if-not-resolved=""False"" data-raw-source=""@hehe""></xref> b&quot;
<img src=""src"" alt=""this img @hehe a haha"" title=""title @hehe b"" /></p>
<p><xref href=""Microsoft.Build.Tasks?text=Tasks"" data-throw-if-not-resolved=""False"" data-raw-source=""@&quot;Microsoft.Build.Tasks?text=Tasks&quot;""></xref>
<a href=""xref:Microsoft.Build.Tasks"">link_text</a>
<xref href=""Microsoft.Build.Tasks#Anchor_1"" data-throw-if-not-resolved=""True"" data-raw-source=""&lt;xref:Microsoft.Build.Tasks#Anchor_1&gt;""></xref>
<xref href=""Microsoft.Build.Tasks?alt=ImmutableArray""/>
<xref href=""Microsoft.Build.Tasks?alt=ImmutableArray"" data-throw-if-not-resolved=""True"" data-raw-source=""&lt;xref:&quot;Microsoft.Build.Tasks?alt=ImmutableArray&quot;&gt;""></xref>
<a href=""xref:Microsoft.Build.Tasks?displayProperty=fullName""/></p>
";
            TestUtility.VerifyMarkup(content, expected);
        }
    }
}
