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
            var source = @":::image type=""content"" source=""example.jpg"" alt-text=""example"":::";
            var expected = @"<img src=""example.jpg"" alt=""example"">";

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
";

            var expected = @"<img src=""example.svg"" role=""presentation"">
<img src=""example.jpg"" alt=""example"" aria-describedby=""42570"">
<div id=""42570"" class=""visually-hidden"">
<p>Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.</p>
</div>
<img src=""example.jpg"" alt=""example"">
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

            TestUtility.VerifyMarkup(source, expected, errors: new[] { "invalid-image" });
        }

        [Fact]
        public void ContentImageTestBlock_InvalidImage_MissingAlt()
        {
            var source = @"
:::image source=""example.svg"":::
";

            var expected = @"<p>:::image source=&quot;example.svg&quot;:::</p>
";

            TestUtility.VerifyMarkup(source, expected, errors: new[] { "invalid-image" });
        }

        [Fact]
        public void ImageWithIconTypeTestBlockGeneral()
        {
            var source = @":::image type=""icon"" source=""example.svg"":::";

            var expected = @"<img src=""example.svg"" role=""presentation"">";

            TestUtility.VerifyMarkup(source, expected);
        }

        [Fact]
        public void ImageBlockTestBlockClosed()
        {
            var source = @":::image source=""example.jpg"" type=""complex"" alt-text=""example"":::Lorem Ipsum
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
