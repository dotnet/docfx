// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;
    using Xunit;

    public class ImageTest
    {
        [Fact]
        public void ImageTestBlockGeneral()
        {
            var source = @":::image type=""content"" source=""example.jpg"" alt-text=""example"":::

:::image type=""content"" source=""example.jpg"" alt-text=""example"" border=""false"":::

:::image type=""content"" source=""example.jpg"" alt-text=""example"" lightbox=""example-expanded.jpg"":::

:::image type=""content"" source=""example.jpg"" alt-text=""example"" lightbox=""example-expanded.jpg"" border=""false"":::

:::image type=""icon"" source=""green-checkmark.png""::: |  |  |  |

|Capability   | Admin  | Member  | Contributor  | Viewer |
|---|---|---|---|---|
| Update and delete the workspace.  |  |   |   |   | 
| Add/remove people, including other admins.  | :::image type=""icon"" source=""green-checkmark.png"":::  |   |   |   |
| Add members or others with lower permissions.  |  X | X  |  |   |";

            var expected = @"<p class=""mx-imgBorder"">
<img src=""example.jpg"" alt=""example"">
</p>
<p>
<img src=""example.jpg"" alt=""example"">
</p>
<p class=""mx-imgBorder"">
<a href=""example-expanded.jpg#lightbox"" data-linktype=""relative-path"">
<img src=""example.jpg"" alt=""example"">
</a>
</p>
<p>
<a href=""example-expanded.jpg#lightbox"" data-linktype=""relative-path"">
<img src=""example.jpg"" alt=""example"">
</a>
</p>
<p>
<img src=""green-checkmark.png"" role=""presentation"">
</p>
<table>
<thead>
<tr>
<th>Capability</th>
<th>Admin</th>
<th>Member</th>
<th>Contributor</th>
<th>Viewer</th>
</tr>
</thead>
<tbody>
<tr>
<td>Update and delete the workspace.</td>
<td></td>
<td></td>
<td></td>
<td></td>
</tr>
<tr>
<td>Add/remove people, including other admins.</td>
<td><p>
<img src=""green-checkmark.png"" role=""presentation"">
</p>
</td>
<td></td>
<td></td>
<td></td>
</tr>
<tr>
<td>Add members or others with lower permissions.</td>
<td>X</td>
<td>X</td>
<td></td>
<td></td>
</tr>
</tbody>
</table>
";

            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void ComplexImageTestBlockGeneral()
        {
            var source = @"
:::image type=""icon"" source=""example.svg"":::

:::image type=""complex"" source=""example.jpg"" alt-text=""example"" loc-scope=""azure""::: 
Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.
:::image-end:::

:::image source=""example.jpg"" alt-text=""example"" loc-scope=""azure"":::

:::image type=""complex"" source=""example.jpg"" alt-text=""example"" loc-scope=""azure"" lightbox=""example-expanded.jpg""::: 
Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.
:::image-end:::

:::image type=""complex"" source=""example.jpg"" alt-text=""example"" loc-scope=""azure"" lightbox=""example-expanded.jpg"" border=""false""::: 
Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.
:::image-end:::
";

            var expected = @"<p>
<img src=""example.svg"" role=""presentation"">
</p>
<p class=""mx-imgBorder"">
<img src=""example.jpg"" alt=""example"" aria-describedby=""3-0"">
<div id=""3-0"" class=""visually-hidden"">
<p>Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.</p>
</div>
</p>
<p class=""mx-imgBorder"">
<img src=""example.jpg"" alt=""example"">
</p>
<p class=""mx-imgBorder"">
<a href=""example-expanded.jpg#lightbox"" data-linktype=""relative-path"">
<img src=""example.jpg"" alt=""example"" aria-describedby=""9-0"">
<div id=""9-0"" class=""visually-hidden"">
<p>Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.</p>
</div>
</a>
</p>
<p>
<a href=""example-expanded.jpg#lightbox"" data-linktype=""relative-path"">
<img src=""example.jpg"" alt=""example"" aria-describedby=""13-0"">
<div id=""13-0"" class=""visually-hidden"">
<p>Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.</p>
</div>
</a>
</p>
";

            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void ContentImageTestBlock_LinkAttribute()
        {
            var source = @"
:::image source=""example.svg"" alt-text=""Lorum Ipsom"" link=""https://marketplace.eclipse.org/marketplace-client-intro?mpc_install=1919278"":::

:::image source=""example.svg"" lightbox=""example.svg"" alt-text=""Lorum Ipsom"" link=""https://marketplace.eclipse.org/marketplace-client-intro?mpc_install=1919278"":::
";

            var expected = @"<p class=""mx-imgBorder"">
<a href=""https://marketplace.eclipse.org/marketplace-client-intro?mpc_install=1919278"">
<img src=""example.svg"" alt=""Lorum Ipsom"">
</a>
</p>
<p class=""mx-imgBorder"">
<a href=""https://marketplace.eclipse.org/marketplace-client-intro?mpc_install=1919278"">
<img src=""example.svg"" alt=""Lorum Ipsom"">
</a>
</p>
";

            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void ImageTestBlock_InvalidImage_MissingSource()
        {
            var source = @"
:::image type=""icon"":::
";

            var expected = @"<p>:::image type=&quot;icon&quot;:::</p>
";

            TestUtility.VerifyMarkup(source, expected, errors: new[] { "invalid-image", "invalid-image" });
        }

        [Fact]
        public void ContentImageTestBlock_InvalidImage_MissingAlt()
        {
            var source = @"
:::image source=""example.svg"":::
";

            var expected = @"<p>:::image source=&quot;example.svg&quot;:::</p>
";

            TestUtility.VerifyMarkup(source, expected, errors: new[] { "invalid-image", "invalid-image" });
        }

        [Fact]
        public void ImageWithIconTypeTestBlockGeneral()
        {
            var source = @":::image type=""icon"" source=""example.svg"":::

:::image type=""icon"" source=""example.svg"" border=""true"":::";

            var expected = @"<p>
<img src=""example.svg"" role=""presentation"">
</p>
<p class=""mx-imgBorder"">
<img src=""example.svg"" role=""presentation"">
</p>
";

            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void ImageBlockTestBlockClosed()
        {
            var source = @":::image source=""example.jpg"" type=""complex"" alt-text=""example"":::
Lorem Ipsum
:::image-end:::";

            TestUtility.VerifyMarkup(source, null);
        }

        [Fact]
        public void ImageTestNotImageBlock()
        {
            var source = @":::row:::
:::column:::
    This is where your content goes.
:::column-end:::
:::row-end:::
";
            var expected = @"<section class=""row"">
<div class=""column"">
<p>This is where your content goes.</p>
</div>
</section>
";

            TestUtility.VerifyMarkup(source, expected);
        }

    }
}
