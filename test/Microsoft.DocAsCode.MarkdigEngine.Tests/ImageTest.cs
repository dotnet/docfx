// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using Markdig.Syntax;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.Plugins;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Xunit;

    public class ImageTest
    {
        static public string LoggerPhase = "Image";

        [Fact]
        public void ImageTestBlockGeneral()
        {
            var source = @":::image type=""content"" source=""example.jpg"" alt-text=""example"":::
";

            var expected = @"<img alt=""example"" src=""example.jpg"">
".Replace("\r\n", "\n");

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);
        }

        [Fact]
        public void ComplexImageTestBlockGeneral()
        {
            var source = @"
:::image type=""icon"" source=""example.svg"":::

:::image type=""complex"" source=""example.jpg"" alt-text=""example""::: 
Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.
:::image-end:::

:::image type=""content"" source=""example.jpg"" alt-text=""example"":::
";

            var expected = @"<img role=""presentation"" src=""example.svg"">
<img alt=""example"" aria-describedby=""a00f6"" src=""example.jpg"">
<div id=""a00f6"" class=""visually-hidden"">
<p>Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.</p>
</div>
<img alt=""example"" src=""example.jpg"">
".Replace("\r\n", "\n");

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);
        }

        [Fact]
        public void ImageWithIconTypeTestBlockGeneral()
        {
            var source = @":::image type=""icon"" source=""example.svg"":::";

            var expected = @"<img role=""presentation"" src=""example.svg"">
".Replace("\r\n", "\n");

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestImageBlockSrcResolveInToken()
        {
            // -r
            //  |- r.md
            //  |- b
            //  |  |- token.md
            //  |  |- img.jpg
            var r = @"
[!include[](b/token.md)]
";
            var token = @"
:::image source=""example.jpg"" type=""content"" alt-text=""example"":::
";
            TestUtility.WriteToFile("r/r.md", r);
            TestUtility.WriteToFile("r/b/token.md", token);
            var marked = TestUtility.MarkupWithoutSourceInfo(r, "r/r.md");

            var expected = @"<img alt=""example"" src=""~/r/b/example.jpg"">
";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestComplexImageBlockSrcResolveInToken()
        {
            // -r
            //  |- r.md
            //  |- b
            //  |  |- token.md
            //  |  |- img.jpg
            var r = @"
[!include[](b/token.md)]
";
            var token = @"
:::image source=""example.jpg"" type=""complex"" alt-text=""example""::: 
Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.
:::image-end:::
";
            TestUtility.WriteToFile("r/r.md", r);
            TestUtility.WriteToFile("r/b/token.md", token);
            var marked = TestUtility.MarkupWithoutSourceInfo(r, "r/r.md");

            var expected = @"<img alt=""example"" aria-describedby=""e68bf"" src=""~/r/b/example.jpg"">
<div id=""e68bf"" class=""visually-hidden"">
<p>Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.</p>
</div>
";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        [Trait("Related", "DfmMarkdown")]
        public void TestImageWithIconTypeBlockSrcResolveInToken()
        {
            // -r
            //  |- r.md
            //  |- b
            //  |  |- token.md
            //  |  |- img.jpg
            var r = @"
[!include[](b/token.md)]
";
            var token = @"
:::image source=""example.svg"" type=""icon"" alt-text=""example"":::
";
            TestUtility.WriteToFile("r/r.md", r);
            TestUtility.WriteToFile("r/b/token.md", token);
            var marked = TestUtility.MarkupWithoutSourceInfo(r, "r/r.md");

            var expected = @"<img role=""presentation"" src=""~/r/b/example.svg"">
";
            Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
        }

        [Fact]
        public void ImageBlockTestBlockClosed()
        {
            var source = @":::image source=""example.jpg"" type=""complex"" alt-text=""example"":::Lorem Ipsum
:::image-end:::";

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                var service = TestUtility.CreateMarkdownService();
                var document = service.Parse(source, "fakepath.md");

                var imageBlock = document.OfType<TripleColonBlock>().FirstOrDefault();
                Assert.NotNull(imageBlock);
                Assert.True(imageBlock.Closed);
            }
            Logger.UnregisterListener(listener);
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

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                var service = TestUtility.CreateMarkdownService();
                var document = service.Parse(source, "fakepath.md");

                var imageBlock = document.OfType<TripleColonBlock>().FirstOrDefault();
                Assert.Null(imageBlock);
            }
            Logger.UnregisterListener(listener);
        }

    }
}
