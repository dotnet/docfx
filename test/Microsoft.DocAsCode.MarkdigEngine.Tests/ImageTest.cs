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
            var source = @":::image id=""exampleId"" source=""example.jpg"" alt-text=""example""::: :::image-end:::
";

            var expected = @"<img src=""example.jpg"" alt=""example"" aria-describedby=""exampleId""><div id=""exampleId"" class=""visually-hidden"">
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
            var source = @":::image id=""exampleId"" source=""example.jpg"" alt-text=""example"":::
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
