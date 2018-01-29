// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System.Collections.Generic;
    
    using Microsoft.DocAsCode.MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.Plugins;
    using Xunit;

    public class InteractiveCodeTest
    {
        private MarkdigMarkdownService _noInteractiveMarkdownService;

        public InteractiveCodeTest()
        {
            var parameter = new MarkdownServiceParameters
            {
                BasePath = ".",
                Extensions = new Dictionary<string, object>
                {
                    { LineNumberExtension.EnableSourceInfo, false },
                    { CodeSnippetExtension.DisableInteractiveCode, true }
                }
            };
            _noInteractiveMarkdownService = new MarkdigMarkdownService(parameter);
        }

        [Fact]
        [Trait("Related", "InteractiveCode")]
        public void TestInteractiveCode_CodeSnippetSimple()
        {
            var source = @"[!code-azurecli-interactive[](InteractiveCode/sample.code)]";
            var marked = TestUtility.MarkupWithoutSourceInfo(source, "Topic.md");
            var noInteractiveMarked = _noInteractiveMarkdownService.Markup(source, "Topic.md");

            Assert.Equal(
                @"<pre><code class=""lang-azurecli"" data-interactive=""azurecli"">hello world!
</code></pre>".Replace("\r\n", "\n"), marked.Html);
            Assert.Equal(
                @"<pre><code class=""lang-azurecli-interactive"">hello world!
</code></pre>".Replace("\r\n", "\n"), noInteractiveMarked.Html);
        }
    }
}
