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
    using Xunit;

    public class TripleColonTest
    {
        static public string LoggerPhase = "TripleColon";

        [Fact]
        public void TripleColonTestGeneral()
        {
            var source = @"::: zone pivot=""windows""
    hello
::: zone-end
";
            var expected = @"<div class=""zone has-pivot"" data-pivot=""windows"">
<pre><code>hello
</code></pre>
</div>
".Replace("\r\n", "\n");

            TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
        }

        [Fact]
        public void TripleColonTestSelfClosing()
        {
            var source = @"::: zone target=""chromeless""
::: form action=""create-resource"" submitText=""Create"" :::
::: zone-end
";

            var expected = @"<div class=""zone has-target"" data-target=""chromeless"">
<form class=""chromeless-form"" data-action=""create-resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Create</button>
</form>
</div>
".Replace("\r\n", "\n");

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                TestUtility.AssertEqual(expected, source, TestUtility.MarkupWithoutSourceInfo);
            }
            Logger.UnregisterListener(listener);

            // Listener should have no error or warning message.
            Assert.Empty(listener.Items);
        }

        [Fact]
        public void TripleColonTestBlockClosed()
        {
            var source = @"::: zone target=""chromeless""
::: form action=""create-resource"" submitText=""Create"" :::
::: zone-end
";

            var expected = @"<div class=""zone has-target"" data-target=""chromeless"">
<form class=""chromeless-form"" data-action=""create-resource"">
<div></div>
<button class=""button is-primary"" disabled=""disabled"" type=""submit"">Create</button>
</form>
</div>
".Replace("\r\n", "\n");

            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseEqualFilter(LoggerPhase);

            Logger.RegisterListener(listener);
            using (new LoggerPhaseScope(LoggerPhase))
            {
                var service = TestUtility.CreateMarkdownService();
                var document = service.Parse(source, "fakepath.md");
                var blocks = new List<TripleColonBlock>();
                var stack = new Stack<ContainerBlock>();
                stack.Push(document);

                // Get all triplecolon blocks in the document using a depth-first iterative tree traversal strategy.
                do
                {
                    var block = stack.Pop();
                    var children = block.Where(x => x.GetType() == typeof(TripleColonBlock)).Select(x => x as TripleColonBlock);
                    blocks.AddRange(children);
                    foreach (var child in children)
                    {
                        stack.Push(child);
                    }

                } while (stack.Count > 0);
                
                foreach (var block in blocks)
                {
                    Assert.True(block.Closed);
                    Assert.False(block.IsOpen);
                }
            }
            Logger.UnregisterListener(listener);
        }
    }
}
