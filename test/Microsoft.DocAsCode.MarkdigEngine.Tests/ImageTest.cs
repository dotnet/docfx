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
            var source = @":::image source=""example.jpg"" alt-text=""example""::: 
Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.
:::image-end:::
";

            var expected = @"<img src=""example.jpg"" alt=""example"" aria-describedby=""88850""><div id=""88850"" class=""visually-hidden"">
<p>Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.</p>
</div>
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
        public void ImageBlockTestBlockClosed()
        {
            var source = @":::image source=""example.jpg"" alt-text=""example"":::Lorem Ipsum
:::image-end:::";

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                var service = TestUtility.CreateMarkdownService();
                var document = service.Parse(source, "fakepath.md");

                var imageBlock = document.OfType<ImageBlock>().FirstOrDefault();
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

                var imageBlock = document.OfType<ImageBlock>().FirstOrDefault();
                Assert.Null(imageBlock);
            }
            Logger.UnregisterListener(listener);
        }

    }
}
